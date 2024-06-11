using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools.Views;

public partial class CANDiagnosticEventView : UserControl
{
    public CANDiagnosticEventView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
