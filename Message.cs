using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace NeffosCSharp
{
    public class Message
    {
        /// <summary>
        /// The namespace that this message sent to.
        /// </summary>
        public string Namespace;

        /// <summary>
        /// The event that this message sent to.
        /// </summary>
        public string Event;

        /// <summary>
        /// The room that this message sent to.
        /// </summary>
        public string Room;

        /// <summary>
        /// The actual body of the incoming data.
        /// </summary>
        public string Body;

        /// <summary>
        /// The Err contains any message's error if defined and not empty.
        /// server-side and client-side can return an error instead of a message from inside event callbacks.
        /// </summary>
        public string Error;

        public bool IsError;
        public bool IsNoOp;
        public bool IsInvalid;

        /// <summary>
        /// The IsForced if true then it means that this is not an incoming action but a force action.
        /// For example when websocket connection lost from remote the OnNamespaceDisconnect `Message.IsForced` will be true
        /// </summary>
        public bool IsForced;

        /// <summary>
        /// The IsLocal reprots whether an event is sent by the client-side itself, i.e when `connect` call on `OnNamespaceConnect` event the `Message.IsLocal` will be true,
        /// server-side can force-connect a client to connect to a namespace as well in this case the `IsLocal` will be false.
        /// </summary>
        public bool IsLocal;

        /// <summary>
        /// The IsNative reports whether the Message is websocket native messages, only Body is filled.
        /// </summary>
        public bool IsNative;

        /// <summary>
        /// The SetBinary can be filled to true if the client must send this message using the Binary format message.
        /// </summary>
        public bool SetBinary;

        public string Wait;

        /// <summary>
        /// unmarshal method returns this Message's `Body` as an object
        /// </summary>
        /// <returns></returns>
        public object Unmarshal()
        {
            if (string.IsNullOrEmpty(Body))
                return null;
            return JsonConvert.DeserializeObject(Body);
        }

        public bool IsConnect()
        {
            return Event.Equals(Configuration.OnNamespaceConnect);
        }

        public bool IsDisconnect()
        {
            return Event.Equals(Configuration.OnNamespaceDisconnect);
        }

        public bool IsRoomJoin()
        {
            return Event.Equals(Configuration.OnRoomJoin);
        }

        public bool IsRoomLeft()
        {
            return Event.Equals(Configuration.OnRoomLeft);
        }

        public bool IsWait()
        {
            if (string.IsNullOrEmpty(Wait))
            {
                return false;
            }

            if (Wait[0] == Configuration.waitIsConfirmationPrefix)
            {
                return true;
            }

            return Wait[0].Equals(Configuration.waitComesFromClientPrefix);
        }

        public byte[] SerializeBinary()
        {
            if (IsNative && string.IsNullOrEmpty(Wait))
            {
                throw new Exception("Use `Message.SerializeNative` to serialize native messages.");
            }

            var isErrorString = Configuration.falseString;
            var isNoOpString = Configuration.falseString;

            if (!string.IsNullOrEmpty(Error))
            {
                isErrorString = Configuration.trueString;
            }

            if (IsNoOp)
            {
                isNoOpString = Configuration.trueString;
            }

            //join message with message separator
            var data = string.Join(Configuration.messageSeparator,
                Wait,
                StringUtils.EscapeMessageField(Namespace),
                StringUtils.EscapeMessageField(Room),
                StringUtils.EscapeMessageField(Event),
                isErrorString,
                isNoOpString,
                string.IsNullOrEmpty(Body) ? string.Empty : Body
            );
            //data to byte array
            var bytes = Encoding.UTF8.GetBytes(data);
            //if binary is set then add binary prefix
            return bytes;
        }

        public string SerializeNative()
        {
            if (!IsNative)
            {
                throw new Exception("Use `Message.SerializeBinary` to serialize binary messages.");
            }

            var isErrorString = Configuration.falseString;
            var isNoOpString = Configuration.falseString;

            if (!string.IsNullOrEmpty(Error))
            {
                isErrorString = Configuration.trueString;
            }

            if (IsNoOp)
            {
                isNoOpString = Configuration.trueString;
            }

            //join message with message separator
            var data = string.Join(Configuration.messageSeparator,
                Wait,
                StringUtils.EscapeMessageField(Namespace),
                StringUtils.EscapeMessageField(Room),
                StringUtils.EscapeMessageField(Event),
                isErrorString,
                isNoOpString,
                string.IsNullOrEmpty(Body) ? string.Empty : Body
            );
            return data;
        }
        public static Message Deserialize(string messageString, bool allowNativeMessage)
        {
            var message = new Message();
            if (string.IsNullOrEmpty(messageString))
            {
                message.IsInvalid = true;
                return message;
            }

            var messageParts = messageString.Split(Configuration.messageSeparator);
            if (messageParts.Length != Configuration.validMessageSepCount)
            {
                if (!allowNativeMessage)
                {
                    message.IsInvalid = true;
                    return message;
                }
                else
                {
                    message.Event = Configuration.OnNativeMessage;
                    message.Body = messageString;
                }
            }

            message.Wait = messageParts[0];
            message.Namespace = StringUtils.UnescapeMessageField(messageParts[1]);
            message.Room = StringUtils.UnescapeMessageField(messageParts[2]);
            message.Event = StringUtils.UnescapeMessageField(messageParts[3]);
            message.IsError = messageParts[4].Equals(Configuration.trueString);
            message.IsNoOp = messageParts[5].Equals(Configuration.trueString);

            var body = messageParts[6];
            if (!string.IsNullOrEmpty(body))
            {
                if (message.IsError)
                    message.Error = body;
                else
                    message.Body = body;
            }
            else
            {
                message.Body = string.Empty;
            }

            message.IsInvalid = false;
            message.IsForced = false;
            message.IsLocal = false;
            message.IsNative = allowNativeMessage && message.Event.Equals(Configuration.OnNativeMessage);

            return message;
        }
    }
}