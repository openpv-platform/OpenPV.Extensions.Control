using Ahsoka.Core;
using Ahsoka.Core.Hardware;
using Ahsoka.Core.Utility;
using Ahsoka.Installer.InstallEngine;
using Ahsoka.Services.Can;
using Ahsoka.Services.Install;
using Ahsoka.Utility;
using Mono.Unix;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Ahsoka.Installer.Components;

internal class CanInstallerComponent : InstallEngineComponent
{

    public const string applicationBinaryName = "coprocessorApplication.bin";
    public const string applicationConfiguration = "applicationConfiguration.pbuff";
    public const string coprocessorConfiguration = "coprocessorConfiguration.pbuff";
    public const string startScriptName = "start_can_helper.sh";

    public const string PackageName = "CANApplication";
    private const string CanExtensionName = "CAN Service Extension";
    public static readonly Guid CanApplicationComponentType = new("FB25736F-B9B7-4581-89D4-10238FC1CA71");

    public override Guid ComponentType => CanApplicationComponentType;

    public override string ComponentName => PackageName;

    public override List<PlatformFamily> SupportedPlatforms => [PlatformFamily.OpenViewLinux];

    protected override PackageComponent OnCreatePackageStream(string buildLocation,
        PackageInformation info,
         List<InstallerPlugin> installedPlugins,
        IProgress<PackageProgressInfo> progress = null)
    {
        var hardwareDef = HardwareInfo.GetHardwareInfo(info.PlatformFamily, info.PlatformQualifier);

        if (hardwareDef == null)
            return null;

        string config = info.ServiceInfo.RuntimeConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName == CanExtensionName)?.ConfigurationFile;
        if (!File.Exists(config))
        {
            string error = "A configuration file was not found for the CAN Service Extension.";
            progress.Report(new PackageProgressInfo() { Message = error });
            throw new ApplicationException(error);
        }

        PackageComponent component = new()
        {
            ComponentType = CanApplicationComponentType,
            Data = new MemoryStream(),
            Version = VersionUtility.GetAppVersionString()
        };

        // Create Archive
        using (ZipArchive archive = new(component.Data, ZipArchiveMode.Create, true))
        {
            // Generate a Configuration for the Main System
            CanApplicationConfiguration canConfig = CanMetadataTools.GenerateApplicationConfig(hardwareDef, config, false);
            var configData = new MemoryStream();
            ProtoBuf.Serializer.Serialize(configData, canConfig);
            var jsonData = Encoding.UTF8.GetBytes(JsonUtility.Serialize(canConfig));
            AddFileToArchive(archive, applicationConfiguration, configData);
            AddFileToArchive(archive, applicationConfiguration + ".json", jsonData);

            // Determine if we are a CoProcessor App or Not
            bool hasCoProcessor = canConfig.CanPortConfiguration.MessageConfiguration.Ports.Any(x => x.CanInterface == CanInterface.Coprocessor);
            bool hasSocket = canConfig.CanPortConfiguration.MessageConfiguration.Ports.Any(x => x.CanInterface == CanInterface.SocketCan);

            // All Configurations must Match Currently.
            if (hasCoProcessor == hasSocket)
            {
                string error = "All Can Ports must be either Socket CAN or CoProcessor.";
                progress.Report(new PackageProgressInfo() { Message = error });
                throw new ApplicationException(error);
            }

            foreach (var item in canConfig.CanPortConfiguration.MessageConfiguration.Ports)
            {
                bool hasSelf = canConfig.CanPortConfiguration.MessageConfiguration.Nodes.Any(x => x.Port == item.Port && x.NodeType == NodeType.Self);
                bool isRaw = item.PromiscuousTransmit && item.PromiscuousReceive;
                if (!hasSelf && !isRaw)
                {
                    string error = $"Please add a 'Self' Node for Port {item.Port} or set PromiscuousTransmit / PromiscuousReceive to true if you are handling all CAN messages in your application.";
                    throw new ApplicationException(error);
                }
            }

            if (hasCoProcessor)
            {
                // Generate a LITE Configuration for the CoProcessor
                canConfig = CanMetadataTools.GenerateApplicationConfig(hardwareDef, config, true);
                var coProcessorConfigData = new MemoryStream();
                ProtoBuf.Serializer.Serialize(coProcessorConfigData, canConfig);
                jsonData = Encoding.UTF8.GetBytes(JsonUtility.Serialize(canConfig));
                AddFileToArchive(archive, coprocessorConfiguration, coProcessorConfigData);
                AddFileToArchive(archive, coprocessorConfiguration + ".json", jsonData);

                // Fetch Application Binary from Support Folder
                var appData = new MemoryStream(File.ReadAllBytes(Path.Combine(info.GetPlatformExtensionFolder(info.PlatformFamily, CanExtensionName), "Coprocessor_Firmware.elf")));

                ModifyApp(coProcessorConfigData, appData);

                AddFileToArchive(archive, applicationBinaryName, appData);

                // Create Start Script
                string start = Properties.CANResources.Ahsoka_Can_Start;
                string pathToInterface = canConfig.CanPortConfiguration.MessageConfiguration.Ports.First().CanInterfacePath;
                string localIPAddress = canConfig.CanPortConfiguration.CommunicationConfiguration.LocalIpAddress;
                string remoteIPAddress = canConfig.CanPortConfiguration.CommunicationConfiguration.RemoteIpAddress;
                start = start.Replace("${InterfacePath}", pathToInterface).Replace("${IpAddressLocal}", localIPAddress).Replace("${IpAddressRemote}", remoteIPAddress);
                AddFileToArchive(archive, startScriptName, new MemoryStream(UTF8Encoding.UTF8.GetBytes(start)));
            }
        }

