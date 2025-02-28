using ProtoBuf;
using System.Collections.Generic;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Ahsoka.Services.IO;

[ProtoContract]
public class IOMessageTypes
{
    public enum Ids
    {
        None = 0,
        
        // message IDs to Set IO Pins
        SetDigitalOutput = 1,
        SetAnalogOutput = 2,

        SetPollInterval = 3,
        GetPollInterval = 4,

        // message IDs to Read IO Pins
        GetAnalogInput = 10,
        GetDigitalInput = 11,

        // message IDs for requesting IO Pins
        RetrieveDigitalOutputs = 20,
        RetrieveDigitalInputs = 21,
        RetrieveAnalogOutputs = 22,
        RetrieveAnalogInputs = 23,

        // message IDs to Set / Get Hardware Configurations
        SetHardwareConfig = 30,
        GetHardwareConfig = 31,

        GetBuzzerConfig = 41,
        SetBuzzerConfig = 42,

        GetBatteryVoltage = 55,
        GetIgnitionPin = 56,

        IgnitionOffNotification = 101,
        IgnitionOnNotification = 102,
    }
}

[ProtoContract]
public class PollingInterval
{
    [ProtoMember(1)]
    public int PollingIntervalMs { get; set; }
}

// **************************************************
// Digital & Analog Input Specific messages & enums
// **************************************************
[ProtoContract]
public class AnalogInputList
{
    [ProtoMember(1, IsRequired = true)]
    public List<AnalogInput> AnalogInputs { get; set; } = new List<AnalogInput>();
}

[ProtoContract]
public class AnalogInput
{
    [ProtoMember(1)]
    public int Pin { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = "";
}

[ProtoContract]
public class DigitalInputList
{
    [ProtoMember(1, IsRequired = true)]
    public List<DigitalInput> DigitalInputs { get; set; } = new List<DigitalInput>();
}

[ProtoContract]
public class DigitalInput
{
    [ProtoMember(1)]
    public int Pin { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = "";
}

// **************************************************
// Digital & Analog Output Specific messages and enums
// **************************************************

[ProtoContract]
public class DigitalOutputList
{
    [ProtoMember(1, IsRequired = true)]
    public List<DigitalOutput> DigitalOutputs { get; set; } = new List<DigitalOutput>();
}

[ProtoContract]
public class DigitalOutput
{
    [ProtoMember(1)]
    public int Pin { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = "";

    [ProtoMember(3)]
    public PinState State { get; set; }
}

public enum PinState
{
    Low = 0,
    High = 1,
}

[ProtoContract]
public class AnalogOutputList
{
    [ProtoMember(1, IsRequired = true)]
    public List<AnalogOutput> AnalogOutputs { get; set; } = new List<AnalogOutput>();
}

[ProtoContract]
public class AnalogOutput
{
    [ProtoMember(1)]
    public int Pin { get; set; }

    [ProtoMember(2)]
    public string Name { get; set; } = "";

    [ProtoMember(3)]
    public double MilliVolts { get; set; }
}

// **********************************************
// Get / Set Response Messages
// **********************************************

[ProtoContract]
public class GetInputResponse
{
    [ProtoMember(1)]
    public double Value { get; set; }

    [ProtoMember(2)]
    public ReturnCode Ret { get; set; }

    [ProtoMember(3)]
    public string ErrorDescription { get; set; } = "";
}

[ProtoContract]
public class SetOutputResponse
{
    [ProtoMember(1)]
    public int Pin { get; set; }

    [ProtoMember(2)]
    public ReturnCode Ret { get; set; }

    [ProtoMember(3)]
    public string ErrorDescription { get; set; } = "";
}

public enum ReturnCode
{
    Failed = 0,
    Success = 1,
}

[ProtoContract]
public class BuzzerConfig
{
    [ProtoMember(1)]
    public bool IsEnabled { get; set; }

    [ProtoMember(2)]
    public int FrequencyInHz { get; set; }

    [ProtoMember(3)]
    public int VolumePct { get; set; }
}

// message to get VBAT and IGN Pin voltages
[ProtoContract]
public class VoltageValue
{
    [ProtoMember(1)]
    public double MilliVolts { get; set; }
}

[ProtoContract]
public class IgnitionState
{
    [ProtoMember(1)]
    public double MilliVolts { get; set; }

    [ProtoMember(2)]
    public IgnitionStates State { get; set; }
}

public enum IgnitionStates
{
    Unknown = 0,
    On = 1,
    Off = 2,
}