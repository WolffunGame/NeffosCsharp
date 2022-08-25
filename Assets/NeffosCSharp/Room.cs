using Cysharp.Threading.Tasks;
using System.Text;

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
            message.Body = Encoding.UTF8.GetBytes(body);
            return _nsConnection.Connection.WriteBinary(message);
        }

        public bool Emit(string eventName, byte[] data)
        {
            var message = new Message();
            message.Event = eventName;
            message.Namespace = _nsConnection.Namespace;
            message.Room = _name;
            message.Body = data;
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