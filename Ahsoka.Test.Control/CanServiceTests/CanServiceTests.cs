using Ahsoka.Installer;
using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Can;
using Ahsoka.Services.Can.Messages;
using Ahsoka.Services.Can.Platform;
using Ahsoka.Services.IO;
using Ahsoka.Services.Network;
using Ahsoka.Services.System;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Test.Control.Properties;
using Ahsoka.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ValueType = Ahsoka.Services.Can.ValueType;

namespace Ahsoka.Test;

[TestClass]
public class CanServiceTests : LinearTestBase
{
    public TestContext TestContext { get; set; }
    readonly AutoResetEvent stateReceived = new(false);
    CanState state;
    readonly Semaphore messageReceived = new(0, 100);
    readonly List<CanMessageData> messages = new();


    internal static void GetParametersTests(PackageInformation info )
    {
        var knownValues = AhsokaMessagesBase.GetAllSystemParameters(info);

        Assert.IsTrue(knownValues.Any(x => x.Key == IOService.Name));
        var systemKeys = knownValues.FirstOrDefault(x => x.Key == IOService.Name);
        Assert.IsTrue(systemKeys.Value.Any(x => x.Name == "Available"));

        Assert.IsTrue(knownValues.Any(x => x.Key == CanService.Name));
        systemKeys = knownValues.FirstOrDefault(x => x.Key == CanService.Name);
        Assert.IsTrue(systemKeys.Value.Any(x => x.Name == "DRIVER_HEARTBEAT_cmd"));
    }

    [TestMethod]
    public void TestCanGenerator()
    {
        string canConfigFile = Path.Combine(Path.GetTempPath(), CanMetadataTools.CanConfigurationExtension);
        string canDBCFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dbc");

        // Force to Current Directory for Code Coverage
        Environment.CurrentDirectory = Path.GetDirectoryName(AppContext.BaseDirectory);
        Console.WriteLine(Environment.CurrentDirectory);

        File.WriteAllText(canDBCFile, CanTestResources.TestFile);

        // Create App Config File.
        CanMetadataTools.GenerateCalibrationFromDBC(canDBCFile, canConfigFile);

        var config = TestAppConfigFile(canConfigFile);

        TestCanGeneration(canConfigFile);

        TestCanService(config);

        File.Delete(canDBCFile);
        File.Delete(canConfigFile);

        AhsokaRuntime.Default.StopAllEndPoints();
    }


    [TestMethod]
    public void TestServiceParameters()
    {
        // Create Test Data from CAN Demo
        string canConfigFile = Path.Combine(Path.GetTempPath(), CanMetadataTools.CanConfigurationExtension);
        string projectFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".packageinfo.json");
        File.WriteAllText(canConfigFile, CanTestResources.STDemoPackage_1_cancalibration);
        File.WriteAllText(projectFile, CanTestResources.OpenLinuxST_PackageInfo);

        // Load package and point the config correctly.
        PackageInformation info = PackageInformation.LoadPackageInformation(projectFile);
        info.ServiceInfo.RuntimeConfiguration.ExtensionInfo.FirstOrDefault(x => x.ExtensionName == "CAN Service Extension").ConfigurationFile = Path.GetFileName(canConfigFile);

        var parameters = AhsokaMessagesBase.GetAllSystemParameters(info);
        Assert.IsTrue(parameters[CanService.Name].Any(x => x.Name == "DRIVER_HEARTBEAT_cmd"));

