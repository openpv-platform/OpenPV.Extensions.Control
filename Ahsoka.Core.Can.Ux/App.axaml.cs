using Ahsoka.DeveloperTools.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools;

internal class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        base.OnFrameworkInitializationCompleted();
    }

    public override void RegisterServices()
    {
        base.RegisterServices();
    }
}