        component.Data.Seek(0, SeekOrigin.Begin);

        return component;
    }

    private void ModifyApp(MemoryStream coprocessorConfiguration, MemoryStream appData)
    {
        // Look up the section we will write.
        var elfData = ELFSharp.ELF.ELFReader.Load(appData, false);
        foreach (var item in elfData.Sections)
        {
            ELFSharp.ELF.Sections.Section<uint> progSection = item as ELFSharp.ELF.Sections.Section<uint>;
            if (item.Name == ".calibration_data")
            {
                var sectionLocation = (int)progSection.Offset;
                var sectionSize = (int)progSection.Size;
                var data = new byte[sectionSize];

                if (coprocessorConfiguration.Length > sectionSize)
                {
                    string error = $"The coprocessor calibration exceeds the size allocated in the Coprocessor Application - Available space is {sectionSize}.";
                    throw new ApplicationException(error);
                }

                // Write Configuration to Area
                coprocessorConfiguration.Seek(0, SeekOrigin.Begin);
                appData.Seek(sectionLocation, SeekOrigin.Begin);
                coprocessorConfiguration.CopyTo(appData);

                // Verify
                var sectionData = progSection.GetContents();
                var coprocessorConfigurationArray = coprocessorConfiguration.ToArray();
                for (int i = 0; i < coprocessorConfigurationArray.Length; i++)
                {
                    if (coprocessorConfigurationArray[i] != sectionData[i])
                        throw new ApplicationException("Error occured adding calibration data.  Data did not match app data.");
                }
            }
        }
    }

    private static void AddFileToArchive(ZipArchive archive, string name, MemoryStream configData)
    {
        AddFileToArchive(archive, name, configData.ToArray());
    }

    private static void AddFileToArchive(ZipArchive archive, string name, byte[] configData)
    {
        var entry = archive.CreateEntry(name);
        using var entryStream = entry.Open();
        using var streamWriter = new BinaryWriter(entryStream);
        streamWriter.Write(configData);
    }

    protected override void OnCleanup(string buildLocation, PackageComponent componentData, PackageInformation info, IProgress<PackageProgressInfo> progress = null)
    {

    }

    protected override InstallResult OnInstallComponent(InstallContext context, PackageComponent item)
    {
        try
        {

            string pathToStartScript = Path.Combine(SystemInfo.HardwareInfo.FactoryInfo.RootPath, startScriptName);
            if (File.Exists(pathToStartScript))
                File.Delete(pathToStartScript);

            string coprocessorPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.CoProcessorApplicationPath);

            context.Log?.Report(new InstallLogInfo() { LogMessage = $"Component '{ComponentName}' Began Extraction", LogMessageType = LogMessageType.Information });

            if (Directory.Exists(coprocessorPath))
                Directory.Delete(coprocessorPath, true);

            using (var ms = new MemoryStream())
            {
                var canInfo = CANHardwareInfoExtension.GetCanInfo(SystemInfo.HardwareInfo.PlatformFamily);

                context.InstallEngine.ExtractPackage(this.ComponentType, ms, context.Progress);

                ms.Seek(0, SeekOrigin.Begin);
                using ZipArchive archive = new(ms, ZipArchiveMode.Update, false);
                archive.ExtractToDirectory(coprocessorPath, true);

                // Copy firmware to /lib/firmware
                string appPath = Path.Combine(coprocessorPath, applicationBinaryName);
                string firmwarePath = canInfo.CANPorts.First().CoprocessorFirmwarePath;

                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Registering Firmware at {appPath}" });

                if (File.Exists(firmwarePath))
                    File.Delete(firmwarePath);

                if (firmwarePath != null && appPath != null)
                    File.CreateSymbolicLink(firmwarePath, appPath);
            }

            // Enable or Disable The CoProcessor / Socket CAN Interfacees.
            string pathToApplication = Path.Combine(coprocessorPath, applicationBinaryName);
            string enableCommand = File.Exists(pathToApplication) ? CanInterface.Coprocessor.ToString().ToLower() : CanInterface.SocketCan.ToString().ToLower();

            // Stops the coprocessor if running
            if (File.Exists("/sys/class/remoteproc/remoteproc0/state"))
                ProcessUtility.RunProcessScript("echo stop > /sys/class/remoteproc/remoteproc0/state", null, out string sOut, out string sErr);

            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Enabling {enableCommand} Application" });
            ProcessUtility.RunProcessScript($"assign-can.sh {enableCommand}", null, out string stdOut, out string stdErr);
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Enabled {enableCommand} Application - {stdOut} - {stdErr}" });

            string pathToConfig = Path.Combine(coprocessorPath, applicationConfiguration);
            if (File.Exists(pathToConfig))
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Installed Coprocessor Application at {pathToApplication} {File.Exists(pathToApplication)}" });
            else
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Failed to Install Coprocessor Application at {pathToApplication}" });

            // Enable Script If it exists
            string pathToNewStartScript = Path.Combine(coprocessorPath, startScriptName);
            if (File.Exists(pathToNewStartScript))
            {
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Enabling CAN Start Script" });
                File.Copy(pathToNewStartScript, pathToStartScript);

                // Assign Permissions.
                if (!OperatingSystem.IsWindows())
                    new UnixFileInfo(pathToStartScript).FileAccessPermissions |= FileAccessPermissions.UserExecute;
            }

            // Set Baud Rate of Ports
            using var fs = new FileStream(pathToConfig, FileMode.Open);
            fs.Seek(0, SeekOrigin.Begin);
            var calibration = ProtoBuf.Serializer.Deserialize<CanApplicationConfiguration>(fs);
            foreach (var port in calibration.CanPortConfiguration.MessageConfiguration.Ports)
            {
                string rate = "";
                switch (port.BaudRate)
                {
                    case CanBaudRate.Baud1mb:
                        rate = "1000000";
                        break;
                    case CanBaudRate.Baud500kb:
                        rate = "500000";
                        break;
                    default:
                        rate = "250000";
                        break;
                }

                // Update Baud Rate in Network Setup
                context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = $"Setting Baud Rate on can{port.Port}" });
                ProcessUtility.RunProcessScript($"sed -i -r \"s/(BitRate=[0-9]*)/BitRate={rate}/g\" /etc/systemd/network/80-can{port.Port}.network", null, out stdOut, out stdErr);

                // Add SyncJumpWidth to Network Setup - This (sjw=4) fixes devices who's baud rate clock is not within tolerance of our clock.
                ProcessUtility.RunProcessScript($"grep -qxF 'SyncJumpWidth=4' /etc/systemd/network/80-can{port.Port}.network || echo 'SyncJumpWidth=4' >> /etc/systemd/network/80-can{port.Port}.network", null, out stdOut, out stdErr);
            }
        }
        catch (Exception ex)
        {
            context.Log?.Report(new InstallLogInfo() { LogMessageType = LogMessageType.Information, LogMessage = ex.Message + ex.StackTrace });
            return InstallResult.ExceptionThrownDuringInstall;
        }

        return InstallResult.Success;
    }
}
