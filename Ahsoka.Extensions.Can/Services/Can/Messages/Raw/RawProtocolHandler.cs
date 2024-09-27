using Ahsoka.ServiceFramework;
using SocketCANSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ahsoka.Services.Can.CanServiceImplementation;

namespace Ahsoka.Services.Can.Messages;
internal class RawProtocolHandler : BaseProtocolHandler
{

    internal RawProtocolHandler(CanHandler messageHandler, CanServiceImplementation service)
        : base(messageHandler, service)
    {

    }

    protected override bool IsEnabled()
    {
        if (Service.Self == null)
            return false;

        return Service.PortConfig.MessageConfiguration.Messages.Any(x => x.MessageType == MessageType.RawStandardFrame || x.MessageType == MessageType.RawExtendedFrame);
    }

    internal override bool GetAvailableMessage(uint id, out AvailableMessage message, bool received = false)
    {
        var found = Service.AvailableMessages.TryGetValue(id, out message);

        if (!found)
            found = Service.AvailableMessages.TryGetValue(id | 0x80000000, out message);

        if (message?.Message.MessageType > MessageType.RawExtendedFrame)
            return false;

        return found;
    }

    internal override bool ProcessMessage(CanMessageData message, out uint modifiedId)
    {
        if (!base.ProcessMessage(message, out modifiedId))
            return false;
        if (GetAvailableMessage(message.Id, out AvailableMessage messageInfo))
        {
            if (messageInfo.Message.MessageType == MessageType.RawExtendedFrame && 
                    Service.PortConfig.MessageConfiguration.Ports.First(x => x.Port == Service.Port).CanInterface == CanInterface.SocketCan)
                modifiedId |= (uint)CanIdFlags.CAN_EFF_FLAG;           
        }
        return true;
    }
}
