using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NeffosCSharp
{
    /// <summary>
    /// The NSConn describes a connected connection to a specific namespace,
    /// it emits with the `Message.Namespace` filled and it can join to multiple rooms.
    /// A single Conn can be connected to one or more namespaces,
    /// each connected namespace is described by this class.
    /// </summary>
    public class NSConnection
    {
        private Connection _connection;
        public Connection Connection => _connection;

        private string _namespace;
        public string Namespace => _namespace;

        private EventMap _events;
        public EventMap Events => _events;

        private Dictionary<string, Room> _rooms;
        public Dictionary<string, Room> Rooms => _rooms;


        internal NSConnection(Connection connection, string @namespace, EventMap events)
        {
            _connection = connection;
            _namespace = @namespace;
            _events = events;
            _rooms = new Dictionary<string, Room>();
        }

        /// <summary>
        /// The emit method sends a message to the server with its `Message.Namespace` filled to this specific namespace.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public bool Emit(string eventName, string body)
        {
            var message = new Message();
            message.Namespace = _namespace;
            message.Event = eventName;
            message.Body = body.ToByteArray();
            return _connection.WriteNative(message);
        }

        /// <summary>
        /// The emitBinary method sends a binary message to the server with its `Message.Namespace` filled to this specific namespace
        /// and `Message.SetBinary` to true.
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public bool EmitBinary(string eventName, byte[] body)
        {
            var message = new Message();
            message.Namespace = _namespace;
            message.Event = eventName;
            message.Body = body;
            message.SetBinary = true;
            return _connection.WriteBinary(message);
        }

        public UniTask<Message> Ask(string eventName, string body)
        {
            return Ask(eventName, body.ToByteArray());
        }

        public UniTask<Message> Ask(string eventName, byte[] body)
        {
            var message = new Message();
            message.Namespace = _namespace;
            message.Event = eventName;
            message.Body = body;
            return _connection.Ask(message);
        }

        /// <summary>
        /// The joinRoom method can be used to join to a specific room, rooms are dynamic.
        /// Returns a `Room` or an error.
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        public async UniTask<Room> JoinRoom(string roomName)
        {
            return await AskRoomJoin(roomName);
        }

        public async UniTask<Room> AskRoomJoin(string roomName)
        {
            var result = _rooms.TryGetValue(roomName, out var room);
            if (result)
            {
                return room;
            }

            var joinMessage = new Message();
            joinMessage.Namespace = _namespace;
            joinMessage.Event = Configuration.OnRoomJoin;
            joinMessage.Room = roomName;
            joinMessage.IsLocal = true;

            try
            {
                await Connection.Ask(joinMessage);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not join room {roomName}", e);
            }

            var error = this.FireEvent(joinMessage);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
                return null;
            }

            room = new Room(this, roomName);
            _rooms.Add(roomName, room);
            joinMessage.Event = Configuration.OnRoomJoined;
            this.FireEvent(joinMessage);
            return room;
        }

        public async UniTask AskRoomLeave(Message message)
        {
            if (!_rooms.ContainsKey(message.Room))
            {
                throw new Exception(Exceptions.ErrorBadRoom);
            }

            try
            {
                await _connection.Ask(message);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not leave room {message.Room}", e);
            }

            var error = this.FireEvent(message);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
                return;
            }

            _rooms.Remove(message.Room);

            message.Event = Configuration.OnRoomLeft;
            this.FireEvent(message);
        }

        /// <summary>
        /// The room method returns a joined `Room`. 
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        public Room GetJoinedRoom(string roomName)
        {
            if (!_rooms.TryGetValue(roomName, out var room))
            {
                return null;
            }

            return room;
        }

        /// <summary>
        /// The leaveAll method sends a leave room signal to all rooms and fires the `OnRoomLeave` and `OnRoomLeft` (if no error occurred) events.
        /// </summary>
        public async UniTask LeaveAll()
        {
            var leaveMessage = new Message();
            leaveMessage.Namespace = _namespace;
            leaveMessage.Event = Configuration.OnRoomLeave;
            leaveMessage.IsLocal = true;
            var tasks = new List<UniTask>();
            foreach (var pair in _rooms)
            {
                leaveMessage.Room = pair.Key;

                var t = AskRoomLeave(leaveMessage);
                tasks.Add(t);
            }

            try
            {
                await UniTask.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            
        }

        public void ForceLeaveAll(bool isLocal)
        {
            var leaveMessage = new Message
            {
                Namespace = _namespace,
                Event = Configuration.OnRoomLeave,
                IsLocal = isLocal,
                IsForced = true
            };

            foreach (var pair in _rooms)
            {
                leaveMessage.Room = pair.Key;
                this.FireEvent(leaveMessage);

                leaveMessage.Event = Configuration.OnRoomLeft;
                this.FireEvent(leaveMessage);
                leaveMessage.Event = Configuration.OnRoomLeave;
            }

            _rooms.Clear();
        }

        public void ReplyRoomJoin(Message message)
        {
            if (string.IsNullOrEmpty(message.Wait) || message.IsNoOp)
            {
                return;
            }

            if (!_rooms.TryGetValue(message.Room, out var room))
            {
                var error = this.FireEvent(message);
                if (!string.IsNullOrEmpty(error))
                {
                    message.Error = error;
                    _connection.WriteBinary(message);
                    return;
                }

                _rooms.Add(message.Room, new Room(this, message.Room));
                message.Event = Configuration.OnRoomJoined;
                this.FireEvent(message);
            }

            _connection.WriteEmptyReply(message.Wait);
        }

        public void ReplyRoomLeave(Message message)
        {
            if (string.IsNullOrEmpty(message.Wait) || message.IsNoOp)
            {
                return;
            }

            if (!_rooms.TryGetValue(message.Room, out var room))
            {
                _connection.WriteEmptyReply(message.Wait);
                return;
            }

            this.FireEvent(message);
            _rooms.Remove(message.Room);
            _connection.WriteEmptyReply(message.Wait);

            message.Event = Configuration.OnRoomLeft;
            this.FireEvent(message);
        }

        public UniTask<string> Disconnect()
        {
            var disconnectMessage = new Message();
            disconnectMessage.Namespace = _namespace;
            disconnectMessage.Event = Configuration.OnNamespaceDisconnect;
            return _connection.AskDisconnect(disconnectMessage);
        }
    }

    public class EventMap : Dictionary<string, Func<NSConnection, Message, string>>
    {
    }

    public class NamespaceMap : Dictionary<string, EventMap>
    {
    }
}