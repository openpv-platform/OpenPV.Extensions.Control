using Ahsoka.Dispatch;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Can;
using Ahsoka.Services.System;
using Ahsoka.System;
using System;

namespace Ahsoka.CS.CAN;

public class Program
{
    static readonly CanUI basicUI = new();

    public static void Main()
    {
        //Enables Multi-Packet Logging
        AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Medium;

        // Creates System Client, overrides behavior to 
        // auto start corresponding service, and initializes
        // the Can Socket. This socket will route all messages
        // from the client across the Can transport layer.
        var config = ConfigurationLoader.GetServiceConfig("SystemService");
        config.Behaviors = EndPointBehaviors.NoBehavior;
        SystemServiceClient systemClient = new(config);
        // Message Id must correspond to a configured message class
        systemClient.Start(new CanClientSocket(1, 2818048, 0x1FF0000));

        // Run System Test and Register Notification Handler
        Dispatcher.Default.InvokeDispatcher(TestSystem, EventArgs.Empty, systemClient);
        Dispatcher.Default.RegisterCallback<SystemServiceClient.AhsokaNotificationArgs>(RecieveSystemData, systemClient);

        // Start a Basic UI.
        basicUI.StartAndRun(Dispatcher.Default);

        // Execute Shutdown
        ApplicationContext.Exit();

        return;
    }

    private static void TestSystem(object sender, EventArgs args)
    {
        SystemServiceClient client = sender as SystemServiceClient;

        var response = client.EchoValue("TestValue");
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Received Message {response}");

        EchoValue requestObject = new EchoValue
        {
            DataValue = "TestNotif"
        };
        client.SendMessageWithResponse<EmptyNotification>(SystemMessageTypes.Ids.RequestNotif, requestObject);
    }

    private static void RecieveSystemData(object sender, SystemServiceClient.AhsokaNotificationArgs sysArgs)
    {
        if (sysArgs.TransportId == SystemMessageTypes.Ids.TestNotif &&
            sysArgs.NotificationObject is EchoValue message)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Received Message {message.DataValue}");
        }
    }
}
