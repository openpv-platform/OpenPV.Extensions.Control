using Ahsoka.ServiceFramework;
using Ahsoka.Services.IO;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ahsoka.Services.System.Platform;

// Can't be run on Windows
[ExcludeFromCodeCoverage]
internal class IOTests
{
    public static void TestSystemService()
    {
        // Start SystemService
        Console.WriteLine($"Starting Runtimes for SystemService Test");

        // Start client for NetworkService
        var client = new IOServiceClient();
        client.NotificationReceived += Client_NotificationReceived;


        // Testing buzzer
        BuzzerTest(client);

        TestIGNPinAndVBat(client);

        MonitorIgnitionNotification(client);

        // Stop the Runtimes
        client.Stop();
    }

    public static void MonitorIgnitionNotification(IOServiceClient client)
    {
        // Start SystemService
        Console.WriteLine($"Listen for Ignition Notification Monitor");

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();

    }

    private static void BuzzerTest(IOServiceClient client)
    {
        Console.WriteLine("Starting Buzzer Test");
        client.GetBuzzerConfig();
        BuzzerConfig buzzerConfig = new()
        {
            IsEnabled = true,
            FrequencyInHz = 2000,
            VolumePct = 25
        };
        client.SetBuzzerConfig(buzzerConfig);
        Thread.Sleep(1000);
        client.GetBuzzerConfig();
        buzzerConfig.FrequencyInHz = 1500;
        buzzerConfig.VolumePct = 50;
        client.SetBuzzerConfig(buzzerConfig);
        Thread.Sleep(1000);
        client.GetBuzzerConfig();
        buzzerConfig.FrequencyInHz = 1000;
        buzzerConfig.VolumePct = 75;
        client.SetBuzzerConfig(buzzerConfig);
        Thread.Sleep(1000);
        client.GetBuzzerConfig();
        buzzerConfig.FrequencyInHz = 500;
        buzzerConfig.VolumePct = 100;
        client.SetBuzzerConfig(buzzerConfig);
        Thread.Sleep(1000);
        client.GetBuzzerConfig();
        buzzerConfig.IsEnabled = false;
        client.SetBuzzerConfig(buzzerConfig); // buzzer is switched off
        Console.WriteLine("Completed Buzzer Test");
    }

    private static void Client_NotificationReceived(object sender, NotificationEventArgs<IOMessageTypes.Ids> e)
    {
        if (e.TransportId == IOMessageTypes.Ids.IgnitionOffNotification)
        {
            Console.WriteLine($"Client: ignition is off");
        }
        else if (e.TransportId == IOMessageTypes.Ids.IgnitionOnNotification)
        {
            Console.WriteLine($"Client: ignition is on");
        }
    }

    private static void TestIGNPinAndVBat(IOServiceClient client)
    {
        Console.WriteLine("[] Testing GetVBat and GetIGNPin function calls...");
        VoltageValue vbat = client.GetVBat();
        Console.WriteLine($"[+] VBAT (milli volts) -> {vbat.MilliVolts}");
        IgnitionState ign = client.GetIGNPin();
        Console.WriteLine($"[+] IGN Pin (milli volts) -> {ign.MilliVolts}");
        Console.WriteLine("[] DONE Testing GetVBat and GetIGNPin function calls!");
    }

}
