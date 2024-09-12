#pragma warning disable CS0618 // Type or member is obsolete
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Can.Messages;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using DbcParserLib;
using DbcParserLib.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ahsoka.Services.Can;

/// <summary>
/// Services for Managing Communications on a CAN Network via an outboard Real Time CAN Gateway
/// </summary>
internal static class CanMetadataTools
{
    public const string CanConfigurationExtension = "CANServiceConfiguration.json";

    internal static CanApplicationConfiguration GenerateApplicationConfig(HardwareInfo hardwareInfo, string config, bool trimStrings)
    {
        var canInfo = CANHardwareInfoExtension.GetCanInfo(hardwareInfo.PlatformFamily);

        CanApplicationConfiguration appConfig = new();
        try
        {
            var clientConfiguration = ConfigurationFileLoader.LoadFile<CanClientConfiguration>(config);

            CanPortConfiguration portConfiguration = new()
            {
                CommunicationConfiguration = new(),
                MessageConfiguration = new(),
                DiagnosticEventConfiguration = new(),
            };

            portConfiguration.MessageConfiguration.Ports.AddRange(clientConfiguration.Ports);
            portConfiguration.MessageConfiguration.Nodes.AddRange(clientConfiguration.Nodes);
            portConfiguration.MessageConfiguration.Messages.AddRange(clientConfiguration.Messages);
            portConfiguration.DiagnosticEventConfiguration.DiagnosticEvents.AddRange(clientConfiguration.DiagnosticEvents);

            CanHandler.Generate(portConfiguration);

            // Set Interface Specific Items (CoProcessor is on an IP Path with TTY Path as the Interface.
            if (clientConfiguration.Ports.First().CanInterface == CanInterface.Coprocessor)
            {
                portConfiguration.MessageConfiguration.Ports.First().CanInterfacePath = canInfo.CANPorts.First().CoprocessorSerialPath;
                portConfiguration.CommunicationConfiguration.LocalIpAddress = clientConfiguration.LocalIpAddress ?? "192.168.0.2";
                portConfiguration.CommunicationConfiguration.RemoteIpAddress = clientConfiguration.RemoteIpAddress ?? "192.168.0.1";
            }

            if (trimStrings)
            {
                foreach (var node in portConfiguration.MessageConfiguration.Nodes)
                {
                    node.Comment = node.Name = null;
                    switch (node.TransportProtocol)
                    {
                        case TransportProtocol.Raw:
                            node.J1939Info = null;
                            node.IsoInfo = null;
                            break;

                        case TransportProtocol.IsoTp:
                            node.J1939Info = null;
                            break;

                        case TransportProtocol.J1939:
                            node.IsoInfo = null;
                            break;
                    }
                }

                foreach (var message in portConfiguration.MessageConfiguration.Messages)
                {
                    message.Comment = message.Name = null;
                    message.Signals.Clear();
                }
            }

            foreach(var dm in portConfiguration.DiagnosticEventConfiguration.DiagnosticEvents)
            {
                dm.Name = dm.Comment = null;
            }

            appConfig.CanPortConfiguration = portConfiguration;
        }

        catch { Console.WriteLine($"Unable to read can calibration {config}"); }
        

        return appConfig;

    }

    internal static void GenerateCalibrationFromDBC(string canDBCFile, string canConfigurationOutputPath)
    {
        CanClientConfiguration configuration = CreateFromDBC(canDBCFile);
        configuration.Name = Path.GetFileName(canConfigurationOutputPath).Replace(CanConfigurationExtension, "");
        configuration.Ports.Add(new PortDefinition()
        {
            Port = 1,
            CanInterfacePath = "can1",
            CanInterface = CanInterface.Coprocessor,
            BaudRate = CanBaudRate.Baud250kb,
            PromiscuousReceive = false,
            PromiscuousTransmit = false,
            UserDefined = false,

        });        
        configuration.RemoteIpAddress = "192.168.8.1";
        configuration.LocalIpAddress = "192.168.8.2";
        configuration.Version = VersionUtility.GetAppVersionString();
        File.WriteAllText(canConfigurationOutputPath, JsonUtility.Serialize(configuration));
    }

