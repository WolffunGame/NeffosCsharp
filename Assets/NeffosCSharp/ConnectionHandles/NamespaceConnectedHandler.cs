using UnityEngine;

namespace NeffosCSharp.ConnectionHandles
{
    public class NamespaceConnectedHandler : ConnectionHandlerBase
    {
        public NamespaceConnectedHandler(string namespaceName)
        {
            Namespace = namespaceName;
        }
        public override string Key => Configuration.OnNamespaceConnected;
        public override string Namespace { get; }

        public override string Handle(NSConnection nsConnection, Message message)
        {
            Debug.Log("Connected to namespace: " + message.Namespace);
            return message.Error;
        }
    }
}