using System;
using System.Collections.Generic;
using BestHTTP;
using BestHTTP.WebSocket;
using Cysharp.Threading.Tasks;
using NeffosCSharp.ConnectionHandles;
using UnityEngine;


namespace NeffosCSharp
{
    public class NeffosClientA : IDisposable
    {
        const string WebsocketReconnectHeaderKey = "X-Websocket-Reconnect";

        public string Key { get; set; }

        public Connection Connection { get; private set; }

        private Options _options;
        private string _endPoint;
        private IConnectionHandler[] _connectionHandlers;


        public NeffosClientA(string endPoint, Options options, params IConnectionHandler[] connectionHandlers)
        {
            _endPoint = endPoint;
            _options = options;
            _connectionHandlers = connectionHandlers;
        }

        //dial with connection handler
        public void Dial(Action<string> reject)
        {
            var namespaces = NamespacesExtensions.ResolveNamespace(_connectionHandlers, reject);

            if (namespaces == null || namespaces.Count == 0)
            {
                throw new Exception("No connection handlers found");
            }

            if (_options.Headers == null)
                _options.Headers = new Dictionary<string, string>();
            if (!_options.Headers.ContainsKey("Authorization"))
                _options.Headers.Add("Authorization", Key);

            if (_options.ReconnectionAttempts > 0)
            {
                if (!_options.Headers.ContainsKey(WebsocketReconnectHeaderKey))
                    _options.Headers.Add(WebsocketReconnectHeaderKey, _options.ReconnectionAttempts.ToString());
            }
            else if (_options.Headers.ContainsKey(WebsocketReconnectHeaderKey))
            {
                _options.Headers.Remove(WebsocketReconnectHeaderKey);
            }

            var ws = new WebSocket(new Uri(_endPoint));
#if !UNITY_WEBGL || UNITY_EDITOR
            ws.StartPingThread = true;

#if !BESTHTTP_DISABLE_PROXY
            if (HTTPManager.Proxy != null)
                ws.OnInternalRequestCreated = (ws, internalRequest) =>
                    internalRequest.Proxy =
                        new HTTPProxy(HTTPManager.Proxy.Address, HTTPManager.Proxy.Credentials, false);
#endif
#endif
            ws.OnInternalRequestCreated += (sender, e) =>
            {
                foreach (var header in _options.Headers)
                {
                    e.AddHeader(header.Key, header.Value);
                }
            };
            Connection = new Connection(ws, namespaces);

            ws.OnMessage += OnMessage;
            ws.OnBinary += OnBinary;
            ws.OnError += OnError;
            ws.OnClosed += OnClosed;
            ws.OnOpen += OnOpen;

            ws.Open();
        }

        void OnMessage(WebSocket webSocket, string message)
        {
            if (Connection == null)
            {
                throw new Exception("Connection is null");
            }

            var error = Connection.Handle(message.ToByteArray());
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }
        }

        void OnBinary(WebSocket webSocket, byte[] data)
        {
            //encode data to string
            var error = Connection.Handle(data);
            if (!string.IsNullOrEmpty(error))
            {
                throw new Exception(error);
            }
        }

        void OnError(WebSocket webSocket, string exception)
        {
            webSocket.OnMessage -= OnMessage;
            webSocket.OnBinary -= OnBinary;
            webSocket.OnError -= OnError;
            webSocket.OnClosed -= OnClosed;
            webSocket.OnOpen -= OnOpen;
            webSocket.OnInternalRequestCreated = null;
            //log
            Debug.Log("reconnecting on error...");

            if (_options.ReconnectionAttempts <= 0)
            {
                Connection.Close();
            }

            var previouslyConnectedNamespacesNamesOnly = new Dictionary<string, List<string>>();
            foreach (var p in Connection.ConnectedNamespaces)
            {
                var previouslyJoinedRooms = new List<string>();
                if (p.Value.Rooms.Count > 0)
                {
                    foreach (var r in p.Value.Rooms)
                    {
                        previouslyJoinedRooms.Add(r.Key);
                    }
                }

                previouslyConnectedNamespacesNamesOnly.Add(p.Key, previouslyJoinedRooms);
            }

            Connection.Close();

            WhenResourceOnline(_endPoint, 10,
                async _ =>
                { 
                    Dial(Debug.LogError);
                    UniTask.Delay(2000);
                    await ConnectToNamespace(previouslyConnectedNamespacesNamesOnly, Connection);
                }).Forget();
        }

