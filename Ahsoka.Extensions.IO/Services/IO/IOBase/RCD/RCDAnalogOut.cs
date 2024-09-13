using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Services.IO;

[ExcludeFromCodeCoverage]
internal class RCDAnalogOutput : IAnalogOutputImplementation
{
    public SetOutputResponse SetOutput(int pin, double millivolts)
    {
        /*
            Nothing to do. RCD Hardware Does Not Have Analog Outputs!
        */
        SetOutputResponse response = new()
        {
            ErrorDescription = "No Analog Outputs On RCD Hardware!"
        };
        return response;
    }
}