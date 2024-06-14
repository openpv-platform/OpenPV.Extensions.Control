using Ahsoka.Services.IO.RCD;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.IO;

[ExcludeFromCodeCoverage]
internal class RCDDigitalInput : IDigitalInputImplementation
{
    /*
        NOTE: voltage divider value obtained by solving the basic circuit equation:
            Vout = Vin(VoltageDivider) 
        Where Vout is the Volts at the microprocessor (The value returned by ADCUtils.GetVoltsRawValue() )
        and Vin is the Volts at the physical pin (The desired value for users).

        ***IMPORTANT***: If the physical resistors on the board change in a new hardware revision, then
        the value of _dinVoltageDivider will need to be adjusted to reflect that change!
    */
    private const double _dinVoltageDivider = ((40.2 + 3.01) / 3.01);

    public GetInputResponse ReadVolts(int pin)
    {
        /*
            This implementation will map the input pin to it's
            Mux array index. The Index will set the Mux select to
            the appropriate values for reading the input raw volatge
            Digital Input 1 is defined by index 6, input 2 is defined by index 7.
        */
        ADCInput adcInput = pin == 1 ? ADCInput.DIN1 : ADCInput.DIN2;

        var response = new GetInputResponse();
        // Once Mux Selects are set, we can read from in_voltage5_raw sysfs file
        float RawValue = ADCUtils.GetVoltsRawValue(adcInput);

        // calculate volts at the input pin by multiplying with inverse of voltage divider
        response.Value = RawValue * _dinVoltageDivider;
        response.Ret = ReturnCode.Success;
        return response;
    }

}
