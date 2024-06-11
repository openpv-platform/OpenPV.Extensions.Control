using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools.Views;

public partial class CANMessageSignalView : UserControl
{
    public CANMessageSignalView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
