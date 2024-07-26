using Ahsoka.ServiceFramework;
using Ahsoka.System;

namespace Ahsoka.Services.IO;

/// <summary>
/// Ahsoka Client for IO Service
/// </summary>
public class IOServiceClient : AhsokaClientBase<IOMessageTypes.Ids>
{
    /// <summary>
    /// Default Constructor which uses the Default Service Configuration
    /// </summary>
    public IOServiceClient() : this(ConfigurationLoader.GetServiceConfig(IOService.Name)) { }

    /// <summary>
    /// Constructor allowing a Custom Service Configuration
    /// </summary>
    /// <param name="config"></param>
    public IOServiceClient(ServiceConfiguration config) : base(config, new IOServiceMessages()) { }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new IOService(this.ServiceConfig);
    }


    /// <summary>
    /// Gets the current state of the Buzzer and its Configuration if the target is equipped with a buzzer.
    /// </summary>
    /// <returns></returns>
    public BuzzerConfig GetBuzzerConfig()
    {
        return SendNotificationWithResponse<BuzzerConfig>(IOMessageTypes.Ids.GetBuzzerConfig);
    }

    /// <summary>
    /// Sets the State, Frequency and Volume of the Onboard Buzzer if equipped
    /// </summary>
    /// <param name="buzzerConfig">The buzzer information to update.</param>
    public void SetBuzzerConfig(BuzzerConfig buzzerConfig)
    {
        SendNotificationWithResponse<EmptyNotification>(IOMessageTypes.Ids.SetBuzzerConfig, buzzerConfig);
    }
    /// <summary>
    ///  Returns the current value of the Battery Input (Power) in millivolts.
    /// </summary>
    /// <returns>Voltage Value</returns>
    public VoltageValue GetVBat()
    {
        return SendNotificationWithResponse<VoltageValue>(IOMessageTypes.Ids.GetBatteryVoltage);
    }

    /// <summary>
    /// Returns the current value of the Ignition Input if equiped in millivolts.
    /// </summary>
    /// <returns>Voltage Value</returns>
    public IgnitionState GetIGNPin()
    {
        return SendNotificationWithResponse<IgnitionState>(IOMessageTypes.Ids.GetIgnitionPin);
    }

    /// <summary>
    /// Set the Polling / Notification Rate of the IO Service (Defaults to 500ms)
    /// Set the rate to Int.Max to stop polling and Use Real Time Reads instead of Polling latest values.
    /// </summary>
    /// <returns>DigitalInput Collection</returns>
    public void SetPollingInterval(PollingInterval message)
    {
        SendNotificationWithResponse(IOMessageTypes.Ids.SetPollInterval,message);
    }

    /// <summary>
    /// Get the Polling / Notification Rate of the IO Service (Defaults to 500ms)
    /// </summary>
    /// <returns>DigitalInput Collection</returns>
    public PollingInterval GetPollingInterval()
    {
        return SendNotificationWithResponse<PollingInterval>(IOMessageTypes.Ids.GetPollInterval);
    }

    /// <summary>
    /// Get the List of Digital Inputs and their Configuration
    /// </summary>
    /// <returns>DigitalInput Collection</returns>
    public DigitalInputList RequestDigitalInputs()
    {
        return SendNotificationWithResponse<DigitalInputList>(IOMessageTypes.Ids.RetrieveDigitalInputs);
    }

    /// <summary>
    /// Get the List of Analog Inputs and their Configuration
    /// </summary>
    /// <returns>Analog Input Collection</returns>
    public AnalogInputList RequestAnalogInputs()
    {
        return SendNotificationWithResponse<AnalogInputList>(IOMessageTypes.Ids.RetrieveAnalogInputs);
    }

    /// <summary>
    /// Get the List of Digital Outputs and their Configuration
    /// </summary>
    /// <returns>Digital Output Collection</returns>
    public DigitalOutputList RequestDigitalOutputs()
    {
        return SendNotificationWithResponse<DigitalOutputList>(IOMessageTypes.Ids.RetrieveDigitalOutputs);
    }

    /// <summary>
    /// Get the List of Analog Outputs and their Configuration
    /// </summary>
    /// <returns>Analog Output Collection</returns>
    public AnalogOutputList RequestAnalogOutputs()
    {
        return SendNotificationWithResponse<AnalogOutputList>(IOMessageTypes.Ids.RetrieveAnalogOutputs);
    }

    /// <summary>
    /// Gets the current value of an Analog Input
    /// </summary>
    /// <param name="analogInput">Analog Input to Query</param>
    /// <returns>Current Value of the Input</returns>
    public GetInputResponse GetAnalogInput(AnalogInput analogInput)
    {
        return SendNotificationWithResponse<GetInputResponse>(IOMessageTypes.Ids.GetAnalogInput, analogInput);
    }

    /// <summary>
    /// Gets the current value of an Digital Input
    /// </summary>
    /// <param name="digitalInput">Digital Input to Query</param>
    /// <returns>Current Value of the Input</returns>
    public GetInputResponse GetDigitalInput(DigitalInput digitalInput)
    {
        return SendNotificationWithResponse<GetInputResponse>(IOMessageTypes.Ids.GetDigitalInput, digitalInput);
    }

    /// <summary>
    /// Sets the value of an Digital Output
    /// </summary>
    /// <param name="digitalOutput">Digital Output to Set</param>
    /// <returns>Result of the Operation</returns>
    public SetOutputResponse SetDigitalOut(DigitalOutput digitalOutput)
    {
        return SendNotificationWithResponse<SetOutputResponse>(IOMessageTypes.Ids.SetDigitalOutput, digitalOutput);
    }

    /// <summary>
    /// Sets the value of an Analog Output
    /// </summary>
    /// <param name="analogOutput">Analog Output to Set</param>
    /// <returns>Result of the Operation</returns>
    public SetOutputResponse SetAnalogOut(AnalogOutput analogOutput)
    {
        return SendNotificationWithResponse<SetOutputResponse>(IOMessageTypes.Ids.SetAnalogOutput, analogOutput);
    }
}
