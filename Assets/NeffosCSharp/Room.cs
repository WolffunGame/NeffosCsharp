using Cysharp.Threading.Tasks;

namespace NeffosCSharp
{
    public class Room
    {
        private NSConnection _nsConnection;
        private string _name;
        
        public Room(NSConnection nsConnection, string name)
        {
            _nsConnection = nsConnection;
            _name = name;
        }

        public bool Emit(string eventName, string body)
        {
            var message = new Message();
            message.Event = eventName;
            message.Namespace = _nsConnection.Namespace;
            message.Room = _name;
            message.Body = body;
            return _nsConnection.Connection.WriteBinary(message);
        }

        public UniTask Leave()
        {
            var message = new Message();
            message.Event = Configuration.OnRoomLeave;
            message.Namespace = _nsConnection.Namespace;
            message.Room = _name;
            return _nsConnection.AskRoomLeave(message);
        }
    }
}