namespace NeffosCSharp.ConnectionHandles
{
    public abstract class ConnectionHandlerBase
    {
        public abstract string Key { get;}
        public abstract string Namespace { get; }

        public abstract string Handle(NSConnection nsConnection, Message message);

    }
}