using Ahsoka.ServiceFramework;
using Ahsoka.Services.IO;

namespace Ahsoka.Services.System;

/// <summary>
/// List of Key Values Available in this Service
/// </summary>
public class IOServiceDataKeys : KnownDataKeys
{
    /// <summary>
    /// Is the Service Available
    /// </summary>
    public const string Available = "Available";

    /// <summary>
    /// Digital Input Value Prefix
    /// </summary>
    public const string DigitalInput_ = "DigitalInput_";

    /// <summary>
    /// Digital Input Value 1
    /// </summary>
    public const string DigitalInput_1 = $"{DigitalInput_}1";
  
    /// <summary>
    /// Digital Input Value 2
    /// </summary>
    public const string DigitalInput_2 = $"{DigitalInput_}2";

    /// <summary>
    /// Digital Input Value 3
    /// </summary>
    public const string DigitalInput_3 = $"{DigitalInput_}3";

    /// <summary>
    /// Analog Input Value 1
    /// </summary>
    public const string AnalogInput_ = "AnalogInput_";

    /// <summary>
    /// Analog Input Value 1
    /// </summary>
    public const string AnalogInput_1 = $"{AnalogInput_}1";

    /// <summary>
    /// Analog Input Value 1
    /// </summary>
    public const string AnalogInput_2 = $"{AnalogInput_}2";

    /// <summary>
    /// Analog Input Value 2
    /// </summary>
    public const string AnalogInput_3 = $"{AnalogInput_}3";


    /// <summary>
    /// Digital Output Value PreFix
    /// </summary>
    public const string DigitalOutput_ = "DigitalOutput_";

    /// <summary>
    /// Digital Output Value 1
    /// </summary>
    public const string DigitalOutput_1 = $"{DigitalOutput_}1";

    /// <summary>
    /// Digital Output Value 2
    /// </summary>
    public const string DigitalOutput_2 = $"{DigitalOutput_}2";

    /// <summary>
    /// Digital Output Value 3
    /// </summary>
    public const string DigitalOutput_3 = $"{DigitalOutput_}3";


    /// <summary>
    /// Analog Output Value Prefix
    /// </summary>
    public const string AnalogOutput_ = "AnalogOutput_";

    /// <summary>
    /// Analog Output Value 1
    /// </summary>
    public const string AnalogOutput_1 = $"{AnalogOutput_}1";

    /// <summary>
    /// Analog Output Value 1
    /// </summary>
    public const string AnalogOutput_2 = $"{AnalogOutput_}2";

    /// <summary>
    /// Analog Output Value 2
    /// </summary>
    public const string AnalogOutput_3 = $"{AnalogOutput_}3";



}
