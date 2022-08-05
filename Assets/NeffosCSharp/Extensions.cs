using System;
using BestHTTP.WebSocket;
using BestHTTP.WebSocket.Extensions;
using NeffosCSharp.ConnectionHandles;
using UnityEngine;

namespace NeffosCSharp
{
    public static class ConnectionExtensions
    {
        public static string FireEvent(this NSConnection ns, Message message)
        {
            if (ns.Events.ContainsKey(message.Event))
            {
                Debug.Log("fired event: " + message.Event);
                return ns.Events[message.Event].Invoke(ns, message);
            }

            if (ns.Events.ContainsKey(Configuration.OnAnyEvent))
            {
                Debug.Log("fired event: " + Configuration.OnAnyEvent);
                return ns.Events[Configuration.OnAnyEvent].Invoke(ns, message);
            }

            return string.Empty;
        }
        
    }

    public static class NamespacesExtensions
    {
        public static EventMap GetEvents(this NamespaceMap namespaces, string @namespace)
        {
            if (namespaces.ContainsKey(@namespace))
            {
                return namespaces[@namespace];
            }

            return null;
        }
        
        public static NamespaceMap ResolveNamespace(ConnectionHandlerBase[] connectionHandlers, Action<string> reject)
        {
            if (connectionHandlers == null || connectionHandlers.Length == 0)
            {
                if (reject != null)
                {
                    reject("Connection Handler is empty");
                }

                return null;
            }

            var namespaces = new NamespaceMap();

            for (var i = 0; i < connectionHandlers.Length; i++)
            {
                var connectionHandler = connectionHandlers[i];
                var events = new EventMap();
                events.Add(connectionHandler.Key, connectionHandler.Handle);
                events.Add(Configuration.OnNamespaceConnected, connectionHandler.OnNamespaceConnected);
                events.Add(Configuration.OnNamespaceDisconnect, connectionHandler.OnNamespaceDisconnect);
                events.Add(Configuration.OnRoomJoined, connectionHandler.OnRoomJoined);
                events.Add(Configuration.OnRoomLeft, connectionHandler.OnRoomLeft);
                namespaces.Add(connectionHandler.Key, events);
            }

            return namespaces;
        }
    }


}