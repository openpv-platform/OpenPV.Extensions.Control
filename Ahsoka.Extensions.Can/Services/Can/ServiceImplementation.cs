using Ahsoka.ServiceFramework;
using Ahsoka.Services.Can.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ahsoka.Services.Can;

internal abstract class CanServiceImplementation
{
    CanDataServicHandler dataHandler = null;
    CanHandler canHandler = null;
    readonly Dictionary<uint, MessageTransmitter> recurringMessageList = new();
    readonly ManualResetEventSlim resetEvent = new(false);
    bool promiscuousReceive = false;
    bool promiscuousTransmit = false;

    internal uint Port { get; private set; }
    internal CanService Service { get; private set; }
    internal CanPortConfiguration PortConfig { get; private set; }
    internal ClientCanFilter ClientCanFilter { get; private set; }
    internal Dictionary<uint, byte[]> DataFilters { get; private set; } = new();
    internal Dictionary<uint, AvailableMessage> AvailableMessages { get; private set; } = new();
    internal NodeDefinition Self { get; private set; }

    internal void Open(CanService service, CanPortConfiguration portConfig, uint port)
    {
        this.Service = service;
        this.PortConfig = portConfig;
        this.Port = port;
     
        // Create Data Service Handler
        dataHandler = new CanDataServicHandler(service);

        this.promiscuousReceive = PortConfig.MessageConfiguration.Ports.First(x => x.Port == Port).PromiscuousReceive;
        if (this.promiscuousReceive) AhsokaLogging.LogMessage(AhsokaVerbosity.High, "CAN: Promiscuous Receive Enabled");
        this.promiscuousTransmit = PortConfig.MessageConfiguration.Ports.First(x => x.Port == Port).PromiscuousTransmit;
        if (this.promiscuousTransmit) AhsokaLogging.LogMessage(AhsokaVerbosity.High, "CAN: Promiscuous Transmit Enabled");

        // Don't filter nodes in or out when debugging enabled
        Self = PortConfig.MessageConfiguration.Nodes.FirstOrDefault(x => x.NodeType == NodeType.Self);

        lock (AvailableMessages)
            foreach (var item in PortConfig.MessageConfiguration.Messages)
            {
                // Setup Filters
                if (item.FilterReceipts)
                    DataFilters[item.Id] = new byte[item.Dlc + item.Dlc % 8];

                AvailableMessages[item.Id] = new AvailableMessage()
                {
                    Message = item,
                    RollCount = 0,
                };

                dataHandler.AddMessage(item.Id, item);
            }

        OnOpen();
        canHandler = new CanHandler(this);
    }

    internal void Close()
    {
        OnClose();
    }

    internal CanMessageResult HandleSendCanRequest(CanMessageDataCollection canMessageDataCollection)
    {
        if (!promiscuousTransmit)
        {
            var available = canHandler.ConfirmAvailable(canMessageDataCollection);
            if (available.Status == MessageStatus.Error)
                return available;
        }

        SendCanMessages(canMessageDataCollection);

        foreach (var message in canMessageDataCollection.Messages)
            if (canHandler.MatchId(message.Id, out var configId, true))
            {
                message.Id = configId;
                dataHandler.HandleMesssage(message);
            }

        return new CanMessageResult() { Status = MessageStatus.Success };
    }

    internal void SendCanMessages(CanMessageDataCollection canMessageDataCollection)
    {
        OnSendCanMessages(canMessageDataCollection);
    }

    internal CanMessageResult HandleSendRecurringRequest(RecurringCanMessage recurringCanMessage)
    {
        if (!promiscuousTransmit)
        {
            var available = canHandler.ConfirmAvailable(recurringCanMessage);
            if (available.Status == MessageStatus.Error)
                return available;
        }

        OnSendRecurringMessage(recurringCanMessage);

        if (canHandler.MatchId(recurringCanMessage.Message.Id, out var configId, true))
        {
            recurringCanMessage.Message.Id = configId;
            dataHandler.HandleMesssage(recurringCanMessage.Message);
        }


        return new CanMessageResult() { Status = MessageStatus.Success };
    }

    internal uint ProcessMessage(CanMessageData messageData)
    {
        if (!promiscuousTransmit)
            return canHandler.ProcessMessage(messageData);
        return messageData.Id;
    }

