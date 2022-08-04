using System;
using System.Collections.Generic;
using System.Text;
using BestHTTP.WebSocket;
using Cysharp.Threading.Tasks;
using NeffosCSharp.ConnectionHandles;

namespace NeffosCSharp
{
    public class Neffos
    {
        const string WebsocketReconnectHeaderKey = "X-Websocket-Reconnect";

        //dial with connection handler
        public UniTask<Connection> DialAsync(string endPoint, ConnectionHandlerBase[] connectionHandlers,
            Options options, Action<string> reject)
        {
            if (!endPoint.StartsWith("ws://") && !endPoint.StartsWith("wss://"))
                endPoint = $"ws://{endPoint}";

            var ucs = new UniTaskCompletionSource<Connection>();

            var namespaces = NamespacesExtensions.ResolveNamespace(connectionHandlers, reject);
            if (namespaces == null)
            {
                ucs.TrySetException(new Exception("No connection handlers found"));
                return ucs.Task;
            }

            if (options.Headers == null)
                options.Headers = new Dictionary<string, string>();

            if (options.ReconnectionAttempts > 0)
            {
                options.Headers.Add(WebsocketReconnectHeaderKey, options.ReconnectionAttempts.ToString());
            }
            else if (options.Headers.ContainsKey(WebsocketReconnectHeaderKey))
            {
                options.Headers.Remove(WebsocketReconnectHeaderKey);
            }

            var websocket = new WebSocket(new Uri(endPoint));
            websocket.OnInternalRequestCreated += (sender, e) =>
            {
                foreach (var header in options.Headers)
                {
                    e.AddHeader(header.Key, header.Value);
                }
            };
            var connection = new Connection(websocket, namespaces);
            websocket.OnMessage += OnMessage;

            websocket.OnBinary += OnBinary;

            websocket.OnOpen += (ws) => { ws.Send(Configuration.ackBinary); };

            websocket.OnError += OnError;

            websocket.OnClosed += OnClosed;

            void OnMessage(WebSocket webSocket, string message)
            {
                var error = connection.Handle(message);
                if (error != null)
                {
                    ucs.TrySetException(new Exception(error));
                }

                if (connection.IsAcknowledged)
                    ucs.TrySetResult(connection);
            }

            void OnBinary(WebSocket webSocket, byte[] data)
            {
                //encode data to string
                var message = Encoding.UTF8.GetString(data);
                var error = connection.Handle(message);
                if (error != null)
                {
                    ucs.TrySetException(new Exception(error));
                }

                if (connection.IsAcknowledged)
                    ucs.TrySetResult(connection);
            }

            void OnError(WebSocket webSocket, string exception)
            {
                connection.Close();
                ucs.TrySetException(new Exception(exception));
            }

            void OnClosed(WebSocket webSocket, ushort code, string reason)
            {
                if (!connection.Closed)
                {
                    webSocket.OnMessage -= OnMessage;
                    webSocket.OnBinary -= OnBinary;
                    webSocket.OnError -= OnError;
                    webSocket.OnClosed -= OnClosed;
                }

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
                //TODO Try to reconnect
            }
            return ucs.Task;
        }
    }
}