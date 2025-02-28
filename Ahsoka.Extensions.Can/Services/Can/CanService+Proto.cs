using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Can;

[ProtoContract]
public class CanMessageTypes
{
    public enum Ids
    {
        None = 0,

        OpenCommunicationChannel = 1,
        CloseCommunicationChannel = 2,
        NetworkStateChanged = 3,

        SendCanMessages = 4,
        SendRecurringCanMessage = 5,
        CanMessagesReceived = 6,

        ApplyMessageFilter = 7,

        CoprocessorIsReadyNotification = 8,
        CoprocessorHeartbeat = 9,
        CanServiceIsReadyNotification = 10,
    }
}

[ProtoContract]
public class CanApplicationConfiguration
{
    [ProtoMember(3)]
    public CanPortConfiguration CanPortConfiguration { get; set; }
}

[ProtoContract]
public class RecurringCanMessage
{
    [ProtoMember(1)]
    public uint CanPort { get; set; }

    [ProtoMember(2)]
    public int TransmitIntervalInMs { get; set; }

    [ProtoMember(3)]
    public int TimeoutBeforeUpdateInMs { get; set; }

    [ProtoMember(10)]
    public CanMessageData Message { get; set; }
}

[ProtoContract]
public class CanMessageDataCollection
{
    [ProtoMember(1)]
    public uint CanPort { get; set; }

    [ProtoMember(2, IsRequired = true)]
    public List<CanMessageData> Messages { get; set; } = new();
}

[ProtoContract]
public class CanMessageData
{
    [ProtoMember(1)]
    public uint Id { get; set; }

    [ProtoMember(3)]
    public uint Dlc { get; set; }

    [ProtoMember(10)]
    public byte[] Data { get; set; }
}

[ProtoContract]
public class CanState
{
    [ProtoMember(1)]
    public uint CanPort { get; set; }

    [ProtoMember(2)]
    public uint CurrentAddress { get; set; }

    [ProtoMember(3, IsRequired = true)]
    public Dictionary<int, uint> NodeAddresses { get; set; } = new Dictionary<int, uint>();
}

[ProtoContract]
public class CanMessageResult
{
    [ProtoMember(1)]
    public MessageStatus Status { get; set; }

    [ProtoMember(2)]
    public string Message { get; set; } = "";
}

public enum MessageStatus
{
    Success = 0,
    Error = 1,
}