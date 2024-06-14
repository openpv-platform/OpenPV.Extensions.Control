
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace Ahsoka.Test;

[TestClass]
public class TestInitializer : LinearTestBase
{
   [AssemblyInitialize]
    public static void AssemblyInit(TestContext context)
    {
        ProcessUtility.RunProcess("taskkill", "/im Ahsoka.CommandLine.exe /f",null, out string result, out string error);

        string platformSupportPath = PlatformSupportPathInfo.GetDeveloperToolPath();

        HardwareInfo.LoadHardwareInfo(platformSupportPath);

        Extensions.LoadExtensions();

        /// Prep Windows for Tests
        var progress = new Progress<string>(Console.WriteLine);

        var prep = RemoteTargetToolFactory.GetToolsForPlatform(PlatformFamily.Windows64);

        // Create Hardware Info
        HardwareInfo hardware = HardwareInfo.GetHardwareInfo(PlatformFamily.Windows64, "Desktop");

        var info = new TargetConnectionInfo() { PlatformFamily = hardware.PlatformFamily, PlatformQualifier = hardware.PlatformQualifier, HostName = "localhost", UserName = "" };

        bool returnValue = prep.Prep(info, null, progress);
    }

}
