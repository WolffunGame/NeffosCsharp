using System;
using System.Collections.Generic;
using BestHTTP;
using BestHTTP.WebSocket;
using Cysharp.Threading.Tasks;
using NeffosCSharp.ConnectionHandles;
using UnityEngine;


namespace NeffosCSharp
{
    public class NeffosClient : IDisposable
    {
        const string WebsocketReconnectHeaderKey = "X-Websocket-Reconnect";

        public string Key { get; set; }

        public AsyncReactiveProperty<NeffosClientState> State { get; } =
            new AsyncReactiveProperty<NeffosClientState>(NeffosClientState.UnKnown);

        public Connection Connection => _connection;

        private UniTaskCompletionSource<Connection> ConnectionTcs { get; set; }
        private Connection _connection;

        private readonly Options _options;
        private readonly string _endPoint;
        private readonly IConnectionHandler[] _connectionHandlers;


        public NeffosClient(string endPoint, Options options, params IConnectionHandler[] connectionHandlers)
        {
            _endPoint = endPoint;
            _options = options;
            _connectionHandlers = connectionHandlers;
        }

        //dial with connection handler
        public UniTask<Connection> DialAsync(Action<string> reject)
        {
            State.Value = NeffosClientState.Connecting;
            ConnectionTcs = new UniTaskCompletionSource<Connection>();

            var namespaces = NamespacesExtensions.ResolveNamespace(_connectionHandlers, reject);

            if (namespaces == null || namespaces.Count == 0)
            {
                ConnectionTcs.TrySetException(new Exception("No connection handlers found"));
            }

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

            var webSocket = new WebSocket(new Uri(_endPoint));
#if !UNITY_WEBGL || UNITY_EDITOR
            webSocket.StartPingThread = true;

#if !BESTHTTP_DISABLE_PROXY
            if (HTTPManager.Proxy != null)
                webSocket.OnInternalRequestCreated = (ws, internalRequest) =>
                    internalRequest.Proxy =
                        new HTTPProxy(HTTPManager.Proxy.Address, HTTPManager.Proxy.Credentials, false);
#endif
#endif
            webSocket.OnInternalRequestCreated += (sender, e) =>
            {
                foreach (var header in _options.Headers)
                {
                    e.AddHeader(header.Key, header.Value);
                }
            };

            _connection = new Connection(webSocket, namespaces);
            _connection.ReconnectTries = _options.ReconnectionAttempts;

            webSocket.OnMessage += OnMessage;
            webSocket.OnBinary += OnBinary;
            webSocket.OnError += OnError;
            webSocket.OnClosed += OnClosed;
            webSocket.OnOpen += OnOpen;

            webSocket.Open();
            //wait for acknowledged
            return ConnectionTcs.Task;
        }

        void OnMessage(WebSocket webSocket, string message)
        {
            if (_connection == null)
            {
                throw new Exception("Connection is null");
            }

            var error = _connection.Handle(message.ToByteArray());
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
                return;
            }

            if (_connection.IsAcknowledged)
            {
                var s = ConnectionTcs.TrySetResult(_connection);
                if (s)
                {
                    State.Value = NeffosClientState.Connected;
                }
            }
        }

        void OnBinary(WebSocket webSocket, byte[] data)
        {
            //encode data to string
            var error = _connection.Handle(data);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
                return;
            }

            if (_connection.IsAcknowledged)
            {
                var s = ConnectionTcs.TrySetResult(_connection);
                if (s)
                {
                    State.Value = NeffosClientState.Connected;
                }
            }
        }

        void OnError(WebSocket webSocket, string exception)
        {
            Debug.LogError(exception);
            _connection?.Close();
            Reconnect(webSocket).Forget();
        }

        void OnClosed(WebSocket webSocket, ushort code, string reason)
        {
            Debug.Log($"WebSocket closed with code: {code} and reason: {reason}");
            Reconnect(webSocket).Forget();
        }

        void OnOpen(WebSocket websocket)
        {
            websocket.Send(Configuration.ackBinary);
        }

        private async UniTask<bool> WhenResourceOnline(string endPoint, float checkEvery)
        {
            // Don't fire webscoket requests just yet.
            // We check if the HTTP endpoint is alive with a simple fetch, if it is alive then we notify the caller
            // to proceed with a websocket request. That way we can notify the server-side how many times
            // this client was trying to reconnect as well.
            // Note:
            // Chrome itself is emitting net::ERR_CONNECTION_REFUSED and the final Bad Request messages to the console on network failures on fetch,
            // there is no way to block them programmatically, we could do a console.clear but this will clear any custom logging the end-dev may has too.
            int tries = 1;
            State.Value = NeffosClientState.Reconnecting;
            var endpoint = endPoint.Replace("ws://", "http://").Replace("wss://", "https://");
            Retry:

            //check if endpoint is online
            var request = new HTTPRequest(new Uri(endpoint));
            await request.Send();

            if (request.State == HTTPRequestStates.Finished)
            {
                if (request.Response != null)
                {
                    return true;
                }

                if (request.Exception != null)
                {
                    Debug.LogError($"[{nameof(NeffosClient)}]: {request.Exception}");
                }
                else if (tries >= _options.ReconnectionAttempts)
                {
                    return false;
                }
            }
            else
            {
                Debug.Log($"[{nameof(NeffosClient)}] Trying to reconnect but failed {request.Exception}");
            }

            //timeout
            await UniTask.Delay(TimeSpan.FromSeconds(checkEvery));
            if (tries >= _options.ReconnectionAttempts)
            {
                return false;
            }

            tries++;
            goto Retry;
        }

        private async UniTask ConnectToNamespace(
            Dictionary<string, List<string>> previouslyConnectedNamespacesNamesOnly, Connection connection)
        {
            foreach (var (key, value) in previouslyConnectedNamespacesNamesOnly)
            {
                var newNsConn = await connection.Connect(key);
                foreach (var room in value)
                {
                    await newNsConn.JoinRoom(room);
                }
            }
        }

        // reconnection is NOT allowed when:
        // 1. server force-disconnect this client.
        // 2. client disconnects itself manually.
        // We check those two ^ with conn.isClosed().
        public async UniTask Reconnect(WebSocket webSocket)
        {
            if (State.Value == NeffosClientState.Reconnecting || _connection.Closed)
            {
                return;
            }

            if (!_connection.Closed)
            {
                webSocket.OnMessage -= OnMessage;
                webSocket.OnBinary -= OnBinary;
                webSocket.OnError -= OnError;
                webSocket.OnClosed -= OnClosed;
                webSocket.OnOpen -= OnOpen;
                webSocket.OnInternalRequestCreated = null;
            }

            //log
            if (_options.ReconnectionAttempts <= 0)
            {
                _connection.Close();
            }

            var previouslyConnectedNamespacesNamesOnly = new Dictionary<string, List<string>>();
            foreach (var p in _connection.ConnectedNamespaces)
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

            _connection.Close();

            var isOnline = await WhenResourceOnline(_endPoint, _options.ReconnectEvery);
            if (isOnline)
            {
                var newConnection = await DialAsync(Debug.LogError);
                await ConnectToNamespace(previouslyConnectedNamespacesNamesOnly, newConnection);
            }
            else
            {
                State.Value = NeffosClientState.Offline;
            }
        }

        public void Dispose()
        {
        }
    }
}