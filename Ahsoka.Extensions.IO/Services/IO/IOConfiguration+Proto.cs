using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.IO;

[ProtoContract]
public class IOApplicationConfiguration
{
    [ProtoMember(1, Name = "IO_Configuration")]
    public IOConfiguration IOConfiguration { get; set; }

    [ProtoMember(10)]
    public bool GeneratorEnabled { get; set; }

    [ProtoMember(11)]
    public string GeneratorNamespace { get; set; } = "";

    [ProtoMember(12)]
    public string GeneratorOutputFile { get; set; } = "";

    [ProtoMember(13)]
    public string GeneratorBaseClass { get; set; } = "";

    [ProtoMember(20)]
    public string Version { get; set; } = "";
}

[ProtoContract]
public class IOConfiguration
{
    [ProtoMember(1, IsRequired = true)]
    public List<AnalogInputConfiguration> AnalogInputs { get; set; } = new List<AnalogInputConfiguration>();

    [ProtoMember(2, IsRequired = true)]
    public List<AnalogOutputConfiguration> AnalogOutputs { get; set; } = new List<AnalogOutputConfiguration>();

    [ProtoMember(3, IsRequired = true)]
    public List<DigitalInputConfiguration> DigitalInputs { get; set; } = new List<DigitalInputConfiguration>();

    [ProtoMember(4, IsRequired = true)]
    public List<DigitalOutputConfiguration> DigitalOutputs { get; set; } = new List<DigitalOutputConfiguration>();

    [ProtoMember(5, IsRequired = true)]
    public List<FrequencyInputConfiguration> FrequencyInputs { get; set; } = new List<FrequencyInputConfiguration>();

    [ProtoMember(6, IsRequired = true)]
    public List<FrequencyOutputConfiguration> FrequencyOutputs { get; set; } = new List<FrequencyOutputConfiguration>();

    [ProtoMember(7, IsRequired = true)]
    public List<CurveDefinition> Curves { get; set; } = new List<CurveDefinition>();
}

[ProtoContract]
public class AnalogInputConfiguration
{
    [ProtoMember(1)]
    public uint ChannelNum { get; set; }

    [ProtoMember(2)]
    public byte[] CurveId { get; set; }

    [ProtoMember(3)]
    public uint DigitalThreshold { get; set; }

    [ProtoMember(4)]
    public uint DigitalHysteresisPercent { get; set; }
}

[ProtoContract]
public class AnalogOutputConfiguration
{
    [ProtoMember(1)]
    public uint ChannelNum { get; set; }

    [ProtoMember(2)]
    public byte[] CurveId { get; set; }

    [ProtoMember(3)]
    public PorBehavior PorBehavior { get; set; }

    [ProtoMember(4)]
    public LocBehavior LocBehavior { get; set; }
}

[ProtoContract]
public class DigitalInputConfiguration
{
    [ProtoMember(1)]
    public uint ChannelNum { get; set; }

    [ProtoMember(2)]
    public DigitalInputType InputType { get; set; }

    [ProtoMember(3)]
    public uint Threshold { get; set; } // percentage of full scale to count high/low
}

[ProtoContract]
public class DigitalOutputConfiguration
{
    [ProtoMember(1)]
    public uint ChannelNum { get; set; }

    [ProtoMember(2)]
    public PorBehavior PorBehavior { get; set; }

    [ProtoMember(3)]
    public LocBehavior LocBehavior { get; set; }
}

[ProtoContract]
public class FrequencyInputConfiguration
{
    [ProtoMember(1)]
    public uint ChannelNum { get; set; }
}

[ProtoContract]
public class FrequencyOutputConfiguration
{
    [ProtoMember(1)]
    public uint ChannelNum { get; set; }

    [ProtoMember(2)]
    public uint DutyCycle { get; set; }

    [ProtoMember(3)]
    public uint Frequency { get; set; }

    [ProtoMember(4)]
    public PorBehavior PorBehavior { get; set; }

    [ProtoMember(5)]
    public LocBehavior LocBehavior { get; set; }
}

[ProtoContract]
public class Coordinate
{
    [ProtoMember(1)]
    public uint X { get; set; }

    [ProtoMember(2)]
    public uint Y { get; set; }
}

[ProtoContract]
public class CurveDefinition
{
    [ProtoMember(1)]
    public string Name { get; set; } = "";

    [ProtoMember(2)]
    public byte[] Id { get; set; }

    [ProtoMember(3)]
    public AnalogInputType InputType { get; set; }

    [ProtoMember(4, IsRequired = true)]
    public List<Coordinate> Coordinates { get; set; } = new List<Coordinate>();
}

public enum AnalogInputType
{
    VoltageInput = 0,
    ResistiveInput = 1,
    CurrentInput = 2,
    DigitalInput = 3,
}

public enum DigitalInputType
{
    PulledHigh = 0,
    PulledLow = 1,
    Floating = 2,
    Divided = 3,
}

public enum PorBehavior
{
    Off = 0,
    OnPositive = 1,
    OnNegative = 2,
    LastCommand = 3,
}

public enum LocBehavior
{
    Off = 0,
    OnPositive = 1,
    OnNegative = 2,
    LastCommand = 3,
}