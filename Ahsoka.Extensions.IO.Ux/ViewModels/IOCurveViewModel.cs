using Ahsoka.DeveloperTools.Core;
using Ahsoka.Extensions.IO.UX.ViewModels.Nodes;
using Ahsoka.Services.IO;
using Ahsoka.Utility;
using Avalonia.Controls;
using Material.Icons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class IOCurveViewModel : ChildViewModelBase<IOSetupViewModel>, ITreeNode
{
    #region Fields
    readonly ICustomerToolViewModel viewModelInterface;
    readonly ObservableCollection<Coordinate> coordinates = new();
    Coordinate selectedCoordinate;
    #endregion

    #region Properties
    public CurveDefinition CurveDefinition { get; init; }

    public string Name 
    {
        get => CurveDefinition.Name;
        set
        {
            CurveDefinition.Name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(NodeDescription));
        }
    }

    public AnalogInputType[] InputTypes { get; init; } = Enum.GetValues<AnalogInputType>();

    public AnalogInputType InputType
    {
        get => CurveDefinition.InputType;
        set
        {
            CurveDefinition.InputType = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<Coordinate> Coordinates
    { 
        get => coordinates;
    }

    public Coordinate SelectedCoordinate
    {
        get => selectedCoordinate;
        set
        {
            selectedCoordinate = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Methods
    public IOCurveViewModel(IOSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, CurveDefinition curveDefinition = null)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (curveDefinition == null)
        {
            curveDefinition = new() { Name = "New Curve", Id = Guid.NewGuid().ToByteArray() };
            setupViewModel.IOApplicationConfiguration.IOConfiguration.Curves.Add(curveDefinition);
        }

        CurveDefinition = curveDefinition;

        // Sort coordinates, first by X, then by Y
        CurveDefinition.Coordinates.Sort(delegate(Coordinate a, Coordinate b)
        {
            return a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y);
        });

        // Add coordinates
        foreach (var item in CurveDefinition.Coordinates)
            Coordinates.Add(item);
    }

    public override string ToString()
    {
        return Name;
    }

    internal void AddCoordinate()
    {
        var newItem = new Coordinate();

        CurveDefinition.Coordinates.Add(newItem);
        Coordinates.Add(newItem);

        OnPropertyChanged(nameof(Coordinates));
    }

    internal async void RemoveCoordinate()
    {
        var continueWork = await viewModelInterface.ShowDialog("Remove Coordinate", "Are you sure you wish to remove the selected coordinate?", "Yes", "Cancel");
        if (!continueWork)
            return;

        this.CurveDefinition.Coordinates.Remove(SelectedCoordinate);
        this.Coordinates.Remove(SelectedCoordinate);
    }
    #endregion

    #region ITreeNode
    public bool IsEnabled { get; set; } = false;

    public bool IsEditable { get; } = false;

    public string NodeDescription
    {
        get => string.IsNullOrEmpty(Name) ? "(no name)" : Name;
        set { }
    }

    public MaterialIconKind Icon => MaterialIconKind.SineWave;

    public IEnumerable<ITreeNode> GetChildren()
    {
        return Enumerable.Empty<ITreeNode>();
    }
    #endregion
}
