using System;
using System.Collections.Generic;
using System.Text;
using BestHTTP.WebSocket;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace NeffosCSharp
{
    public class Connection : IDisposable
    {
        private readonly WebSocket _webSocket;
        public int ReconnectTries { get; set; }
        private bool _isAcknowledged;
        public bool IsAcknowledged => _isAcknowledged;
        
        private bool _allowNativeMessages;

        private string _id;
        private bool _closed;
        public bool Closed => _closed;
        
        private Dictionary<string, Action> _waitServerConnectNotifiers;

        private List<byte[]> _queue;
        private Dictionary<string, Action<Message>> _waitingMessages;
        private NamespaceMap _namespaces;
        private Dictionary<string, NSConnection> _connectedNamespaces;
        public Dictionary<string, NSConnection> ConnectedNamespaces => _connectedNamespaces;

        public Connection(WebSocket webSocket, NamespaceMap namespaces)
        {
            _webSocket = webSocket;
            _namespaces = namespaces;
            _isAcknowledged = false;
            ReconnectTries = 0;
            _closed = false;

            var hasEmptyNamespace = _namespaces.ContainsKey(string.Empty);
            _allowNativeMessages =
                hasEmptyNamespace && _namespaces[string.Empty].ContainsKey(Configuration.OnNativeMessage);

            _queue = new List<byte[]>();
            _waitingMessages = new Dictionary<string, Action<Message>>(10);
            _connectedNamespaces = new Dictionary<string, NSConnection>(10);
        }

        public NSConnection GetNamespace(string @namespace)
        {
            if (@namespace == null)
                return null;

            if(_connectedNamespaces.ContainsKey(@namespace))
                return _connectedNamespaces[@namespace];
            return null;
        }

        public bool WasReconnected => ReconnectTries > 0;

        public string Handle(byte[] response)
        {
            if (!_isAcknowledged)
            {
                var error = HandleAck(response);
                if (string.IsNullOrEmpty(error))
                {
                    _isAcknowledged = true;
                    HandleQueue();
                }
                else
                {
                    _webSocket.Close();
                }
                return error;
            }

            return HandleMessage(response);
        }

        private string HandleAck(byte[] response)
        {
            var data = Encoding.UTF8.GetString(response);

            var typ = data[0];
            switch (typ)
            {
                case Configuration.ackIDBinary:
                    var id = data.Substring(1);
                    _id = id;
                    break;
                case Configuration.ackNotOKBinary:
                    var errorText = data.Substring(1);
                    return errorText;
                default:
                    _queue.Add(response);
                    break;
            }
            return null;
        }

        public void HandleQueue()
        {
            if (_queue.Count <= 0)
            {
                return;
            }

            //handle queue items and remove them from the queue
            while (_queue.Count > 0)
            {
                var message = _queue[0];
                _queue.RemoveAt(0);
                HandleMessage(message);
            }
        }

        private string HandleMessage(byte[] data)
        {
#if ALL_LOG
            Debug.Log("Handle Message " + JsonConvert.SerializeObject(data.ToUTF8String()));
#endif
            var message = Message.Deserialize(data, this._allowNativeMessages);


            if (message == null)
            {
                return Exceptions.ErrorInvalidPayLoad;
            }

            if (message.IsNative && _allowNativeMessages)
            {
                var ns = GetNamespace(string.Empty);
                return ns.FireEvent(message);
            }

            if (message.IsWait())
            {
                var cb = _waitingMessages[message.Wait];
                cb(message);
                return string.Empty;
            }

            var nsConnection = GetNamespace(message.Namespace);
            switch (message.Event)
            {
                case Configuration.OnNamespaceConnect:
                    ReplyConnect(message);
                    break;
                case Configuration.OnNamespaceDisconnect:
                    ReplyDisconnect(message);
                    break;
                case Configuration.OnRoomJoin:
                    nsConnection?.ReplyRoomJoin(message);
                    break;
                case Configuration.OnRoomLeave:
                    nsConnection?.ReplyRoomLeave(message);
                    break;
                default:
                    if (nsConnection == null)
                    {
                        return Exceptions.ErrorBadNamespace;
                    }

                    message.IsLocal = false;
                    var error = nsConnection.FireEvent(message);
                    if (!string.IsNullOrEmpty(error))
                    {
                        message.Error = error;
                        WriteBinary(message);
                        return error;
                    }

                    break;
            }

            return string.Empty;
        }

        public UniTask<NSConnection> Connect(string namespaceName)
        {
            return AskConnect(namespaceName);
        }

        public UniTask<NSConnection> WaitServerConnect(string namespaceName)
        {
            if(_waitServerConnectNotifiers == null)
            {
                _waitServerConnectNotifiers = new Dictionary<string, Action>();
            }
            
            var cts = new UniTaskCompletionSource<NSConnection>();
            _waitServerConnectNotifiers.Add(namespaceName, () =>
            {
                _waitServerConnectNotifiers.Remove(namespaceName);
                cts.TrySetResult(GetNamespace(namespaceName));
            });
            return cts.Task;
        }

        public bool WriteBinary(Message message)
        {
            if (_closed)
                return false;

            if (!message.IsConnect() && !message.IsDisconnect())
            {
                //namespace pre-write check
                var ns = GetNamespace(message.Namespace);
                if (ns == null)
                    return false;

                //room pre-write check
                if (!string.IsNullOrEmpty(message.Room) && !message.IsRoomJoin() && !message.IsRoomLeft())
                {
                    if (!ns.Rooms.ContainsKey(message.Room))
                    {
                        //// tried to send to a not joined room.
                        return false;
                    }
                }
            }
            
            var buff = message.SerializeBinary();
            _webSocket.Send(buff);
            return true;
        }
        
        public bool WriteNative(Message message)
        {
            if (_closed)
                return false;
            if (!message.IsConnect() && !message.IsDisconnect())
            {
                //namespace pre-write check
                var ns = GetNamespace(message.Namespace);
                if (ns == null)
                    return false;

                //room pre-write check
                if (!string.IsNullOrEmpty(message.Room) && !message.IsRoomJoin() && !message.IsRoomLeft())
                {
                    if (!ns.Rooms.ContainsKey(message.Room))
                    {
                        //// tried to send to a not joined room.
                        return false;
                    }
                }
            }
            
            var buff = message.SerializeNative();
            _webSocket.Send(buff);
            return true;
        }

        //The ask method writes a message to the server and blocks until a response or an error received.
        public UniTask<Message> Ask(Message message)
        {
            if (_closed)
                return UniTask.FromException<Message>(new Exception(Exceptions.ErrorClosed));

            //id = current time in tick
            var id = $"{Configuration.waitComesFromClientPrefix.ToString()}{DateTime.Now.Ticks.ToString()}";
            message.Wait = id;

            var tcs = new UniTaskCompletionSource<Message>();
            //wait for response or error from server
            if (!_waitingMessages.ContainsKey(id))
            {
                _waitingMessages.Add(id, (m) =>
                {
                    if (!string.IsNullOrEmpty(m.Error))
                    {
                        tcs.TrySetException(new Exception(m.Error));
                    }
                    else
                    {
                        tcs.TrySetResult(m);
                    }
                });
            }
            
            var wrote = WriteBinary(message);
            if (!wrote)
                return UniTask.FromException<Message>(new Exception(Exceptions.ErrorWrite));
            
            return tcs.Task;
        }

        public void WriteEmptyReply(string wait)
        {
            var message = $"{wait}{Configuration.messageSeparator}";
            _webSocket.Send(message);
        }

        public async UniTask<string> AskDisconnect(Message message)
        {
            var ns = GetNamespace(message.Namespace);
            if (ns == null)
                return Exceptions.ErrorBadNamespace;
            try
            {
                await Ask(message);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            ns.ForceLeaveAll(true);
            _connectedNamespaces.Remove(message.Namespace);
            message.IsLocal = true;
            return ns.FireEvent(message);
        }

        public async UniTask<NSConnection> AskConnect(string @namespace)
        {
            var ns = GetNamespace(@namespace);
            if (ns != null)
                return ns;

            var events = _namespaces.GetEvents(@namespace);
            if (events == null)
            {
                throw new Exception(Exceptions.ErrorBadNamespace);
            }

            var connectMessage = new Message
            {
                Namespace = @namespace,
                Event = Configuration.OnNamespaceConnect,
                IsLocal = true,
                SetBinary = true
            };

            ns = new NSConnection(this, @namespace, events);
            var error = ns.FireEvent(connectMessage);
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }

            try
            {
                await Ask(connectMessage);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            _connectedNamespaces.Add(@namespace, ns);
            connectMessage.Event = Configuration.OnNamespaceConnected;
            ns.FireEvent(connectMessage);
            return ns;
        }

        private void ReplyDisconnect(Message message)
        {
            if (string.IsNullOrEmpty(message.Wait) || message.IsNoOp)
            {
                return;
            }

            var ns = GetNamespace(message.Namespace);
            if (ns == null)
            {
                WriteEmptyReply(message.Wait);
                return;
            }

            ns.ForceLeaveAll(true);

            WriteEmptyReply(message.Wait);
            ns.FireEvent(message);
        }

        public void ReplyConnect(Message message)
        {
            if (string.IsNullOrEmpty(message.Wait) || message.IsNoOp)
            {
                return;
            }

            var ns = GetNamespace(message.Namespace);
            if (ns == null)
            {
                WriteEmptyReply(message.Wait);
                return;
            }

            var events = _namespaces.GetEvents(message.Namespace);
            if (events == null)
            {
                message.Error = Exceptions.ErrorBadNamespace;
                WriteBinary(message);
                return;
            }

            ns = new NSConnection(this, message.Namespace, events);
            _connectedNamespaces.Add(message.Namespace, ns);
            WriteEmptyReply(message.Wait);
            message.Event = Configuration.OnNamespaceConnected;
            ns.FireEvent(message);

            if (_waitServerConnectNotifiers != null && _waitServerConnectNotifiers.Count > 0)
            {
                if (_waitServerConnectNotifiers.ContainsKey(message.Namespace))
                {
                    _waitServerConnectNotifiers[message.Namespace]();
                }
            }
        }

        public void Close()
        {
            if(_closed)
            {
                return;
            }
            
            var disconnectMessage = new Message
            {
                Event = Configuration.OnNamespaceDisconnect,
                IsLocal = true,
                IsForced = true
            };

            foreach (var ns in _connectedNamespaces.Values)
            {
                ns.ForceLeaveAll(true);
                
                disconnectMessage.Namespace = ns.Namespace;
                ns.FireEvent(disconnectMessage);
            }
            _connectedNamespaces.Clear();
            
            _waitingMessages.Clear();
            _closed = true;

            if (_webSocket.State == WebSocketStates.Open)
            {
                _webSocket.Close();
            }
        }
        
        public void Dispose()
        {
            Close();
        }
    }
}