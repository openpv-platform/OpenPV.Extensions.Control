using Ahsoka.Core;
using System;
using System.Collections.Generic;

namespace Ahsoka.Services.Can;

internal class CanDataServicHandler
{
    Dictionary<uint, CanMessageInfo> messageHandlers = new();
    CanService service;

    internal CanDataServicHandler(CanService service)
    {
        this.service = service;
    }

    internal void AddMessage(uint id, MessageDefinition message)
    {
        if (!message.IncludeInDataService)
            return;

        var signalInfo = new CanMessageInfo();
        foreach (var sig in message.Signals)
        {
            var info = new CanPropertyInfo((int)sig.StartBit, (int)sig.BitLength, sig.ByteOrder, sig.ValueType, sig.Scale, sig.Offset, (int)sig.Id, sig.DefaultValue, sig.Minimum, sig.Maximum) { Name = sig.Name };

            if (sig.MuxRole == MuxRole.Multiplexor)
            {
                signalInfo.MultiPlexor = info;
            }
            else if (sig.MuxRole == MuxRole.Multiplexed)
            {
                if (!signalInfo.MuxProperties.ContainsKey(sig.MuxGroup))
                    signalInfo.MuxProperties[sig.MuxGroup] = new List<CanPropertyInfo>();

                signalInfo.MuxProperties[sig.MuxGroup].Add(info);
            }
            else
                signalInfo.StandardProperties.Add(info);
        }

        messageHandlers.Add(message.Id, signalInfo);
    }

    public void HandleMesssage(CanMessageData message)
    {
        if (messageHandlers.TryGetValue(message.Id, out CanMessageInfo propList))
        {
            if (propList.MultiPlexor != null)
            {
                // Decode MultiPlexor
                var value = propList.MultiPlexor.GetValue<uint>(message.Data);

                // Decode Matching Values
                if (propList.MuxProperties.TryGetValue(value, out var values))
                {
                    foreach (var prop in values)
                        SendValue(prop, message);
                }
            }

            // Decode All Other Props.
            foreach (var prop in propList.StandardProperties)
                SendValue(prop, message);

        }
    }

    private void SendValue(CanPropertyInfo item, CanMessageData message)
    {
        try
        {
            object value;
            switch (item.ValueType)
            {
                case ValueType.Signed:
                    value = item.GetValue<int>(message.Data);
                    break;
                case ValueType.Unsigned:
                    value = item.GetValue<uint>(message.Data);
                    break;
                case ValueType.Float:
                    value = item.GetValue<float>(message.Data);
                    break;
                case ValueType.Double:
                    value = item.GetValue<double>(message.Data);
                    break;
                case ValueType.Enum:
                    value = item.GetValue<float>(message.Data).ToString();
                    break;
                default:
                    value = null;
                    break;
            }

            if (value != null)
                service.UpdateCacheValue(item.Name, value);
        }
        catch
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"An error occured reading CAN Message Property {item.Name} with data '{BitConverter.ToString(message.Data)}'");
        }
    }

    class CanMessageInfo
    {
        public CanPropertyInfo MultiPlexor;
        public Dictionary<uint, List<CanPropertyInfo>> MuxProperties = new();
        public List<CanPropertyInfo> StandardProperties = new();
    }
}
