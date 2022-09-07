using UnityEngine;

namespace NeffosCSharp.ConnectionHandles
{
    public class MainNamespaceHandler : IConnectionHandler
    {
        public string Key => "Game";

        public string OnNamespaceConnected(NSConnection connection, Message message)
        {
            Debug.Log("Connected to namespace: " + message.Namespace);
            return message.Error;
        }

        public string OnNamespaceDisconnect(NSConnection connection, Message message)
        {
            Debug.Log("Disconnected from namespace: " + message.Namespace);
            return message.Error;
        }

        public string OnRoomJoined(NSConnection connection, Message message)
        {
            Debug.Log("Joined room " + message.Room);
            return message.Error;
        }

        public string OnRoomLeft(NSConnection connection, Message message)
        {
            Debug.Log("Left Room " + message.Room);
            return message.Error;
        }

        public string Handle(NSConnection nsConnection, Message message)
        {
            Debug.Log($"Server says: {message.Body} <color=red>event</color> {message.Event}");
            return message.Error;
        }
    }
}