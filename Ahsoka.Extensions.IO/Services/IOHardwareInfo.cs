﻿using System.Collections.Generic;

namespace Ahsoka.Core.IO.Hardware;

/// <summary>
/// Information about the Raw Physical IO Ports supported on the Hardware
/// </summary>
public partial class IOHardwareInfoExtension
{
    /// <summary>
    /// Temp Code until Real Hardware Defs / or Config are added
    /// </summary>
    /// <param name="family"></param>
    /// <returns></returns>
    public static IOHardwareInfoExtension GetIOInfo(PlatformFamily family, string qualifier)
    {
        switch (family)
        {
            case PlatformFamily.Windows64:
            case PlatformFamily.Ubuntu64:
            case PlatformFamily.MacOSArm64:
                return new IOHardwareInfoExtension()
                {
                    AnalogInputs = [1, 2],
                    AnalogOutputs = [],
                    DigitalInputs = [1, 2],
                    DigitalOutputs = [1, 2],
                };

            case PlatformFamily.OpenViewLinux:
                switch (qualifier)
                {
                    case "Atlas":
                        return new IOHardwareInfoExtension()
                        {
                            AnalogInputs = [1, 2],
                            AnalogOutputs = [],
                            DigitalInputs = [1, 2, 3],
                            DigitalOutputs = [1, 2, 3, 4],
                        };
                    default:
                        return new IOHardwareInfoExtension()
                        {
                            AnalogInputs = [1, 2],
                            AnalogOutputs = [],
                            DigitalInputs = [1, 2],
                            DigitalOutputs = [1, 2,],
                        };
                }


            case PlatformFamily.OpenViewLinuxPro:
                return new IOHardwareInfoExtension()
                {
                    AnalogInputs = [],
                    AnalogOutputs = [],
                    DigitalInputs = [],
                    DigitalOutputs = [],
                };

            default:
                return new IOHardwareInfoExtension()
                {
                    AnalogInputs = [],
                    AnalogOutputs = [],
                    DigitalInputs = [],
                    DigitalOutputs = [],
                };
        }
    }

    /// <summary>
    /// List of Analog Input ID's
    /// </summary>
    public List<int> AnalogInputs { get; set; } = new List<int>();

    /// <summary>
    /// List of Analog Output ID's
    /// </summary>
    public List<int> AnalogOutputs { get; set; } = new List<int>();

    /// <summary>
    /// List of Digital Input ID's
    /// </summary>
    public List<int> DigitalInputs { get; set; } = new List<int>();

    /// <summary>
    /// List of Digital Output ID's
    /// </summary>
    public List<int> DigitalOutputs { get; set; } = new List<int>();

}
