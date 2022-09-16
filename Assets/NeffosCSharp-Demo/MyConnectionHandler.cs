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

        public string Handle(NSConnection nsConnection, Message message)
        {
            //log received event
            Debug.Log("Received event: " + message.Event + " from namespace: " + message.Namespace + " error: " + message.Error);
            if (message.IsError)
            {
                var errorMsg = message.Body.ToUTF8String();
                Debug.LogError($"[Event {message.Event}]HandleDisconnect error: " + errorMsg);
                return errorMsg;
            }

            try
            {
                if (message.Event.Equals("OnDisconnect"))
                {
                    var client = GameObject.FindObjectOfType<DemoNeffos>();
                    client.client1.Close();
                    //nsConnection.Connection.Close();
                    Debug.Log($"[Event {message.Event}]HandleDisconnect: " + message.Body.ToUTF8String());
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return message.Error;
        }
    }
}