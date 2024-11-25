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
    static uint lastSpeed = 0;
    static readonly uint speedDirection = 1000;

    public static void Main()
    {
        // Add App Service Clients We start a System Service mostly 
        // so we an talk to the app from our OpenPV Toolkit but also
        // so we can access things like brightness
        SystemServiceClient systemClient = new();
        CanServiceClient canClient = new();

        Dispatcher.Default.AddStartupItem(systemClient);

        // Listen to CAN Message Notifications (Recieve Messages) from the CanClient Dispatch Source
        Dispatcher.Default.RegisterCallback<CanServiceClient.AhsokaNotificationArgs>(RecieveCanData, canClient);

        // Here we will register a callback to run every 5s to update our TSC1 Data
        Dispatcher.Default.RegisterTimerCallback(UpdateCANMessageData, TimeSpan.FromSeconds(5), canClient);

        // Open the Channel After the System has Started the Services
        Dispatcher.Default.InvokeDispatcher(IntializeCAN, EventArgs.Empty, canClient);

        // Start a Basic UI.  This function will start our UI 
        // as well as run the Dispatcher in its Main Thread
        basicUI.StartAndRun(Dispatcher.Default);

        // Execute Shutdown
        ApplicationContext.Exit();

        return;
    }


    private static void IntializeCAN(object sender, EventArgs args)
    {
        CanServiceClient canClient = sender as CanServiceClient;
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Starting CAN Service");

        // Open Communication with the CoProcessor or SocketCAN
        canClient.OpenCommunicationChannel();

        // Create a Motor Control Message and Set Values
        MotorCmd recurringCanMessage = new()
        {
            Steer = 1,
            Drive = 2
        };

        // Set the Motor Command to transmit every 100ms..
        canClient.SendRecurringCanMessage(new RecurringCanMessage()
        {
            CanPort = 1,
            Message = recurringCanMessage.CreateCanMessageData(),
            TimeoutBeforeUpdateInMs = int.MaxValue / 2, // Don't timeout
            TransmitIntervalInMs = 100
        });
    }

    private static void RecieveCanData(object sender, CanServiceClient.AhsokaNotificationArgs canArgs)
    {
        if (canArgs.TransportId == CanMessageTypes.Ids.CanMessagesReceived &&
            canArgs.NotificationObject is CanMessageDataCollection message)
        {
            foreach (var item in message.Messages)
            {
                MotorCmd debug = new(item);
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Received Message {debug.Id}");
            }
        }
        else if (canArgs.TransportId == CanMessageTypes.Ids.NetworkStateChanged &&
            canArgs.NotificationObject is CanState state)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Can State Received");
            foreach (var address in state.NodeAddresses)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Node {address.Key}: {address.Value}");
            }
        }
    }

    private static void UpdateCANMessageData(object sender, EventArgs args)
    {
        // Rev the Speed
        lastSpeed += speedDirection;
        if (lastSpeed > 6000)
            lastSpeed = 1000;

        CanServiceClient canClient = sender as CanServiceClient;

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Updating TSC1 Message to {lastSpeed}");
        // Prepare TSC1 Message
        TSC1 tsc1Message = new()
        {
            Tsc1TransmissionRate = Tsc1TransmissionRateValues.Use_standard_TSC1_transmission_rates_of_10_ms_to_engine,
            EngineRequestedTorqueTorqueLimit = 125,
            EngineRequestedSpeedSpeedLimit = lastSpeed,
        };

        // Configure the TSC1 to Send at 10ms. 
        canClient.SendRecurringCanMessage(new RecurringCanMessage()
        {
            CanPort = 1,
            Message = tsc1Message.CreateCanMessageData(),
            TimeoutBeforeUpdateInMs = int.MaxValue / 2,
            TransmitIntervalInMs = 10
        });
    }

}
