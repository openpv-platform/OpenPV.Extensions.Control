using Ahsoka.Core;
using Ahsoka.Installer;
using Org.BouncyCastle.Crypto;
using System.Collections.Generic;

namespace Ahsoka.Services.IO;

public class IOServiceMessages : AhsokaMessagesBase
{
    /// <summary>
    /// Is the Service Available
    /// </summary>
    public const string CurrentIgnitionState = "Available";

    /// <summary>
    /// Is the Service Available
    /// </summary>
    public const string Available = "Available";

    /// <summary>
    /// Digital Input Value Prefix
    /// </summary>
    public const string DigitalInput_ = "DigitalInput_";

    /// <summary>
    /// Digital Input Value 1
    /// </summary>
    public const string DigitalInput_1 = $"{DigitalInput_}1";

    /// <summary>
    /// Digital Input Value 2
    /// </summary>
    public const string DigitalInput_2 = $"{DigitalInput_}2";

    /// <summary>
    /// Digital Input Value 3
    /// </summary>
    public const string DigitalInput_3 = $"{DigitalInput_}3";

    /// <summary>
    /// Analog Input Value 1
    /// </summary>
    public const string AnalogInput_ = "AnalogInput_";

    /// <summary>
    /// Analog Input Value 1
    /// </summary>
    public const string AnalogInput_1 = $"{AnalogInput_}1";

    /// <summary>
    /// Analog Input Value 1
    /// </summary>
    public const string AnalogInput_2 = $"{AnalogInput_}2";

    /// <summary>
    /// Analog Input Value 2
    /// </summary>
    public const string AnalogInput_3 = $"{AnalogInput_}3";


    /// <summary>
    /// Digital Output Value PreFix
    /// </summary>
    public const string DigitalOutput_ = "DigitalOutput_";

    /// <summary>
    /// Digital Output Value 1
    /// </summary>
    public const string DigitalOutput_1 = $"{DigitalOutput_}1";

    /// <summary>
    /// Digital Output Value 2
    /// </summary>
    public const string DigitalOutput_2 = $"{DigitalOutput_}2";

    /// <summary>
    /// Digital Output Value 3
    /// </summary>
    public const string DigitalOutput_3 = $"{DigitalOutput_}3";


    /// <summary>
    /// Analog Output Value Prefix
    /// </summary>
    public const string AnalogOutput_ = "AnalogOutput_";

    /// <summary>
    /// Analog Output Value 1
    /// </summary>
    public const string AnalogOutput_1 = $"{AnalogOutput_}1";

    /// <summary>
    /// Analog Output Value 1
    /// </summary>
    public const string AnalogOutput_2 = $"{AnalogOutput_}2";

    /// <summary>
    /// Analog Output Value 2
    /// </summary>
    public const string AnalogOutput_3 = $"{AnalogOutput_}3";


    public IOServiceMessages() : base(IOService.Name)
    {
        // Requests to Set Digital / Analog Outputs
        this.RegisterServiceRequest(IOMessageTypes.Ids.SetDigitalOutput, typeof(DigitalOutput), typeof(SetOutputResponse));
        this.RegisterServiceRequest(IOMessageTypes.Ids.SetAnalogOutput, typeof(AnalogOutput), typeof(SetOutputResponse));

        // Requests to Get Digital / Analog Inputs
        this.RegisterServiceRequest(IOMessageTypes.Ids.GetAnalogInput, typeof(AnalogInput), typeof(GetInputResponse));
        this.RegisterServiceRequest(IOMessageTypes.Ids.GetDigitalInput, typeof(DigitalInput), typeof(GetInputResponse));

        // Requests to retrieve available Digital / Analog Inputs
        this.RegisterServiceRequest(IOMessageTypes.Ids.RetrieveDigitalInputs, typeof(EmptyNotification), typeof(DigitalInputList));
        this.RegisterServiceRequest(IOMessageTypes.Ids.RetrieveAnalogInputs, typeof(EmptyNotification), typeof(AnalogInputList));

        // Requests to retrieve available Digital / Analog Outputs
        this.RegisterServiceRequest(IOMessageTypes.Ids.RetrieveDigitalOutputs, typeof(EmptyNotification), typeof(DigitalOutputList));
        this.RegisterServiceRequest(IOMessageTypes.Ids.RetrieveAnalogOutputs, typeof(EmptyNotification), typeof(AnalogOutputList));

        this.RegisterServiceRequest(IOMessageTypes.Ids.SetPollInterval, typeof(PollingInterval), typeof(EmptyNotification));

        this.RegisterServiceRequest(IOMessageTypes.Ids.GetPollInterval, typeof(EmptyNotification), typeof(PollingInterval));

        this.RegisterServiceRequest(IOMessageTypes.Ids.GetBuzzerConfig, typeof(EmptyNotification), typeof(BuzzerConfig));
        this.RegisterServiceRequest(IOMessageTypes.Ids.SetBuzzerConfig, typeof(BuzzerConfig), typeof(EmptyNotification));
        this.RegisterServiceRequest(IOMessageTypes.Ids.GetBatteryVoltage, typeof(EmptyNotification), typeof(VoltageValue));
        this.RegisterServiceRequest(IOMessageTypes.Ids.GetIgnitionPin, typeof(EmptyNotification), typeof(IgnitionState));
        this.RegisterServiceNotification(IOMessageTypes.Ids.IgnitionOffNotification, typeof(EmptyNotification));
        this.RegisterServiceNotification(IOMessageTypes.Ids.IgnitionOnNotification, typeof(EmptyNotification));

    }

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation = true)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\IOService.proto", Properties.IOResources.IOService);
        return result;
    }


    protected override void OnGetParameters(out string service, List<ParameterData> values, PackageInformation packageInfo)
    {
        service = IOService.Name;

        values.Add(new() { Name = Available, DefaultValue = false, ValueType = ParameterValueTypes.Boolean });
        values.Add(new() { Name = DigitalInput_1, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = DigitalInput_2, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = DigitalInput_3, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = DigitalOutput_1, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = DigitalOutput_2, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = DigitalOutput_3, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = AnalogOutput_1, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = AnalogOutput_2, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = AnalogOutput_3, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = AnalogInput_1, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = AnalogInput_2, ValueType = ParameterValueTypes.Double });
        values.Add(new() { Name = AnalogInput_3, ValueType = ParameterValueTypes.Double });
    }
}

