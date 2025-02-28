using Ahsoka.Core.Utility;
using Avalonia;
using Avalonia.Svg.Skia;
using System;

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
        // Initialing Assembly Resolver
        AssemblyResolver.Init();

        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);
        GC.KeepAlive(typeof(SvgImage).Assembly);

        var builder = BuildAvaloniaApp();

        return builder.StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);

    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont()
            .UsePlatformDetect();

}
