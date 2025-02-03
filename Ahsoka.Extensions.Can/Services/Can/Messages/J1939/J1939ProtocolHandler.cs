using Ahsoka.Services.Can.Platform;
using SocketCANSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Ahsoka.Services.Can.CanServiceImplementation;

namespace Ahsoka.Services.Can.Messages;
internal class J1939ProtocolHandler : BaseProtocolHandler
{
    const int PDU2Threshold = 240;


    readonly List<object> startupQueue = new();
    internal bool transmittingJ1939 = false;

    internal CanState CanState { get; init; } = new();

    internal J1939ProtocolHandler(CanHandler messageHandler, CanServiceImplementation service)
        : base(messageHandler, service)
    {
        if (!Enabled)
            return;

        lock (CanState)
        {
            CanState.CanPort = Service.Port;
            foreach (var node in Service.PortConfig.MessageConfiguration.Nodes)
            {
                if (node.TransportProtocol == TransportProtocol.J1939 && node.J1939Info.AddressType == NodeAddressType.Static)
                    CanState.NodeAddresses[node.Id] = (uint)node.J1939Info.AddressValueOne;
            }

            J1939PropertyDefinitions.ParseAddresses(Service.Self.J1939Info.Addresses, out var minAddress, out var maxAddress);
            if (Service.Self.J1939Info.UseAddressClaim)
                CanState.CurrentAddress = J1939PropertyDefinitions.BroadcastAddress;
            else
                CanState.CurrentAddress = minAddress;

            PropagateCanState();
        }

        var list = ConstructList();
        foreach (var message in list)
        {
            var instance = Activator.CreateInstance(message, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { MessageHandler, this, Service }, null, null) as BaseMessageHandler;
            if (instance.Enabled)
                Messages.Add(instance);
        }
    }

    protected override bool IsEnabled()
    {
        if (Service.Self == null)
            return false;

        return Service.PortConfig.MessageConfiguration.Messages.Any(x => x.MessageType == MessageType.J1939ExtendedFrame);
    }

    internal static void Generate(CanPortConfiguration config)
    {
        var list = ConstructList();

        foreach (var message in list)
        {
            var method = message.GetMethod("Generate", BindingFlags.Static | BindingFlags.NonPublic);
            method?.Invoke(null, new object[] { config });
        }
    }

    protected static IEnumerable<Type> ConstructList()
    {
        return new List<Type>
        {
            typeof(ACMessageHandler),
            typeof(RQSTMessageHandler),
            typeof(TPMessageHandler)
        };
    }

    internal override bool ConfirmAvailable(CanMessageData messageData, object info, out CanMessageResult result)
    {
        if (!base.ConfirmAvailable(messageData, info, out result))
            return false;

        if (!transmittingJ1939)
            if (CanState.CurrentAddress == J1939PropertyDefinitions.BroadcastAddress)
            {
                startupQueue.Add(info);
                result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"J1939 node waiting for address claim, message has been queued" };
            }
            else
                result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"J1939 node either failed to claim address or was superseded by a higher priority ECU" };

        return true;
    }

    internal override bool ProcessMessage(CanMessageData message, out bool shouldSend)
    {
        if (!base.ProcessMessage(message, out shouldSend))
            return false;

        if (GetAvailableMessage(message.Id, out AvailableMessage messageInfo))
        {
            var j1939id = new J1939PropertyDefinitions.Id(message.Id);

            if (messageInfo.Message.OverrideSourceAddress)
                message.Id |= (CanState.CurrentAddress & 0xFF);
            if (messageInfo.Message.OverrideDestinationAddress && j1939id.PDUF < PDU2Threshold)
                message.Id |= (CanState.NodeAddresses[messageInfo.Message.ReceiveNodes[Service.Port]] & 0xFF) << 8;

            if (Service.PortConfig.MessageConfiguration.Ports.First(x => x.Port == Service.Port).CanInterface == CanInterface.SocketCan)
                message.Id |= (uint)CanIdFlags.CAN_EFF_FLAG;

            if (message.Dlc > 8 && !(this.Service is DesktopServiceImplementation))
            {
                shouldSend = false;
                MessageHandler.SendPredefined(new SendInformation()
                {
                    messageData = message
                });
            }
        }
        return true;
    }

    internal override bool GetAvailableMessage(uint id, out AvailableMessage result, bool received = false)
    {
        try
        {
            result = new();
            var j1939Id = new J1939PropertyDefinitions.Id(id);

            var mask = j1939Id.PDUF >= PDU2Threshold ? 0x3FFFF00 : 0x3FF0000;
            var messages = Service.AvailableMessages.Values.Where(x => x.Message.MessageType == MessageType.J1939ExtendedFrame && (x.Message.Id & mask) == (id & mask));
            foreach (var message in messages)
            {
                var available = true;
                if (message.Message.OverrideDestinationAddress)
                {
                    bool knownDestination = message.Message.ReceiveNodes[Service.Port] != -1;

                    if ((j1939Id.PDUF < PDU2Threshold) && !((received && (j1939Id.PDUS == CanState.CurrentAddress || message.Message.ReceiveNodes[Service.Port] == J1939PropertyDefinitions.BroadcastAddress))
                            || (!received && knownDestination)))
                        available &= false;
                }

                if (message.Message.OverrideSourceAddress)
                {
                    if (!((received && (j1939Id.SourceAddress == CanState.NodeAddresses[message.Message.TransmitNodes[Service.Port]] || message.Message.TransmitNodes[Service.Port] == J1939PropertyDefinitions.BroadcastAddress))
                            || !received))
                        available &= false;
                }

                if (available)
                {
                    result = message;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    internal void ReleaseStartupQueue()
    {
        foreach (var message in startupQueue)
            if (message.GetType() == typeof(CanMessageDataCollection))
                Service.HandleSendCanRequest((CanMessageDataCollection)message);
            else
                Service.HandleSendRecurringRequest((RecurringCanMessage)message);
    }

    internal virtual void CancelRecurringJ1939()
    {
        Service.CancelProtocolTransmissions(MessageType.J1939ExtendedFrame);
    }

    internal virtual void PropagateCanState()
    {
        Service.Service.NotifyStateUpdate(CanState);
        Service.UpdateCanState(CanState);
    }
}
