namespace Ahsoka.Services.IO;

internal class WinDigitalOutput : IDigitalOutputImplementation
{
    public SetOutputResponse SetOutput(int pin, PinState state)
    {
        /*
            Windows Functionality Not Currently Implemented! 
        */
        SetOutputResponse response = new()
        {
            ErrorDescription = "Windows Digital Outputs Not Implemented"
        };
        return response;
    }
}
