using Ahsoka.DeveloperTools;
using Ahsoka.DeveloperTools.Views;
using Avalonia.Controls;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ahsoka.Extensions.Can.UX.ViewModels.Nodes;

internal class CanGroupNode<T> : ICanTreeNode<T>
{
    public ObservableCollection<T> Children
    {
        get;
        init;
    } = new();

    public bool IsEnabled { get; set; } = true;

    public bool IsEditable { get; set; } = false;

    public string NodeDescription { get; set; }

    public MaterialIconKind Icon
    {
        get;
        set;
    }

    public bool IsExpanded { get; internal set; }

    public IEnumerable<ICanTreeNode> GetChildren()
    {
        return Children.Cast<ICanTreeNode>();
    }

}


