using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Extensions.Can.UX.ViewModels.Nodes;
using Ahsoka.Services.Can;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Media;
using Material.Icons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ahsoka.DeveloperTools;
internal class PortViewModel : ChildViewModelBase<CanSetupViewModel>, ICanTreeNode
{
    #region Fields
    UserControl currentView;
    private readonly ICustomerToolViewModel viewModelInterface;
    bool isEnabled = false;
    string promiscuousDetails;
    PortDefinition portDefinition;
    #endregion

    #region Props
    public uint Port 
    {
        get { return portDefinition.Port; }
        set
        {
            portDefinition.Port = value;
            OnPropertyChanged();
        }
    }

    public CanInterface[] CanInterfaces { get; init; } = Enum.GetValues<CanInterface>();

    public bool PromiscuousTransmit
    {
        get { return portDefinition.PromiscuousTransmit; }
        set
        {
            portDefinition.PromiscuousTransmit = value;
            OnPropertyChanged();
            RefreshPromiscuousDetails();
        }
    }

    public bool PromiscuousReceive
    {
        get { return portDefinition.PromiscuousReceive; }
        set
        {
            portDefinition.PromiscuousReceive = value;
            OnPropertyChanged();
            RefreshPromiscuousDetails();
        }
    }

    public CanInterface CanInterface
    {
        get { return portDefinition.CanInterface; }
        set
        {
            portDefinition.CanInterface = value;
            OnPropertyChanged();
        }
    }

    public CanBaudRate[] BaudRates { get; init; } = Enum.GetValues<CanBaudRate>();

    public CanBaudRate BaudRate
    {
        get { return portDefinition.BaudRate; }
        set
        {
            portDefinition.BaudRate = value;
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
            if (value)
            {
                var temp = ParentViewModel.CanConfiguration.Ports.FirstOrDefault(x => x.Port == this.Port);
                if (temp == null)
                    ParentViewModel.CanConfiguration.Ports.Add(this.portDefinition);
            }
            else
            {
                var portDef = ParentViewModel.CanConfiguration.Ports.FirstOrDefault(x => x.Port == this.Port);
                ParentViewModel.CanConfiguration.Ports.Remove(portDef);
            }

            isEnabled = value;
            OnPropertyChanged();
        }
    }
    #endregion

    #region Methods

    public PortViewModel( CanSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, PortDefinition definition)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;

        portDefinition = definition;
        RefreshPromiscuousDetails();
    }
  
    public override string ToString()
    {
        return $"CAN Port {Port}";
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
    #endregion

    #region TreeNode
    public UserControl UserControl
    {
        get;
        init;
    }

    public bool IsEditable { get; set; } = false;

    public string NodeDescription
    {
        get { return $"CAN Port {Port}"; }
        set { }
    }

    public MaterialIconKind Icon
    {
        get
        {
            return MaterialIconKind.Connection;
        }
    }

    public UserControl GetUserControl()
    {
        var view = ParentViewModel.PortEditView;
        view.DataContext = null;
        view.DataContext = this;
        return view;
    }

    public IEnumerable<ICanTreeNode> GetChildren() { return Enumerable.Empty<ICanTreeNode>(); }

    #endregion
}
