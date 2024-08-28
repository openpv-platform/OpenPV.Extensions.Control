using Ahsoka.Services.IO.RCD;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.IO;

[ExcludeFromCodeCoverage]
internal class RCDAnalogInput : IAnalogInputImplementation
{
    /*
        NOTE: voltage divider value obtained by solving the basic circuit equation:
            Vout = Vin(VoltageDivider) 
        Where Vout is the Volts at the microprocessor (The value returned by ADCUtils.GetVoltsRawValue() )
        and Vin is the Volts at the physical pin (The desired value for users).

        ***IMPORTANT***: If the physical resistors on the board change in a new hardware revision, then
        the value of _ainVoltageDivider will need to be adjusted to reflect that change!
    */
    private const double _ainVoltageDivider = ((5.1 + 10) / 10);

    public GetInputResponse ReadVolts(int pin)
    {
        /*
            This implementation will map the input pin to it's
            Mux array index. The Index will set the Mux select to
            the appropriate values for reading the input raw voltage.
        */
        ADCInput muxInput = pin == 1 ? ADCInput.AIN1 : ADCInput.AIN2;

        var response = new GetInputResponse();
        // Once Mux Selects are set, we can read from in_voltage5_raw sysfs file
        float RawValue = ADCUtils.GetVoltsRawValue(muxInput);

        // calculate volts at the input pin by multiplying with inverse of voltage divider
        response.Value = RawValue * _ainVoltageDivider;
        response.Ret = ReturnCode.Success;
        return response;
    }


}
