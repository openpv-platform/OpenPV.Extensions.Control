using Ahsoka.Core;
using Ahsoka.Core.Dispatch;
using Ahsoka.Services.System;
using Gtk;
using System;

namespace Ahsoka.Demo.GTK.Controllers;

internal class BrightnessController
{
    Scale brightScale = null;
    readonly BrightnessInfo brightnessModel = new() { Percentage = 90 };
    private readonly SystemServiceClient systemService;

    public BrightnessController(SystemServiceClient systemService)
    {
        this.systemService = systemService;
    }

    public void InitPanel(Frame fixedPanel)
    {
        // ** Initialize Brightness to a high level in case it had been set to 0 (black) before power cycle
        systemService.SetBrightness(brightnessModel);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"============ InitPanel Brightness \n");
        VBox box = new()
        {
            Margin = 20
        };
        fixedPanel.Add(box);

        // Brightness Label and Brightness Scale Control
        brightScale = new Scale(Orientation.Vertical, 1, 100, 10)
        {
            Value = brightnessModel.Percentage,
            Inverted = true
        };
        box.PackStart(brightScale, true, true, 10);

        var label = new Label("Brightness")
        {
            Valign = Align.End,
            Halign = Align.Center,
            HeightRequest = 50
        };
        box.PackEnd(label, false, false, 10);

        Dispatcher.Default.RegisterTimerCallback(HandleTimeoutMessage, TimeSpan.FromMilliseconds(250));
    }

    public void HandleTimeoutMessage(object model, EventArgs args)
    {
        // Time Dispatcher Runs in Main Thead so safe to do UI Updates 
        // Get Changes outside of User Scope...temp, etc.
        // but avoid long running operations.
        if (brightnessModel.Percentage != (int)brightScale.Value)
        {
            brightnessModel.Percentage = (int)brightScale.Value;
            systemService.SetBrightness(brightnessModel);
        }
    }

}

