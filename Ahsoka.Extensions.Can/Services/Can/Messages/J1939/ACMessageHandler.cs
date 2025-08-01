﻿using Ahsoka.Core;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Messages;
internal class ACMessageHandler : J1939MessageHandlerBase
{
    readonly ManualResetEventSlim ACEvent = new(false);
    J1939ProtocolHandler protocol = null;

    protected ACMessageHandler(CanHandler messageHandler, J1939ProtocolHandler protocolHandler, CanServiceImplementation service)
        : base(messageHandler, protocolHandler, service, 0xEE, 0, 6)
    {
        //Protocol = protocolHandler;
    }

    protected override bool IsEnabled()
    {
        if (Service.Self == null || Service.PromiscuousTransmit)
            return false;

        return Service.Self.J1939Info.UseAddressClaim;
    }

    private static void Generate(CanPortConfiguration config)
    {
        NodeDefinition self = config.MessageConfiguration.Nodes.FirstOrDefault(x => x.NodeType == NodeType.Self);
        if (self != null && self.TransportProtocol == TransportProtocol.J1939 && self.J1939Info.UseAddressClaim)
        {
            var standardDefinitions = CanSystemInfo.StandardCanMessages;
            var standardMessage = standardDefinitions.Messages.First(x => x.Name == "AC");
            config.MessageConfiguration.Messages.Add(standardMessage);
        }
    }

