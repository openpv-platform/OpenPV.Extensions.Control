using Ahsoka.Core.Dispatch;
using Ahsoka.Services.Data;
using Ahsoka.Services.IO;
using Gtk;
using System;
using System.Collections.Generic;

namespace Ahsoka.Demo.GTK.Controllers;

internal class IOController
{
    readonly IOServiceClient ioService;
    readonly DataServiceClient dataService;
    readonly List<IOData> ioItems = new();
    Dictionary<string, double> latestValues = new();

    public IOController(IOServiceClient ioService, DataServiceClient dataService)
    {
        this.ioService = ioService;
        this.dataService = dataService;
    }

    public void InitPanel(VBox fixedPanel)
    {
        // Update IO Values Every Second
        Dispatcher.Default.RegisterTimerCallback(HandleTimeoutMessage, TimeSpan.FromMilliseconds(200)); // Update UI and Toggle Outputs
        Dispatcher.Default.RegisterCallback<DataServiceClient.AhsokaNotificationArgs>(HandleDataNotification, dataService); // Listen to Data Service for IO Updates

        // Setup Analog IO Inputs
        foreach (var item in ioService.RequestAnalogInputs().AnalogInputs)
        {
            // ** Analog Input Labels
            Label tempLabel = new($"Analog (0-5v) Input #{item.Pin}: Not Found")
            {
                Valign = Align.End,
                HeightRequest = 50
            };
            fixedPanel.Add(tempLabel);
            latestValues[IOServiceMessages.AnalogInput_ + item.Pin.ToString()] = 0.0f;

            ioItems.Add(new IOData<AnalogInput, double>()
            {
                IOConfig = item,
                TextLabel = tempLabel,
                Value = -99,
                GetFunction = (item, oldValue) => { return latestValues[IOServiceMessages.AnalogInput_ + item.Pin.ToString()] / 1000; },
                TextFunction = (newValue) => { return $"\"Analog (0-5v) Input #{item.Pin}: {newValue:F4}V"; }
            });
        }

        // Setup Digital Inputs
        foreach (var item in ioService.RequestDigitalInputs().DigitalInputs)
        {
            // ** Analog Input Labels
            Label tempLabel = new($"Digital/Analog (0-36v) Input #{item.Pin}: Not Found")
            {
                Valign = Align.End,
                HeightRequest = 50
            };
            fixedPanel.Add(tempLabel);

            // Init Latest Values
            latestValues[IOServiceMessages.DigitalInput_ + item.Pin.ToString()] = 0.0f;

            ioItems.Add(new IOData<DigitalInput, double>()
            {
                IOConfig = item,
                TextLabel = tempLabel,
                Value = -99,
                GetFunction = (item, oldValue) => { return latestValues[IOServiceMessages.DigitalInput_ + item.Pin.ToString()] / 1000; },
                TextFunction = (newValue) => { return $"Digital/Analog (0-36v) Input #{item.Pin}: {newValue:F4}V"; },
            });
        }

        // *Setup DigitalOuts
        foreach (var item in ioService.RequestDigitalOutputs().DigitalOutputs)
        {
            // ** Digital Output Labels
            var label = new Label($"Digital Output #{item.Pin + 1} State: Not Found")
            {
                Valign = Align.End,
                HeightRequest = 50
            };
            fixedPanel.Add(label);

            ioItems.Add(new IOData<DigitalOutput, PinState>()
            {
                IOConfig = item,
                TextLabel = label,
                Value = (PinState)(item.Pin % 2),
                TextFunction = (newValue) => { return $"Digital Output #{item.Pin} State: {newValue} "; },
                GetFunction = (item, oldValue) =>
                {
                    // Some basic math to flip the pins every 10s
                    int pinStateInt = (int)(Math.Ceiling(DateTime.Now.Second / 10.0f) % 2);
                    var returnValue = pinStateInt + (item.Pin) - 1 == 1 ? PinState.High : PinState.Low;

                    // Set Digital State and Update
                    item.State = returnValue;
                    ioService.SetDigitalOut(item);
                    return returnValue;
                },
            });

        }
    }

    public void HandleTimeoutMessage(object model, EventArgs args)
    {
        // Call IOMonitor and Callback UpdateUI if There are Changes
        Dispatcher.Default.InvokeDispatcherAsync(IOMonitor, UpdateUI);
    }

    private void HandleDataNotification(object arg, DataServiceClient.AhsokaNotificationArgs args)
    {
        // Here we listen to changes in the IO Values
        if (args.TransportId == DataMessageTypes.Ids.NotifyKeyValueChanged)
        {
            KeyValueNotification notif = args.NotificationObject as KeyValueNotification;
            foreach (var item in notif.KeyValues)
                latestValues[item.KeyInfo.Key] = item.FloatValue;
        }
        else if (args.TransportId == DataMessageTypes.Ids.NotifyKeyAdded)
        {
            KeyList notif = args.NotificationObject as KeyList;
            foreach (var item in notif.Keys)
            {
                // Subscribe to IO Values as they are added.
                if (item.Origin == IOService.Name)
                {
                    Console.WriteLine($"Subscribing to {item.Origin}:{item.Key}:{item.MininumAccessLevel}");
                    var keySubscription = new KeySubscriptionRequest();
                    keySubscription.SubscribeToKeys.Add(item);
                    dataService.SubscribeToKeys(keySubscription);
                }
            }
        }
    }

    // Monitor Function will be executed in the Background so will not block UI.
    bool IOMonitor()
    {
        bool hasChanges = false;
        foreach (var item in ioItems)
            if (item.UpdateValues())
                hasChanges = true;

        return hasChanges;
    }

    // Update UI will be executed in Main Thread so can access the UI.
    void UpdateUI()
    {
        // Lock since we are going to access the dictionary.
        foreach (var item in ioItems)
            item.UpdateUI();
    }

    /// <summary>
    /// Interface for IO Items
    /// </summary>
    interface IOData
    {
        public bool UpdateValues();
        public void UpdateUI();
    }

    // Template for Holding Various IO Types
    class IOData<T, ValType> : IOData
    {
        public DateTime LastUpdate = DateTime.MinValue;
        public Label TextLabel;
        public ValType Value;
        public T IOConfig;
        public Func<T, ValType, ValType> GetFunction;
        public Func<ValType, string> TextFunction;

        public bool UpdateValues()
        {
            if (GetFunction != null)
            {
                var tempVal = GetFunction.Invoke(IOConfig, Value);
                if (!tempVal.Equals(Value))
                {
                    Value = tempVal;
                    return true;
                }
            }

            return false;
        }

        public void UpdateUI()
        {
            TextLabel.Text = TextFunction?.Invoke(Value);
        }
    }
}


