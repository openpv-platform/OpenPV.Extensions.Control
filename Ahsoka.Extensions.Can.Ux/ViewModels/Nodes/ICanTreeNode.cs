using Material.Icons;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ahsoka.Extensions.Can.UX.ViewModels.Nodes;

internal interface ICanTreeNode<T> : ICanTreeNode
{
    public ObservableCollection<T> Children
    {
        get;
        init;
    }
}

internal interface ICanTreeNode
{
    public bool IsEnabled { get; set; }

    public bool IsEditable { get; }

    public bool IsSelected { get; set; }

    public string NodeDescription { get; set; }

    public MaterialIconKind Icon { get; }

    public IEnumerable<ICanTreeNode> GetChildren();
}
