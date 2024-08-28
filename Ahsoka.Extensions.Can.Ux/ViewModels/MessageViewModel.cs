using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Services.Can;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Linq;

namespace Ahsoka.DeveloperTools;

internal class MessageViewModel : ChildViewModelBase<CanSetupViewModel>
{
    #region Fields
    const int extendedFrameBitPosition = 31;
    const int PDU2Threshold = 240;
    const int BroadcastVal = 255;
    const int NodeDisabled = -1;

    UserControl currentView;
    UserControl currentSignalView;

    MessageSignalDefinition selectedSignal;
    bool isSelected = false;
    readonly J1939Helper.Id j1939Id = new();
    readonly ObservableCollection<MessageSignalDefinition> signals = new();
    readonly ObservableCollection<SignalModel> signalsModels = new();
    NodeViewModel port0SelectedTransmitter, port0SelectedReceiver, port1SelectedTransmitter, port1SelectedReceiver;
    private ICustomerToolViewModel viewModelInterface;
    #endregion

    #region ID Handling J1939 <-> RAW
    public bool IsJ1939 { get { return MessageType == MessageType.J1939ExtendedFrame; } }
    public bool IsPDU2 { get { return PDUF >= PDU2Threshold; } }

    public MessageType MessageType
    {
        get { return MessageDefinition.MessageType; }
        set
        {
            if (value != MessageDefinition.MessageType)
            {
                MessageDefinition.Id = NegateExtendedBit(MessageDefinition.Id);

                if (value == MessageType.J1939ExtendedFrame)
                {
                    MessageDefinition.Id = NegateExtendedBit(MessageDefinition.Id);
                    j1939Id.ExtractValues(MessageDefinition.Id);
                    MessageDefinition.SetAddressOnSend = true;
                }
                else
                    MessageDefinition.SetAddressOnSend = false;

                // Restore Extended Frame Bit
                if (value != MessageType.RawStandardFrame)
                    MessageDefinition.Id = AddExtendedBit(MessageDefinition.Id);
                else
                    MessageDefinition.Id = NegateExtendedBit(MessageDefinition.Id);
            }

            MessageDefinition.MessageType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IdMasked));
            OnPropertyChanged(nameof(IsJ1939));
        }
    }

    public uint Priority
    {
        get
        {
            return j1939Id.Priority;
        }
        set
        {
            j1939Id.Priority = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(J1939Id));
            OnPropertyChanged(nameof(IdMasked));
            
        }
    }

    public uint DataPage
    {
        get
        {
            return j1939Id.DataPage;
        }
        set
        {
            j1939Id.DataPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(J1939Id));
            OnPropertyChanged(nameof(IdMasked));
        }
    }

    public uint PDUF
    {
        get
        {
            return j1939Id.PDUF;
        }
        set
        {
            j1939Id.PDUF = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsPDU2));
            OnPropertyChanged(nameof(IdMasked));
        }
    }

    public uint Group
    {
        get
        {
            return j1939Id.PDUS;
        }
        set
        {
            j1939Id.PDUS = value;
            OnPropertyChanged();           
            OnPropertyChanged(nameof(IdMasked));
        }
    }

    public uint J1939Id
    {
        get
        {
            NodeViewModel Transmitter, Receiver = null;
            if (MessageDefinition.TransmitNodes[1] != NodeDisabled && MessageDefinition.ReceiveNodes[1] != NodeDisabled)
            {
                Transmitter = ParentViewModel.Nodes.First(x => x.NodeDefinition.Id == (MessageDefinition.TransmitNodes[1]));
                Receiver = ParentViewModel.Nodes.First(x => x.NodeDefinition.Id == (MessageDefinition.ReceiveNodes[1]));
            }
            else if (MessageDefinition.TransmitNodes[0] != NodeDisabled && MessageDefinition.ReceiveNodes[0] != NodeDisabled)
            {
                Transmitter = ParentViewModel.Nodes.First(x => x.NodeDefinition.Id == (MessageDefinition.TransmitNodes[0]));
                Receiver = ParentViewModel.Nodes.First(x => x.NodeDefinition.Id == (MessageDefinition.ReceiveNodes[0]));
            }
            else
                return j1939Id.WriteToUint();

            if (Transmitter.TransportProtocol != TransportProtocol.J1939)
                j1939Id.SourceAddress = 0;
            else if (Transmitter.IsSelf && Transmitter.ACMin >= Transmitter.ACMax)
                j1939Id.SourceAddress = Transmitter.ACMin;
            else if (Transmitter.IsSelf && Transmitter.ACMin < Transmitter.ACMax)
                j1939Id.SourceAddress = 0;
            else if (Transmitter.NodeDefinition.J1939Info.AddressType == NodeAddressType.Static)
                j1939Id.SourceAddress = (uint)Transmitter.NodeDefinition.J1939Info.AddressValueOne;
            else
                j1939Id.SourceAddress = 0;

            if (IsPDU2)
                j1939Id.PDUS = Group;
            else if (Receiver.TransportProtocol != TransportProtocol.J1939)
                j1939Id.PDUS = 0;
            else if (Receiver.IsSelf && Receiver.ACMin >= Receiver.ACMax)
                j1939Id.PDUS = Receiver.ACMin;
            else if (Receiver.IsSelf && Receiver.ACMin < Receiver.ACMax)
                j1939Id.PDUS = 0;
            else if (Receiver.NodeDefinition.J1939Info.AddressType == NodeAddressType.Static)
                j1939Id.PDUS = (uint)Receiver.NodeDefinition.J1939Info.AddressValueOne;
            else
                j1939Id.SourceAddress = 0;

            var id = j1939Id.WriteToUint();

            MessageDefinition.Id = AddExtendedBit(id);

            return id;
        }
    }

    public uint IdMasked
    {
        get
        {
            uint id;
            if (IsJ1939)
                id = J1939Id;
            else
                id = this.MessageDefinition.Id;

            // Remove Extended Frame 
            if (MessageType != MessageType.RawStandardFrame)
                id = NegateExtendedBit(id);

            return id;
        }
        set
        {
            if (MessageType == MessageType.J1939ExtendedFrame)
            {
                j1939Id.ExtractValues(value);
                MessageDefinition.Id = j1939Id.WriteToUint();
            }
            else
                MessageDefinition.Id = value;

            if (MessageType != MessageType.RawStandardFrame)
                MessageDefinition.Id = AddExtendedBit(MessageDefinition.Id);

            OnPropertyChanged();
        }
    }
    #endregion

    #region Roll Count and CRC
    public MessageSignalDefinition CrcSignal
    {
        get
        {
            return MessageDefinition.Signals.FirstOrDefault(x => x.StartBit == MessageDefinition.CrcBit);
        }
        set
        {
            if (value != null)
                MessageDefinition.CrcBit = value.StartBit;
        }
    }

    public MessageSignalDefinition RollCountSignal
    {
        get
        {
            return MessageDefinition.Signals.FirstOrDefault(x => x.StartBit == MessageDefinition.RollCountBit);
        }
        set
        {
            if (value != null)
            {
                MessageDefinition.RollCountBit = value.StartBit;
                MessageDefinition.RollCountLength = value.BitLength;
            }

        }
    }

    public bool HasCrc
    {
        get { return CrcType != CrcType.None; }
        set
        {
        }
    }

    public CrcType CrcType
    {
        get { return MessageDefinition.CrcType; }
        set
        {
            MessageDefinition.CrcType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasCrc));
        }
    }

    public bool HasRollCount
    {
        get { return MessageDefinition.HasRollCount; }
        set
        {
            MessageDefinition.HasRollCount = value;
            OnPropertyChanged();
        }
    }

    public uint RollCountBit
    {
        get { return MessageDefinition.RollCountBit; }
        set
        {
            MessageDefinition.RollCountBit = value;
            OnPropertyChanged();
        }
    }

    public uint RollCountLength
    {
        get { return MessageDefinition.RollCountLength; }
        set
        {
            MessageDefinition.RollCountLength = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Properties
    internal Ahsoka.Services.Can.ValueType[] ValueTypes { get; init; } = Enum.GetValues<Ahsoka.Services.Can.ValueType>();
    internal ByteOrder[] ByteOrders { get; init; } = Enum.GetValues<ByteOrder>();

    internal SignalModel SelectedSignalValue { get; set; }

    public ObservableCollection<MessageSignalDefinition> Signals { get { return signals; } }

    public ObservableCollection<SignalModel> SignalValues { get { return signalsModels; } }

    public MessageDefinition MessageDefinition { get; set; }

    public MessageSignalDefinition SelectedSignal
    {
        get { return selectedSignal; }
        set
        {
            selectedSignal = value; OnPropertyChanged();
        }
    }

    public enum TransmitOption
    {
        No_Node,
        Sending,
        Receiving
    }

    public TransmitOption[] TransmitOptions { get; init; } = Enum.GetValues<TransmitOption>();

    public CrcType[] CrcTypes { get; init; } = Enum.GetValues<CrcType>();

    public MuxRole[] MuxRoles { get; init; } = Enum.GetValues<MuxRole>();

    public MessageType[] MessageTypes { get; init; } = Enum.GetValues<MessageType>();

    public string Transmitter
    {
        get
        {
            var transmit = "";

            if (IsJ1939)
            {
                foreach (var node in MessageDefinition.TransmitNodes)
                {
                    var nodeVm = ParentViewModel.Nodes.FirstOrDefault(x => x.NodeDefinition.Id == node);
                    if (nodeVm != null && (nodeVm.SelectedPort == null || nodeVm.SelectedPort.IsEnabled))
                        transmit += $"{nodeVm.NodeDefinition.Id}: {nodeVm.Name}";
                }
            }              
            else
            {
                if (SelectedTransmitNode_P0?.SelectedPort?.IsEnabled ?? false)
                    transmit += $"0: {TransmitOption_P0} ";
                if (SelectedTransmitNode_P1?.SelectedPort?.IsEnabled ?? false)
                    transmit += $"1: {TransmitOption_P1} ";
            }
            return transmit;
        }
    }

    public ObservableCollection<NodeViewModel> Nodes_P0
    {
        get;
        set;
    } = new ObservableCollection<NodeViewModel>();

    public ObservableCollection<NodeViewModel> Nodes_P1
    {
        get;
        set;
    } = new ObservableCollection<NodeViewModel>();

    public NodeViewModel SelectedTransmitNode_P0
    {
        get { return port0SelectedTransmitter; }
        set
        {
            port0SelectedTransmitter = value; OnPropertyChanged();
            if (value != null)
                UpdateNodeValues(true, 0, value.NodeDefinition.Id);
        }
    }

    public NodeViewModel SelectedReceiveNode_P0
    {
        get { return port0SelectedReceiver; }
        set
        {
            port0SelectedReceiver = value; 
            OnPropertyChanged();
            if (value != null)
                UpdateNodeValues(false, 0, value.NodeDefinition.Id);
        }
    }

    public TransmitOption TransmitOption_P0
    {
        get 
        {
            if (port0SelectedTransmitter.NodeDefinition.Id < 0)
                return TransmitOption.No_Node;
            else if ((bool)(port0SelectedTransmitter.IsSelf))
                return TransmitOption.Sending;
            else
                return TransmitOption.Receiving;
        }
        set
        {
            if (value == TransmitOption.No_Node)
                SelectedTransmitNode_P0 = Nodes_P0.FirstOrDefault(x => !x.IsSelf &&
                    x.NodeDefinition.NodeType == NodeType.UserDefined && x.NodeDefinition.Id < 0);
            if (value == TransmitOption.Sending)
                SelectedTransmitNode_P0 = Nodes_P0.FirstOrDefault(x => x.IsSelf);
            else
                SelectedTransmitNode_P0 = Nodes_P0.FirstOrDefault(x => !x.IsSelf && 
                    x.NodeDefinition.NodeType == NodeType.UserDefined && x.NodeDefinition.Id >= 0);
            OnPropertyChanged();
        }
    }

    public NodeViewModel SelectedTransmitNode_P1
    {
        get { return port1SelectedTransmitter; }
        set
        {
            port1SelectedTransmitter = value; OnPropertyChanged();
            if (value != null)
                UpdateNodeValues(true, 1, value.NodeDefinition.Id);
        }
    }
    
    public NodeViewModel SelectedReceiveNode_P1
    {
        get { return port1SelectedReceiver; }
        set
        {
            port1SelectedReceiver = value;
            OnPropertyChanged();
            if (value != null)
                UpdateNodeValues(false, 1, value.NodeDefinition.Id);
        }
    }

    public TransmitOption TransmitOption_P1
    {
        get
        {
            if (port1SelectedTransmitter.NodeDefinition.Id < 0)
                return TransmitOption.No_Node;
            else if ((bool)(port1SelectedTransmitter.IsSelf))
                return TransmitOption.Sending;
            else
                return TransmitOption.Receiving;
        }
        set
        {
            if (value == TransmitOption.No_Node)
                SelectedTransmitNode_P1 = Nodes_P1.FirstOrDefault(x => !x.IsSelf &&
                    x.NodeDefinition.NodeType == NodeType.UserDefined && x.NodeDefinition.Id < 0);
            if (value == TransmitOption.Sending)
                SelectedTransmitNode_P1 = Nodes_P1.FirstOrDefault(x => x.IsSelf);
            else
                SelectedTransmitNode_P1 = Nodes_P1.FirstOrDefault(x => !x.IsSelf &&
                    x.NodeDefinition.NodeType == NodeType.UserDefined && x.NodeDefinition.Id >= 0);
            OnPropertyChanged();
        }
    }

    [MaxLength(255)]
    public string Name
    {
        get { return MessageDefinition.Name; }
        set
        {
            MessageDefinition.Name = value;
            OnPropertyChanged();
        }
    }

    public int Rate
    {
        get { return MessageDefinition.Rate; }
        set
        {
            MessageDefinition.Rate = value;
            OnPropertyChanged();
        }
    }

    public bool FilterReceipt
    {
        get { return MessageDefinition.FilterReceipts; }
        set
        {
            MessageDefinition.FilterReceipts = value;
            OnPropertyChanged();
        }
    }

    [Range(0, 60000)]
    public Int32 Timeout
    {
        get { return MessageDefinition.TimeoutMs; }
        set
        {
            MessageDefinition.TimeoutMs = value;
            OnPropertyChanged();
        }
    }

    [MaxLength(255)]
    public string Comment
    {
        get { return MessageDefinition.Comment; }
        set
        {
            MessageDefinition.Comment = value;
            OnPropertyChanged();
        }
    }

    public bool IsEditable
    {
        get { return MessageDefinition.UserDefined; }
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
    #endregion

    #region Methods

    public MessageViewModel(CanSetupViewModel canViewModel, ICustomerToolViewModel viewModelRoot, MessageDefinition definition)
        : base(canViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (definition == null)
        {
            definition = new() { Name = "New Message", Id = 100, TransmitNodes = new int[] {NodeDisabled, NodeDisabled }, ReceiveNodes = new int[] { NodeDisabled, NodeDisabled }, SetAddressOnSend = false};

            // FindNode Index
            for (uint i = 0; i < UInt32.MaxValue; i++)
                if (!canViewModel.Messages.Any(x => x.MessageDefinition.Id == i))
                {
                    definition.Id = i;
                    break;
                }
            canViewModel.CanClientCalibration.Messages.Add(definition);
        }


        MessageDefinition = definition;

        RebuildNodeList(this, null);
        canViewModel.Nodes.CollectionChanged += RebuildNodeList;

        if (definition.TransmitNodes.Length > 0)
            SelectedTransmitNode_P0 = Nodes_P0.FirstOrDefault(x => x.NodeDefinition.Id == definition.TransmitNodes[0]);

        if (definition.ReceiveNodes.Length > 0)
            SelectedReceiveNode_P0 = Nodes_P0.FirstOrDefault(x => x.NodeDefinition.Id == definition.ReceiveNodes[0]);

        if (definition.TransmitNodes.Length > 1)
            SelectedTransmitNode_P1 = Nodes_P1.FirstOrDefault(x => x.NodeDefinition.Id == definition.TransmitNodes[1]);

        if (definition.ReceiveNodes.Length > 1)
            SelectedReceiveNode_P1 = Nodes_P1.FirstOrDefault(x => x.NodeDefinition.Id == definition.ReceiveNodes[1]);

        MessageDefinition.Signals.Sort(delegate (MessageSignalDefinition x, MessageSignalDefinition y)
        {
            return x.StartBit.CompareTo(y.StartBit);
        });

        j1939Id = new(MessageDefinition.Id);
        SelectedSignal = MessageDefinition.Signals.FirstOrDefault();


        this.signals = new ObservableCollection<MessageSignalDefinition>(MessageDefinition.Signals);
    }

    private void RebuildNodeList(object sender, NotifyCollectionChangedEventArgs e)
    {
        Nodes_P0.Clear();
        Nodes_P1.Clear();
        Nodes_P0.Add(new NodeViewModel(ParentViewModel, viewModelInterface, new NodeDefinition() { Id = NodeDisabled, Name = "(None)" }));
        Nodes_P1.Add(new NodeViewModel(ParentViewModel, viewModelInterface, new NodeDefinition() { Id = NodeDisabled, Name = "(None)" }));
        foreach (var node in ParentViewModel.Nodes)
        {
            foreach (var port in node.NodeDefinition.Ports)
                if (port == 0)
                    Nodes_P0.Add(node);
                else if (port == 1)
                    Nodes_P1.Add(node);
        }
    }

    public void ShowEditor()
    {
        currentView = new CANMessageEditView() { DataContext = this };
        viewModelInterface.ShowPopup(currentView);
    }

    public void EditSignalTransforms()
    {
        currentSignalView = new CANMessageSignalView() { DataContext = this };
        viewModelInterface.ShowPopup(currentSignalView);
    }

    public void EditValueTable()
    {
        LoadSignalValues();
        currentSignalView = new CANMessageValueView() { DataContext = this };
        viewModelInterface.ShowPopup(currentSignalView);
    }

    public void EditMuxInfo()
    {
        currentSignalView = new CANMessageMuxView() { DataContext = this };
        viewModelInterface.ShowPopup(currentSignalView);
    }

    public void CloseEditor()
    {
        if (currentSignalView != null)
        {
            viewModelInterface.DismissPopup(currentSignalView);

            if (currentSignalView is CANMessageValueView)
                UpdateSignalValues();

            currentSignalView = null;
        }
        else
        {
            viewModelInterface.DismissPopup(currentView);
            currentView = null;
        }
    }

    internal void AddItem()
    {
        var signalDef = new MessageSignalDefinition
        {
            Name = "New Signal",
            BitLength = 4,
            Id = 0,
            ValueType = Services.Can.ValueType.Unsigned,
            Scale = 1.0f
        };

        // FindNode Index
        if (signals.Count > 0)
        {
            for (uint i = 0; i < 255; i++)
                if (!this.Signals.Any(x => x.Id == i))
                {
                    signalDef.Id = i;
                    signalDef.Name += $" {i}";
                    break;
                }

            signalDef.StartBit = this.signals.Max(x => x.StartBit + x.BitLength);

        }

        this.MessageDefinition.Signals.Add(signalDef);
        this.Signals.Add(signalDef);
    }

    internal async void RemoveItem()
    {
        var continueWork = await viewModelInterface.ShowDialog("Remove Signal", "Are you sure you wish to remove the Selected Signal?", "Yes", "Cancel");

        if (!continueWork)
            return;

        this.MessageDefinition.Signals.Remove(selectedSignal);
        this.Signals.Remove(selectedSignal);
    }

    internal void AddValue()
    {
        if (this.selectedSignal != null)
            for (int i = 0; i < 255; i++)
                if (!this.SelectedSignal.Values.Any(x => x.Key == i))
                {
                    var signal = new SignalModel() { Key = i, Value = "New Value" };
                    this.SignalValues.Add(signal);
                    break;
                }
    }

    internal void RemoveValue()
    {
        if (SelectedSignalValue != null)
        {
            this.SignalValues.Remove(SelectedSignalValue);
        }
    }

    internal uint AddExtendedBit(uint value)
    {
        return value | (1u << extendedFrameBitPosition);
    }

    internal uint NegateExtendedBit(uint id)
    {
        return id & ~(1 << extendedFrameBitPosition);
    }

    private void LoadSignalValues()
    {
        this.SignalValues.Clear();
        if (this.selectedSignal != null)
            foreach (var item in this.SelectedSignal.Values)
                this.SignalValues.Add(new() { Key = item.Key, Value = item.Value });
    }

    private void UpdateSignalValues()
    {
        if (selectedSignal == null)
            return;

        this.SelectedSignal?.Values.Clear();
        foreach (var item in this.SignalValues)
            SelectedSignal.Values[item.Key] = item.Value;
    }

    private void UpdateNodeValues(bool transmitter, int port, int node)
    {
        var values = transmitter ? MessageDefinition.TransmitNodes.ToList() : MessageDefinition.ReceiveNodes.ToList();
        if (values.Count < port)
            values.Capacity = port;
        values[port] = node;

        var secondaryVal = node == NodeDisabled ? NodeDisabled : BroadcastVal;

        if (transmitter)
        {
            MessageDefinition.TransmitNodes[port] = node;
            if(MessageDefinition.MessageType != MessageType.J1939ExtendedFrame)
                MessageDefinition.ReceiveNodes[port] = secondaryVal;
        }         
        else
        {           
            MessageDefinition.ReceiveNodes[port] = node;
            if (MessageDefinition.MessageType != MessageType.J1939ExtendedFrame)
                MessageDefinition.TransmitNodes[port] = secondaryVal;
        }

        OnPropertyChanged(nameof(IdMasked));
        OnPropertyChanged(nameof(Transmitter));
    }

    #endregion
}


internal class SignalModel
{
    [Range(0, int.MaxValue)]
    public int Key { get; set; }
    [MaxLength(255)]
    public string Value { get; set; }
}

