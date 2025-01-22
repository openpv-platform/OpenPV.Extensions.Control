using Ahsoka.ServiceFramework;
using System;
using System.IO;

namespace Ahsoka.Services.Can
{
    /// <summary>
    /// Handles Encoding for Service Can Messages
    /// </summary>
    public class SocketMessageEncoding
    {
        static readonly int headerLengthSize = sizeof(short);

        public static CanMessageData MessageToCAN(AhsokaServiceMessage serviceMessage, uint id, bool isNotification = false)
        {
            if (serviceMessage.ClientId != null)
                serviceMessage.Header.ClientId = serviceMessage.ClientId;

            using MemoryStream headerStream = new();
            ProtoBuf.Serializer.Serialize(headerStream, serviceMessage.Header);

            return MessageToCAN(headerStream.ToArray(), serviceMessage.MessageData, id, isNotification);
        }

        public static CanMessageData MessageToCAN(AhsokaClientMessage clientMessage, uint id, bool isNotification = false)
        {
            using MemoryStream headerStream = new();
            ProtoBuf.Serializer.Serialize(headerStream, clientMessage.Header);

            return MessageToCAN(headerStream.ToArray(), clientMessage.MessageData, id, isNotification);
        }

        static CanMessageData MessageToCAN(byte[] header, byte[] data, uint id, bool isNotification)
        {
            var message = new CanMessageData
            {
                Id = id
            };

            var headerOffset = headerLengthSize * 2 + sizeof(bool);
            message.Dlc = (uint)(headerOffset + header.Length + data.Length);

            message.Data = new byte[message.Dlc];
            BitConverter.GetBytes((short)header.Length).CopyTo(message.Data, 0);
            BitConverter.GetBytes((short)data.Length).CopyTo(message.Data, headerLengthSize);
            BitConverter.GetBytes(isNotification).CopyTo(message.Data, headerOffset - sizeof(bool));
            header.CopyTo(message.Data, headerOffset);
            data.CopyTo(message.Data, headerOffset + header.Length);
            return message;
        }

        public static void MessageFromCAN(CanMessageData message, out AhsokaServiceMessage received, out bool isNotification)
        {
            received = new AhsokaServiceMessage();
            var decoded = MessageFromCAN(message);

            AhsokaMessageHeader messageHeader = null;

            using (MemoryStream header = new(decoded.header))
            {
                messageHeader = ProtoBuf.Serializer.Deserialize<AhsokaMessageHeader>(header);
            }

            received.Header = messageHeader;
            received.MessageData = decoded.data;
            received.ClientId = received.Header.ClientId;
            isNotification = decoded.isNotification;
        }

        public static void MessageFromCAN(CanMessageData message, out AhsokaClientMessage received, out bool isNotification)
        {
            received = new AhsokaClientMessage();
            var decoded = MessageFromCAN(message);

            AhsokaMessageHeader messageHeader = null;

            using (MemoryStream header = new(decoded.header))
            {
                messageHeader = ProtoBuf.Serializer.Deserialize<AhsokaMessageHeader>(header);
            }

            received.Header = messageHeader;
            received.MessageData = decoded.data;
            isNotification = decoded.isNotification;
        }

        static ReceivedMessage MessageFromCAN(CanMessageData message)
        {
            var outMessage = new ReceivedMessage();
            var headerOffset = headerLengthSize * 2 + sizeof(bool);

            var headerLength = BitConverter.ToInt16(message.Data);
            var dataLength = BitConverter.ToInt16(message.Data, headerLengthSize);
            outMessage.isNotification = BitConverter.ToBoolean(message.Data, headerOffset - sizeof(bool));
            outMessage.header = new byte[headerLength];
            outMessage.data = new byte[dataLength];
            Array.Copy(message.Data, headerOffset, outMessage.header, 0, headerLength);
            Array.Copy(message.Data, headerOffset + headerLength, outMessage.data, 0, outMessage.data.Length);
            return outMessage;
        }

        public class ReceivedMessage
        {
            internal byte[] header;
            internal byte[] data;
            internal bool isNotification;
        }
    }
}