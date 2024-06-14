using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Services.Can;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ahsoka.DeveloperTools;

internal class DiagnosticEventViewModel : ChildViewModelBase<CanSetupViewModel>
{
    const string dtc = "OBD DTC";
    const string dm = "J1939 DM";
    UserControl currentView;
    ICustomerToolViewModel viewModelInterface;

    bool isSelected = false;
 
    public DiagnosticEventDefinition EventDefinition { get; set; }

    public bool IsJ1939
    {
        get { return EventDefinition.ShouldSerializeJ1939Dm(); }
        set
        {

        }
    }


    public List<DTCFault> FaultTypes
    {
        get;
    } = new List<DTCFault>() { DTCFault.Powertrain, DTCFault.Network, DTCFault.Body, DTCFault.Chassis };

    public List<J1930Lamp> LampTypes
    {
        get;
    } = new List<J1930Lamp>() { J1930Lamp.Yellow, J1930Lamp.Red, J1930Lamp.Status3, J1930Lamp.Status4 };

    public List<string> EventTypes
    { 
        get;
    } = new List<string>() { dm, dtc };

    public string EventType
    {
        get { return EventDefinition.ShouldSerializeJ1939Dm() ? dm : dtc; }
        set
        {
            if (value != EventType)
                ToggleEventType();
        }
    }
   
    [MaxLength(255)]
    public string Comment
    {
        get { return EventDefinition.Comment; }
        set
        {
            EventDefinition.Comment = value;
            OnPropertyChanged();
        }
    }

    [MaxLength(255)]
    public string Name
    {
        get { return EventDefinition.Name; }
        set
        {
            EventDefinition.Name = value;
            OnPropertyChanged();
        }
    }

    public uint Address
    {
        get { return EventDefinition.Address; }
        set
        {
            EventDefinition.Address = value;
            OnPropertyChanged();
        }
    }
    public bool IsSelected
    {
        get { return isSelected; }
        set
        {
            isSelected = value; OnPropertyChanged();
            OnPropertyChanged(nameof(IconColor));
            OnPropertyChanged(nameof(BackColor));
        }
    }


    public IBrush IconColor
    {
        get { return IsSelected ? Brushes.SteelBlue : Brushes.Gainsboro; }
        set { }
    }

    public IBrush BackColor
    {
        get { return IsSelected ? Brushes.WhiteSmoke : Brushes.White; }
        set { }
    }

    public DiagnosticEventViewModel( CanSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, DiagnosticEventDefinition definition)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;
        if (definition == null)
        {
            definition = new DiagnosticEventDefinition() { Name = "New Event", Address = 255, ObdDtc = new() { Code = 1, VehicleSystem = 1, ManufacturerCode = 0, FaultType = DTCFault.Powertrain } };
            setupViewModel.CanClientCalibration.DiagnosticEvents.Add(definition);
        }

        this.EventDefinition = definition;
    }

    public void ShowEditor()
    {
        currentView = new CANDiagnosticEventEditView() { DataContext = this };
        viewModelInterface.ShowPopup(currentView);
    }

    public void CloseEditor()
    {
        viewModelInterface.DismissPopup(currentView);
        currentView = null;
        OnPropertyChanged(nameof(Id));
    }

    public void ToggleEventType()
    {
        if (EventDefinition.ShouldSerializeJ1939Dm())
        {
            EventDefinition.ObdDtc = new OBDEventInfo() { FaultType = DTCFault.Powertrain };
        }
        else
        {
            EventDefinition.J1939Dm = new J1939EventInfo() { Spn = 123, Fmi = 1};
        }

        OnPropertyChanged(nameof(EventType));
        OnPropertyChanged(nameof(Id));
        OnPropertyChanged(nameof(IsJ1939));
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Address));
    }

    public string Id
    {
        get
        {
            return EventDefinition.ShouldSerializeJ1939Dm() ? $"{EventDefinition.J1939Dm.Spn} / {EventDefinition.J1939Dm.Fmi}" : $"{GetFaultCode(EventDefinition.ObdDtc)}";
        }
    }

    private object GetFaultCode(OBDEventInfo obdDtc)
    {
        return string.Concat(obdDtc.FaultType.ToString().AsSpan(0, 1), obdDtc.ManufacturerCode.ToString(), obdDtc.VehicleSystem.ToString(), obdDtc.Code.ToString("00"));
    }
}