        File.Delete(projectFile);
        File.Delete(canConfigFile);

    }

    private static CanApplicationConfiguration TestAppConfigFile(string canConfigFile)
    {
        CanClientConfiguration config = ConfigurationFileLoader.LoadFile<CanClientConfiguration>(canConfigFile);
        config.Nodes.FirstOrDefault(x => x.Name == "IO").NodeType = NodeType.Self;
        config.Nodes.FirstOrDefault(x => x.Name == "IO").TransportProtocol = TransportProtocol.J1939;
        var info = new J1939NodeDefinition()
        {
            UseAddressClaim = true,
            Addresses = "40,41",
        };
        config.Nodes.FirstOrDefault(x => x.Name == "IO").J1939Info = info;
        Assert.IsTrue(config.Nodes.FirstOrDefault(x => x.Name == "SENSOR") != null);
        config.Nodes.Add(CanSystemInfo.StandardCanMessages.Nodes.First(x => x.Name == "ANY"));
        File.WriteAllText(canConfigFile, JsonUtility.Serialize(config));

        CanApplicationConfiguration appConfig = CanMetadataTools.GenerateApplicationConfig(SystemInfo.HardwareInfo, canConfigFile, false);

        Assert.IsTrue(appConfig.CanPortConfiguration.MessageConfiguration.Nodes.FirstOrDefault(x => x.Name == "SENSOR") != null);

        return appConfig;
    }

    private void TestCanService(CanApplicationConfiguration appConfig)
    {
        // Write Config to Output Location
        string coprocessorPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.CoProcessorApplicationPath);
        string configPath = Path.Combine(coprocessorPath, CanInstallerComponent.applicationConfiguration);

        if (!Directory.Exists(Path.GetDirectoryName(configPath)))
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));

        // Update test Values
        var firstMessage = appConfig.CanPortConfiguration.MessageConfiguration.Messages.First(x => x.Id == 500); // Test Message ID
        uint id = firstMessage.Id;
        firstMessage.FilterReceipts = true;
        firstMessage.TimeoutMs = 1000;

        // Other ID for Testing.
        var secondMessage = appConfig.CanPortConfiguration.MessageConfiguration.Messages.First(x => x.Id == 501); // Test Message ID
        uint otherId = secondMessage.Id; // Test using TSC1 which in the DBC.

        var tsc1 = appConfig.CanPortConfiguration.MessageConfiguration.Messages.First(x => x.Name == "TSC1"); // Test Message ID
        tsc1.RollCountBit = tsc1.Signals.First().StartBit;
        tsc1.RollCountLength = tsc1.Signals.First().BitLength;
        tsc1.CrcBit = tsc1.Signals.Last().StartBit;
        tsc1.CrcType = CrcType.Tsc1;
        tsc1.HasRollCount = true;
        tsc1.MessageType = MessageType.J1939ExtendedFrame;
        tsc1.TransmitNodes = firstMessage.TransmitNodes;
        tsc1.ReceiveNodes = new int[2] { -1, 2};

        // Write Config File
        if (File.Exists(configPath))
           File.Delete(configPath);

        using (FileStream output = File.OpenWrite(configPath))
            ProtoBuf.Serializer.Serialize(output, appConfig);

        // Create Service Client and Start Runtime
        CanServiceClient client = new();
        client.Start();


        // Start Comms with CoProcessor.
        client.OpenCommunicationChannel();

        CanPortConfiguration config = client.Calibrations.CanPortConfiguration;
        Assert.IsNotNull(config);

        // List to Message Notifications
        client.NotificationReceived += (sender, args) =>
        {
            if (args.TransportId == CanMessageTypes.Ids.CanMessagesReceived)
            {
                messages.AddRange(((CanMessageDataCollection)args.NotificationObject).Messages);
                messageReceived.Release();
            }
            else if (args.TransportId == CanMessageTypes.Ids.NetworkStateChanged)
            {
                state = (CanState)args.NotificationObject;
                stateReceived.Set();
            }
        };


        // Test State Notification
        stateReceived.WaitOne();
        Assert.IsNotNull(state);
        Assert.IsNotNull(state.NodeAddresses.Count > 0);

        // Test Send Message Calls
        TestCanModel canMessage = CreateTestViewModel();
        canMessage.TestSigned = 1;
        var status = client.SendCanMessages(1, canMessage); // Send One
        canMessage.TestSigned = 2;
        status = client.SendCanMessages(1, canMessage); // Send Two
        canMessage.TestSigned = 3;
        status = client.SendCanMessages(1, canMessage);

        // Test Send Recurring Messages
        // We use "other id" since its not filtered.
        var data = canMessage.CreateCanMessageData();
        data.Id = otherId;
        status = client.SendRecurringCanMessage(new RecurringCanMessage()
        {
            CanPort = 1,
            Message = data,
            TimeoutBeforeUpdateInMs = 1000,
            TransmitIntervalInMs = 5
        });

        Thread.Sleep(100);

        status = client.SendRecurringCanMessage(new RecurringCanMessage()
        {
            CanPort = 1,
            Message = data,
            TimeoutBeforeUpdateInMs = 1000,
            TransmitIntervalInMs = 5
        });

        Thread.Sleep(100);

        // Test Receieve (verify echoed Messages)
        while (messages.Count < 5)
            messageReceived.WaitOne();

        // First Message
        TestCanModel returnMsg = new(messages[0]);
        Assert.AreEqual(1, returnMsg.TestSigned);

        // Second Message
        returnMsg = new(messages[1]);
        Assert.AreEqual(2, returnMsg.TestSigned);

        // Third Message - Recurring
        returnMsg = new(messages[2]);
        Assert.AreEqual(3, returnMsg.TestSigned);

        // Third Message - Second Receive of Recurring Message
        returnMsg = new(messages[3]);
        Assert.AreEqual(3, returnMsg.TestSigned);

        // Cover TSC1 and Error Conditions
        var canMessage2 = CreateTestViewModel();

        data = canMessage2.CreateCanMessageData();
        data.Id = secondMessage.Id + 1;
        status = client.SendCanMessages(1, data); // Send Error Message

        data.Id = tsc1.Id;
        status = client.SendCanMessages(1, data); // Send Roll Count / TSC1


        // Sleep to Recurring message Expires (Expires at 500ms)
        Thread.Sleep(1000);

        // Clear Message for Filter Tests
        messages.Clear();

        // Test Can Filtering
        ClientCanFilter filter = new()
        {
            CanPort = 1,
            CanIdLists = new[] { otherId }
        };
        client.ApplyCanFilter(filter);

        // Send Message that should NOT come through;
        client.SendCanMessages(1, returnMsg);

        // Mutate Message to Pass Filter
        data = returnMsg.CreateCanMessageData();
        data.Id = otherId;
        client.SendCanMessages(1, returnMsg);

        // We should get ONE message and it shoudl match the filter 
        while (messages.Count < 1)
            messageReceived.WaitOne();

        // Verify its our NEW ID not the Old One.
        TestCanModel finalMessage = new(messages[0]);
        Assert.AreEqual(finalMessage.Id, otherId);

        // Clear Filters
        filter.CanIdLists = Array.Empty<uint>();
        client.ApplyCanFilter(filter);

        // Clear Message for Data Filter Tests
        messages.Clear();

        // Send Twice the Same and Once with a Change.
        TestCanModel filteredMessage = CreateTestViewModel();
        filteredMessage.TestSigned = 62;
        client.SendCanMessages(1, filteredMessage);
        client.SendCanMessages(1, filteredMessage);
        filteredMessage.TestSigned = 64;
        client.SendCanMessages(1, filteredMessage);

        // Wait for 2 Messages should be first and third message
        while (messages.Count < 2)
            messageReceived.WaitOne();

        // First Message (Second Message Should Be Filtered)
        filteredMessage = new(messages[0]);
        Assert.AreEqual(62, filteredMessage.TestSigned);

        // Third Message Message
        filteredMessage = new(messages[1]);
        Assert.AreEqual(64, filteredMessage.TestSigned);

        // Start Comms with CoProcessor.
        client.CloseCommunicationChannel();
        Assert.IsNull(client.Calibrations);

        // Clean Up.
        File.Delete(configPath);

    }

    private static void TestCanGeneration(string canConfigFile)
    {
        string canPackageInfoFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "PackageInfo.json");
        PackageInformation packageInformation = new()
        {
            ServiceInfo = new()
            {
                RuntimeConfiguration = new RuntimeConfiguration()
                {

                }
            }
        };

        var serviceConfiguration = ConfigurationLoader.GetServiceConfig("CanService");
        packageInformation.ServiceInfo.RuntimeConfiguration.Services.Add(new ServiceConfiguration()
        {
            ServiceName = serviceConfiguration.ServiceName,
            DataChannel = serviceConfiguration.DataChannel,
            TcpConnectionAddress = serviceConfiguration.TcpConnectionAddress,
            TcpListenAddress = serviceConfiguration.TcpListenAddress,
        });

        packageInformation.ServiceInfo.RuntimeConfiguration.ExtensionInfo = new List<ExtensionInfo>() { new ExtensionInfo() { ExtensionName = "CAN Service Extension", ConfigurationFile = canConfigFile } };

        // Load Test Generator
        Ahsoka.System.Extensions.AddPrivateExtension(Assembly.GetExecutingAssembly());

        
        File.WriteAllText(canPackageInfoFile, JsonUtility.Serialize(packageInformation));

        packageInformation = PackageInformation.LoadPackageInformation(canPackageInfoFile);
        // Validate Parameter Info
        GetParametersTests(packageInformation);

        string outputFileNameDotNet = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".generated.cs");
        string outputFileNameCPP = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".generated.hpp");

        CanMetadataTools.GenerateMessageClasses(canPackageInfoFile, outputFileNameDotNet, "TestNameSpace", null, Installer.ApplicationType.Dotnet);
        string classFileContent = File.ReadAllText(outputFileNameDotNet);
        Assert.IsTrue(classFileContent.Length > 0);
        
        Assert.IsTrue(classFileContent.Contains("/*ExtendHeader*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendConstructor*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendSetter*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendMethods*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendAfterClassOutput*/"));

        CanMetadataTools.GenerateMessageClasses(canPackageInfoFile, outputFileNameCPP, "TestNameSpace", null, Installer.ApplicationType.Cpp);
        classFileContent = File.ReadAllText(outputFileNameCPP);
        Assert.IsTrue(classFileContent.Length > 0);

        Assert.IsTrue(classFileContent.Contains("/*ExtendHeader+Header*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendMethods+Header*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendAfterClassOutput*/"));

        string cppFile = Path.Combine(Path.GetDirectoryName(outputFileNameCPP),Path.GetFileNameWithoutExtension(outputFileNameCPP) + ".cpp");
        classFileContent = File.ReadAllText(cppFile);
        Assert.IsTrue(classFileContent.Length > 0);

        Assert.IsTrue(classFileContent.Contains("/*ExtendHeader+Impl*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendConstructor*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendSetter*/"));
        Assert.IsTrue(classFileContent.Contains("/*ExtendMethods+Impl*/"));
       
        
        // Generate Class File.
        File.Delete(outputFileNameDotNet);
        File.Delete(outputFileNameCPP);
        File.Delete(cppFile);
     }

    [TestMethod]
    public void TestCanViewModel()
    {
        TestCanModel model = CreateTestViewModel();
        Assert.IsTrue(model.TestSigned == 64);
        Assert.IsTrue(model.TestUnsigned == 256);
        Assert.IsTrue(model.TestEnum == TestEnumValues.Test2EnumTwo);
        Assert.IsTrue(model.TestFloat == 12345.0f);
        Assert.IsTrue(model.TestDouble == 54321.0f);
        Assert.IsTrue(model.TestBit == 1);

        // Get UnScaled Value..should be half of Normal Value
        UInt32 value = model.GetRawValue<UInt32>("TestUnsigned");
        Assert.IsTrue(value == 128);

        // Test Values Get Unset.
        model.TestUnsigned = 0;
        Assert.IsTrue(model.TestUnsigned == 0);

        var data = model.CreateCanMessageData();
        Assert.IsTrue(data.Id == 500);
        Assert.IsTrue(data.Dlc == 17);

        CanPropertyInfo info = new CanPropertyInfo(0, 32, ByteOrder.LittleEndian, ValueType.Unsigned, 1000, 0, 0);

        var candata = new byte[8] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        var result = info.GetValue<uint>(candata, true);
    }

    private static TestCanModel CreateTestViewModel()
    {
        TestCanModel model = new()
        {
            TestBit = 1,
            TestSigned = 64,
            TestUnsigned = 256,
            TestEnum = TestEnumValues.Test2EnumTwo,
            TestFloat = 12345.0f,
            TestDouble = 54321.0f
        };

        return model;
    }

    [TestMethod]
    public void GenerateBlankConfigFromDBC()
    {
        string canConfigFile = Path.Combine(Path.GetTempPath(), CanMetadataTools.CanConfigurationExtension);
        string canDBCFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".dbc");
        File.WriteAllText(canDBCFile, CanTestResources.TestFile);

        CanMetadataTools.GenerateCalibrationFromDBC(canDBCFile, canConfigFile);

        var config = ConfigurationFileLoader.LoadFile<CanClientConfiguration>(canConfigFile);
        Assert.IsTrue(config.Nodes.Count > 0);
        Assert.IsTrue(config.Messages.Count > 0);

        File.Delete(canDBCFile);
        File.Delete(canConfigFile);
    }

    [TestMethod]
    public void TestCanHandler()
    {
        string file = "CANServiceConfiguration.json";
        File.WriteAllText(file, CanTestResources.CANServiceConfiguration);
      
        var service = new CanService();

        var impl = new DesktopServiceImplementation();
        var config = CanMetadataTools.GenerateApplicationConfig(SystemInfo.HardwareInfo, JsonUtility.Deserialize<CanClientConfiguration>(CanTestResources.CANServiceConfiguration), false);
        impl.Open(service, config.CanPortConfiguration, 1);

        var handler = new CanHandler(impl);
        handler.Init();

        // Wait for Address Claim to Finish.   
        // In real system recieve messages are redirected until AC is done.
        Thread.Sleep(500);

        CanMessageDataCollection collection = new() { CanPort = 1 };

        //Test raw extended frame
        TestRaw rawMessage = new() { Test = 10 };
        collection.Messages.Add(rawMessage.CreateCanMessageData());

        //Test J1939 with static node addresses
        TestStatic staticMessage = new() { NewSignal = 1 };
        collection.Messages.Add(staticMessage.CreateCanMessageData());

        //Test J1939 with overridden addresses
        TestOverride overrideMessage = new() { Test = 10 };
        overrideMessage.Protocol.SourceAddress = 127;
        overrideMessage.Protocol.DestinationAddress = 16;
        collection.Messages.Add(overrideMessage.CreateCanMessageData());

        //Confirm messages available in config
        var output = handler.ConfirmAvailable(collection);
        Assert.IsTrue(output.Status == MessageStatus.Success);

        foreach (var message in  collection.Messages)
            handler.ProcessMessage(message);

        //Confirm processes ids are correct
        Assert.IsTrue(collection.Messages[0].Id == 0x80E2AFDC);
        Assert.IsTrue(collection.Messages[1].Id == 0x803C0A28);
        Assert.IsTrue(collection.Messages[2].Id == 0x802B107F);

        //Test address claim send
        SendInformation sendInfo = new() { name = "AC", destinationAddress = 255, sourceAddress = 40 };
        handler.SendPredefined(sendInfo);

        //Test receiving AC message
        J1939PropertyDefinitions.Id acId = new(0x98EEFF00);
        acId.SourceAddress = 20;
        CanMessageData acData = new()
        {
            Id = acId.WriteToUint(),
            Dlc = 8,
            Data = [1, 0, 0, 0, 0, 0, 0, 0]
        };
        handler.Receive(acData);

        //Test AC recive at same address
        acId.SourceAddress = 40;
        acData.Id = acId.WriteToUint();
        handler.Receive(acData);

        //Test broadcast multipacket
        TestMultiPacket testMultiPacket = new() { Data1 = 12, Data2 = 2.4 };
        testMultiPacket.Protocol.SourceAddress = 40;
        testMultiPacket.Protocol.DestinationAddress = 255;
        sendInfo = new() { messageData = testMultiPacket.CreateCanMessageData()  };
        handler.SendPredefined(sendInfo);

        Thread.Sleep(1000);

        //Test AC receive from higher priority node
        //Shuts off J1939 transmission
        acData.Data[7] = 1;
        handler.Receive(acData);
    }
}

