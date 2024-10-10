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

internal class IOAnalogOutputViewModel : ChildViewModelBase<IOSetupViewModel>, ITreeNode
{
    #region Fields
    readonly ICustomerToolViewModel viewModelInterface;
    private IOCurveViewModel selectedCurve;
    #endregion

    #region Properties
    public AnalogOutputConfiguration PortConfiguration { get; init; }

    public uint ChannelNum
    {
        get => PortConfiguration != null ? PortConfiguration.ChannelNum : 0;
    }

    public PorBehavior[] PorBehaviors { get; init; } = Enum.GetValues<PorBehavior>();

    public LocBehavior[] LocBehaviors { get; init; } = Enum.GetValues<LocBehavior>();

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
    public IOAnalogOutputViewModel(IOSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, AnalogOutputConfiguration portConfiguration = null)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (portConfiguration == null)
        {
            portConfiguration = new() { ChannelNum = (uint)setupViewModel.AnalogOutputs.Count };
            setupViewModel.IOApplicationConfiguration.IOConfiguration.AnalogOutputs.Add(portConfiguration);
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
        get => $"Analog Output {ChannelNum}";
        set { }
    }

    public MaterialIconKind Icon => MaterialIconKind.AlphaACircleOutline;

    public IEnumerable<ITreeNode> GetChildren()
    {
        return Enumerable.Empty<ITreeNode>();
    }
    #endregion
}
