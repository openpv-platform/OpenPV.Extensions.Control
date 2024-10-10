using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools.Views;
public partial class IOAnalogInputEditView : UserControl
{
    public IOAnalogInputEditView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