public static class CanModelMetadata
{
    internal static Dictionary<uint, Dictionary<string, CanPropertyInfo>> metaData = new();
    internal static Dictionary<string, CanPropertyInfo> GetMetadata(uint canID) { return metaData[canID]; }
    internal static void AddMetaData(uint canID, Dictionary<string, CanPropertyInfo> properties)
    {
        metaData.Add(canID, properties);
    }

    internal static CanPropertyInfo GetPropertyInfo(uint canID, uint signalID)
    {
        return metaData[canID].Values.FirstOrDefault(x => x.SignalId == signalID);
    }

    internal static CanPropertyInfo GetPropertyInfo(uint canID, string signalName)
    {
        return metaData[canID][signalName];
    }

    static CanModelMetadata()
    {
        // TestCanModel Metadata - 500
        metaData.Add(500, new Dictionary<string, CanPropertyInfo>()
        {
            { "TestEnum", new(4, 8, ByteOrder.LittleEndian, ValueType.Enum, 1, 0, 1,uniqueId: 1) },
            { "TestUnsigned", new(12, 8, ByteOrder.LittleEndian, ValueType.Unsigned, 2, 0, 2,uniqueId: 2) },
            { "TestSigned", new(20, 8, ByteOrder.LittleEndian, ValueType.Signed, 1, 0, 3, 3,uniqueId:3 ) },
            { "TestFloat", new(32, 32, ByteOrder.LittleEndian, ValueType.Float, 1, 0, 4, 4,uniqueId:4 ) },
            { "TestDouble", new(64, 64, ByteOrder.LittleEndian, ValueType.Double, 1, 0, 5, 5,uniqueId:5 ) },
            { "TestBit", new(64 + 64, 1, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 6, 1,uniqueId: 1) },
        });

        // Decode Info for TestOverride 2818048
        metaData.Add(2818048, new Dictionary<string, CanPropertyInfo>()
        {
            {nameof(TestOverride.Test), new( 0, 64, ByteOrder.LittleEndian, ValueType.Double, 1, 0, 0, 0, 0, 0, 1259925567)},
        });

        // Decode Info for TestRaw 14856156
        metaData.Add(14856156, new Dictionary<string, CanPropertyInfo>()
        {
            {nameof(TestRaw.Test), new( 0, 24, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 0, 0, 0, 0, 997346722)},
        });

        // Decode Info for TestStatic 3932160
        metaData.Add(3932160, new Dictionary<string, CanPropertyInfo>()
        {
            {nameof(TestStatic.NewSignal), new( 0, 24, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 0, 0, 0, 0, 2063764300)},
        });

        // Decode Info for TestMultiPacket 204800000
        metaData.Add(204800000, new Dictionary<string, CanPropertyInfo>()
        {
            {nameof(TestMultiPacket.Crc), new( 0, 8, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 0, 0, 0, 0, 3329861171)},
            {nameof(TestMultiPacket.Data1), new( 8, 56, ByteOrder.LittleEndian, ValueType.Double, 1, 0, 1, 0, 0, 0, 1715044506)},
            {nameof(TestMultiPacket.Data2), new( 64, 64, ByteOrder.LittleEndian, ValueType.Double, 1, 0, 2, 0, 0, 0, 3289022618)},
        });
    }
}


