using Ahsoka.Core;
using Ahsoka.Core.Dispatch;
using Ahsoka.Services.IO;
using Gtk;
using System;

namespace Ahsoka.Demo.GTK.Controllers;

internal class BuzzerController
{
    Scale buzzVolumeScale = null;
    Scale buzzFreqScale = null;
    private BuzzerConfig buzzConfig = new() { IsEnabled = false, VolumePct = 0, FrequencyInHz = 500 };
    private readonly IOServiceClient systemService;

    public BuzzerController(IOServiceClient systemService)
    {
        this.systemService = systemService;
    }

    public void InitPanel(Frame fixedPanel)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"============ InitPanel Buzzer \n");

        buzzConfig = systemService.GetBuzzerConfig();

        HBox box = new()
        {
            Margin = 20
        };
        fixedPanel.Add(box);

        VBox volumeBox = new();
        box.Add(volumeBox);

        // Buzzer Volume Label and buzzVolumeScale Control
        buzzVolumeScale = new Scale(Orientation.Vertical, 0, 100, 10)
        {
            Inverted = true,
            Value = 0
        };
        volumeBox.PackStart(buzzVolumeScale, true, true, 10);

        var buzzVolumeLabel = new Label("Volume")
        {
            Valign = Align.End,
            Halign = Align.Center,
            HeightRequest = 50
        };
        volumeBox.PackEnd(buzzVolumeLabel, false, false, 10);

        VBox frequencyBox = new();
        box.Add(frequencyBox);

        // Buzzer Frequency Label and buzzFreqScale Control
        buzzFreqScale = new Scale(Orientation.Vertical, 500, 3000, 10)
        {
            Inverted = true,
            Value = 500
        };
        frequencyBox.PackStart(buzzFreqScale, true, true, 10);

        var buzzFreqLabel = new Label("Frequency")
        {
            Valign = Align.End,
            Halign = Align.Center,
            HeightRequest = 50
        };
        frequencyBox.PackEnd(buzzFreqLabel, false, false, 10);

        Dispatcher.Default.RegisterTimerCallback(HandleTimeoutMessage, TimeSpan.FromMilliseconds(500));
    }

    public void HandleTimeoutMessage(object model, EventArgs args)
    {
        // Update Buzzer Ever .5s
        // Time Dispatcher Callback Runs in Main Thead so safe to do UI Updates 
        // but avoid long running operations.
        if (buzzConfig.VolumePct != (int)buzzVolumeScale.Value ||
            buzzConfig.FrequencyInHz != (int)buzzFreqScale.Value)
        {
            buzzConfig.VolumePct = (int)buzzVolumeScale.Value;
            buzzConfig.FrequencyInHz = (int)buzzFreqScale.Value;

            buzzConfig.IsEnabled = buzzVolumeScale.Value >= 10;
            buzzConfig.VolumePct = (int)buzzVolumeScale.Value;
            buzzConfig.FrequencyInHz = (int)buzzFreqScale.Value;

            systemService.SetBuzzerConfig(buzzConfig);
        }
    }

}
