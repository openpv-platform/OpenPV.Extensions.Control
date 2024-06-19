using Ahsoka.ServiceFramework;
using Ahsoka.Services.IO;
using Ahsoka.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        AhsokaRuntime.ShutdownAll();
    }


    [TestMethod]
    public void TestBaseServiceComponents()
    {
        // Create Client Runtime and Add Services
        IOServiceClient systemClient = new();
        AhsokaRuntime.CreateBuilder()
                     .AddClients(systemClient)
                     .StartWithInternalServices();

      
        var buzzerConfig = systemClient.GetBuzzerConfig();
        systemClient.SetBuzzerConfig(buzzerConfig);

        var vbat = systemClient.GetVBat();
        var pin = systemClient.GetIGNPin();

        // Stop the Runtimes
        AhsokaRuntime.ShutdownAll();
    }

    [TestMethod]
    public void TestMissingServiceConfig()
    {
        var service1 = ConfigurationLoader.GetServiceConfig("TestConfig");
        var service2 = ConfigurationLoader.GetServiceConfig("TestConfig2");

        Assert.IsFalse(service1.DataListenURI.Equals(service2.DataListenURI));
    }

}