public partial class TestCanModel : CanViewModelBase
{
    #region Auto Generated Properties

    public TestEnumValues TestEnum
    {
        get { return OnGetValue<TestEnumValues>(); }
        set { OnSetValue(value); }
    }

    public uint TestUnsigned
    {
        get { return OnGetValue<uint>(); }
        set { OnSetValue(value); }
    }

    public int TestSigned
    {
        get { return OnGetValue<int>(); }
        set { OnSetValue(value); }
    }

    public int TestBit
    {
        get { return OnGetValue<int>(); }
        set { OnSetValue(value); }
    }


    public float TestFloat
    {
        get { return OnGetValue<float>(); }
        set { OnSetValue(value); }
    }

    public double TestDouble
    {
        get { return OnGetValue<double>(); }
        set { OnSetValue(value); }
    }

    #endregion

    #region Auto Generated Methods
    protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.GetMetadata(500); }
    static TestCanModel() { }
    public TestCanModel() : base(500, 17) { } // CANID: 0x1f4
    public TestCanModel(CanMessageData message) : base(message) { }
    #endregion
}

public enum TestEnumValues
{
    Test2EnumOne = 1,
    Test2EnumTwo = 2
}


public partial class TestOverride : CanViewModelBase
{
    public const uint CanId = 2818048; // CANID: 0x2b0000
    public const int Dlc = 8;
    #region Auto Generated Properties
    J1939Helper protocol;
    public J1939Helper Protocol
    {
        get
        {
            if (protocol == null)
                protocol = new J1939Helper(message);
            return protocol;
        }
    }