    internal void FilterIncomingMessage(CanMessageData messageData, out bool shouldSend)
    {
        shouldSend = true;

        if (promiscuousReceive)
            return;

        if (canHandler == null)
        {
            shouldSend = false;
            return;
        }

        var extraBytes = messageData.Data.Length % 8;
        if (extraBytes != 0)
        {
            var oldData = messageData.Data;
            Array.Copy(oldData, messageData.Data = new byte[oldData.Length + (8 - extraBytes)], oldData.Length);
        }

        shouldSend = canHandler.Receive(messageData);

        if (shouldSend && canHandler.MatchId(messageData.Id, out var configId, true))
        {
            var data = new CanMessageData()
            {
                Id = configId,
                Dlc = messageData.Dlc,
                Data = messageData.Data,
            };
            dataHandler.HandleMesssage(data);
        }
    }

    internal void SetClientMessageFilter(ClientCanFilter clientCanFilter)
    {
        if (clientCanFilter.CanIdLists.Length > 0)
            this.ClientCanFilter = clientCanFilter;
        else
            this.ClientCanFilter = null;

    }

    protected Task ProcessRecurringMessages(CancellationTokenSource source)
    {
        return Task.Run(() =>
        {
            try
            {
                while (!source.Token.IsCancellationRequested)
                {
                    var delay = int.MaxValue;

                    lock (recurringMessageList)
                    {
                        var datetime = DateTime.Now;
                        foreach (var item in recurringMessageList.ToArray())
                        {
                            if (datetime > item.Value.Expiration)
                            {
                                // Message Expired...Remove From Collection.
                                recurringMessageList.Remove(item.Key);
                            }
                            else if (datetime >= item.Value.NextTransmit)
                            {
                                item.Value.NextTransmit = datetime.AddMilliseconds(item.Value.Message.TransmitIntervalInMs);
                                
                                // Set Next Send Intervals.
                                var messageCollection = new CanMessageDataCollection
                                {
                                    CanPort = Port
                                };
                                messageCollection.Messages.Add(item.Value.Message.Message);
                                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending recursive {item.Value.Message.Message.Id}");
                                OnSendCanMessages(messageCollection);
                            }

                            // Calculate Next Transmit Interval
                            delay = Math.Min(delay, (int)(item.Value.NextTransmit - datetime).TotalMilliseconds);
                        }

                        // Reset Handle During Lock
                        resetEvent.Reset();
                    }

                    // Wait for time or Reset
                    if (delay > 0)
                        resetEvent.Wait(delay, source.Token);
                }
            }
            catch (OperationCanceledException) { }

        });
    }

    protected void AddRecurringMessage(RecurringCanMessage message)
    {
        lock (recurringMessageList)
        {
            // Minimum Message Time = 10
            if (message.TransmitIntervalInMs < 10)
                message.TransmitIntervalInMs = 10;

            // Update Transmit Record
            if (recurringMessageList.TryGetValue(message.Message.Id, out MessageTransmitter value))
            {
                // Update Transmit Time if New...otherwise keep same time
                if (value.Message.TransmitIntervalInMs != message.TransmitIntervalInMs)
                    value.NextTransmit = DateTime.Now.AddMilliseconds(message.TransmitIntervalInMs);

                // Update and Extend Message Expiration
                value.Message = message;
                value.Expiration = DateTime.Now.AddMilliseconds(message.TimeoutBeforeUpdateInMs);
            }
            else
            {
                // Add New Message
                recurringMessageList[message.Message.Id] = new MessageTransmitter()
                {
                    Message = message,
                    NextTransmit = DateTime.Now.AddMilliseconds(message.TransmitIntervalInMs),
                    Expiration = DateTime.Now.AddMilliseconds(message.TimeoutBeforeUpdateInMs)
                };
            }

            // Wake up Recurring Message Thread
            resetEvent.Set();
        }
    }

    internal virtual void CancelProtocolTransmissions(MessageType messageType)
    {
        lock (recurringMessageList)
            foreach (var message in recurringMessageList.Where(x => AvailableMessages[x.Value.Message.Message.Id].Message.MessageType == messageType).ToList())
                recurringMessageList.Remove(message.Key);
    }

    internal NodeDefinition GetNode(int nodeId)
    {
        return PortConfig.MessageConfiguration.Nodes.FirstOrDefault(x => x.Id == nodeId);
    }

    virtual internal void UpdateCanState(CanState canState) { }

    abstract protected void OnOpen();

    abstract protected void OnClose();

    abstract protected void OnSendCanMessages(CanMessageDataCollection canMessageDataCollection);

    abstract protected void OnSendRecurringMessage(RecurringCanMessage canMessageDataCollection);

    private class MessageTransmitter
    {
        internal RecurringCanMessage Message;
        internal DateTime NextTransmit;
        internal DateTime Expiration;
    }

    public class AvailableMessage
    {
        internal MessageDefinition Message;
        internal uint RollCount;
    }
}