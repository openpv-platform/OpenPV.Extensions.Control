using Ahsoka.Installer;
using Ahsoka.Services.Can;
using Ahsoka.System;
using Ahsoka.Utility;
using ELFSharp.MachO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class CanCommands
{
    [CommandLineMethod(@"     --GenerateCalibrationFromDBC: Generate Configuration File from DBC
            [PathToDBC] Path to OpenPV CAN Calibration File (.cancalibration.json)
            [PathToDestinationFile] Path to Output OpenPV CAN Calibration File (.cancalibration.json)")]
    private static void GenerateCalibrationFromDBC(string pathToDBC, string pathToDestinationFile)
    {
        CanMetadataTools.GenerateCalibrationFromDBC(pathToDBC, pathToDestinationFile);
    }

    [CommandLineMethod(@"     --GenerateCANClasses: Generate Model Classes from Configuration File
            [PathToPackageFile] Path to PackageInfoFile for the projecte (.PackageInfo.json
            [PathToOutputFolder] Folder where Class Definitions will be created
            [NameSpace] NameSpace for Output Classes
            [BaseClass] Base Class (or View Model) for Output Classes
            [Language] Dotnet or Cpp Supported")]
    private static void GenerateCANClasses(string pathToPackageFile, string pathToOutputFolder, string nameSpace, string baseClass, string language)
    {
        CanMetadataTools.GenerateMessageClasses(pathToPackageFile, pathToOutputFolder, nameSpace, baseClass, Enum.Parse<ApplicationType>(language));
    }
}

internal class CanCodeGenerator : IExtensionGenerator
{
    public void GetCommands(PackageInformation packageInfo, Dictionary<string, GeneratorCommandType> commandsToExecute, CommandTypes commandtypes)
    {
        string config = packageInfo.ServiceInfo.RuntimeConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName == "CAN Service Extension").ConfigurationFile;
        string configFile = Path.Combine(Path.GetDirectoryName(packageInfo.GetPackageInfoPath()), config);
        if (File.Exists(configFile) && commandtypes.HasFlag(CommandTypes.ModelGenerators))
        {
            var calibration = JsonUtility.Deserialize<CanClientCalibration>(File.ReadAllText(configFile));
            commandsToExecute.Add($"--GenerateCANClasses \"{Path.GetFileName(packageInfo.GetPackageInfoPath())}\" \"{calibration.GeneratorOutputFile}\" \"{calibration.GeneratorNamespace}\" \"{calibration.GeneratorBaseClass}\" {packageInfo.ApplicationType}",
                      GeneratorCommandType.AhsokaCommandLine);
        }
    }
}