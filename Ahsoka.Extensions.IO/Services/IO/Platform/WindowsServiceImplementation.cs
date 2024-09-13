using Ahsoka.Core.IO.Hardware;
using Ahsoka.System.Hardware;


namespace Ahsoka.Services.IO.Platform;

internal class DesktopServiceImplementation : IOServiceImplementationBase
{
    #region Fields
    #endregion

    #region Methods
    protected override void OnHandleInit(IOHardwareInfoExtension IOInfo,
         out IAnalogInputImplementation analogInputImplementation,
         out IAnalogOutputImplementation analogOutputImplementation,
         out IDigitalInputImplementation digitalInputImplementation,
         out IDigitalOutputImplementation digitalOutputImplementation)
    {
        analogInputImplementation = new WinAnalogInput();
        analogOutputImplementation = new WinAnalogOutput();
        digitalInputImplementation = new WinDigitalInput();
        digitalOutputImplementation = new WinDigitalOutput();
    }

    internal override BuzzerConfig GetBuzzerConfig()
    {
        // Not Implemented
        return new BuzzerConfig()
        {
            IsEnabled = false,
            FrequencyInHz = 1000,
            VolumePct = 50
        };
    }

    internal override void SetBuzzerConfig(BuzzerConfig buzzerConfig)
    {

    }


    internal override VoltageValue GetVBat()
    {
        // *****Not Implemented*****
        VoltageValue response = new()
        {
            MilliVolts = 0
        };
        return response;
    }

    internal override IgnitionState GetIGNPin()
    {
        // *****Not Implemented*****
        IgnitionState response = new()
        {
            MilliVolts = 0,
            State = IgnitionStates.On
        };
        return response;
    }
    #endregion
}