    private static CanClientConfiguration CreateFromDBC(string pathToCanDbc)
    {
        CanClientConfiguration clientCalibration = new()
        {
            Version = VersionUtility.GetAppVersionString()
        };

        var dbc = Parser.ParseFromPath(pathToCanDbc);

        int nodeID = 0;

        // Generate Nodes and Align
        Dictionary<string, NodeDefinition> nodes = new();

        // Update Comments
        foreach (var node in dbc.Nodes)
        {
            var newNode = new NodeDefinition()
            {
                Comment = node.Comment,
                Name = node.Name,
                Id = nodeID,
                NodeType = NodeType.UserDefined,
                Port = 1,
                J1939Info = new J1939NodeDefinition() { AddressType = NodeAddressType.Static, AddressValueOne = 0 }
            };

            clientCalibration.Nodes.Add(newNode);
            nodes[newNode.Name] = newNode;

            nodeID++;
        }

        // Add Messages and Check for Node.
        foreach (var message in dbc.Messages)
        {
            bool nodeFound = nodes.TryGetValue(message.Transmitter, out NodeDefinition node);

            var messageDef = new MessageDefinition()
            {
                Comment = message.Comment,
                Dlc = message.DLC,
                Id = message.ID,
                MessageType = message.IsExtID ? MessageType.RawExtendedFrame : MessageType.RawStandardFrame,
                Name = message.Name,
                UserDefined = true,
                TransmitNodes = nodeFound ? new int[] {-1, node.Id } : new int[] { -1, -1 },
                ReceiveNodes = new int[] { -1, -1 },
                OverrideSourceAddress = false
            };

            if (message.CycleTime(out int cycleTime))
                messageDef.Rate = cycleTime;
             
            if (message.IsExtID)
            {
                bool isJ1939 = message.Signals.SelectMany(x => x.CustomProperties).Any(x => x.Key == "SPN");
                if (isJ1939)
                {
                    messageDef.MessageType = MessageType.J1939ExtendedFrame;
                    messageDef.OverrideSourceAddress = true;
                    messageDef.Id &= ~(0xFFu); // Remove Address
                }
            }

            clientCalibration.Messages.Add(messageDef);

            // Generate All Signals
            foreach (var signal in message.Signals)
            {


                var signalDef = new MessageSignalDefinition()
                {
                    BitLength = signal.Length,
                    StartBit = signal.StartBit,
                    ByteOrder = signal.ByteOrder == 0 ? ByteOrder.BigEndian : ByteOrder.LittleEndian,
                    DefaultValue = signal.InitialValue,
                    Id = signal.ID,
                    Maximum = signal.Maximum,
                    Minimum = signal.Minimum,
                    Name = signal.Name,
                    Offset = signal.Offset,
                    Scale = signal.Factor,
                    Unit = signal.Unit,
                    Comment = signal.Comment,
                };

                var spnVal = signal.CustomProperties.FirstOrDefault(x => x.Key == "SPN");
                if (spnVal.Value?.IntegerCustomProperty != null)
                    signalDef.Id = (uint)spnVal.Value.IntegerCustomProperty.Value;

                switch (signal.ValueType)
                {
                    case DbcValueType.Signed:
                        signalDef.ValueType = ValueType.Signed;
                        break;
                    case DbcValueType.Unsigned:
                        signalDef.ValueType = ValueType.Unsigned;
                        break;
                    case DbcValueType.IEEEFloat:
                        signalDef.ValueType = ValueType.Float;
                        break;
                    case DbcValueType.IEEEDouble:
                        signalDef.ValueType = ValueType.Double;
                        break;
                    default:
                        break;
                }

                if (!String.IsNullOrEmpty(signal.Multiplexing))
                {
                    var data = signal.MultiplexingInfo();
                    signalDef.MuxGroup = (uint)data.Group;
                    signalDef.MuxRole = (data.Role == MultiplexingRole.Multiplexor ? MuxRole.Multiplexor : MuxRole.Multiplexed);
                }
                else
                {
                    signalDef.MuxGroup = 0;
                    signalDef.MuxRole = MuxRole.NotMultiplexed;
                }

                // Generate Valid Values table
                if (signal.ValueTableMap != null)
                {
                    foreach (var item in signal.ValueTableMap)
                        signalDef.Values[item.Key] = item.Value.Trim('"');
                }
                /*
                if (signal.ValueTable != null)
                {
                    foreach (var item in signal.ValueTable.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        string key = item.Substring(0, item.IndexOf(' '));
                        string value = item.Substring(item.IndexOf(" ")).Replace("\"", "").Trim();
                        signalDef.Values[int.Parse(key)] = value.Trim('"');
                    }
                }*/


                // Generate Reciever Nodes
                if (signal.Receiver != null)
                {
                    List<int> recieveNodes = new();
                    foreach (var nodeName in signal.Receiver)
                        if (nodes.TryGetValue(nodeName, out NodeDefinition receiveNode))
                            recieveNodes.Add(receiveNode.Id);

                    // Default Node Not Added.
                    if (recieveNodes.Count > 0)
                        signalDef.ReceiverNodeIds = recieveNodes.ToArray();
                }

                messageDef.Signals.Add(signalDef);
            }
        }

        return clientCalibration;
    }

