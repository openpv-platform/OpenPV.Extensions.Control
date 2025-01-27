using Ahsoka.ServiceFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using static Ahsoka.Services.Can.CanServiceImplementation;

namespace Ahsoka.Services.Can.Messages;
internal abstract class BaseProtocolHandler
{
    protected CanHandler MessageHandler { get; init; }
    protected CanServiceImplementation Service { get; init; }
    protected List<BaseMessageHandler> Messages { get; init; } = new();

    internal bool Enabled { get; init; }

    protected BaseProtocolHandler(CanHandler messageHandler, CanServiceImplementation service)
    {
        this.MessageHandler = messageHandler;
        this.Service = service;
        this.Enabled = IsEnabled();
    }

    internal void Init()
    {
        foreach (var message in Messages)
        {
            message.OnInit();
        }
    }

    internal virtual bool ConfirmAvailable(CanMessageData messageData, object info, out CanMessageResult result )
    {
        result = new CanMessageResult();

        if (!GetAvailableMessage(messageData.Id, out var messageInfo))
        {
            result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"CAN message not found in configuration with Id: {messageData.Id}" };
            return false;
        }
            
        if (!messageInfo.Message.TransmitNodes.Contains(Service.Self.Id) && !messageInfo.Message.TransmitNodes.Contains(255))
            result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"CAN message not set to transmit from node {Service.Self.Id} with message Id: {messageData.Id}" };

        return true;
    }

    internal virtual bool ProcessMessage(CanMessageData message, out bool shouldSend)
    {
        shouldSend = true;

        lock (Service.AvailableMessages)
        {
            // Only messages in the DB are processed
            if (GetAvailableMessage(message.Id, out AvailableMessage messageInfo))
            {
                if (messageInfo.Message.HasRollCount)
                {
                    var signal = messageInfo.Message.Signals.First(x => x.StartBit == messageInfo.Message.RollCountBit);
                    var propertyInfo = new CanPropertyInfo((int)messageInfo.Message.RollCountBit, (int)messageInfo.Message.RollCountLength, signal.ByteOrder, signal.ValueType, signal.Scale, signal.Offset);
                    int wordIndex = propertyInfo.StartBit / 64;
                    var val = new ulong[] { BitConverter.ToUInt64(message.Data, wordIndex) };
                    propertyInfo.SetValue(ref val, messageInfo.RollCount, false);
                    Array.Copy(BitConverter.GetBytes(val[0]), 0, message.Data, wordIndex, sizeof(ulong));
                    messageInfo.RollCount = propertyInfo.GetValue<uint>(val, false);
                    messageInfo.RollCount++;
                }

                if (messageInfo.Message.CrcType == CrcType.Tsc1)
                {
                    var idArray = BitConverter.GetBytes(message.Id);
                    var checksum = message.Data[0] + message.Data[1] + message.Data[2] + message.Data[3] + message.Data[4] + message.Data[5] + message.Data[6]
                        + (messageInfo.RollCount & 0x0Fu) + idArray[0] + idArray[1] + idArray[2] + idArray[3];
                    checksum = (((checksum >> 6) & 0x03) + (checksum >> 3) + checksum) & 0x07;

                    var signal = messageInfo.Message.Signals.First(x => x.StartBit == messageInfo.Message.CrcBit);
                    var checksumProperty = new CanPropertyInfo((int)messageInfo.Message.CrcBit, 4, signal.ByteOrder, signal.ValueType, signal.Scale, signal.Offset);
                    int wordIndex = checksumProperty.StartBit / 64;
                    var val = new ulong[] { BitConverter.ToUInt64(message.Data, wordIndex) };
                    checksumProperty.SetValue(ref val, (uint)checksum, false);
                    Array.Copy(BitConverter.GetBytes(val[0]), 0, message.Data, wordIndex, sizeof(ulong));
                }
                else if (messageInfo.Message.CrcType == CrcType.CheckSum) 
                {
                    var sum = 0;
                    foreach (var data in message.Data)
                        sum += data;

                    var signal = messageInfo.Message.Signals.First(x => x.StartBit == messageInfo.Message.CrcBit);
                    var checksumProperty = new CanPropertyInfo((int)messageInfo.Message.CrcBit, (int)signal.BitLength, signal.ByteOrder, signal.ValueType, signal.Scale, signal.Offset);
                    int wordIndex = checksumProperty.StartBit / 64;
                    var val = new ulong[] { BitConverter.ToUInt64(message.Data, wordIndex) };
                    checksumProperty.SetValue(ref val, (uint)sum, false);
                    Array.Copy(BitConverter.GetBytes(val[0]), 0, message.Data, wordIndex, sizeof(ulong));

                }
            }
            else
                return false;
        }
        return true;
    }

    internal bool SendPredefined(SendInformation sendInfo, out CanMessageResult result)
    {
        result = null;
        foreach (var message in Messages)
        {
            if (message.OnSend(sendInfo, out result))
                return true;
        }

        result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"Message Failed to Send" };
        return false;
    }

    internal virtual bool OnReceive(CanMessageData messageData, out bool shouldSend)
    {
        shouldSend = false;

        if (!GetAvailableMessage(messageData.Id, out var messageInfo, true))
            return false;

        if (!messageInfo.Message.UserDefined)
        {
            foreach (var message in Messages)
                if (message.OnReceive(messageData))
                    break;
            shouldSend = false;
            return true;
        }

        if (Service.ClientCanFilter != null)
            shouldSend = Service.ClientCanFilter.CanIdLists.Contains(messageData.Id);
        else
            shouldSend = true;

        if (Service.DataFilters.TryGetValue(messageData.Id, out byte[] value))
        {
            shouldSend = shouldSend && !messageData.Data.SequenceEqual(value); // Only Send if Data Changed.
            Service.DataFilters[messageData.Id] = messageData.Data; // Capture Latest Value.
        }

        return true;
    }

    internal bool MatchId(uint id, out uint configId, bool received = false)
    {
        configId = 0;
        if (GetAvailableMessage(id, out var message, received))
        {
            configId = message.Message.Id;
            return true;
        }
        return false;
    }

    internal virtual bool InAvailableMessages(uint id, bool received = false)
    {
        AvailableMessage message;
        return GetAvailableMessage(id, out message, received);
    }

    internal virtual bool GetAvailableMessage(uint id, out AvailableMessage message, bool received = false)
    {
        var found = Service.AvailableMessages.TryGetValue(id, out message);

        if (!found)
            found = Service.AvailableMessages.TryGetValue(id | 0x80000000, out message);
        return found;
    }

    protected abstract bool IsEnabled();  
}
