using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ahsoka.Services.Can.Messages;
internal class CanHandler
{
    readonly List<BaseProtocolHandler> protocols = new();

    internal CanHandler(CanServiceImplementation canServiceImplementation)
    {
        var list = ConstructList();
        foreach (var message in list)
        {
            var instance = Activator.CreateInstance(message, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { this, canServiceImplementation }, null, null) as BaseProtocolHandler;
            if (instance.Enabled)
                protocols.Add(instance);
        }
    }

    private static IEnumerable<Type> ConstructList()
    {
        return new List<Type>
        {
            // Raw must get processed first
            typeof(RawProtocolHandler),
            typeof(J1939ProtocolHandler)           
        };
    }

    internal static void Generate(CanPortConfiguration config)
    {
        var list = ConstructList();

        foreach (var protocol in list)
        {
            var method = protocol.GetMethod("Generate", BindingFlags.Static | BindingFlags.NonPublic);
            method?.Invoke(null, new object[] { config });
        }
    }

    internal bool ProcessMessage(CanMessageData messageData)
    {
        foreach (var protocol in protocols)
        {
            if (protocol.ProcessMessage(messageData, out bool shouldSend))
                return shouldSend;            
        }

        return false;
    }

    internal CanMessageResult SendPredefined(SendInformation sendInfo)
    {
        foreach (var protocol in protocols)
        {
            if (protocol.SendPredefined(sendInfo, out var result))
                return result;
        }
        return new CanMessageResult() { Status = MessageStatus.Error, Message = $"Message Failed to Send" };
    }

    internal bool Receive(CanMessageData messageData)
    {
        foreach (var protocol in protocols)
        {
            if (protocol.OnReceive(messageData, out var shouldSend))
                return shouldSend;
        }
        return false;
    }

    internal bool MatchId(uint id, out uint configId, bool received = false)
    {
        configId = 0;
        foreach (var protocol in protocols)
        {
            if (protocol.MatchId(id, out configId, received))
                return true;
        }
        return false;
    }

    internal CanMessageResult ConfirmAvailable(object data)
    {
        List<CanMessageData> messages;
        if (data.GetType() == typeof(CanMessageDataCollection))
            messages = ((CanMessageDataCollection)data).Messages;
        else
            messages = new List<CanMessageData>() { ((RecurringCanMessage)data).Message };

        var returnMessage = new CanMessageResult() { Status = MessageStatus.Success, Message = "" };
        foreach (var message in messages)
        {
            foreach (var protocol in protocols)
            {
                if (protocol.ConfirmAvailable(message, data, out CanMessageResult result))
                {
                    if (result.Status == MessageStatus.Error)
                        returnMessage.Status = MessageStatus.Error;
                    returnMessage.Message += $"{result.Message}/n";
                    break;
                }
            }
        }
            

        return returnMessage;
    }
}

internal class SendInformation
{
    internal string name;
    internal CanMessageData messageData;
    internal uint destinationAddress, sourceAddress;

    internal SendInformation() { }
}
