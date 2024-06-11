using Avalonia;
using Avalonia.Svg.Skia;
using System;
using System.Threading;

namespace Ahsoka.DeveloperTools;

// Face Application to support Xaml Desiogner
internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        return 0;
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont()
            .UsePlatformDetect();

}
