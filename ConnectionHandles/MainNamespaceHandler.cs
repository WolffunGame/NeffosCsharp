using System;
using UnityEngine;

namespace NeffosCSharp.ConnectionHandles
{
    public class MainNamespaceHandler : IConnectionHandler
    {
        public string Key => "Game";

        public string OnNamespaceConnected(NSConnection connection, Message message)
        {
            return message.Error;
        }

        public string OnNamespaceDisconnect(NSConnection connection, Message message)
        {
            return message.Error;
        }

        public string OnRoomJoined(NSConnection connection, Message message)
        {
            return message.Error;
        }

        public string OnRoomLeft(NSConnection connection, Message message)
        {
            return message.Error;
        }

        public string Handle(NSConnection nsConnection, Message message)
        {
            return message.Error;
        }
    }
}