    internal override void OnInit()
    {
        protocol = (J1939ProtocolHandler)Protocol;
        protocol.transmittingJ1939 = false;

        Task.Run(() =>
        { 
            InitializeAddressClaim();
        }).ContinueWith(result =>
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"J1939 Address Claim Initialization Finished");
            if (protocol.transmittingJ1939)
                protocol.ReleaseStartupQueue();
        });
    }

    internal override bool OnReceive(CanMessageData messageData)
    {
        if (!Enabled)
            return false;

        var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);
        var messageCollection = new CanMessageDataCollection
        {
            CanPort = Service.Port
        };

        lock (protocol.CanState)
        {
            if (j1939Id.PDUF == PDUF)
            {
                if (j1939Id.PDUS == protocol.CanState.CurrentAddress)
                    return true;

                if (j1939Id.SourceAddress == protocol.CanState.CurrentAddress)
                {
                    var response = new CanMessageData
                    {
                        Dlc = 8,
                        Data = BitConverter.GetBytes(Service.Self.J1939Info.Name)
                    };

                    var acName = BitConverter.ToUInt64(messageData.Data);
                    if (acName > Service.Self.J1939Info.Name)
                    {
                        response.Id = CreateMessageId(protocol.CanState.CurrentAddress, J1939PropertyDefinitions.BroadcastAddress);
                        messageCollection.Messages.Add(response);
                    }
                    else
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"External J1939 Node has priority for address");
                        protocol.CanState.CurrentAddress = J1939PropertyDefinitions.NullAddress;
                        protocol.CanState.NodeAddresses[Service.Self.Id] = protocol.CanState.CurrentAddress;
                        response.Id = CreateMessageId(J1939PropertyDefinitions.NullAddress, J1939PropertyDefinitions.BroadcastAddress);
                        if (protocol.transmittingJ1939 == false)
                            ACEvent.Set();
                        else
                        {
                            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"J1939 transmission shutting down");
                            protocol.transmittingJ1939 = false;
                            messageCollection.Messages.Add(response);
                            Service.SendCanMessages(messageCollection);
                            lock (protocol.CanState)
                                protocol.CanState.CurrentAddress = J1939PropertyDefinitions.NullAddress;
                            protocol.CancelRecurringJ1939();
                            protocol.PropagateCanState();
                            return true;
                        }
                    }
                }
                else
                {
                    var claimedName = new J1939PropertyDefinitions.Name(BitConverter.ToUInt64(messageData.Data));

                    foreach (var node in Service.PortConfig.MessageConfiguration.Nodes.Where(x => x.TransportProtocol == TransportProtocol.J1939))
                        if ((node.J1939Info.AddressType == NodeAddressType.Static && node.J1939Info.AddressValueOne == j1939Id.SourceAddress) ||
                                (node.J1939Info.AddressType == NodeAddressType.SystemAddress && node.J1939Info.AddressValueOne == claimedName.Definition.VehicleSystem) ||
                                (node.J1939Info.AddressType == NodeAddressType.SystemFunctionAddress && node.J1939Info.AddressValueOne == claimedName.Definition.VehicleSystem && node.J1939Info.AddressValueTwo == claimedName.Definition.Function) ||
                                (node.J1939Info.AddressType == NodeAddressType.SystemInstanceAddress && node.J1939Info.AddressValueOne == claimedName.Definition.VehicleSystem && node.J1939Info.AddressValueTwo == claimedName.Definition.Function && node.J1939Info.AddressValueThree == claimedName.Definition.FunctionInstance))
                            lock (protocol.CanState)
                                protocol.CanState.NodeAddresses[node.Id] = j1939Id.SourceAddress;

                    protocol.PropagateCanState();
                }
            }

            if (messageCollection.Messages.Count > 0)
            {
                Service.SendCanMessages(messageCollection);
                return true;
            }
            return false;

        }
    }

    internal override bool OnSend(SendInformation sendInfo, out CanMessageResult result)
    {
        result = null;
        if (!Enabled || sendInfo.name != "AC")
            return false;

        var response = new CanMessageData();
        var messageCollection = new CanMessageDataCollection
        {
            CanPort = Service.Port
        };
        response.Id = CreateMessageId(sendInfo.sourceAddress, sendInfo.destinationAddress);
        response.Dlc = 8;
        response.Data = BitConverter.GetBytes(Service.Self.J1939Info.Name);
        messageCollection.Messages.Add(response);
        Service.SendCanMessages(messageCollection);
        return true;
    }

    private void InitializeAddressClaim()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Beginning J1939 Address Claim");

        uint identity;
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(SystemInfo.HardwareInfo.FactoryInfo.SerialNumber)))
        {
            var bytes = MD5.Create().ComputeHash(stream);
            identity = BitConverter.ToUInt32(bytes);
        }
        var name = new J1939PropertyDefinitions.Name(Service.Self.J1939Info);
        Service.Self.J1939Info.Name = name.WriteToUlong(identity);

        J1939PropertyDefinitions.ParseAddresses(Service.Self.J1939Info.Addresses, out var minAddress, out var maxAddress);

        bool addressClaimed = false;
        while (!addressClaimed)
        {
            var response = new CanMessageData();
            var messageCollection = new CanMessageDataCollection
            {
                CanPort = Service.Port
            };

            response.Dlc = 8;
            response.Data = BitConverter.GetBytes(Service.Self.J1939Info.Name);
            if (minAddress > maxAddress)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Unable to claim a J1939 address");
                response.Id = CreateMessageId(J1939PropertyDefinitions.NullAddress, J1939PropertyDefinitions.BroadcastAddress);
                messageCollection.Messages.Add(response);
                Service.SendCanMessages(messageCollection);
                lock (protocol.CanState)
                    protocol.CanState.CurrentAddress = J1939PropertyDefinitions.NullAddress;
                protocol.CancelRecurringJ1939();
                addressClaimed = true;
            }
            else
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Claiming J1939 address {minAddress}");
                response.Id = CreateMessageId(minAddress, J1939PropertyDefinitions.BroadcastAddress);
                messageCollection.Messages.Add(response);
                Service.SendCanMessages(messageCollection);
                ACEvent.Wait(250);

                lock (protocol.CanState)
                    if (protocol.CanState.CurrentAddress != J1939PropertyDefinitions.NullAddress || protocol.CanState.CurrentAddress != J1939PropertyDefinitions.BroadcastAddress)
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Claimed J1939 address");
                        protocol.CanState.CurrentAddress = minAddress;
                        protocol.transmittingJ1939 = true;
                        addressClaimed = true;
                    }
                    else
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"J1939 address could not be claimed");
                        minAddress++;
                    }
            }
        }

        protocol.CanState.NodeAddresses[Service.Self.Id] = protocol.CanState.CurrentAddress;
        protocol.PropagateCanState();
    }
}
