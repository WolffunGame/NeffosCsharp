using System;
using NeffosCSharp;
using NeffosCSharp.ConnectionHandles;
using UnityEngine;

namespace Scenes
{
    public class MyConnectionHandler : IConnectionHandler
    {
        public string Key => "Game";

        public string OnNamespaceConnected(NSConnection connection, Message message)
        {
            //Debug.Log("Connected to namespace: " + message.Namespace);
            return message.Error;
        }

        public string OnNamespaceDisconnect(NSConnection connection, Message message)
        {
            //Debug.Log("Disconnected from namespace: " + message.Namespace);
            return message.Error;
        }

        public string OnRoomJoined(NSConnection connection, Message message)
        {
            //Debug.Log("Joined room " + message.Room);
            return message.Error;
        }

        public string OnRoomLeft(NSConnection connection, Message message)
        {
            //Debug.Log("Left Room " + message.Room);
            return message.Error;
        }

        private const string MultipleAccountEvent = "DisconnectPrevDevice";

        public string Handle(NSConnection nsConnection, Message message)
        {
            Debug.Log("[MyConnectionHandler.Handle] Received event from server: " + message.Event);
            if (message.Event.Equals(MultipleAccountEvent))
            {
                var client = GameObject.FindObjectOfType<DemoNeffos>();
                client.Client.Close();
                Debug.Log($"[Event {message.Event}] Multiple device login detected: ");
            }

            return message.Error;
        }
    }
}