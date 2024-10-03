using Avalonia.Controls;
using Material.Icons;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata.Ecma335;
using System.ServiceModel.Description;

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

    public string NodeDescription { get; set; }

    public MaterialIconKind Icon { get; }

    public IEnumerable<ICanTreeNode> GetChildren();
}
