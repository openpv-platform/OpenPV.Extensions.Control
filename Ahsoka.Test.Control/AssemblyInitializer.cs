
using Ahsoka.Services.Can;
using Ahsoka.Services.IO;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Test.Control.Properties;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ahsoka.Test;

[TestClass]
public class TestInitializer : LinearTestBase
{
   [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        // Just forcing Libraries to Load
        var ioAsm = typeof(IOService).Assembly;
        var canAsm = typeof(CanService).Assembly;

        ClassLoader.AddAssembly(ioAsm);
        ClassLoader.AddAssembly(canAsm);

        // Fall back to Developer Support Folder if running Standalone.
        if (HardwareInfo.GetHardwareInfoDescriptions().Count == 0)
        {
            var hd = JsonUtility.Deserialize<HardwareInfo>(CanTestResources.WindowsHardwareConfiguration);
            HardwareInfo.AddHardwareInfo(hd);
        }

        Ahsoka.System.Extensions.LoadExtensions();

        /// Prep Windows for Tests
        var progress = new Progress<string>(Console.WriteLine);

        var prep = RemoteTargetToolFactory.GetToolsForPlatform(PlatformFamily.Windows64);

        // Load Hardware Info
        HardwareInfo hardware = HardwareInfo.GetHardwareInfo(PlatformFamily.Windows64, "Desktop");

        var info = new TargetConnectionInfo() { PlatformFamily = hardware.PlatformFamily, PlatformQualifier = hardware.PlatformQualifier, HostName = "localhost", UserName = "" };

        // Prep Local Machine
        bool returnValue = prep.Prep(info, null, progress);

    }

}
