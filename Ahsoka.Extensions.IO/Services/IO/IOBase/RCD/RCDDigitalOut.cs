using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.IO;

[ExcludeFromCodeCoverage]
internal class PinConfig
{
    internal GpioController controller;
    internal int pin;
}

[ExcludeFromCodeCoverage]
internal class RCDDigitalOutput : IDigitalOutputImplementation
{
    readonly Dictionary<int, PinConfig> outputPins = new();
    GpioController controller1;
    GpioController controller2;
    GpioController controller3;
    GpioController controller4;
    public RCDDigitalOutput()
    {
        controller1 = new(PinNumberingScheme.Logical, new LibGpiodDriver(4)); // Chip 4
        controller2 = new(PinNumberingScheme.Logical, new LibGpiodDriver(6)); // Chip 6
        controller3 = new(PinNumberingScheme.Logical, new LibGpiodDriver(1)); // Chip 1
        controller4 = new(PinNumberingScheme.Logical, new LibGpiodDriver(2)); // Chip 2

        outputPins.Add(1, new PinConfig() { controller = controller1, pin = 8 });
        outputPins.Add(2, new PinConfig() { controller = controller2, pin = 9 });
        outputPins.Add(3, new PinConfig() { controller = controller3, pin = 11 });
        outputPins.Add(4, new PinConfig() { controller = controller4, pin = 1 });
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
        var outputPin = outputPins[pin];

        // Set the pin state. !!NOTE: RCD has Low Side Outputs so setting pv to HIGH
        // will actually set the output LOW and vice versa!!
        PinValue pv = (state == PinState.High) ? PinValue.High : PinValue.Low;

        outputPin.controller.OpenPin(outputPin.pin, PinMode.Output);
        outputPin.controller.Write(outputPin.pin, pv);
        outputPin.controller.ClosePin(outputPin.pin);

        // Response isn't 100% neccesary now but to handle errors it can be useful
        SetOutputResponse response = new()
        {
            Pin = pin,
            Ret = ReturnCode.Success
        };
        return response;
    }
}
