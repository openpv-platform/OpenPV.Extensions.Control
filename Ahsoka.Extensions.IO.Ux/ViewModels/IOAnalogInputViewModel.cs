using Ahsoka.Core.Utility;
using Ahsoka.DeveloperTools.Core;
using Ahsoka.Extensions.IO.UX.ViewModels.Nodes;
using Ahsoka.Services.IO;
using Material.Icons;
using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class IOAnalogInputViewModel : ChildViewModelBase<IOSetupViewModel>, ITreeNode
{
    #region Fields
    readonly ICustomerToolViewModel viewModelInterface;
    private IOCurveViewModel selectedCurve;
    #endregion

    #region Properties
    public AnalogInputConfiguration PortConfiguration { get; init; }

    public uint ChannelNum
    {
        get => PortConfiguration != null ? PortConfiguration.ChannelNum : 0;
    }

    public IOCurveViewModel SelectedCurve
    {
        get => selectedCurve;
        set
        {
            selectedCurve = value;

            PortConfiguration.CurveId = value != null ? value.CurveDefinition.Id : null;

            OnPropertyChanged();
        }
    }

    public uint DigitalThreshold
    {
        get => PortConfiguration.DigitalThreshold;
        set
        {
            PortConfiguration.DigitalThreshold = value;
            OnPropertyChanged();
        }
    }

    public uint DigitalHysteresisPercent
    {
        get => PortConfiguration.DigitalHysteresisPercent;
        set
        {
            PortConfiguration.DigitalHysteresisPercent = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Methods
    public IOAnalogInputViewModel(IOSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, AnalogInputConfiguration portConfiguration = null)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (portConfiguration == null)
        {
            portConfiguration = new() { ChannelNum = (uint)setupViewModel.AnalogInputs.Count };
            setupViewModel.IOApplicationConfiguration.IOConfiguration.AnalogInputs.Add(portConfiguration);
        }

        PortConfiguration = portConfiguration;
    }

    public void ClearSelectedCurve()
    {
        SelectedCurve = null;
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
        get => $"Analog Input {ChannelNum}";
        set { }
    }

    public MaterialIconKind Icon => MaterialIconKind.AlphaACircle;

    public IEnumerable<ITreeNode> GetChildren()
    {
        return Enumerable.Empty<ITreeNode>();
    }
    #endregion
}
