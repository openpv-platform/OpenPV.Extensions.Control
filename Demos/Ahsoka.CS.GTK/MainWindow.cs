using Ahsoka.Core.Dispatch;
using Ahsoka.Demo.GTK.Controllers;
using Ahsoka.Services.Data;
using Ahsoka.Services.IO;
using Ahsoka.Services.System;
using Gtk;
using Pango;

namespace Ahsoka.Core.Drawing;

public class MainWindow : Window
{
    #region Methods
    public MainWindow() : base(WindowType.Toplevel)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"============ DisplayScreen constructor \n");

        // Load Simple CSS Style for Sliders
        Gtk.CssProvider provider = new();
        provider.LoadFromData("scale slider {margin: 0px;min-width: 40px;min-height: 40px;} " +
            "scale value { margin-bottom: 20px; color: steelblue } " +
            "label {font-size: 12pt; }");
        Gtk.StyleContext.AddProviderForScreen(Gdk.Screen.Default, provider, 800);

        SetupWindow();

        InitWindow();
    }

    private void InitWindow()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"============ DisplayScreen InitWindow() \n");

        // Set Window Background to White with Old Style API
        this.ModifyBg(StateType.Normal, new Gdk.Color(0xFF, 0xFF, 0xFF));

        // Create VBOX for the Main U
        VBox mainPanel = new();
        this.Add(mainPanel);

        // Add Header to VBox 
        CreateHeader(mainPanel);

        // Add Main UI
        CreateUI(mainPanel);

        // Start Dispatcher (This will Release Startup As Well)
        Dispatcher.Default.StartAndRun(Application.Invoke);
    }

    private void CreateHeader(VBox mainPanel)
    {
        // Create a Header with Title and Close Button 
        HBox header = new();
        header.ModifyBg(StateType.Normal, new Gdk.Color(0xF1, 0xF1, 0xF1)); // Light Silver
        header.HeightRequest = 60;
        mainPanel.PackStart(header, false, true, 0);

        // Create Header Text
        Label label = new("Ahsoka.Demo.GTK");
        label.ModifyFont(new FontDescription() { Family = "Arial", Style = Pango.Style.Normal, Weight = Pango.Weight.Bold, Size = (int)(20 * Pango.Scale.PangoScale) });
        header.PackStart(label, false, false, 12);

        // Create an Application Close Button
        Button buttonClose = new("X")
        {
            WidthRequest = 20,
            HeightRequest = 20,
            Margin = 10
        };
        buttonClose.Clicked += (o, e) => { CloseApplication(); };
        header.PackEnd(buttonClose, false, false, 0);
    }

    private void CreateUI(VBox mainPanel)
    {
        DataServiceClient dataService = new();
        SystemServiceClient systemService = new();
        IOServiceClient ioService = new();

        // Main UI Creates Three Columes
        HBox mainUI = new()
        {
            Margin = 20
        };
        mainPanel.PackStart(mainUI, true, true, 0);

        Frame systemFrame = new("System")
        {
            LabelXalign = .5f,
            Margin = 10,
            MarginTop = 0
        };
        mainUI.PackStart(systemFrame, true, true, 0);

        // Construct Brightness Controller for This Panel
        var brightnessController = new BrightnessController(systemService);
        brightnessController.InitPanel(systemFrame);

        Frame buzzer = new("Buzzer")
        {
            LabelXalign = .5f,
            Margin = 10,
            MarginTop = 0
        };
        mainUI.PackStart(buzzer, true, true, 0);

        var buzzerController = new BuzzerController(ioService);
        buzzerController.InitPanel(buzzer);

        Frame ioFrame = new("Discrete IO Current Values")
        {
            LabelXalign = .5f,
            Margin = 10,
            MarginTop = 0
        };
        mainUI.Add(ioFrame);

        VBox ioBox = new()
        {
            Margin = 40,
            MarginTop = 20
        };
        ioFrame.Add(ioBox);

        var cpuTempController = new CPUTempController(systemService);
        cpuTempController.InitPanel(ioBox);

        var ioInController = new IOController(ioService, dataService);
        ioInController.InitPanel(ioBox);
    }

    private void SetupWindow()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"============ DisplayScreen SetupWindow() \n");

        int screenWidth = 800;
        int screenHeight = 480;

        // Show in a Window on Windows and Full Screen on Linux / Others
        if (SystemInfo.CurrentPlatform is PlatformFamily.Windows64 or
            PlatformFamily.MacOSArm64)
        {
            SetDefaultSize(screenWidth, screenHeight);
        }
        else
        {
            HideTitlebarWhenMaximized = true;
            Maximize();
        }
    }

    public void CloseApplication()
    {
        // Close Window
        this.Close();

        // Close Application and Process
        ApplicationContext.Exit();
    }
    #endregion
}
