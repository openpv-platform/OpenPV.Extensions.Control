using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Services.Can;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Microsoft.Extensions.Azure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class NodeViewModel : ChildViewModelBase<CanSetupViewModel>
{
    UserControl currentView;

    PortViewModel selectedPort;
    bool isSelected = false;
    string labelOne, labelTwo, labelThree = string.Empty;
    string addressDetails = string.Empty;
    private ICustomerToolViewModel viewModelInterface;

    public NodeDefinition NodeDefinition { get; set; }

    public TransportProtocol[] TransportProtocols { get; init; } = Enum.GetValues<TransportProtocol>();

    public TransportProtocol TransportProtocol
    {
        get { return NodeDefinition.TransportProtocol; }
        set
        {
            NodeDefinition.TransportProtocol = value;
            IsJ1939 = NodeDefinition.TransportProtocol == TransportProtocol.J1939;
            ParentViewModel.SetStandardNode(IsJ1939, "ANY");
            ParentViewModel.UpdateAddressClaim();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsJ1939));
        }
    }

    public NodeAddressType[] NodeAddressTypes { get; init; } = Enum.GetValues<NodeAddressType>();

    public NodeAddressType NodeAddressType
    {
        get { return NodeDefinition.J1939Info.AddressType; }
        set
        {
            NodeDefinition.J1939Info.AddressType = value;
            ParentViewModel.UpdateAddressClaim();
            UpdateAddressLabels();
            OnPropertyChanged();
        }
    }

    public ObservableCollection<PortViewModel> Ports
    {
        get { return ParentViewModel.Ports;  }
    }

    public PortViewModel SelectedPort
    {
        get { return selectedPort; }
        set 
        { 
            selectedPort = value; 
            if (value != null)
                NodeDefinition.Ports = new int[] { (int)value.Port };
            OnPropertyChanged();
        }
    }

    [MaxLength(255)]
    public string Comment
    {
        get { return NodeDefinition.Comment; }
        set
        {
            NodeDefinition.Comment = value;
            OnPropertyChanged();
        }
    }

    [MaxLength(255)]
    public string Name
    {
        get { return NodeDefinition.Name; }
        set
        {
            NodeDefinition.Name = value;
            OnPropertyChanged();
        }
    }

    public bool IsSelf
    {
        get { return NodeDefinition.NodeType == NodeType.Self; }
        set
        {
            if (value)
            {
                ParentViewModel.SetStandardNode(IsJ1939, "ANY");
                foreach (var item in ParentViewModel.Nodes.Where(x => x.NodeDefinition.NodeType != NodeType.Any))
                    if (item != this)
                        item.IsSelf = false;

                NodeDefinition.NodeType = NodeType.Self;
            }
            else
            {
                NodeDefinition.NodeType = NodeType.UserDefined;
            }

            ParentViewModel.UpdateAddressClaim();
            RefreshAddressDetails();
            OnPropertyChanged();
        }
    }

    public bool IsEditable
    {
        get { return NodeDefinition.NodeType == NodeType.UserDefined || NodeDefinition.NodeType == NodeType.Self; }
    }


    public bool IsJ1939
    {
        get;
        set;
    }

    public string AddressDetails
    {
        get { return addressDetails; }
        set { addressDetails = value; OnPropertyChanged(); }
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

    public string LabelOne
    {
        get { return labelOne; }
        set
        {
            labelOne = value;
            OnPropertyChanged();
        }
    }

    public string LabelTwo
    {
        get { return labelTwo; }
        set
        {
            labelTwo = value;
            OnPropertyChanged();
        }
    }

    public string LabelThree
    {
        get { return labelThree; }
        set
        {
            labelThree = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 7)]
    public uint IndustryGroup
    {
        get { return NodeDefinition.J1939Info.IndustryGroup; }
        set
        {
            NodeDefinition.J1939Info.IndustryGroup = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 15)]
    public uint VehicleSystemInstance
    {
        get { return NodeDefinition.J1939Info.VehicleSystemInstance; }
        set
        {
            NodeDefinition.J1939Info.VehicleSystemInstance = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 127)]
    public uint VehicleSystem
    {
        get { return NodeDefinition.J1939Info.VehicleSystem; }
        set
        {
            NodeDefinition.J1939Info.VehicleSystem = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 255)]
    public uint Function
    {
        get { return NodeDefinition.J1939Info.Function; }
        set
        {
            NodeDefinition.J1939Info.Function = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 31)]
    public uint FunctionInstance
    {
        get { return NodeDefinition.J1939Info.FunctionInstance; }
        set
        {
            NodeDefinition.J1939Info.FunctionInstance = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 7)]
    public uint ECUInstance
    {
        get { return NodeDefinition.J1939Info.ECUinstance; }
        set
        {
            NodeDefinition.J1939Info.ECUinstance = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 2047)]
    public uint ManufacturerCode
    {
        get { return NodeDefinition.J1939Info.ManufacturerCode; }
        set
        {
            NodeDefinition.J1939Info.ManufacturerCode = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 253)]
    public uint ACMin
    {
        get
        {
            var success = uint.TryParse(NodeDefinition.J1939Info.Addresses.Split(",")[0], out uint value);
            return success ? value : 0;
        }
        set
        {
            NodeDefinition.J1939Info.Addresses = $"{value},{ACMax}";
            ParentViewModel.UpdateAddressClaim();
            OnPropertyChanged();
        }

    }

    [Range(0, 253)]
    public uint ACMax
    {
        get
        {
            var success = uint.TryParse(NodeDefinition.J1939Info.Addresses.Split(",")[1], out uint value);
            return success ? value : 0;
        }
        set
        {
            NodeDefinition.J1939Info.Addresses = $"{ACMin},{value}";
            ParentViewModel.UpdateAddressClaim();
            OnPropertyChanged();
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

    public NodeViewModel(CanSetupViewModel setupViewModel, ICustomerToolViewModel viewModelRoot, NodeDefinition definition)
        : base(setupViewModel)
    {
        viewModelInterface = viewModelRoot;
        if (definition == null)
        {
            definition = new NodeDefinition() { J1939Info = new(), Name = "New Node", Id = 0, Ports = Array.Empty<int>() };

            // FindNode Index
            for (int i = 0; i < 255; i++)
                if (!setupViewModel.Nodes.Any(x => x.NodeDefinition.Id == i))
                {
                    definition.Id = i;
                    definition.Name += $" {i}";
                    break;
                }

            setupViewModel.CanClientCalibration.Nodes.Add(definition);

        }        

        NodeDefinition = definition;
        if (NodeDefinition.J1939Info?.Addresses == "")
            NodeDefinition.J1939Info.Addresses = "0,0";
        IsJ1939 = NodeDefinition.TransportProtocol == TransportProtocol.J1939;

        // Load the Ports in the List
        if (NodeDefinition.Name != "ANY")
            SelectedPort = Ports.FirstOrDefault(x => x.Port == NodeDefinition.Ports?.FirstOrDefault());

        UpdateAddressLabels();
        RefreshAddressDetails();
    }

    internal void ToggleTransport()
    {
        var list = Enum.GetValues(typeof(TransportProtocol)).Cast<TransportProtocol>();

        if (this.TransportProtocol == list.Last())
            this.TransportProtocol = list.First();
        else
            this.TransportProtocol = (TransportProtocol)((int)this.TransportProtocol + 1);

        RefreshAddressDetails();

    }

    private void RefreshAddressDetails()
    {
        switch (TransportProtocol)
        {
            case TransportProtocol.Raw:
                this.AddressDetails = "No Address Information";
                break;
            case TransportProtocol.J1939:
                this.AddressDetails = IsSelf ? $"J1939 Name: 0x{new J1939Helper.Name(NodeDefinition.J1939Info).WriteToUlong(0):X16}" : $"{NodeDefinition.J1939Info.AddressType.ToString().SplitCamelCase()} ({GetAddressDetail()})";
                break;
            case TransportProtocol.IsoTp:
                this.AddressDetails = "Not Supported";
                break;
            default:
                break;
        }

    }

    private string GetAddressDetail()
    {
        if (NodeDefinition.J1939Info != null)
        {
            switch (NodeDefinition.J1939Info.AddressType)
            {
                case NodeAddressType.Static:
                case NodeAddressType.SystemAddress:
                    return $"{NodeDefinition.J1939Info.AddressValueOne}";
                case NodeAddressType.SystemFunctionAddress:
                    return $"{NodeDefinition.J1939Info.AddressValueOne},{NodeDefinition.J1939Info.AddressValueTwo}";
                case NodeAddressType.SystemInstanceAddress:
                    return $"{NodeDefinition.J1939Info.AddressValueOne},{NodeDefinition.J1939Info.AddressValueTwo},{NodeDefinition.J1939Info.AddressValueThree}";
            }
        }

        return string.Empty;
    }

    public void ShowEditor()
    {
        currentView = new CANNodeEditView() { DataContext = this };
        viewModelInterface.ShowPopup(currentView);
    }

    public void CloseEditor()
    {
        viewModelInterface.DismissPopup(currentView);
        currentView = null;
        RefreshAddressDetails();
    }

    private void UpdateAddressLabels()
    {
        LabelOne = LabelTwo = LabelThree = string.Empty;
        if (NodeDefinition.J1939Info != null)
        {
            switch (NodeDefinition.J1939Info.AddressType)
            {
                case NodeAddressType.Static:
                    LabelOne = "Source Address:";
                    break;
                case NodeAddressType.SystemAddress:
                    LabelOne = "Vehicle System:";
                    break;
                case NodeAddressType.SystemFunctionAddress:
                    LabelOne = "Vehicle System:";
                    LabelTwo = "Function Address:";
                    break;
                case NodeAddressType.SystemInstanceAddress:
                    LabelOne = "Vehicle System:";
                    LabelTwo = "Function Address:";
                    LabelThree = "Function Instance:";
                    break;
                default:
                    break;
            }
        }
    }   

    public override string ToString()
    {
        if (NodeDefinition.Id == -1)
            return "(No Node)";

        return NodeDefinition.NodeType == NodeType.Self ?
            $"{NodeDefinition.Id}: {NodeDefinition.Name} (Self)" :
            $"{NodeDefinition.Id}: {NodeDefinition.Name}";
    }
}
