using Ahsoka.ServiceFramework;
using SocketCANSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Ahsoka.Services.Can.CanServiceImplementation;

namespace Ahsoka.Services.Can.Messages;
internal class J1939ProtocolHandler : BaseProtocolHandler
{    
    readonly List<object> startupQueue = new();
    internal bool transmittingJ1939 = false;

    internal CanState CanState { get; init; } = new();

    internal J1939ProtocolHandler(CanHandler messageHandler, CanServiceImplementation service)
        :base(messageHandler, service)
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

            J1939Helper.ParseAddresses(Service.Self.J1939Info.Addresses, out var minAddress, out var maxAddress);
            if (Service.Self.J1939Info.UseAddressClaim)
                CanState.CurrentAddress = J1939Helper.BroadcastAddress;
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
            if (CanState.CurrentAddress == J1939Helper.BroadcastAddress)
            {
                startupQueue.Add(info);
                result = new CanMessageResult() { Status = MessageStatus.Success };
            }
            else
                result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"J1939 node either failed to claim address or was superseded by a higher priority ECU" };
        else if (messageData.Dlc > 8)
            result = MessageHandler.SendPredefined(new SendInformation() { messageData = messageData });

        return true;
    }

    internal override bool ProcessMessage(CanMessageData message, out uint modifiedId)
    {
        if (!base.ProcessMessage(message, out modifiedId))
            return false;

        if (GetAvailableMessage(message.Id, out AvailableMessage messageInfo))
        {
            var j1939id = new J1939Helper.Id(message.Id);

            modifiedId |= (CanState.CurrentAddress & 0xFF); 
            if (j1939id.PDUF < 240)
                modifiedId |= (CanState.NodeAddresses[messageInfo.Message.ReceiveNodes[Service.Port]] & 0xFF) << 8;
           
            if (Service.PortConfig.MessageConfiguration.Ports.First(x => x.Port == Service.Port).CanInterface == CanInterface.SocketCan)
                modifiedId |= (uint)CanIdFlags.CAN_EFF_FLAG;
        }
        return true;
    }

    internal override bool InAvailableMessages(uint id, bool received = false)
    {
        try
        {
            if (Service.AvailableMessages.ContainsKey(id))
                return true;

            var j1939Id = new J1939Helper.Id(id);

            var mask = j1939Id.PDUF >= 240 ? 0x1FFFFF00 : 0x1FFF0000;

            var messages = Service.AvailableMessages.Values.Where(x => x.Message.MessageType == MessageType.J1939ExtendedFrame && (x.Message.Id & mask) == (id & mask));
            foreach (var message in messages)
            {
                var knownDestination = Service.GetNode(message.Message.ReceiveNodes[Service.Port]).J1939Info.AddressType == NodeAddressType.Static;

                if (received && (j1939Id.PDUS == CanState.CurrentAddress || message.Message.ReceiveNodes[Service.Port] == J1939Helper.BroadcastAddress) &&
                    (j1939Id.SourceAddress == CanState.NodeAddresses[message.Message.TransmitNodes[Service.Port]] || message.Message.TransmitNodes[Service.Port] == J1939Helper.BroadcastAddress))
                    return true;
                else if (!received && (j1939Id.PDUS == CanState.NodeAddresses[message.Message.ReceiveNodes[Service.Port]] || !knownDestination) &&
                    (message.Message.TransmitNodes[Service.Port] == Service.Self.Id || message.Message.TransmitNodes[Service.Port] == J1939Helper.BroadcastAddress))
                    return true;
                else if (j1939Id.PDUF >= 240 && (!received || (j1939Id.SourceAddress == CanState.NodeAddresses[message.Message.TransmitNodes[Service.Port]] || message.Message.TransmitNodes[Service.Port] == J1939Helper.BroadcastAddress)))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
        
    }

    internal override bool GetAvailableMessage(uint id, out AvailableMessage result, bool received = false)
    {
        try
        {
            result = new();

            var j1939Id = new J1939Helper.Id(id);

            var mask = j1939Id.PDUF >= 240 ? 0x1FFFFF00 : 0x1FFF0000;

            var messages = Service.AvailableMessages.Values.Where(x => x.Message.MessageType == MessageType.J1939ExtendedFrame && (x.Message.Id & mask) == (id & mask));
            foreach (var message in messages)
            {
                bool knownDestination;
                if (message.Message.ReceiveNodes[Service.Port] == -1)
                    knownDestination = false;
                else
                    knownDestination = Service.GetNode(message.Message.ReceiveNodes[Service.Port]).J1939Info.AddressType == NodeAddressType.Static;

                if (received && (j1939Id.PDUS == CanState.CurrentAddress || message.Message.ReceiveNodes[Service.Port] == J1939Helper.BroadcastAddress) &&
                    (j1939Id.SourceAddress == CanState.NodeAddresses[message.Message.TransmitNodes[Service.Port]] || message.Message.TransmitNodes[Service.Port] == J1939Helper.BroadcastAddress))
                    result = message;
                else if (!received && (j1939Id.PDUS == CanState.NodeAddresses[message.Message.ReceiveNodes[Service.Port]] || !knownDestination) &&
                    (message.Message.TransmitNodes[Service.Port] == Service.Self.Id || message.Message.TransmitNodes[Service.Port] == J1939Helper.BroadcastAddress))
                    result = message;
                else if (j1939Id.PDUF >= 240 && (!received || (j1939Id.SourceAddress == CanState.NodeAddresses[message.Message.TransmitNodes[Service.Port]] || message.Message.TransmitNodes[Service.Port] == J1939Helper.BroadcastAddress)))
                    result = message;

                if (result.Message != null)
                    return true;
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
