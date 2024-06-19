using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.System;
using System.Collections.Generic;

namespace Ahsoka.Services.IO;

internal class IOServiceMessages : AhsokaMessagesBase
{
    public IOServiceMessages()
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

    public override Dictionary<string, byte[]> GetAdditionalClientResources(ApplicationType type, bool includeImplementation)
    {
        var result = base.GetAdditionalClientResources(type);
        result.Add("Ahsoka.Proto\\IOService.proto", Properties.IOResources.IOService);
        return result;
    }

    public override KnownDataKeys GetKnownValues()
    {
        return new IOServiceDataKeys();
    }
}

