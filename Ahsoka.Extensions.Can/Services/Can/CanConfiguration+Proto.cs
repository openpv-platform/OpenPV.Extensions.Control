using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.Can;

// Customer Can Configuration for Can Services
// This object is parsed by the Customer Tools at build time to generate 
[ProtoContract]
public class CanClientConfiguration
{
    [ProtoMember(1)]
    public string Name { get; set; } = "";

    [ProtoMember(6)]
    public string LocalIpAddress { get; set; } = "";

    [ProtoMember(7)]
    public string RemoteIpAddress { get; set; } = "";

    [ProtoMember(9, IsRequired = true)]
    public List<PortDefinition> Ports { get; set; } = new List<PortDefinition>();

    [ProtoMember(10, IsRequired = true)]
    public List<NodeDefinition> Nodes { get; set; } = new List<NodeDefinition>();

    [ProtoMember(11, IsRequired = true)]
    public List<MessageDefinition> Messages { get; set; } = new List<MessageDefinition>();

    [ProtoMember(13)]
    public bool GeneratorEnabled { get; set; }

    [ProtoMember(14)]
    public string GeneratorNamespace { get; set; } = "";

    [ProtoMember(15)]
    public string GeneratorOutputFile { get; set; } = "";

    [ProtoMember(16)]
    public string GeneratorBaseClass { get; set; } = "";

    [ProtoMember(20)]
    public string Version { get; set; } = "";
}

// Can Configuration used at Runtime
[ProtoContract]
public class CanPortConfiguration
{
    [ProtoMember(1, IsRequired = true)]
    public CommunicationConfiguration CommunicationConfiguration { get; set; } = new();

    [ProtoMember(2, IsRequired = true)]
    public MessageConfiguration MessageConfiguration { get; set; } = new();
}

// Information about Communicating with the Main Processor (Ports, Addressees)
[ProtoContract]
public class CommunicationConfiguration
{
    [ProtoMember(6)]
    public string LocalIpAddress { get; set; } = "";

    [ProtoMember(7)]
    public string RemoteIpAddress { get; set; } = "";
}

// Message / Signal Descriptions, Filters Etc.
[ProtoContract]
public class MessageConfiguration
{
    [ProtoMember(1, IsRequired = true)]
    public List<NodeDefinition> Nodes { get; set; } = new List<NodeDefinition>();

    [ProtoMember(2, IsRequired = true)]
    public List<MessageDefinition> Messages { get; set; } = new List<MessageDefinition>();

    [ProtoMember(3, IsRequired = true)]
    public List<PortDefinition> Ports { get; set; } = new List<PortDefinition>();
}

//Description of Port
[ProtoContract]
public class PortDefinition
{
    [ProtoMember(1)]
    public uint Port { get; set; }

    [ProtoMember(2)]
    public string CanInterfacePath { get; set; } = "";

    [ProtoMember(3)]
    public CanBaudRate BaudRate { get; set; }

    [ProtoMember(4)]
    public CanInterface CanInterface { get; set; }

    [ProtoMember(5)]
    public bool PromiscuousTransmit { get; set; }

    [ProtoMember(6)]
    public bool PromiscuousReceive { get; set; }

    [ProtoMember(7)]
    public bool UserDefined { get; set; }
}

// Descriptions of Network Nodes
[ProtoContract]
public partial class NodeDefinition
{
    [ProtoMember(1)]
    public int Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = "";

    [ProtoMember(3)]
    public string Comment { get; set; } = "";

    [ProtoMember(4)]
    public NodeType NodeType { get; set; }

    [ProtoMember(8)]
    public TransportProtocol TransportProtocol { get; set; }

    [ProtoMember(9)]
    public J1939NodeDefinition J1939Info { get; set; }

    [ProtoMember(10)]
    public IsoTPNodeDefinition IsoInfo { get; set; }

    [ProtoMember(11, IsPacked = true)]
    public int[] Ports { get; set; }
}

[ProtoContract]
public class IsoTPNodeDefinition
{
    [ProtoMember(5)]
    public int TransmitId { get; set; }

    [ProtoMember(6)]
    public int ReceiveId { get; set; }
}

[ProtoContract]
public partial class J1939NodeDefinition
{
    [ProtoMember(4)]
    public NodeAddressType AddressType { get; set; }

    [ProtoMember(5)]
    public int AddressValueOne { get; set; }

    [ProtoMember(6)]
    public int AddressValueTwo { get; set; }

    [ProtoMember(7)]
    public int AddressValueThree { get; set; }

    [ProtoMember(8)]
    public uint IndustryGroup { get; set; }

    [ProtoMember(9)]
    public uint VehicleSystemInstance { get; set; }

    [ProtoMember(10)]
    public uint VehicleSystem { get; set; }

    [ProtoMember(11)]
    public uint Function { get; set; }

    [ProtoMember(12)]
    public uint FunctionInstance { get; set; }

    [ProtoMember(13, Name = "ECU_Instance")]
    public uint ECUInstance { get; set; }

    [ProtoMember(14)]
    public uint ManufacturerCode { get; set; }

    [ProtoMember(15)]
    public ulong Name { get; set; }

    [ProtoMember(16)]
    public string Addresses { get; set; } = "";

    [ProtoMember(17)]
    public bool UseAddressClaim { get; set; }
}

// Message / Signal Descriptions, Filters Etc.
[ProtoContract]
public partial class MessageDefinition
{
    [ProtoMember(1)]
    public uint Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = "";

    [ProtoMember(3)]
    public MessageType MessageType { get; set; }

    [ProtoMember(4)]
    public uint Dlc { get; set; }

    [ProtoMember(5)]
    public int Rate { get; set; }

