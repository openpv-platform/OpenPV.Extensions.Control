using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Core;
using Ahsoka.Extensions.IO.UX.ViewModels.Nodes;
using Ahsoka.Services.IO;
using Material.Icons;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class IOFrequencyInputViewModel : ChildViewModelBase<IOSetupViewModel>, ITreeNode
{
    #region Fields
    readonly ICustomerToolViewModel viewModelInterface;
    #endregion

    #region Properties
    public FrequencyInputConfiguration PortConfiguration { get; init; }

    public uint ChannelNum
    {
        get => PortConfiguration != null ? PortConfiguration.ChannelNum : 0;
    }
    #endregion

    #region Methods
    public IOFrequencyInputViewModel(IOSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, FrequencyInputConfiguration portConfiguration = null)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (portConfiguration == null)
        {
            portConfiguration = new() { ChannelNum = (uint)setupViewModel.FrequencyInputs.Count };
            setupViewModel.IOApplicationConfiguration.IOConfiguration.FrequencyInputs.Add(portConfiguration);
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
        get => $"Frequency Input {ChannelNum}";
        set { }
    }

    public MaterialIconKind Icon => MaterialIconKind.AlphaFCircle;

    public IEnumerable<ITreeNode> GetChildren()
    {
        return Enumerable.Empty<ITreeNode>();
    }
    #endregion
}
