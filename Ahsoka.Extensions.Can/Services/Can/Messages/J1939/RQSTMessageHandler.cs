using Ahsoka.Utility;
using System;
using System.Linq;

namespace Ahsoka.Services.Can.Messages;
internal class RQSTMessageHandler : J1939MessageHandlerBase
{
    new readonly J1939ProtocolHandler Protocol = null;

    protected RQSTMessageHandler(CanHandler messageHandler, J1939ProtocolHandler protocolHandler, CanServiceImplementation service)
        : base(messageHandler, protocolHandler, service, 0xEA, 0, 6)
    {
        Protocol = protocolHandler;
    }

    protected override bool IsEnabled()
    {
        if (Service.Self == null || Service.PromiscuousTransmit)
            return false;

        return Service.Self.TransportProtocol == TransportProtocol.J1939 && Service.Self.J1939Info.UseAddressClaim;
    }

    private static void Generate(CanPortConfiguration config)
    {
        NodeDefinition self = config.MessageConfiguration.Nodes.FirstOrDefault(x => x.NodeType == NodeType.Self);
        if (self != null && self.TransportProtocol == TransportProtocol.J1939 && self.J1939Info.UseAddressClaim)
        {
            var standardDefinitions = CanSystemInfo.StandardCanMessages;
            var standardMessage = standardDefinitions.Messages.First(x => x.Name == "RQST");
            config.MessageConfiguration.Messages.Add(standardMessage);
        }
    }

    internal override void OnInit()
    {
        return;
    }

    internal override bool OnReceive(CanMessageData messageData)
    {
        if (!Enabled)
            return false;

        var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);

        lock (Protocol.CanState)
        {
            if (j1939Id.PDUF == PDUF && BitConverter.ToUInt32(new byte[] { messageData.Data[0], messageData.Data[1], messageData.Data[2], 0 }) == 0x00EE00)
            {
                if (j1939Id.PDUS != Protocol.CanState.CurrentAddress && j1939Id.PDUS != J1939PropertyDefinitions.BroadcastAddress)
                    return true;

                var sendInfo = new SendInformation() { name = "AC", destinationAddress = J1939PropertyDefinitions.BroadcastAddress };
                if (Protocol.CanState.CurrentAddress == J1939PropertyDefinitions.NullAddress)
                    sendInfo.sourceAddress = J1939PropertyDefinitions.NullAddress;
                else
                    sendInfo.sourceAddress = Protocol.CanState.CurrentAddress;

                MessageHandler.SendPredefined(sendInfo);
                return true;
            }
        }
        return false;
    }

    internal override bool OnSend(SendInformation sendInfo, out CanMessageResult result)
    {
        result = null;
        return false;
    }
}