    internal static void GenerateMessageClasses(string pathToPackageInfo, string pathToDestinationFile, string nameSpace, string baseClass = null, ApplicationType type = ApplicationType.Dotnet)
    {
        Console.WriteLine($"Reading Package Info at {pathToPackageInfo}");
        Console.WriteLine();

        var generatorExtensions = new GeneratorUtility("Ahsoka.Core.Can", type); 
        
        string pathToSearch = string.Empty;
        if (Environment.CurrentDirectory != Path.GetDirectoryName(pathToPackageInfo))
            pathToSearch = Path.GetDirectoryName(pathToPackageInfo);

        PackageInformation package = PackageInformation.LoadPackageInformation(pathToPackageInfo);

        // Create Directory if Missing
        string directory = Path.GetDirectoryName(pathToDestinationFile);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string canCalibration = package.ServiceInfo.RuntimeConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName == "CAN Service Extension")?.ConfigurationFile;
        if (String.IsNullOrEmpty(canCalibration))
        {
            Console.WriteLine($"Config does not contain a CAN configuration file");
            return;
        }

        var calPath = Path.Combine(pathToSearch, canCalibration);
        if (!File.Exists(calPath))
        {
            Console.WriteLine($"Could not find file {calPath} to process");
            return;
        }

        StringBuilder outputFileData = new();

        if (type == ApplicationType.Dotnet)
            GenerateDotNet(nameSpace, baseClass, calPath, outputFileData, generatorExtensions);

        else if (type == ApplicationType.Cpp)
        {
            string fileName = Path.Combine(Path.GetDirectoryName(pathToDestinationFile), Path.GetFileNameWithoutExtension(pathToDestinationFile) + ".cpp");
            StringBuilder outputFileDataCPP = new();

            GenerateCPP(nameSpace, baseClass, calPath, outputFileData, outputFileDataCPP, pathToDestinationFile, generatorExtensions);
            File.WriteAllText(fileName, outputFileDataCPP.ToString());

            var path = Path.GetDirectoryName(pathToDestinationFile);
            File.WriteAllText(Path.Combine(path, "CanPropertyInfo.h"), Properties.CANResources.CanPropertyInfo);
            File.WriteAllText(Path.Combine(path, "CanViewModelBase.h"), Properties.CANResources.CanViewModelBase);
            File.WriteAllText(Path.Combine(path, "CANProtocolHelper.h"), Properties.CANResources.CANProtocolHelper);
            File.WriteAllText(Path.Combine(path, "J1939Helper.h"), Properties.CANResources.J1939Helper);
        }

