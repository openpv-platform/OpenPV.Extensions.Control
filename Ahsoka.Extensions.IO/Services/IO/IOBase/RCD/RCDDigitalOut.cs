using Ahsoka.Services.IO.RCD;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.IO;

[ExcludeFromCodeCoverage]
internal class RCDDigitalOutput : IDigitalOutputImplementation
{
    GpioController controller1;
    GpioController controller2;
    public RCDDigitalOutput()
    {
        controller1 = new(PinNumberingScheme.Logical, new LibGpiodDriver(4)); // Chip 4
        controller2 = new(PinNumberingScheme.Logical, new LibGpiodDriver(6)); // Chip 6
    }

    public SetOutputResponse SetOutput(int pin, PinState state)
    {
        /*
            Implementation will select the approriate GPIO Chip and
            line offset to determine which specific pin on the micro
            is needed for setting high or low.
            RCD Digital Output 1 is on GPIO Chip 4, while output 2 is on chip 6.
            Running the commands  '$gpiodetect' and '$gpioinfo' on the RCD can
            provide more details.
        */
        GpioController controller = (pin == 1) ? controller1 : controller2;
        int MpPin = (pin == 1) ? 8 : 9;

        // Set the pin state. !!NOTE: RCD has Low Side Outputs so setting pv to HIGH
        // will actually set the output LOW and vice versa!!
        PinValue pv = (state == PinState.High) ? PinValue.High : PinValue.Low;
        
        controller.OpenPin(MpPin, PinMode.Output);
        controller.Write(MpPin, pv);
        controller.ClosePin(MpPin);

        // Response isn't 100% neccesary now but to handle errors it can be useful
        SetOutputResponse response = new()
        {
            Pin = pin,
            Ret = ReturnCode.Success
        };
        return response;
    }
}
