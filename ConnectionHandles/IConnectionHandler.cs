namespace NeffosCSharp.ConnectionHandles
{
    public interface IConnectionHandler
    {
        public  string Key { get;}
        
        public  string OnNamespaceConnected(NSConnection connection, Message message);
        public  string OnNamespaceDisconnect(NSConnection connection, Message message);
        public  string OnRoomJoined(NSConnection connection, Message message);
        public  string OnRoomLeft(NSConnection connection, Message message);
        public  string Handle(NSConnection nsConnection, Message message);

    }
}