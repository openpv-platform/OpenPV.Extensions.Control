using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Services.Can;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class CanSetupViewModel : ExtensionViewModelBase
{
    string originalCANConfigText = string.Empty;
    private CanClientCalibration canClientCalibration = new();
    private ObservableCollection<NodeViewModel> nodes = new();
    private NodeViewModel selectedNode;
    private ObservableCollection<DiagnosticEventViewModel> diagnosticEvents = new();
    private DiagnosticEventViewModel selectedDiagnosticEvent;
    private ObservableCollection<MessageViewModel> messages = new();
    private MessageViewModel selectedMessage;
    private ObservableCollection<PortViewModel> ports = new();
    private PortViewModel selectedPort;
    private int selectedTabIndex = 0;
    private bool port0Enabled, port1Enabled = false;
    string configurationPath;
    string projectFolder;
    HardwareInfo hardwareInfo;

    internal int SelectedTab { get { return selectedTabIndex; } set { selectedTabIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowAddRemove)); EvaluatePort(); } }

    public bool ShowAddRemove { get { return SelectedTab != 2; } set { } }

    public bool Port0Enabled
    {
        get { return port0Enabled; }
        set { port0Enabled = value; OnPropertyChanged(); }
    }

    public bool Port1Enabled
    {
        get { return port1Enabled; }
        set { port1Enabled = value; OnPropertyChanged(); }
    }

    public ObservableCollection<MessageViewModel> Messages
    {
        get { return messages; }
        set
        {
            messages = value;
            OnPropertyChanged();
        }
    }

    public MessageViewModel SelectedMessage
    {
        get { return selectedMessage; }
        set
        {
            if (selectedMessage != null)
                selectedMessage.IsSelected = false;
            selectedMessage = value;
            if (selectedMessage != null)
                selectedMessage.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<NodeViewModel> Nodes
    {
        get { return nodes; }
        set
        {
            nodes = value;
            OnPropertyChanged();
        }
    }

    public NodeViewModel SelectedNode
    {
        get { return selectedNode; }
        set
        {
            if (selectedNode != null)
                selectedNode.IsSelected = false;

            selectedNode = value;

            if (selectedNode != null)
                selectedNode.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<PortViewModel> Ports
    {
        get { return ports; }
        set
        {
            ports = value;
            OnPropertyChanged();
        }
    }

    public PortViewModel SelectedPort
    {
        get { return selectedPort; }
        set
        {
            if (selectedPort != null)
                selectedPort.IsSelected = false;

            selectedPort = value;

            if (selectedPort != null)
                selectedPort.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public ObservableCollection<DiagnosticEventViewModel> DiagnosticEvents
    {
        get { return diagnosticEvents; }
        set
        {
            diagnosticEvents = value;
            OnPropertyChanged();
        }
    }

    public DiagnosticEventViewModel SelectedDiagnosticEvent
    {
        get { return selectedDiagnosticEvent; }
        set
        {
            if (selectedDiagnosticEvent != null)
                selectedDiagnosticEvent.IsSelected = false;

            selectedDiagnosticEvent = value;

            if (selectedDiagnosticEvent != null)
                selectedDiagnosticEvent.IsSelected = true;

            OnPropertyChanged();
        }
    }

    public CanClientCalibration CanClientCalibration
    {
        get => canClientCalibration;
        set
        {
            canClientCalibration = value;
            originalCANConfigText = JsonUtility.Serialize(canClientCalibration);
        }
    }

    public bool GeneratorEnabled
    {
        get { return CanClientCalibration.GeneratorEnabled; }
        set { CanClientCalibration.GeneratorEnabled = value; OnPropertyChanged(); }
    }

    public string GeneratorBaseClass
    {
        get { return CanClientCalibration.GeneratorBaseClass; }
        set { CanClientCalibration.GeneratorBaseClass = value; OnPropertyChanged(); }
    }

    public string GeneratorNamespace
    {
        get { return CanClientCalibration.GeneratorNamespace; }
        set { CanClientCalibration.GeneratorNamespace = value; OnPropertyChanged(); }
    }

    public string GeneratorOutputFile
    {
        get { return CanClientCalibration.GeneratorOutputFile; }
        set { CanClientCalibration.GeneratorOutputFile = value; OnPropertyChanged(); }
    }

    public string ConfigurationPath { get => configurationPath; set { configurationPath = value; OnPropertyChanged(); } }

    public CanSetupViewModel()
    {
        
    }

    protected override UserControl OnGetView()
    {
        return new CANSetup() { DataContext = this };
    }

    protected override void OnInitExtension(HardwareInfo hardwareInfo, string projectInfoFolder, string configurationFile)
    {
        this.hardwareInfo = hardwareInfo;
        projectFolder = projectInfoFolder;
        configurationPath = configurationFile;

        LoadCANConfiguration();

        EvaluatePort();
    }

    protected override string OnSave(string packageInfoFolder)
    {
        /// Generate FileName if We Edited the Data but have no path.
        if (String.IsNullOrEmpty(configurationPath))
            if (nodes.Count > 0 || messages.Count > 0 || ports.Count > 0)
                ConfigurationPath = CreateCalibationName();

        ValidateConfiguration();

        if (!String.IsNullOrEmpty(configurationPath))
        {
            string fileName = Path.Combine(packageInfoFolder, configurationPath);
            string data = JsonUtility.Serialize(CanClientCalibration);
            
            bool hasChanges = data != originalCANConfigText;

            // Config Changed...may need to regenerate the Models.
            if (hasChanges)
            {
                originalCANConfigText = data;
                this.CustomerToolViewModel.SdkNeedsUpdate();
            }

            File.WriteAllText(fileName, data);
        }

        return configurationPath;
    }

    protected override bool OnHasChanges()
    {
        string newConfig = JsonUtility.Serialize(CanClientCalibration);
        bool hasChanges = newConfig != originalCANConfigText;

        // Config Changed...may need to regenerate the Models.
        if (hasChanges)
            this.CustomerToolViewModel?.SdkNeedsUpdate();

        return hasChanges;
    }

    protected override void OnClose()
    {
        CanClientCalibration = null;
        configurationPath = null;
    }

    internal async void SetConfigurationDirectory()
    {
        string currentPath = "";
        if (configurationPath != null)
            currentPath = Path.Combine(projectFolder, configurationPath);

        var path = await CustomerToolViewModel.FileLocatorAsync("Pick CAN Calibration", "cancalibration.json", currentPath);
        if (path != null && File.Exists(path))
        {
            ConfigurationPath = Path.GetRelativePath(projectFolder, path);
            LoadCANConfiguration();
        }
    }

    internal async void CreateFromDBC()
    {
        var pathToDBC = await CustomerToolViewModel.FileLocatorAsync("Pick DBC Calibration", "dbc", configurationPath);

        if (!File.Exists(pathToDBC))
            return;

        // Run Creation Process and Capture File.
        CustomerToolViewModel.StartProcess();

        // Clear and Begin Work
        CustomerToolViewModel.AddConsoleLine($"Creating CAN Configuration from DBC: {configurationPath}");

        var progress = new Progress<CommandLineProgressInfo>(info =>
        {
            if (info.IsWriteLine)
                CustomerToolViewModel.AddConsoleLine(info.Message);
            else
                CustomerToolViewModel.AddConsoleText(info.Message);
        });

        await Task.Run(() =>
        {
            string workingDirectory = projectFolder;
            string pathToOutputFile = Path.Combine(workingDirectory, CreateCalibationName());

            CustomerToolViewModel.RunCommandLineProcess($" --GenerateCalibrationFromDBC \"{pathToDBC}\" \"{pathToOutputFile}\"", workingDirectory, progress, CancellationToken.None);

            if (File.Exists(pathToOutputFile))
                return pathToOutputFile;
            else
                return string.Empty; // fileName;

        }).ContinueWith(result =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (result.Status == TaskStatus.Faulted)
                {
                    CustomerToolViewModel.AddConsoleLine(result.Exception.Message);
                    CustomerToolViewModel.ShowDialog("Creation Error", $"{result.Exception?.InnerException?.Message}", "OK");
                }
                else if (result.Status == TaskStatus.RanToCompletion && File.Exists(result.Result))
                {
                    this.configurationPath = Path.GetRelativePath(projectFolder, result.Result);
                    CustomerToolViewModel.AddConsoleLine($"Create Completed at {configurationPath}");
                    LoadCANConfiguration();
                }

                CustomerToolViewModel.FinishProcess("", result.Status == TaskStatus.RanToCompletion && File.Exists(result.Result));
            });
        });
    }

    private string CreateCalibationName()
    {
        return $"CANService.cancalibration.json";
    }

    internal void RemoveConfiguration()
    {
        configurationPath = null;
        CanClientCalibration = new CanClientCalibration();
        Nodes.Clear();
        Messages.Clear();

        OnPropertyChanged(nameof(ConfigurationPath));
        OnPropertyChanged(nameof(Nodes));
        OnPropertyChanged(nameof(Messages));
    }

    private void LoadCANConfiguration()
    {
        if (configurationPath != null)
        {
            string pathToConfig = Path.Combine(projectFolder, configurationPath);
            if (File.Exists(pathToConfig))
            {
                try
                {  
                    originalCANConfigText = File.ReadAllText(pathToConfig);

                    CanClientCalibration = ConfigurationFileLoader.LoadFile<CanClientCalibration>(pathToConfig);

                    CanClientCalibration.GeneratorNamespace = String.IsNullOrEmpty(CanClientCalibration.GeneratorNamespace) ? "Ahsoka.CS.CAN" : CanClientCalibration.GeneratorNamespace;
                    CanClientCalibration.GeneratorOutputFile = String.IsNullOrEmpty(CanClientCalibration.GeneratorOutputFile) ? "obj\\GeneratedObjects.cs" : CanClientCalibration.GeneratorOutputFile;
                    CanClientCalibration.GeneratorBaseClass = String.IsNullOrEmpty(CanClientCalibration.GeneratorBaseClass) ? "CanViewModelBase" : CanClientCalibration.GeneratorBaseClass;

                    Ports.Clear();
                    foreach (PortDefinition port in CanClientCalibration.Ports.OrderBy(x => x.Port))
                        Ports.Add(new PortViewModel(this, CustomerToolViewModel, port) { IsEnabled = true }) ;

                    foreach (CanPort port in HardwareInfo.GetHardwareInfo(hardwareInfo.PlatformFamily, hardwareInfo.PlatformQualifier).CANInfo.CANPorts)
                        if (!Ports.Any(x => x.Port == port.Port))
                            Ports.Add(new PortViewModel(this, CustomerToolViewModel, new PortDefinition()
                            {
                                Port = port.Port,
                                CanInterfacePath = port.SocketCanInterfacePath,
                                BaudRate = CanBaudRate.Baud250kb,
                                CanInterface = CanInterface.SocketCan,
                                PromiscuousTransmit = false,
                                PromiscuousReceive = false,
                                UserDefined = true,                    
                            }) { IsEnabled = false });

                    Nodes.Clear();
                    foreach (NodeDefinition node in CanClientCalibration.Nodes.OrderBy(x => x.Id))
                        Nodes.Add(new NodeViewModel( this, CustomerToolViewModel, node));

                    Messages.Clear();
                    foreach (MessageDefinition msg in CanClientCalibration.Messages.OrderBy(x => x.Id))
                        Messages.Add(new MessageViewModel(this, CustomerToolViewModel, msg));

                    DiagnosticEvents.Clear();
                    foreach (DiagnosticEventDefinition eventDef in CanClientCalibration.DiagnosticEvents)
                        DiagnosticEvents.Add(new DiagnosticEventViewModel(this, CustomerToolViewModel, eventDef));

                    OnPropertyChanged(nameof(CanInterface));
                }
                catch
                {
                    CanClientCalibration = new CanClientCalibration(); // Create Default 
                }
            }
            else
                CanClientCalibration = new CanClientCalibration(); // Create Default 
        }

        // Bump Calibration file to Current.
        CanClientCalibration.Version = VersionUtility.GetAppVersionString();

    }

    internal void AddItem()
    {
        if (SelectedTab == 1)
        {
            var nodeViewModel = new NodeViewModel(this, CustomerToolViewModel, null);
            this.Nodes.Add(nodeViewModel);
        }
        else if (SelectedTab == 0)
        {
            var nodeViewModel = new MessageViewModel(this, CustomerToolViewModel, null);
            nodeViewModel.MessageDefinition.UserDefined = true;
            this.Messages.Add(nodeViewModel);
           
            EvaluatePort();
        }
        else if (SelectedTab == 3)
        {
            var vm = new DiagnosticEventViewModel(this, CustomerToolViewModel, null);
            this.DiagnosticEvents.Add(vm);
        }
    }

    private void EvaluatePort()
    {
        Port0Enabled = this.Ports.Any(x => x.IsEnabled && x.Port == 0);
        Port1Enabled = this.Ports.Any(x => x.IsEnabled && x.Port == 1);
    }

    internal async void RemoveItem()
    {
        if (SelectedTab == 1)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Node", "Are you sure you wish to remove the Selected Node?", "Yes", "Cancel");

            if (!continueWork)
                return;

            if (selectedNode != null)
            {
                this.CanClientCalibration.Nodes.Remove(selectedNode.NodeDefinition);
                this.Nodes.Remove(selectedNode);
                this.selectedNode = this.nodes.FirstOrDefault();
            }
        }
        else if (SelectedTab == 0)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Message", "Are you sure you wish to remove the Selected Message?", "Yes", "Cancel");
            if (!continueWork)
                return;

            this.CanClientCalibration.Messages.Remove(SelectedMessage.MessageDefinition);
            this.Messages.Remove(SelectedMessage);
            this.SelectedMessage = this.messages.FirstOrDefault();
        }
        else if (SelectedTab == 3)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Diagnostic Event", "Are you sure you wish to remove the Selected Event?", "Yes", "Cancel");
            if (!continueWork)
                return;

            this.CanClientCalibration.DiagnosticEvents.Remove(SelectedDiagnosticEvent.EventDefinition);
            this.DiagnosticEvents.Remove(SelectedDiagnosticEvent);
            this.selectedDiagnosticEvent = this.DiagnosticEvents.FirstOrDefault();
        }
    }

    private void ValidateConfiguration()
    {
        canClientCalibration.Ports.Clear();
        foreach(var port in ports)
            if (port.IsEnabled)
                canClientCalibration.Ports.Add(port.PortDefinition);

        foreach (var message in Messages)
        {
            if (message.MessageDefinition.Signals.Count() > 0)
            {
                uint maxBits = message.MessageDefinition.Signals.Max(x => x.StartBit + x.BitLength);
                if (maxBits > 0)
                {
                    uint dlc = (uint)Math.Ceiling(maxBits / 8.0);
                    if (message.MessageDefinition.Dlc != dlc)
                        message.MessageDefinition.Dlc = dlc;
                }
            }
        }
    }

    internal void SetStandardNode(bool include, string name)
    {
        var standardDefinitions = CanSystemInfo.StandardCanMessages;
        var standardNode = standardDefinitions.Nodes.First(x => x.Name == name);
        var nodeViewModel = new NodeViewModel(this,CustomerToolViewModel, standardNode);

        var node = Nodes.FirstOrDefault(x => x.Name == name);
        var inList = node != null;
        if (include && !inList)
        {
            Nodes.Add(nodeViewModel);
            CanClientCalibration.Nodes.Add(nodeViewModel.NodeDefinition);
        }
        else if (!include && inList)
        {
            Nodes.Remove(node);
            CanClientCalibration.Nodes.Remove(node.NodeDefinition);
        }
    }

    internal void UpdateAddressClaim()
    {
        var node = Nodes.FirstOrDefault(x => x.IsSelf);
        if (node != null)
        {
            node.NodeDefinition.J1939Info.UseAddressClaim = Nodes.Any(x => x.TransportProtocol == TransportProtocol.J1939 &&
                                                            x.NodeDefinition.J1939Info.AddressType != NodeAddressType.Static) ||
                                                            node.ACMax > node.ACMin;
        }
    }


}
