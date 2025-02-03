using Ahsoka.Core;
using Ahsoka.Core.Dispatch;
using Ahsoka.Services.System;
using Gtk;
using System;

namespace Ahsoka.Demo.GTK.Controllers;

internal class CPUTempController
{
    Label Lbl_CPUtemp = null;
    double tempC;
    private readonly SystemServiceClient systemService;

    public CPUTempController(SystemServiceClient systemService)
    {
        this.systemService = systemService;
    }

    public void InitPanel(VBox fixedPanel)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"============ InitPanel CPUTemp \n");

        // CPU Temperature Label
        Lbl_CPUtemp = new Label("CPU Temperature: Not Found")
        {
            Valign = Align.End,
            HeightRequest = 50
        };
        fixedPanel.Add(Lbl_CPUtemp);

        Dispatcher.Default.RegisterTimerCallback(HandleTimeoutMessage, TimeSpan.FromSeconds(1));
    }

    public void HandleTimeoutMessage(object model, EventArgs args)
    {
        // Time Dispatcher Runs in Main Thead so safe to do UI Updates 
        // but avoid long running operations.
        var newTemp = systemService.GetHardwareInformation().CpuTempInC;
        if (newTemp != tempC)
        {
            tempC = newTemp;
            Lbl_CPUtemp.Text = string.Format("{0} {1,10:F2} {2} {3,0:F2} {4}", "CPU Temperature: ", tempC, "C      ", tempC * (9.0 / 5.0) + 32.0, "F");
        }
    }
}
