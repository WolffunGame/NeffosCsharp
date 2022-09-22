namespace NeffosCSharp
{
    public enum NeffosClientState
    {
        /// <summary>
        /// Closed by application or not yet connected.
        /// </summary>
        UnKnown = 0,
        /// <summary>
        /// Try to connect to the server some times but failed.
        /// </summary>
        Offline = 1,
        /// <summary>
        /// Calling Dial method.
        /// </summary>
        Connecting = 2,
        /// <summary>
        /// Connected to the server.
        /// </summary>
        Connected = 3,
        /// <summary>
        /// Trying to reconnect.
        /// </summary>
        Reconnecting = 4,
        /// <summary>
        /// Failed to reconnect the previous namespaces and rooms.
        /// Should tell user to reload the game.
        /// </summary>
        FailedToReconnectPreviously = 5,
        /// <summary>
        /// Trying to reconnect but the connection was forced to close.
        /// </summary>
        ReconnectButWasClosed = 6
    }
}