using Ahsoka.Core;
using Ahsoka.Services.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Ahsoka.Test;

[TestClass]
public class IOServiceTests : LinearTestBase
{
    public TestContext TestContext { get; set; }


    [TestMethod]
    public void AutoStartTests()
    {
        // Create Client Runtime and Add Services
        IOServiceClient systemClient = new();

        var buzzerConfig = systemClient.GetBuzzerConfig();
        systemClient.SetBuzzerConfig(buzzerConfig);

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }



    [TestMethod]
    public void TestServiceParameters()
    {
        var parameters = AhsokaMessagesBase.GetAllSystemParameters();
        Assert.IsTrue(parameters[IOService.Name].Any(x => x.Name == IOServiceMessages.AnalogInput_1));
    }

    [TestMethod]
    public void TestBaseServiceComponents()
    {
        // Create Client Runtime and Add Services
        IOServiceClient ioClient = new();
        ioClient.Start();


        var buzzerConfig = ioClient.GetBuzzerConfig();
        ioClient.SetBuzzerConfig(buzzerConfig);

        var vbat = ioClient.GetVBat();
        var pin = ioClient.GetIGNPin();

        // Stop the Runtimes
        AhsokaRuntime.Default.StopAllEndPoints();
    }

    [TestMethod]
    public void TestMissingServiceConfig()
    {
        var service1 = ConfigurationLoader.GetServiceConfig("TestConfig");
        var service2 = ConfigurationLoader.GetServiceConfig("TestConfig2");

        Assert.IsFalse(service1.DataListenURI.Equals(service2.DataListenURI));
    }

}


