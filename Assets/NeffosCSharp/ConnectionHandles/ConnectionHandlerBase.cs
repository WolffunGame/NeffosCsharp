namespace NeffosCSharp.ConnectionHandles
{
    public abstract class ConnectionHandlerBase
    {
        public abstract string Key { get;}
        
        public abstract string OnNamespaceConnected(NSConnection connection, Message message);
        public abstract string OnNamespaceDisconnect(NSConnection connection, Message message);
        public abstract string OnRoomJoined(NSConnection connection, Message message);
        public abstract string OnRoomLeft(NSConnection connection, Message message);
        public abstract string Handle(NSConnection nsConnection, Message message);

    }
}