    public double Test
    {
        get { return OnGetValue<double>(); }
        set { OnSetValue(value); }
    }

    #endregion

    #region Auto Generated Methods
    protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.GetMetadata(CanId); }
    public TestOverride() : base(CanId, Dlc)
    {
    }
    public TestOverride(CanMessageData message) : base(message)
    {
    }

    #endregion
}

public partial class TestRaw : CanViewModelBase
{
    public const uint CanId = 14856156; // CANID: 0xe2afdc
    public const int Dlc = 3;
    #region Auto Generated Properties

    public uint Test
    {
        get { return OnGetValue<uint>(); }
        set { OnSetValue(value); }
    }

    #endregion

    #region Auto Generated Methods
    protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.GetMetadata(CanId); }
    public TestRaw() : base(CanId, Dlc)
    {
    }
    public TestRaw(CanMessageData message) : base(message)
    {
    }

    #endregion
}

public partial class TestStatic : CanViewModelBase
{
    public const uint CanId = 3932160; // CANID: 0x3c0000
    public const int Dlc = 3;
    #region Auto Generated Properties
    J1939Helper protocol;
    public J1939Helper Protocol
    {
        get
        {
            if (protocol == null)
                protocol = new J1939Helper(message);
            return protocol;
        }
    }


    public uint NewSignal
    {
        get { return OnGetValue<uint>(); }
        set { OnSetValue(value); }
    }

    #endregion

    #region Auto Generated Methods
    protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.GetMetadata(CanId); }
    public TestStatic() : base(CanId, Dlc)
    {
    }
    public TestStatic(CanMessageData message) : base(message)
    {
    }

    #endregion
}


public partial class TestMultiPacket : CanViewModelBase
{
    public const uint CanId = 204800000; // CANID: 0xc350000
    public const int Dlc = 16;
    #region Auto Generated Properties
    J1939Helper protocol;
    public J1939Helper Protocol
    {
        get
        {
            if (protocol == null)
                protocol = new J1939Helper(message);
            return protocol;
        }
    }


    public uint Crc
    {
        get { return OnGetValue<uint>(); }
        set { OnSetValue(value); }
    }

    public double Data1
    {
        get { return OnGetValue<double>(); }
        set { OnSetValue(value); }
    }

    public double Data2
    {
        get { return OnGetValue<double>(); }
        set { OnSetValue(value); }
    }

    #endregion

    #region Auto Generated Methods
    protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.GetMetadata(CanId); }
    public TestMultiPacket() : base(CanId, Dlc)
    {
    }
    public TestMultiPacket(CanMessageData message) : base(message)
    {
    }

    #endregion
}