        void OnClosed(WebSocket webSocket, ushort code, string reason)
        {
            if (Connection.Closed)
            {
                // reconnection is NOT allowed when:
                // 1. server force-disconnect this client.
                // 2. client disconnects itself manually.
                // We check those two ^ with conn.isClosed().
                Debug.Log("manual disconnect.");
            }
            else
            {
                webSocket.OnMessage -= OnMessage;
                webSocket.OnBinary -= OnBinary;
                webSocket.OnError -= OnError;
                webSocket.OnClosed -= OnClosed;
                webSocket.OnOpen -= OnOpen;
                webSocket.OnInternalRequestCreated = null;

                //log
                Debug.Log("reconnecting on closed...");

                if (_options.ReconnectionAttempts <= 0)
                {
                    Connection.Close();
                }

                var previouslyConnectedNamespacesNamesOnly = new Dictionary<string, List<string>>();
                foreach (var p in Connection.ConnectedNamespaces)
                {
                    var previouslyJoinedRooms = new List<string>();
                    if (p.Value.Rooms.Count > 0)
                    {
                        foreach (var r in p.Value.Rooms)
                        {
                            previouslyJoinedRooms.Add(r.Key);
                        }
                    }

                    previouslyConnectedNamespacesNamesOnly.Add(p.Key, previouslyJoinedRooms);
                }

                Connection.Close();

                WhenResourceOnline(_endPoint, 10,
                    _ =>
                    {
                        Dial(Debug.LogError);
                        ConnectToNamespace(previouslyConnectedNamespacesNamesOnly, Connection).Forget();
                    });
            }
        }

        void OnOpen(WebSocket websocket)
        {
            websocket.Send(Configuration.ackBinary);
        }

        private async UniTask WhenResourceOnline(string endPoint, int checkEvery, Action<int> nofifyOnline)
        {
            // Don't fire webscoket requests just yet.
            // We check if the HTTP endpoint is alive with a simple fetch, if it is alive then we notify the caller
            // to proceed with a websocket request. That way we can notify the server-side how many times
            // this client was trying to reconnect as well.
            // Note:
            // Chrome itself is emitting net::ERR_CONNECTION_REFUSED and the final Bad Request messages to the console on network failures on fetch,
            // there is no way to block them programmatically, we could do a console.clear but this will clear any custom logging the end-dev may has too.
            int tries = 1;


            var endpoint = endPoint.Replace("ws://", "http://").Replace("wss://", "https://");
            Retry:


            //check if endpoint is online
            var request = new HTTPRequest(new Uri(endpoint), (req, resp) =>
            {
                if (resp != null)
                {
                    nofifyOnline(tries);
                    //log
                    Debug.Log($"<color=red>Neffos: Resource is online, tries: {tries}</color>");
                    tries = 6;
                }
                else
                {
                    //log
                    Debug.Log("<color=red>response failed</color>");
                }
            });
            request.Send();

            //timeout
            await UniTask.Delay(TimeSpan.FromSeconds(checkEvery));
            if (tries >= 5)
            {
                //log
                Debug.Log("tries >= 5 return");
                return;
            }

            tries++;
            goto Retry;
        }

        private async UniTask ConnectToNamespace(Dictionary<string, List<string>> previouslyConnectedNamespacesNamesOnly, Connection connection)
        {
            foreach (var (key, value) in previouslyConnectedNamespacesNamesOnly)
            {
                var newNsConn = await connection.Connect(key);
                //log
                Debug.Log($"Neffos: Reconnecting to namespaces: {key}");
                foreach (var room in value)
                {
                    await newNsConn.JoinRoom(room);
                    //log
                    Debug.Log($"Neffos: Reconnecting to rooms: {room}");
                }
            }
        }

        public void Dispose()
        {
        }
    }
}