using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools.Views;

public partial class CANMessageEditView : UserControl
{
    public CANMessageEditView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
