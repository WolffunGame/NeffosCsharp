using System;
using System.Collections.Generic;
using System.Text;
using BestHTTP;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Math.EC;
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
        public UniTask<Connection> DialAsync(string endPoint, ConnectionHandlerBase[] connectionHandlers,
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

            options.Headers.Add("Authorization", Key);
            
            if (options.ReconnectionAttempts > 0)
            {
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
                ws.OnInternalRequestCreated = (ws, internalRequest) => internalRequest.Proxy = new HTTPProxy(HTTPManager.Proxy.Address, HTTPManager.Proxy.Credentials, false);
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
                ucs.TrySetException(new Exception(exception));
                connection.Close();
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

                // if (options.ReconnectionAttempts <= 0)
                // {
                //     connection.Close();
                // }

                // var previouslyConnectedNamespacesNamesOnly = new Dictionary<string, List<string>>();
                // foreach (var p in connection.ConnectedNamespaces)
                // {
                //     var previouslyJoinedRooms = new List<string>();
                //     if (p.Value.Rooms.Count > 0)
                //     {
                //         foreach (var r in p.Value.Rooms)
                //         {
                //             previouslyJoinedRooms.Add(r.Key);
                //         }
                //     }
                //     previouslyConnectedNamespacesNamesOnly.Add(p.Key, previouslyJoinedRooms);
                // }
                // connection.Close();
                //TODO Try to reconnect
            }
            void OnOpen(WebSocket websocket)
            {
                Debug.Log("Websocket opened");
                ws.Send(Configuration.ackBinary);
            }
            return ucs.Task;
        }

        public void Dispose()
        {
            
        }
    }
}