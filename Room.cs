using Cysharp.Threading.Tasks;
using System.Text;

namespace NeffosCSharp
{
    public class Room
    {
        private NSConnection _nsConnection;
        private string _name;

        public string Name => _name;
        
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
            message.Body = body.ToByteArray();
            return _nsConnection.Connection.WriteBinary(message);
        }

        public bool EmitBinary(string eventName, byte[] data)
        {
            var message = new Message();
            message.Event = eventName;
            message.Namespace = _nsConnection.Namespace;
            message.Room = _name;
            message.Body = data;

            return _nsConnection.Connection.WriteBinary(message);
        }

        public UniTask<Message> AskBinary(string eventName, byte[] data)
        {
            var message = new Message();
            message.Event = eventName;
            message.Namespace = _nsConnection.Namespace;
            message.Room = _name;
            message.Body = data;

            return _nsConnection.Connection.Ask(message);
        }

        public UniTask<Message> Ask(string eventName, string data)
        {
            return AskBinary(eventName, data.ToByteArray());
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