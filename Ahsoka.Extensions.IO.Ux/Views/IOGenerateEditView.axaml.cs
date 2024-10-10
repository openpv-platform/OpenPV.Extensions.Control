using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ahsoka.DeveloperTools.Views;

public partial class IOGenerateEditView : UserControl
{
    public IOGenerateEditView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