        File.WriteAllText(pathToDestinationFile, outputFileData.ToString());
    }

    #region Generate CPP
    private static void GenerateCPP(string nameSpace, string baseClass, string pathToCalibration, StringBuilder outputFileData, StringBuilder outputFileDataCPP, string headerFileName, GeneratorUtility generatorExtensions)
    {
        // Add Header
        outputFileData.AppendLine("#pragma once");
        outputFileData.AppendLine();
        outputFileData.AppendLine("#include <string>");
        outputFileData.AppendLine("#include \"AhsokaServices.h\"");
        outputFileData.AppendLine("#include \"CanViewModelBase.h\"");

        // Allow Plugin to Extend the Header Using / Import Statements
        generatorExtensions.ExtendHeader(outputFileData, nameSpace, true);

        outputFileData.AppendLine();
        outputFileData.Append($"namespace {nameSpace}\r\n{{");

        // Add Header
        outputFileDataCPP.AppendLine("#include <string>");
        outputFileDataCPP.AppendLine($"#include \"{Path.GetFileName(headerFileName)}\"");
        outputFileDataCPP.AppendLine();

        // Allow Plugin to Extend the Header Using / Import Statements
        generatorExtensions.ExtendHeader(outputFileDataCPP, nameSpace, false);

        outputFileDataCPP.Append($"namespace {nameSpace}\r\n{{");

        StringBuilder metadataBuilder = new();
        StringBuilder metadataAccessors = new();
        StringBuilder metadataCreator = new();

        
        var clientCalibration = ConfigurationFileLoader.LoadFile<CanClientConfiguration>(pathToCalibration);
        foreach (var item in clientCalibration.Messages)
        {
            // Create Message Class
            try
            {
                GenerateClassCpp(item, outputFileData, metadataBuilder, metadataCreator, metadataAccessors, outputFileDataCPP, baseClass, generatorExtensions);
                outputFileData.AppendLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Importing Item: {item} with error {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
               

        outputFileDataCPP.AppendLine();
        outputFileDataCPP.AppendLine($"\t// Start of Class Implmentation for CanModelMetadata");
        outputFileData.AppendLine(Properties.CANResources.CPPMetadataDictionary);

        // Add Metadata Implementations
        if (metadataBuilder.Length > 0)
        {
            outputFileDataCPP.AppendLine(Properties.CANResources.CPPMetadataDictionaryIMPL
                .Replace("%METADATA_BODY%", metadataBuilder.ToString()).Replace("%METADATA_BUILDER%", metadataCreator.ToString()));

            outputFileDataCPP.AppendLine($"\t// Metadata Accessors");
            outputFileDataCPP.AppendLine(metadataAccessors.ToString());
        }

        outputFileDataCPP.AppendLine("}"); // Close Namespace.
        outputFileData.AppendLine("}"); // Close Namespace.
    }

    private static void GenerateClassCpp(MessageDefinition definition, StringBuilder fileOutputBuilder, StringBuilder metadataBuilder, StringBuilder metadataCreator, StringBuilder metadataAccessors, StringBuilder outputFileDataCPP, string baseClass = null, GeneratorUtility generatorExtensions = null)
    {
        StringBuilder enumBuilder = new();
        StringBuilder propBuilder = new();
        StringBuilder propEnumBuilder = new();

       
        baseClass ??= "CanViewModelBase";
        definition.Id &= 0x1FFFFFFF;

        string className = definition.Name.AsIdentifier();

        outputFileDataCPP.AppendLine();
        outputFileDataCPP.AppendLine($"\t//Start of Class Implmentation for {className}");
        if(definition.MessageType == MessageType.J1939ExtendedFrame)
        {
            outputFileDataCPP.AppendLine();
            outputFileDataCPP.AppendLine($"\tJ1939Helper& TestMessage::GetProtocol() {{ return protocol; }}");
        }

        metadataBuilder.AppendLine($"\t\t//Add Props for {className} - CANID: {definition.Id}");
        metadataBuilder.AppendLine($"\t\tstd::map<int, CanPropertyInfo> {className}Props;");

        metadataCreator.AppendLine($"\t\t\tcase {definition.Id}:");
        metadataCreator.AppendLine($"\t\t\t\treturn unique_ptr<{className}>(new {className}(data));");

        // Add Props First
        foreach (MessageSignalDefinition signalDefinition in definition.Signals)
            WriteMessagePropertyCPP(propEnumBuilder, propBuilder, enumBuilder, metadataBuilder, outputFileDataCPP, signalDefinition, className, generatorExtensions);

        metadataBuilder.AppendLine($"\t\tmetadata[{definition.Id}] = {className}Props;");

        // Add Enum for Class Class
        if (enumBuilder.Length > 0)
            fileOutputBuilder.AppendLine(enumBuilder.ToString());

        // Class Header
        fileOutputBuilder.AppendLine($"\tclass {className} : public {baseClass}");
        fileOutputBuilder.AppendLine("\t{");
        fileOutputBuilder.AppendLine("\t\tpublic:\r\n");

        // Generate Property Enumeration
        if (propEnumBuilder.Length > 0)
        {
            fileOutputBuilder.Append("\t\t\tenum Properties\r\n\t\t\t{");
            fileOutputBuilder.AppendLine(propEnumBuilder.ToString());
            fileOutputBuilder.Append("\t\t\t};\r\n");
        }

        // Calculate Length From Signals
        uint byteLength = 0;
        if (definition.Signals.Count > 0)
        {
            var lastSignal = definition.Signals.OrderBy(x => x.StartBit).Last();
            byteLength = (uint)Math.Ceiling((lastSignal.StartBit + lastSignal.BitLength) / 8.0f);
        }

        // Public Methods
        fileOutputBuilder.AppendLine();
        fileOutputBuilder.AppendLine($"\t\t\t{className}(); // CANID: 0x{definition.Id.ToString("x")}");
        fileOutputBuilder.AppendLine($"\t\t\t{className}(CanMessageData message); // CANID: 0x{definition.Id.ToString("x")}");
        if (definition.MessageType == MessageType.J1939ExtendedFrame)
        {
            fileOutputBuilder.AppendLine();
            fileOutputBuilder.AppendLine($"\t\t\tJ1939Helper& GetProtocol();");
        }
        
        // Extend Header Methods
        generatorExtensions.ExtendMethods(fileOutputBuilder, className, true);

        outputFileDataCPP.AppendLine();
        outputFileDataCPP.AppendLine($"\t{className}::{className}() : {baseClass}({definition.Id},{byteLength})  // CANID: 0x{definition.Id.ToString("x")}");
        outputFileDataCPP.AppendLine($"\t{{");
        generatorExtensions.ExtendConstructor(outputFileDataCPP,className);
        outputFileDataCPP.AppendLine($"\t}}");

        outputFileDataCPP.AppendLine($"\t{className}::{className}(CanMessageData message) : {baseClass}(message)  // CANID: 0x{definition.Id.ToString("x")}");
        outputFileDataCPP.AppendLine($"\t{{");
        generatorExtensions.ExtendConstructor(outputFileDataCPP, className);
        outputFileDataCPP.AppendLine($"\t}}");

        // Extend Header outputFileDataCPP
        generatorExtensions.ExtendMethods(outputFileDataCPP, className, false);

        // Generate Property Enumeration
        if (propBuilder.Length > 0)
            fileOutputBuilder.AppendLine(propBuilder.ToString());

       
        // Generate Property Enumeration
        metadataBuilder.AppendLine("");
        metadataAccessors.AppendLine($"\tstd::map<int, CanPropertyInfo>& {className}::GetMetadata() {{ return CanModelMetadata::CanMetadata()->GetMetadata({definition.Id}); }}");

        fileOutputBuilder.AppendLine("\t\tprotected:");
        fileOutputBuilder.AppendLine("\r\n\t\t\tJ1939Helper protocol = J1939Helper(message);");
        fileOutputBuilder.AppendLine("\t\t\tstd::map<int, CanPropertyInfo>& GetMetadata();");

        // Finish Main
        fileOutputBuilder.AppendLine("\t};"); // End Class

        generatorExtensions.ExtendAfterClassOutput(fileOutputBuilder,className);
    }

    private static void WriteMessagePropertyCPP(StringBuilder propEnum, StringBuilder propMethods, StringBuilder enumOutput, StringBuilder metadata, StringBuilder outputFileDataCPP, MessageSignalDefinition definition, string className, GeneratorUtility generatorUtility)
    {
        string propName = definition.Name.AsIdentifier();

        if (propName.StartsWith(className))
            propName = propName.Substring(className.Length);

        string type;
        string canType;

        if (definition.Values.Count > 0)
        {
            // Generate an Enum
            canType = "ValueType::Enum";
            type = propName + "Values";

            enumOutput.AppendLine();
            enumOutput.AppendLine($"\tenum {type}\r\n\t{{");

            // Find Last Key
            int last = definition.Values.Max(x => x.Key);
            foreach (var item in definition.Values.OrderBy(x => x.Key))
            {
                string enumValue = item.Value.AsIdentifier();
                if (enumValue.StartsWith(className))
                    enumValue = enumValue.Substring(className.Length);

                enumOutput.AppendLine($"\t\t{enumValue.AsIdentifier()}={item.Key}{(item.Key == last ? "" : ",")}");
            }

            enumOutput.AppendLine("\t};");
        }
        else
        {
            switch (definition.ValueType)
            {
                case ValueType.Unsigned:
                    canType = "ValueType::Unsigned";
                    type = "uint";
                    break;
                case ValueType.Float:
                    canType = "ValueType::Float";
                    type = "float";
                    break;
                case ValueType.Double:
                    canType = "ValueType::Double";
                    type = "double";
                    break;
                case ValueType.Signed:
                default:
                    canType = "ValueType::Signed";
                    type = "int";
                    break;
            }
        }

        propEnum.Append($"\r\n\t\t\t\t{propName},");

        propMethods.AppendLine();
        propMethods.AppendLine($"\t\t\t{type} Get{propName}();");
        propMethods.AppendLine($"\t\t\tvoid Set{propName}({type} value);");

        outputFileDataCPP.AppendLine();
        outputFileDataCPP.AppendLine($"\t{type} {className.AsIdentifier()}::Get{propName}() {{ return OnGetValue<{type}>({propName}); }}");
        outputFileDataCPP.Append($"\tvoid {className.AsIdentifier()}::Set{propName}({type} value) {{ OnSetValue<{type}>(value, {propName});");
        generatorUtility.ExtendSetter(outputFileDataCPP, propName, type);
        outputFileDataCPP.AppendLine("}");

        // Add Metadata to Static Dictionary.
        string endianness = definition.ByteOrder == ByteOrder.LittleEndian ? "ByteOrder::LittleEndian" : "ByteOrder::BigEndian";
        metadata.Append($"\t\t{className.AsIdentifier()}Props[{className.AsIdentifier()}::Properties::{propName}] =  CanPropertyInfo({definition.StartBit}, {definition.BitLength}, {endianness}, {canType}, {definition.Scale}, {definition.Offset}, {definition.Id}, {definition.DefaultValue}, {definition.Minimum}, {definition.Maximum});\r\n");
    }
    #endregion

    #region Generate Dotnet
    private static void GenerateDotNet(string nameSpace, string baseClass, string pathToCalibration, StringBuilder outputFileData, GeneratorUtility generatorExtensions)
    {
        // Add Header
        outputFileData.AppendLine("using Ahsoka.System;");
        outputFileData.AppendLine("using Ahsoka.Services.Can;");
        outputFileData.AppendLine("using System.Collections.Generic;");
        outputFileData.AppendLine("using System;");
        outputFileData.AppendLine("using System.Linq;");
        outputFileData.AppendLine("using ValueType = Ahsoka.Services.Can.ValueType;");

        // Allow Plugin to Extend the Header Using / Import Statements
        generatorExtensions.ExtendHeader(outputFileData, nameSpace);
       
        outputFileData.AppendLine();
        outputFileData.AppendLine($"namespace {nameSpace};"); //need generic solution here
        outputFileData.AppendLine();

        StringBuilder metadataBuilder = new();
        StringBuilder metadataItems = new();
        
        var clientCalibration = ConfigurationFileLoader.LoadFile<CanClientConfiguration>(pathToCalibration);

        foreach (var item in clientCalibration.Messages)
        {
            // Create Message Class
            try
            {
                GenerateClassDotNet(item, outputFileData, metadataBuilder, metadataItems, baseClass, generatorExtensions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Importing Item: {item} with error {ex.Message}");
                throw new Exception(ex.Message);
            }
        }     

        // Generate Metadata Class
        outputFileData.AppendLine(Properties.CANResources.CSharpMetadataDictionary.Replace("%METADATA_BODY%", metadataItems.ToString()).Replace("%METADATA_BUILDER%", metadataBuilder.ToString()));
    }

    private static void GenerateClassDotNet(MessageDefinition definition, StringBuilder fileOutputBuilder, StringBuilder metadataBuilder, StringBuilder metadataItems, string baseClass = null, GeneratorUtility generatorExtensions = null)
    {
        baseClass ??= "CanViewModelBase";
        definition.Id &= 0x1FFFFFFF;

        string className = definition.Name.AsIdentifier().ToCamelCase();

        // Class Header
        fileOutputBuilder.AppendLine();
        fileOutputBuilder.AppendLine($"public partial class {className} : {baseClass}");
        fileOutputBuilder.AppendLine("{");

        // Calculate Length From Signals
        uint byteLength = 0;
        if (definition.Signals.Count > 0)
        {
            var lastSignal = definition.Signals.OrderBy(x => x.StartBit).Last();
            byteLength = (uint)Math.Ceiling((lastSignal.StartBit + lastSignal.BitLength) / 8.0f);
        }

        fileOutputBuilder.AppendLine($"\tpublic const uint CanId = {definition.Id}; // CANID: 0x{definition.Id.ToString("x")}");
        fileOutputBuilder.AppendLine($"\tpublic const int Dlc = {byteLength};");

        fileOutputBuilder.AppendLine("\t#region Auto Generated Properties");
        if (definition.MessageType == MessageType.J1939ExtendedFrame)
        {
            fileOutputBuilder.AppendLine("\tJ1939Helper protocol;");
            fileOutputBuilder.AppendLine("\tpublic J1939Helper Protocol");
            fileOutputBuilder.AppendLine("\t{");
            fileOutputBuilder.AppendLine("\t\tget");
            fileOutputBuilder.AppendLine("\t\t{");
            fileOutputBuilder.AppendLine("\t\t\tif (protocol == null)");
            fileOutputBuilder.AppendLine("\t\t\t\tprotocol = new J1939Helper(message);");
            fileOutputBuilder.AppendLine("\t\t\treturn protocol;");
            fileOutputBuilder.AppendLine("\t\t}");
            fileOutputBuilder.AppendLine("\t}");
            fileOutputBuilder.AppendLine();
        }

        // Method Header
        StringBuilder methodBuilder = new();
        methodBuilder.AppendLine();
        methodBuilder.AppendLine("\t#region Auto Generated Methods");
        methodBuilder.AppendLine($"\tprotected override Dictionary<string, CanPropertyInfo> GetMetadata() {{ return CanModelMetadata.EventMetadata.GetMetadata(CanId); }}");
        methodBuilder.AppendLine($"\tpublic {className}() : base(CanId, Dlc)");
        methodBuilder.AppendLine($"\t{{");

        generatorExtensions.ExtendConstructor( methodBuilder, className); 

        methodBuilder.AppendLine($"\t}} ");
        methodBuilder.AppendLine($"\tpublic {className}(CanMessageData message) : base(message) ");
        methodBuilder.AppendLine($"\t{{");
        
        generatorExtensions.ExtendConstructor(methodBuilder, className);
        
        methodBuilder.AppendLine($"\t}}\r\n");

        metadataBuilder.AppendLine($"\t\t\tcase {className}.CanId:");
        metadataBuilder.AppendLine($"\t\t\t\treturn new {className}(data);");
        metadataItems.AppendLine($"\r\n\t\t// Decode Info for {className} {definition.Id}");
        metadataItems.AppendLine($"\t\tmetadata.Add({definition.Id}, new Dictionary<string, CanPropertyInfo>()");
        metadataItems.AppendLine("\t\t{");

        // Enum Builder
        StringBuilder enumBuilder = new();

        // Add Props First
        foreach (MessageSignalDefinition signalDefinition in definition.Signals)
            WriteMessagePropertyDotNet(fileOutputBuilder, methodBuilder, enumBuilder, signalDefinition, metadataItems, className, generatorExtensions);

        metadataItems.AppendLine("\t\t});");

        // Finish Props
        fileOutputBuilder.AppendLine();
        fileOutputBuilder.AppendLine("\t#endregion");

        generatorExtensions.ExtendMethods(methodBuilder, className, false);

        // Finish Methods
        methodBuilder.AppendLine("\t#endregion");
     
        // Add Methods to Main
        fileOutputBuilder.Append(methodBuilder);

        // Finish Main
        fileOutputBuilder.AppendLine("}");

        // If Enums...addd them here...
        if (enumBuilder.Length > 0)
            fileOutputBuilder.AppendLine(enumBuilder.ToString());

        generatorExtensions.ExtendAfterClassOutput(fileOutputBuilder, className);

    }

    private static void WriteMessagePropertyDotNet(StringBuilder mainOutput, StringBuilder methodBody, StringBuilder enumOutput, MessageSignalDefinition definition, StringBuilder metadataBuilder, string className, GeneratorUtility generatorExtensions)
    {
        string propName = definition.Name.AsIdentifier().ToCamelCase();

        if (propName.StartsWith(className))
            propName = propName.Substring(className.Length);

        string type;
        string canType;

        if (definition.Values.Count > 0)
        {
            // Generate an Enum
            canType = "ValueType.Enum";
            type = propName + "Values";

            enumOutput.AppendLine();
            enumOutput.AppendLine($"public enum {type}\r\n{{");

            // Find Last Key
            int last = definition.Values.Max(x => x.Key);
            foreach (var item in definition.Values.OrderBy(x => x.Key))
            {
                string enumValue = item.Value.AsIdentifier();
                if (enumValue.StartsWith(className))
                    enumValue = enumValue.Substring(className.Length);

                enumOutput.AppendLine($"\t{enumValue.AsIdentifier()}={item.Key}{(item.Key == last ? "" : ",")}");
            }

            enumOutput.AppendLine("}");
        }
        else
        {
            switch (definition.ValueType)
            {
                case ValueType.Unsigned:
                    canType = "ValueType.Unsigned";
                    type = "uint";
                    break;
                case ValueType.Float:
                    canType = "ValueType.Float";
                    type = "float";
                    break;
                case ValueType.Double:
                    canType = "ValueType.Double";
                    type = "double";
                    break;
                case ValueType.Signed:
                default:
                    canType = "ValueType.Signed";
                    type = "int";
                    break;
            }
        }

        string getValue = $"get {{ return OnGetValue<{type}>();  }}";

        StringBuilder setBuilder = new StringBuilder($"set {{ OnSetValue(value);");

        // Extend Setter
        generatorExtensions.ExtendSetter(setBuilder, propName.AsIdentifier(), type);
        setBuilder.Append($" }}");

        string endianNess = definition.ByteOrder == ByteOrder.LittleEndian ? "ByteOrder.LittleEndian" : "ByteOrder.BigEndian";

        // Add Property
        mainOutput.AppendLine($"\r\n\tpublic {type} {propName.AsIdentifier()} \r\n\t{{\r\n\t\t{getValue}\r\n\t\t{setBuilder.ToString()} \r\n\t}}");

        var uniqueId = ParameterData.GenerateUniqueID(CanService.Name, className, propName);

        // Add Metadata to Static Dictionary.
        metadataBuilder.AppendLine($"\t\t\t{{nameof({className}.{propName}), new( {definition.StartBit}, {definition.BitLength}, {endianNess}, {canType}, {definition.Scale}, {definition.Offset}, {definition.Id}, {definition.DefaultValue}, {definition.Minimum}, {definition.Maximum}, {uniqueId})}},");
    }
    #endregion

}