using Ahsoka.DeveloperTools.Core;
using Ahsoka.Extensions.IO.UX.ViewModels.Nodes;
using Ahsoka.Services.IO;
using Ahsoka.Utility;
using Avalonia.Controls;
using Material.Icons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class IODigitalOutputViewModel : ChildViewModelBase<IOSetupViewModel>, ITreeNode
{
    #region Fields
    readonly ICustomerToolViewModel viewModelInterface;
    #endregion

    #region Properties
    public DigitalOutputConfiguration PortConfiguration { get; init; }

    public uint ChannelNum
    {
        get => PortConfiguration != null ? PortConfiguration.ChannelNum : 0;
    }

    public PorBehavior[] PorBehaviors { get; init; } = Enum.GetValues<PorBehavior>();

    public LocBehavior[] LocBehaviors { get; init; } = Enum.GetValues<LocBehavior>();

    public PorBehavior PorBehavior
    {
        get => PortConfiguration.PorBehavior;
        set
        {
            PortConfiguration.PorBehavior = value;
            OnPropertyChanged();
        }
    }

    public LocBehavior LocBehavior
    {
        get => PortConfiguration.LocBehavior;
        set
        {
            PortConfiguration.LocBehavior = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Methods
    public IODigitalOutputViewModel(IOSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, DigitalOutputConfiguration portConfiguration = null)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (portConfiguration == null)
        {
            portConfiguration = new() { ChannelNum = (uint)setupViewModel.DigitalOutputs.Count };
            setupViewModel.IOApplicationConfiguration.IOConfiguration.DigitalOutputs.Add(portConfiguration);
        }

        PortConfiguration = portConfiguration;
    }

    public void RefreshNodeDescription()
    {
        OnPropertyChanged(nameof(NodeDescription));
    }
    #endregion

    #region ITreeNode
    public bool IsEnabled { get; set; } = false;

    public bool IsEditable { get; } = false;

    public string NodeDescription
    {
        get => $"Digital Output {ChannelNum}";
        set { }
    }

    public MaterialIconKind Icon => MaterialIconKind.AlphaDCircleOutline;

    public IEnumerable<ITreeNode> GetChildren()
    {
        return Enumerable.Empty<ITreeNode>();
    }
    #endregion
}
