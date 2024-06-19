namespace Ahsoka.Services.IO;

internal class WinAnalogOutput : IAnalogOutputImplementation
{
    public SetOutputResponse SetOutput(int pin, double millivolts)
    {
        /*
            Windows Functionality Not Currently Implemented! 
        */
        SetOutputResponse response = new()
        {
            ErrorDescription = "Windows Analog Outputs Not Implemented"
        };
        return response;
    }

}