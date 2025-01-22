using Ahsoka.ServiceFramework;
using Ahsoka.Services.Can;
using System;
using static Ahsoka.Services.Can.SocketMessageEncoding;

namespace Ahsoka.Socket
{
    internal class CanServiceSocket : GatewaySocket
    {
        public bool IsConnected { get; private set; }
        public uint Port { get; private set; }
        public uint Id { get; private set; }
        public uint Mask { get; private set; }

        internal CanServiceSocket(uint port, uint id, uint mask = 0x1FFFFFFF) : base()
        {
            Port = port;
            Id = id;
            Mask = mask;
        }

        public void SendToGateway(CanMessageData message)
        {
            MessageFromCAN(message, out AhsokaServiceMessage clientMessage, out bool isNotification);
            RouteMessageToEndPoint(this, clientMessage);
        }

        public bool FilterMessage(CanMessageData message)
        {
            return (message.Id & Mask) == (Id & Mask);
        }
    }
}
