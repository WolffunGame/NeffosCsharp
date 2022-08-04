using System;
using BestHTTP.WebSocket;
using BestHTTP.WebSocket.Extensions;
using NeffosCSharp.ConnectionHandles;

namespace NeffosCSharp
{
    public static class ConnectionExtensions
    {
        public static string FireEvent(this NSConnection ns, Message message)
        {
            if (ns.Events.ContainsKey(message.Event))
            {
                return ns.Events[message.Event].Invoke(ns, message);
            }

            if (ns.Events.ContainsKey(Configuration.OnAnyEvent))
            {
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
            // 1. if contains function instead of a string key then it's Events otherwise it's Namespaces.
            // 2. if contains a mix of functions and keys then ~put those functions to the namespaces[""]~ it is NOT valid.

            var events = new EventMap();
            var totalKeys = 0;
            
            for (var i = 0; i < connectionHandlers.Length; i++)
            {
                totalKeys++;
                var connectionHandler = connectionHandlers[i];
                events.Add(connectionHandler.Key, connectionHandler.Handle);
            }
            
            if (totalKeys == 0)
            {
                if (reject != null)
                {
                    reject("Connection Handler is empty");
                }
                
                return null;
            }

            namespaces.Add(Configuration.OnAnyEvent, events);
            return namespaces;
        }
    }


}