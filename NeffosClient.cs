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

        //dial with connection handler
        public UniTask<Connection> DialAsync(string endPoint, IConnectionHandler[] connectionHandlers,
            Options options, Action<string> reject)
        {
            var ucs = new UniTaskCompletionSource<Connection>();

            var namespaces = NamespacesExtensions.ResolveNamespace(connectionHandlers, reject);

            if (namespaces == null || namespaces.Count == 0)
            {
                ucs.TrySetException(new Exception("No connection handlers found"));
                return ucs.Task;
            }

            if (options.Headers == null)
                options.Headers = new Dictionary<string, string>();
            if (!options.Headers.ContainsKey("Authorization"))
                options.Headers.Add("Authorization", Key);

            if (options.ReconnectionAttempts > 0)
            {
                if (!options.Headers.ContainsKey(WebsocketReconnectHeaderKey))
                    options.Headers.Add(WebsocketReconnectHeaderKey, options.ReconnectionAttempts.ToString());
            }
            else if (options.Headers.ContainsKey(WebsocketReconnectHeaderKey))
            {
                options.Headers.Remove(WebsocketReconnectHeaderKey);
            }

            var ws = new WebSocket(new Uri(endPoint));
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
                foreach (var header in options.Headers)
                {
                    e.AddHeader(header.Key, header.Value);
                }
            };
            var connection = new Connection(ws, namespaces);

            ws.OnMessage += OnMessage;
            ws.OnBinary += OnBinary;
            ws.OnError += OnError;
            ws.OnClosed += OnClosed;
            ws.OnOpen += OnOpen;

            ws.Open();

            void OnMessage(WebSocket webSocket, string message)
            {
                var error = connection.Handle(message.ToByteArray());
                if (!string.IsNullOrEmpty(error))
                {
                    ucs.TrySetException(new Exception(error));
                }

                if (connection.IsAcknowledged)
                    ucs.TrySetResult(connection);
            }

            void OnBinary(WebSocket webSocket, byte[] data)
            {
                //encode data to string
                var error = connection.Handle(data);
                if (!string.IsNullOrEmpty(error))
                {
                    ucs.TrySetException(new Exception(error));
                }

                if (connection.IsAcknowledged)
                    ucs.TrySetResult(connection);
            }

            void OnError(WebSocket webSocket, string exception)
            {
                if (!connection.Closed)
                {
                    webSocket.OnMessage -= OnMessage;
                    webSocket.OnBinary -= OnBinary;
                    webSocket.OnError -= OnError;
                    webSocket.OnClosed -= OnClosed;
                    webSocket.OnInternalRequestCreated = null;
                }

                //log
                Debug.Log("reconnecting on error...");

                if (options.ReconnectionAttempts <= 0)
                {
                    connection.Close();
                }

                var previouslyConnectedNamespacesNamesOnly = new Dictionary<string, List<string>>();
                foreach (var p in connection.ConnectedNamespaces)
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

                connection.Close();

                WhenResourceOnline(endPoint, 10,
                    async _ =>
                    {
                        var newConnection = await DialAsync(endPoint, connectionHandlers, options, reject);
                        //var newConnection = new Connection(ws, namespaces);
                        ConnectToNamespace(previouslyConnectedNamespacesNamesOnly, newConnection).Forget();
                    }).Forget();
            }

            void OnClosed(WebSocket webSocket, ushort code, string reason)
            {
                if (!connection.Closed)
                {
                    webSocket.OnMessage -= OnMessage;
                    webSocket.OnBinary -= OnBinary;
                    webSocket.OnError -= OnError;
                    webSocket.OnClosed -= OnClosed;
                    webSocket.OnInternalRequestCreated = null;
                }

                //log
                Debug.Log("reconnecting on closed...");

                if (options.ReconnectionAttempts <= 0)
                {
                    connection.Close();
                }

                var previouslyConnectedNamespacesNamesOnly = new Dictionary<string, List<string>>();
                var previousNsConnections = connection.ConnectedNamespaces.Values;
                foreach (var p in connection.ConnectedNamespaces)
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

                connection.Close();

                WhenResourceOnline(endPoint, 10,
                    _ =>
                    {
                        //DialAsync(endPoint, connectionHandlers, options, reject).Forget();
                        var newConnection = new Connection(ws, namespaces);
                        ConnectToNamespace(previouslyConnectedNamespacesNamesOnly, newConnection).Forget();
                    }).Forget();
            }

            void OnOpen(WebSocket websocket)
            {
                ws.Send(Configuration.ackBinary);
            }

            return ucs.Task;
        }
        private bool _isOnline = false;
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
            _isOnline = false;

            //check if endpoint is online
            var request = new HTTPRequest(new Uri(endpoint), (req, resp) =>
            {
                if (resp != null)
                {
                    _isOnline = true;
                    nofifyOnline(tries);
                    //log
                    Debug.Log($"<color=red>Neffos: Resource is online, tries: {tries}</color>");
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
            if (tries >= 5 || _isOnline)
            {
                //log
                Debug.Log("tries >= 5 return");
                return;
            }

            tries++;
            goto Retry;
        }

        private async UniTask ConnectToNamespace(
            Dictionary<string, List<string>> previouslyConnectedNamespacesNamesOnly, Connection connection)
        {
            foreach (var (key, value) in previouslyConnectedNamespacesNamesOnly)
            {
                var newNsConn = await connection.AskConnect(key);
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