    [ProtoMember(6)]
    public string Comment { get; set; } = "";

    // Transport Configuration
    [ProtoMember(8)]
    public bool HasRollCount { get; set; }

    [ProtoMember(9)]
    public uint RollCountBit { get; set; }

    [ProtoMember(10)]
    public uint RollCountLength { get; set; }

    [ProtoMember(11)]
    public CrcType CrcType { get; set; }

    [ProtoMember(12)]
    public uint CrcBit { get; set; }

    [ProtoMember(14)]
    public int TimeoutMs { get; set; }

    [ProtoMember(15)]
    public bool FilterReceipts { get; set; }

    [ProtoMember(16)]
    public bool UserDefined { get; set; }

    [ProtoMember(18, IsPacked = true)]
    public int[] TransmitNodes { get; set; }

    [ProtoMember(19, IsPacked = true)]
    public int[] ReceiveNodes { get; set; }

    //Only used for J1939 messages
    [ProtoMember(20)]
    public bool OverrideSourceAddress { get; set; }

    //Only used for J1939 messages
    [ProtoMember(21)]
    public bool OverrideDestinationAddress { get; set; }

    [ProtoMember(22, IsRequired = true)]
    public List<MessageSignalDefinition> Signals { get; set; } = new List<MessageSignalDefinition>();
}

// Message / Signal Descriptions, Filters Etc.
[ProtoContract]
public partial class MessageSignalDefinition
{
    [ProtoMember(1)]
    public uint Id { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = "";

    [ProtoMember(3)]
    public uint StartBit { get; set; }

    [ProtoMember(4)]
    public uint BitLength { get; set; }

    [ProtoMember(5)]
    public ByteOrder ByteOrder { get; set; }

    [ProtoMember(6)]
    public ValueType ValueType { get; set; }

    [ProtoMember(7)]
    public double DefaultValue { get; set; }

    [ProtoMember(8)]
    public double Scale { get; set; }

    [ProtoMember(9)]
    public double Offset { get; set; }

    [ProtoMember(10)]
    public double Minimum { get; set; }

    [ProtoMember(11)]
    public double Maximum { get; set; }

    [ProtoMember(12)]
    public string Unit { get; set; } = "";

    [ProtoMember(13)]
    public MuxRole MuxRole { get; set; }

    [ProtoMember(14)]
    public uint MuxGroup { get; set; }

    [ProtoMember(15)]
    public string Comment { get; set; } = "";

    [ProtoMember(20, IsRequired = true)]
    public Dictionary<int, string> Values { get; set; } = new Dictionary<int, string>();

    [ProtoMember(21, IsPacked = true)]
    public int[] ReceiverNodeIds { get; set; }
}

// Descriptions of Diagnostic Messages to Watch or Sent.
[ProtoContract]
public class J1939EventInfo
{
    [ProtoMember(1)]
    public uint Spn { get; set; }

    [ProtoMember(2)]
    public uint Fmi { get; set; }

    [ProtoMember(3)]
    public J1930Lamp Lamp { get; set; }
}

public enum J1930Lamp
{
    Yellow = 0,
    Red = 1,
    Status3 = 2,
    Status4 = 3,
}

// Descriptions of Diagnostic Messages to Watch or Sent.
[ProtoContract]
public class OBDEventInfo
{
    [ProtoMember(1)]
    public DTCFault FaultType { get; set; }

    [ProtoMember(2)]
    public uint ManufacturerCode { get; set; }

    [ProtoMember(3)]
    public uint VehicleSystem { get; set; }

    [ProtoMember(4)]
    public uint Code { get; set; }
}

public enum DTCFault
{
    Powertrain = 0,
    Body = 1,
    Chassis = 2,
    Network = 3,
}

// Descriptions of Diagnostic Messages to Watch or Sent.
[ProtoContract]
public class ClientCanFilter
{
    [ProtoMember(1)]
    public uint CanPort { get; set; }

    [ProtoMember(2, IsPacked = true)]
    public uint[] CanIdLists { get; set; }
}

// Custom Calibration Values for use in setting Behaviors in the CAN Processor.
[ProtoContract]
public class CustomPlatformCalibration
{
    [ProtoMember(1)]
    public bool PromiscuousTransmit { get; set; }

    [ProtoMember(2)]
    public bool PromiscuousReceive { get; set; }
}

public enum NodeAddressType
{
    Static = 0,
    SystemAddress = 1,
    SystemFunctionAddress = 2,
    SystemInstanceAddress = 3,
}

public enum MuxRole
{
    NotMultiplexed = 0,
    Multiplexor = 1,
    Multiplexed = 2,
}


public enum TransportProtocol
{
    Raw = 0,
    J1939 = 1,
    IsoTp = 2,
}

public enum MessageType
{
    RawStandardFrame = 0,
    RawExtendedFrame = 1,
    J1939ExtendedFrame = 3,
}

public enum ValueType
{
    Signed = 0,
    Unsigned = 1,
    Float = 3,
    Double = 4,
    Enum = 5,
}

public enum CrcType
{
    None = 0,
    CheckSum = 2,
    Tsc1 = 3,
}

public enum CanInterface
{
    NoUsed = 0,
    Coprocessor = 1,
    SocketCan = 2,
    EcomWindows = 3,
}

public enum CanBaudRate
{
    Baud250kb = 0,
    Baud500kb = 1,
    Baud1mb = 2,
}

public enum NodeType
{
    UserDefined = 0,
    Predefined = 1,
    Self = 2,
    Any = 3,
}

//LittleEndian conflicts with cpp generation
public enum ByteOrder
{
    OrderLittleEndian = 0,
    OrderBigEndian = 1,
}