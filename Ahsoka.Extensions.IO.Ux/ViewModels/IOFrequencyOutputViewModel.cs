using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Core;
using Ahsoka.Extensions.IO.UX.ViewModels.Nodes;
using Ahsoka.Services.IO;
using Material.Icons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class IOFrequencyOutputViewModel : ChildViewModelBase<IOSetupViewModel>, ITreeNode
{
    #region Fields
    readonly ICustomerToolViewModel viewModelInterface;
    #endregion

    #region Properties
    public FrequencyOutputConfiguration PortConfiguration { get; init; }

    public uint ChannelNum
    {
        get => PortConfiguration != null ? PortConfiguration.ChannelNum : 0;
    }

    public PorBehavior[] PorBehaviors { get; init; } = Enum.GetValues<PorBehavior>();

    public LocBehavior[] LocBehaviors { get; init; } = Enum.GetValues<LocBehavior>();

    public uint DutyCycle
    {
        get => PortConfiguration.DutyCycle;
        set
        {
            PortConfiguration.DutyCycle = value;
            OnPropertyChanged();
        }
    }

    public uint Frequency
    {
        get => PortConfiguration.Frequency;
        set
        {
            PortConfiguration.Frequency = value;
            OnPropertyChanged();
        }
    }

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
    public IOFrequencyOutputViewModel(IOSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, FrequencyOutputConfiguration portConfiguration = null)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (portConfiguration == null)
        {
            portConfiguration = new() { ChannelNum = (uint)setupViewModel.FrequencyOutputs.Count };
            setupViewModel.IOApplicationConfiguration.IOConfiguration.FrequencyOutputs.Add(portConfiguration);
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
        get => $"Frequency Output {ChannelNum}";
        set { }
    }

    public MaterialIconKind Icon => MaterialIconKind.AlphaFCircleOutline;

    public IEnumerable<ITreeNode> GetChildren()
    {
        return Enumerable.Empty<ITreeNode>();
    }
    #endregion
}
