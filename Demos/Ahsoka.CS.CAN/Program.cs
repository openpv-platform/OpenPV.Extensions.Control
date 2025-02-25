using Ahsoka.Core;
using Ahsoka.Core.Dispatch;
using Ahsoka.Services.Can;
using Ahsoka.Services.System;
using System;
using System.ComponentModel;
using System.Linq;

namespace Ahsoka.CS.CAN;

public class Program
{
    static MainUI mainUi = new MainUI();

    static uint lastSpeed = 0;
    static readonly uint speedDirection = 1000;
    static uint count = 0;

    public static void Main()
    {
        // Add App Service Clients We start a System Service mostly 
        // so we an talk to the app from our OpenPV Toolkit but also
        // so we can access things like brightness
        SystemServiceClient systemClient = new();
        CanServiceClient canClient = new();

        // Start our UI and Setup the Various Buttons and Text Areas
        var window = mainUi.StartUI("OpenPV Can Demo");
        mainUi.InitStateChanged(false, "Waiting for Can");

        // These Events will listen for our Model Changes and Refresh Our UI.
        // we are also calling the RefreshUI once to start to init the status values
        RefreshUI(count, 0, new PropertyChangedEventArgs(""));

        Dispatcher.Default.AddStartupItem(systemClient);

        // Listen to CAN Message Notifications (Recieve Messages) from the CanClient Dispatch Source
        Dispatcher.Default.RegisterCallback<CanServiceClient.AhsokaNotificationArgs>(RecieveCanData, canClient);

        // Here we will register a callback to run every 5s to update our TSC1 Data
        Dispatcher.Default.RegisterTimerCallback(UpdateCANMessageData, TimeSpan.FromSeconds(5), canClient);

        // Open the Channel After the System has Started the Services
        Dispatcher.Default.InvokeDispatcher(IntializeCAN, EventArgs.Empty, canClient);

        // Start a Basic UI and run in the Dispatcher's Main Thread
        Dispatcher.Default.StartAndRun(window.Invoke);

        // Now start the Main Drawing Loop 
        // we will block here until the app shuts down.
        window.ShowAndRun();

        Dispatcher.Default.Stop();

        // Execute Shutdown
        ApplicationContext.Exit();

        return;
    }

    private static void RefreshUI(uint count, uint id, PropertyChangedEventArgs e)
    {
        mainUi.UpdateStatusText("Received Message Count:", $"{count}");
        mainUi.UpdateStatusText("Last Id:", id.ToString("X"));
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

        mainUi.InitStateChanged(true, "Can Ready");
    }

    private static void RecieveCanData(object sender, CanServiceClient.AhsokaNotificationArgs canArgs)
    {
        if (canArgs.TransportId == CanMessageTypes.Ids.CanMessagesReceived &&
            canArgs.NotificationObject is CanMessageDataCollection message)
        {
            foreach (var item in message.Messages)
            {
                count++;
                MotorCmd debug = new(item);
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Received Message {debug.Id}");
            }
            RefreshUI(count, message.Messages.Last().Id & 0x1FFFFFFF, new PropertyChangedEventArgs(""));
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
