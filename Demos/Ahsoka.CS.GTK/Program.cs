using Ahsoka.Core;
using Ahsoka.Core.Utility;
using Gtk;

namespace Ahsoka.AnalogDemo.GTK;

public class Program
{
    static readonly bool useDebugger = false;

    public static void Main()
    {
        // Show Warnings and Errors as well as Performance Metrics
        AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Medium | AhsokaVerbosity.Performance;
        using var stopwatch = new AhsokaStopwatch("Ahsoka.Demo.GTK.Main()");

        if (useDebugger)
            DebugUtility.WaitForDebugger();

        // Run Application
        RunApplication();

        // Close Client
        ApplicationContext.Exit();
    }

    static void RunApplication()
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"============ Inside Program.RunApplication()\n");

        // Init GTK Application
        Gtk.Application.Init();

        // Create Main Window and Run the Applicaiton
        using Core.Drawing.MainWindow window = new();
        {

            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"============ Program.RunApplication DisplayScreen.window new\n");
            window.DeleteEvent += WindowClosed;

            window.ShowAll();
            window.Show();

            Gtk.Application.Run();
        }
    }


    private static void WindowClosed(object o, DeleteEventArgs args)
    {
        Gtk.Application.Quit();
    }
}
