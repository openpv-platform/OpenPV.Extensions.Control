using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Extensions.Can.UX.ViewModels.Nodes;
using Ahsoka.Services.Can;
using Ahsoka.Services.Can.Messages;
using Ahsoka.Utility;
using Avalonia.Controls;
using Material.Icons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Ahsoka.DeveloperTools;

internal class MessageViewModel : ChildViewModelBase<CanSetupViewModel>, ICanTreeNode
{
    #region Fields
    const int extendedFrameBitPosition = 31;
    const int PDU2Threshold = 240;
    public const int BroadcastVal = 255;
    public const int AnyNodeID = 255;
    public const int NodeDisabled = -1;
   
    UserControl currentView;
    UserControl currentSignalView;

    MessageSignalDefinition selectedSignal;
    readonly J1939PropertyDefinitions.Id j1939Id = new();
    readonly ObservableCollection<MessageSignalDefinition> signals = new();
    readonly ObservableCollection<SignalModel> signalsModels = new();
    private ICustomerToolViewModel viewModelInterface;
    #endregion

    #region ID Handling J1939 <-> RAW
    public bool IsJ1939 { get { return MessageType == MessageType.J1939ExtendedFrame; } }
    public bool IsPDU2 { get { return PDUF >= PDU2Threshold; } set { } }

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
                    MessageDefinition.OverrideSourceAddress = true;
                }
                else
                    MessageDefinition.OverrideSourceAddress = false;

                // Restore Extended Frame Bit
                if (value != MessageType.RawStandardFrame)
                    MessageDefinition.Id = AddExtendedBit(MessageDefinition.Id);
                else
                    MessageDefinition.Id = NegateExtendedBit(MessageDefinition.Id);
            }

            MessageDefinition.MessageType = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsJ1939));

            ReGenerateMask();
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

    public uint PDUS
    {
        get
        {
            return j1939Id.PDUS;
        }
        set
        {
            j1939Id.PDUS = value;
            OnPropertyChanged();           
        }
    }

    public bool OverrideSourceAddress
    {
        get
        {
            return MessageDefinition.OverrideSourceAddress;
        }
        set
        {
            MessageDefinition.OverrideSourceAddress = value;
            OnPropertyChanged();
            ReGenerateMask();
        }
    }

    public bool OverrideDestinationAddress
    {
        get
        {
            return MessageDefinition.OverrideDestinationAddress;
        }
        set
        {
            MessageDefinition.OverrideDestinationAddress = value;
            OnPropertyChanged();
            ReGenerateMask();
        }
    }

    public uint PGN
    {
        get { return j1939Id.PGN; }
        set 
        {
            j1939Id.PGN = value;
            j1939Id.WritePGNToUint();

            IdMasked = j1939Id.WriteToUint();

            OnPropertyChanged();
            OnPropertyChanged(nameof(PDUF));
            OnPropertyChanged(nameof(PDUS));
            OnPropertyChanged(nameof(IsPDU2));
            OnPropertyChanged(nameof(DataPage));
        }
    }

    public uint IdMasked
    {
        get
        {
            uint id = this.MessageDefinition.Id;

            // Remove Extended Frame 
            if (MessageType != MessageType.RawStandardFrame)
                id = NegateExtendedBit(id);

            return id;
        }
        set
        {
            MessageDefinition.Id = value;
            if (MessageType != MessageType.RawStandardFrame)
                MessageDefinition.Id = AddExtendedBit(MessageDefinition.Id);

            OnPropertyChanged();
        }
    }
   
    public string Id
    {
        get { return IsJ1939 ? $"PGN: {PGN}" : $"CID: {IdMasked}"; }
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

    public CrcType[] CrcTypes { get; init; } = Enum.GetValues<CrcType>();

    public MuxRole[] MuxRoles { get; init; } = Enum.GetValues<MuxRole>();

    public MessageType[] MessageTypes { get; init; } = Enum.GetValues<MessageType>();

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

    #endregion

    #region Methods

    public MessageViewModel(CanSetupViewModel canViewModel, ICustomerToolViewModel viewModelRoot, MessageDefinition definition)
        : base(canViewModel)
    {
        viewModelInterface = viewModelRoot;

        if (definition == null)
        {
            definition = new() { Name = "New Message", Id = 100, TransmitNodes = new int[] { NodeDisabled, NodeDisabled }, ReceiveNodes = new int[] { NodeDisabled, NodeDisabled } , UserDefined = true};

            // FindNode Index
            for (uint i = 0; i < UInt32.MaxValue; i++)
                if (!canViewModel.Messages.Any(x => x.MessageDefinition.Id == i))
                {
                    definition.Id = i;
                    break;
                }

            canViewModel.CanConfiguration.Messages.Add(definition);
        }

        MessageDefinition = definition;
       
        ValidatePorts(canViewModel, definition);

        MessageDefinition.Signals.Sort(delegate (MessageSignalDefinition x, MessageSignalDefinition y)
        {
            return x.StartBit.CompareTo(y.StartBit);
        });


        j1939Id = new(MessageDefinition.Id);
        SelectedSignal = MessageDefinition.Signals.FirstOrDefault();

        if (definition.IdMask == 0)
            ReGenerateMask();

        this.signals = new ObservableCollection<MessageSignalDefinition>(MessageDefinition.Signals);
    }

    private static void ValidatePorts(CanSetupViewModel canViewModel, MessageDefinition definition)
    {
        int count = canViewModel.Ports.Count;
        if (definition.ReceiveNodes.Count() != count)
        {
            if (count > definition.ReceiveNodes.Count())
            {
                var portValues = Enumerable.Repeat(-1, count).ToArray();
                definition.ReceiveNodes.CopyTo(portValues, 0);
                definition.ReceiveNodes = portValues;
            }
            else
            {
                definition.ReceiveNodes = definition.ReceiveNodes.Take(count).ToArray();
            }
        }

        if (definition.TransmitNodes.Count() != count)
        {
            if (count > definition.TransmitNodes.Count())
            {
                var portValues = Enumerable.Repeat(-1, count).ToArray();
                definition.TransmitNodes.CopyTo(portValues, 0);
                definition.TransmitNodes = portValues;
            }
            else
            {
                definition.TransmitNodes = definition.TransmitNodes.Take(count).ToArray();
            }
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

    private void ReGenerateMask()
    {
        if (MessageType == MessageType.J1939ExtendedFrame)
        {
            uint mask = 0x1C000000; // Mask Priority Out
           
            
            if (OverrideSourceAddress) // Mask Source Address
                mask |= 0xFF;

            if (OverrideDestinationAddress && !IsPDU2) // Mask DestinationAddress if PFU1
                mask |= 0xFF00;

            MessageDefinition.IdMask = mask;
        }
        else
        {
            MessageDefinition.IdMask = 0xFFFFFFFF;
        }
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

    #endregion

    #region TreeNode
    bool ICanTreeNode.IsEditable { get; } = false;
    bool ICanTreeNode.IsEnabled { get; set; } = false;

    public string NodeDescription
    {
        get
        {
            return this.Name;
        }
        set { }
    }

    public MaterialIconKind Icon
    {
        get
        {
            return MaterialIconKind.FileDocumentOutline;
        }
    }

    public UserControl GetUserControl()
    {
        var view = ParentViewModel.MessageEditView;
        view.DataContext = null;
        view.DataContext = this;
        return view;
    }

    public IEnumerable<ICanTreeNode> GetChildren() { return Enumerable.Empty<ICanTreeNode>(); }

    #endregion
}


internal class SignalModel
{
    [Range(0, int.MaxValue)]
    public int Key { get; set; }
    [MaxLength(255)]
    public string Value { get; set; }
}

