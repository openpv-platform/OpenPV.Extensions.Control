namespace Ahsoka.Services.IO;

internal class WinAnalogInput : IAnalogInputImplementation
{
    public GetInputResponse ReadVolts(int pin)
    {
        /*
            Windows Functionality Not Currently Implemented! 
        */
        GetInputResponse response = new()
        {
            Value = pin * .5,
            ErrorDescription = "Windows Analog Inputs Not Implemented"
        };
        return response;
    }


}