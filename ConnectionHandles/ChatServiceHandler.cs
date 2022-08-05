using UnityEngine;

namespace NeffosCSharp.ConnectionHandles
{
    public class ChatServiceHandler : ConnectionHandlerBase
    {
        //TODO: Change Key here
        public override string Key => "Game";
        public override string OnNamespaceConnected(NSConnection connection, Message message)
        {
            Debug.Log("Connected to namespace: " + message.Namespace);
            return message.Error;
        }

        public override string OnNamespaceDisconnect(NSConnection connection, Message message)
        {
            Debug.Log("Disconnected from namespace: " + message.Namespace);
            return message.Error;
        }

        public override string OnRoomJoined(NSConnection connection, Message message)
        {
            Debug.Log("Joined room " + message.Room);
            return message.Error;
        }

        public override string OnRoomLeft(NSConnection connection, Message message)
        {
            Debug.Log("Left Room "+message.Room);
            return message.Error;
        }

        public override string Handle(NSConnection nsConnection, Message message)
        {
            Debug.Log("Server says: " + message.Body);
            return message.Error;
        }
    }
}