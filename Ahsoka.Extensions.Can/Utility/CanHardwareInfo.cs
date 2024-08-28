using Ahsoka.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahsoka.Utility
{

    /// <summary>
    /// Information about the Raw Physical IO Ports supported on the Hardware
    /// </summary>
    public partial class CANHardwareInfoExtension
    {
        /// <summary>
        /// Temp Code until Real Hardware Defs / or Config are added
        /// </summary>
        /// <param name="family"></param>
        /// <returns></returns>
        public static CANHardwareInfoExtension GetCanInfo(PlatformFamily family)
        {
            switch (family)
            {
                case PlatformFamily.Windows64:
                case PlatformFamily.Ubuntu64:
                case PlatformFamily.MacosArm64:
                    return new CANHardwareInfoExtension() { CANPorts = [new CanPort() { Port = 0 }] };

                case PlatformFamily.OpenViewLinux:
                    return new CANHardwareInfoExtension()
                    {
                        CANPorts = [new CanPort() { Port = 0, SocketCanInterfacePath = "can0", CoprocessorFirmwarePath = "/lib/firmware/rproc-m4-fw", CoprocessorSerialPath = "/dev/ttyRPMSG0" }, 
                                    new CanPort() { Port = 1, SocketCanInterfacePath = "can1", CoprocessorFirmwarePath = "/lib/firmware/rproc-m4-fw", CoprocessorSerialPath = "/dev/ttyRPMSG0" }]
                    };

                case PlatformFamily.OpenViewLinuxPro:
                    return new CANHardwareInfoExtension() { CANPorts = [] };

                default:
                    return new CANHardwareInfoExtension() { CANPorts = [] };
            }
        }

      
        /// <summary>
        /// Collection of Module / Adapter Information Objects.
        /// </summary>
        public List<CanPort> CANPorts { get; init; } = new List<CanPort>();
    }


    /// <summary>
    /// Can Port Info
    /// </summary>
    public class CanPort : ICanDeserializeBase
    {
        /// <summary>
        /// Adapter Index
        /// </summary>
        public uint Port { get; set; }

        /// <summary>
        /// Unique Path for this Adapter
        /// </summary>
        public string SocketCanInterfacePath { get; set; }

        /// <summary>
        /// Unique Path for this Adapter
        /// </summary>
        public string CoprocessorSerialPath { get; set; }

        /// <summary>
        /// Unique Path for this Adapter
        /// </summary>
        public string CoprocessorFirmwarePath { get; set; }
    }

}
