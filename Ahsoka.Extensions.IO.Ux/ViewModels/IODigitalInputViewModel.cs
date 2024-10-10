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

internal class IODigitalInputViewModel : ChildViewModelBase<IOSetupViewModel>, ITreeNode
{
    #region Fields
    readonly ICustomerToolViewModel viewModelInterface;
    #endregion

    #region Properties
    public DigitalInputConfiguration PortConfiguration { get; init; }

    public uint ChannelNum
    {
        get => PortConfiguration != null ? PortConfiguration.ChannelNum : 0;
    }

    public DigitalInputType[] DigitalInputTypes { get; init; } = Enum.GetValues<DigitalInputType>();

    public DigitalInputType InputType
    {
        get => PortConfiguration.InputType;
        set
        {
            PortConfiguration.InputType = value;
            OnPropertyChanged();
        }
    }

    public uint Threshold
    {
        get => PortConfiguration.Threshold;
        set
        {
            PortConfiguration.Threshold = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Methods
    public IODigitalInputViewModel(IOSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, DigitalInputConfiguration portConfiguration = null)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (portConfiguration == null)
        {
            portConfiguration = new() { ChannelNum = (uint)setupViewModel.DigitalInputs.Count };
            setupViewModel.IOApplicationConfiguration.IOConfiguration.DigitalInputs.Add(portConfiguration);
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
        get => $"Digital Input {ChannelNum}";
        set { }
    }

    public MaterialIconKind Icon => MaterialIconKind.AlphaDCircle;

    public IEnumerable<ITreeNode> GetChildren()
    {
        return Enumerable.Empty<ITreeNode>();
    }
    #endregion
}
