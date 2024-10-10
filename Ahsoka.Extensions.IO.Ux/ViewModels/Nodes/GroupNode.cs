using Avalonia.Controls;
using Material.Icons;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ahsoka.Extensions.IO.UX.ViewModels.Nodes;

internal class GroupNode<T> : ITreeNode<T>
{
    public ObservableCollection<T> Children { get; init; } = new();

    public bool IsEnabled { get; set; } = true;

    public bool IsEditable { get; set; } = false;

    public string NodeDescription { get; set; }

    public MaterialIconKind Icon { get; set; }

    public bool IsExpanded { get; internal set; }

    public IEnumerable<ITreeNode> GetChildren() { return Children.Cast<ITreeNode>(); }
}


