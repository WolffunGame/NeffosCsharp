namespace NeffosCSharp
{
    public class Configuration
    {
        /* The OnNamespaceConnect is the event name that it's fired on before namespace connect. */
        public const string OnNamespaceConnect = "_OnNamespaceConnect";

        /* The OnNamespaceConnected is the event name that it's fired on after namespace connect. */
        public const string OnNamespaceConnected = "_OnNamespaceConnected";

        /* The OnNamespaceDisconnect is the event name that it's fired on namespace disconnected. */
        public const string OnNamespaceDisconnect = "_OnNamespaceDisconnect";

        /* The OnRoomJoin is the event name that it's fired on before room join. */
        public const string OnRoomJoin = "_OnRoomJoin";

        /* The OnRoomJoined is the event name that it's fired on after room join. */
        public const string OnRoomJoined = "_OnRoomJoined";

        /* The OnRoomLeave is the event name that it's fired on before room leave. */
        public const string OnRoomLeave = "_OnRoomLeave";

        /* The OnRoomLeft is the event name that it's fired on after room leave. */
        public const string OnRoomLeft = "_OnRoomLeft";

        /* The OnAnyEvent is the event name that it's fired, if no incoming event was registered, it's a "wilcard". */
        public const string OnAnyEvent = "_OnAnyEvent";

        /* The OnNativeMessage is the event name, which if registered on empty ("") namespace
        it accepts native messages(Message.Body and Message.IsNative is filled only). */
        public const string OnNativeMessage = "_OnNativeMessage";

        public const string ackBinary = "M"; // see `onopen`, comes from client to server at startup.

        // see `handleAck`.
        // comes from server to client after ackBinary and ready as a prefix, the rest message is the conn's ID.
        public const char ackIDBinary = 'A';

        // comes from server to client if `Server#OnConnected` errored as a prefix, the rest message is the error text.
        public const char ackNotOKBinary = 'H';

        public const char waitIsConfirmationPrefix = '#';
        public const char waitComesFromClientPrefix = '$';

        /* Obsiously, the below should match the server's side. */
        public const string messageSeparator = ";";
        public const string messageFieldSeparatorReplacement = "@%!semicolon@%!";
        public const int validMessageSepCount = 7;
        public const string trueString = "1";
        public const string falseString = "0";
        
        public const string badNamespaceError = "Bad Namespace";
    }
}