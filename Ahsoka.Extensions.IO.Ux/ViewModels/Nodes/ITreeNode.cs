using Material.Icons;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ahsoka.Extensions.IO.UX.ViewModels.Nodes;

internal interface ITreeNode<T> : ITreeNode
{
    public ObservableCollection<T> Children { get; init; }
}

internal interface ITreeNode
{
    public bool IsEnabled { get; set; }

    public bool IsEditable { get; }

    public string NodeDescription { get; set; }

    public MaterialIconKind Icon { get; }

    public IEnumerable<ITreeNode> GetChildren();
}
