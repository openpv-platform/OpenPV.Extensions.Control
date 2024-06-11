using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Services.Can;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Linq;

namespace Ahsoka.DeveloperTools;
internal class PortViewModel : ChildViewModelBase<CanSetupViewModel>
{
    UserControl currentView;
    private readonly ICustomerToolViewModel viewModelInterface;
    bool isSelected = false;
    bool isEnabled = false;
    string promiscuousDetails;

    public PortDefinition PortDefinition { get; set; }

    public override string ToString()
    {
        return $"CAN Port {Port}";
    }

    public uint Port 
    {
        get { return PortDefinition.Port; }
        set
        {
            PortDefinition.Port = value;
            OnPropertyChanged();
        }
    }

    public CanInterface[] CanInterfaces { get; init; } = Enum.GetValues<CanInterface>();

    public bool PromiscuousTransmit
    {
        get { return PortDefinition.PromiscuousTransmit; }
        set
        {
            PortDefinition.PromiscuousTransmit = value;
            OnPropertyChanged();
            RefreshPromiscuousDetails();
        }
    }

    public bool PromiscuousReceive
    {
        get { return PortDefinition.PromiscuousReceive; }
        set
        {
            PortDefinition.PromiscuousReceive = value;
            OnPropertyChanged();
            RefreshPromiscuousDetails();
        }
    }

    public CanInterface CanInterface
    {
        get { return PortDefinition.CanInterface; }
        set
        {
            PortDefinition.CanInterface = value;
            OnPropertyChanged();
        }
    }

    public CanBaudRate[] BaudRates { get; init; } = Enum.GetValues<CanBaudRate>();

    public CanBaudRate BaudRate
    {
        get { return PortDefinition.BaudRate; }
        set
        {
            PortDefinition.BaudRate = value;
            OnPropertyChanged();
        }
    }

    public string PromiscuousDetails
    {
        get { return promiscuousDetails; }
        set
        {
            promiscuousDetails = value;
            OnPropertyChanged();
        }
    }

    public bool IsEnabled
    {
        get { return isEnabled; }
        set
        {
            isEnabled = value;
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

    public PortViewModel( CanSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, PortDefinition definition)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        PortDefinition = definition;
        RefreshPromiscuousDetails();
    }

    internal void ToggleInterface()
    {
        var list = Enum.GetValues(typeof(CanInterface)).Cast<CanInterface>();

        if (this.CanInterface == list.Last())
            this.CanInterface = list.First();
        else
            this.CanInterface = (CanInterface)((int)this.CanInterface + 1);
    }

    internal void ToggleBaud()
    {
        var list = Enum.GetValues(typeof(CanBaudRate)).Cast<CanBaudRate>();

        if (this.BaudRate == list.Last())
            this.BaudRate = list.First();
        else
            this.BaudRate = (CanBaudRate)((int)this.BaudRate + 1);
    }
    
    public void ShowEditor()
    {
        currentView = new CANPortEditView() { DataContext = this };
        viewModelInterface.ShowPopup(currentView);
    }

    public void CloseEditor()
    {
        viewModelInterface.DismissPopup(currentView);
        currentView = null;
        RefreshPromiscuousDetails();
    }

    private void RefreshPromiscuousDetails()
    {
        var details = "Promiscuous enabled for ";
        if (!PromiscuousReceive && !PromiscuousTransmit)
            details = "";
        if (PromiscuousReceive)
            details += "receive";
        if (PromiscuousReceive && PromiscuousTransmit)
            details += "/";
        if (PromiscuousTransmit)
            details += "transmit";

        this.PromiscuousDetails = details;
    }  
}
