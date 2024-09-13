namespace Ahsoka.Services.IO;

internal class WinDigitalInput : IDigitalInputImplementation
{
    public GetInputResponse ReadVolts(int pin)
    {
        /*
            Windows Functionality Not Currently Implemented! 
        */
        GetInputResponse response = new()
        {
            Value = pin * .5,
            ErrorDescription = "Windows Digital Inputs Not Implemented"
        };
        return response;
    }



}
