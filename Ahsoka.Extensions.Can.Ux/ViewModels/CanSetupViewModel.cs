using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Extensions.Can.UX.ViewModels.Nodes;
using Ahsoka.Services.Can;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Controls;
using Avalonia.Threading;
using Material.Icons;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.DeveloperTools;

internal class CanSetupViewModel : ExtensionViewModelBase, ICanTreeNode
{
    #region Fields

    CanGroupNode<MessageViewModel> rootMessageNode = null;
    CanGroupNode<CanSetupViewModel> rootToolNode = null;
    CanGroupNode<PortViewModel> rootPortNode = null;
    CanGroupNode<NodeViewModel> rootNodeNode = null;

    private CanClientConfiguration canConfiguration = new();
    string configurationPath;
    string originalCANConfigText = string.Empty;
    ICanTreeNode selectedTreeNode = null;
    string projectFolder;
    HardwareInfo hardwareInfo;
    #endregion

    #region Properties
    public ObservableCollection<ICanTreeNode> RootNodes
    {
        get;
        set;
    } = new();

    public ICanTreeNode SelectedTreeNode
    {
        get { return selectedTreeNode; }
        set
        {
            var firstItem = value?.GetChildren().FirstOrDefault();
            if (firstItem != null)
                value = firstItem;

            selectedTreeNode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedUserControl));
        }
    }

    public UserControl SelectedUserControl
    {
        get
        {
            return selectedTreeNode?.GetUserControl();
        }
        set
        {
        }
    }

    public bool GeneratorEnabled
    {
        get { return CanConfiguration.GeneratorEnabled; }
        set { CanConfiguration.GeneratorEnabled = value; OnPropertyChanged(); }
    }

    public string GeneratorBaseClass
    {
        get { return CanConfiguration.GeneratorBaseClass; }
        set { CanConfiguration.GeneratorBaseClass = value; OnPropertyChanged(); }
    }

    public string GeneratorNamespace
    {
        get { return CanConfiguration.GeneratorNamespace; }
        set { CanConfiguration.GeneratorNamespace = value; OnPropertyChanged(); }
    }

    public string GeneratorOutputFile
    {
        get { return CanConfiguration.GeneratorOutputFile; }
        set { CanConfiguration.GeneratorOutputFile = value; OnPropertyChanged(); }
    }

    public CanClientConfiguration CanConfiguration
    {
        get => canConfiguration;
        set => canConfiguration = value;
    }

    public CANPortEditView PortEditView { get; init; } = new();

    public CANNodeEditView NodeEditView { get; init; } = new();

    public CANMessageEditView MessageEditView { get; init; } = new();

    public ObservableCollection<PortViewModel> Ports
    {
        get { return rootPortNode.Children; }
    }

    public ObservableCollection<NodeViewModel> Nodes
    {
        get { return rootNodeNode.Children; }
    }

    public ObservableCollection<MessageViewModel> Messages
    {
        get { return rootMessageNode.Children; }
    }
    #endregion

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
    }

    protected override string OnSave(string packageInfoFolder)
    {
        /// Generate FileName if We Edited the Data but have no path.
        /// Rename Config if not "Standard Name"
        if (String.IsNullOrEmpty(configurationPath) || !configurationPath.Contains(CreateConfigurationName()))
            if (rootNodeNode.Children.Count > 0 || rootMessageNode.Children.Count > 0 || rootPortNode.Children.Count > 0)
                configurationPath = CreateConfigurationName();

        ValidateConfiguration();

        if (!String.IsNullOrEmpty(configurationPath))
        {
            string fileName = Path.Combine(packageInfoFolder, configurationPath);
            string data = JsonUtility.Serialize(CanConfiguration);

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
        string newConfig = JsonUtility.Serialize(CanConfiguration);
        bool hasChanges = newConfig != originalCANConfigText;

        // Config Changed...may need to regenerate the Models.
        if (hasChanges)
            this.CustomerToolViewModel?.SdkNeedsUpdate();

        return hasChanges;
    }

    protected override void OnClose()
    {
        CanConfiguration = null;
        configurationPath = null;
        SelectedTreeNode = null;
        SelectedUserControl = null;
    }

    internal async void SetConfigurationDirectory()
    {
        string currentPath = "";
        if (configurationPath != null)
            currentPath = Path.Combine(projectFolder, configurationPath);

        var path = await CustomerToolViewModel.FileLocatorAsync("Pick CAN Calibration", "json", "CAN Configuration", currentPath);
        if (path != null && File.Exists(path))
        {
            var targetPath = Path.Combine(projectFolder, CreateConfigurationName());
            File.Copy(path, targetPath, true);

            configurationPath = CreateConfigurationName();
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
            string pathToOutputFile = Path.Combine(workingDirectory, CreateConfigurationName());

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

    private string CreateConfigurationName()
    {
        return $"CANConfiguration.json";
    }

    internal void RemoveConfiguration()
    {
        configurationPath = null;
        CanConfiguration = new();
        rootPortNode.Children.Clear();
        rootMessageNode.Children.Clear();
        rootNodeNode.Children.Clear();
        RefreshGenerateSettings();
        AddHardwarePorts();
        OnPropertyChanged(nameof(ConfigurationPath));
        CheckForAnyNode();
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

                    CanConfiguration = ConfigurationFileLoader.LoadFile<CanClientConfiguration>(pathToConfig);

                    CanConfiguration.GeneratorNamespace = String.IsNullOrEmpty(CanConfiguration.GeneratorNamespace) ? "Ahsoka.CS.CAN" : CanConfiguration.GeneratorNamespace;
                    CanConfiguration.GeneratorOutputFile = String.IsNullOrEmpty(CanConfiguration.GeneratorOutputFile) ? "obj\\GeneratedCanObjects.cs" : CanConfiguration.GeneratorOutputFile;
                    CanConfiguration.GeneratorBaseClass = String.IsNullOrEmpty(CanConfiguration.GeneratorBaseClass) ? "CanViewModelBase" : CanConfiguration.GeneratorBaseClass;
                }
                catch
                {
                    CanConfiguration = new(); // Create Default 
                }
            }
            else
            {
                CanConfiguration = new(); // Create Default 
            }
        }
       
        RootNodes.Clear();
        rootMessageNode = new() { NodeDescription = "Messages", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootNodeNode = new() { NodeDescription = "Nodes", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootPortNode = new() { NodeDescription = "Ports", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootToolNode = new() { NodeDescription = "Can Tools",  Icon = MaterialIconKind.Toolbox, IsExpanded = true };
        
        // Add Hardware Ports - Do First for DropDowns
        AddHardwarePorts();

        // Next do Nodes So Messages have Nodes List
        foreach (var node in CanConfiguration.Nodes)
            rootNodeNode.Children.Add(new NodeViewModel(this, CustomerToolViewModel, node));

        // Add Messages
        foreach (var message in CanConfiguration.Messages)
            rootMessageNode.Children.Add(new MessageViewModel(this, CustomerToolViewModel, message));

        // Setup Tools
        rootToolNode.Children.Add(this);
    
        // Add Nodes to Tree in Order
        RootNodes.Add(rootMessageNode);
        RootNodes.Add(rootNodeNode);
        RootNodes.Add(rootPortNode);
        RootNodes.Add(rootToolNode);

        // Bump Calibration file to Current.
        CanConfiguration.Version = VersionUtility.GetAppVersionString();

        SelectedTreeNode = rootMessageNode.Children.Count > 0 ? rootMessageNode.Children.First() : rootPortNode.Children.FirstOrDefault();

        CheckForAnyNode();
    }

    internal void CheckForAnyNode()
    {
        bool include = Nodes.Any(x => x.IsJ1939 && x.NodeDefinition.NodeType != NodeType.Any);

        var standardDefinitions = CanSystemInfo.StandardCanMessages;
        var standardNode = standardDefinitions.Nodes.First(x => x.Name == "ANY");
        
        var node = Nodes.FirstOrDefault(x => x.Name == "ANY");
        var inList = node != null;
        if (include && !inList)
        {
            node = new NodeViewModel(this, CustomerToolViewModel, standardNode);
            Nodes.Insert(0, node);
            CanConfiguration.Nodes.Add(node.NodeDefinition);
        }

        if (Nodes.IndexOf(node) != 0)
        {
            Nodes.Remove(node);
            Nodes.Insert(0, node);
        }
    }

    internal void AddMessage()
    {
        var vm = new MessageViewModel(this, CustomerToolViewModel, null);
        rootMessageNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal void AddNode()
    {
        var vm = new NodeViewModel(this, CustomerToolViewModel, null);
        rootNodeNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal async void RemoveItem()
    {
        if (selectedTreeNode is MessageViewModel messageNode)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Message", "Are you sure you wish to remove the Selected Message?", "Yes", "Cancel");
            if (!continueWork)
                return;

            canConfiguration.Messages.Remove(messageNode.MessageDefinition);
            rootMessageNode.Children.Remove(messageNode);
            SelectedTreeNode = rootMessageNode.Children.Count > 0 ? rootMessageNode.Children.First() : rootPortNode.Children.FirstOrDefault();
        }
        else if (selectedTreeNode is NodeViewModel nodeNode)
        {
            if (nodeNode.NodeDefinition.NodeType == NodeType.Any)
            {
                await CustomerToolViewModel.ShowDialog("Invalid Function", "The 'Any' node can not be removed.", "OK");
                return;
            }
            else
            {
                var continueWork = await CustomerToolViewModel.ShowDialog("Remove Node", "Are you sure you wish to remove the Selected Node?", "Yes", "Cancel");
                if (!continueWork)
                    return;

                canConfiguration.Nodes.Remove(nodeNode.NodeDefinition);
                rootNodeNode.Children.Remove(nodeNode);
                SelectedTreeNode = rootNodeNode.Children.Count > 0 ? rootNodeNode.Children.First() : rootPortNode.Children.FirstOrDefault();
            }
        }
    }

    private void ValidateConfiguration()
    {
        if (canConfiguration == null)
            foreach (var message in rootMessageNode.Children)
            {
                if (message is MessageViewModel messageNode)
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
    }

    private void AddHardwarePorts()
    {
        var canInfo = CANHardwareInfoExtension.GetCanInfo(hardwareInfo.PlatformFamily);

        foreach (CanPort port in canInfo.CANPorts)
        {
            bool isEnabled = true;
            var portDef = canConfiguration.Ports.FirstOrDefault(x => x.Port == port.Port);
            if (portDef == null)
            {
                // Create Temp Def
                isEnabled = false;
                portDef = new PortDefinition()
                {
                    Port = port.Port,
                    CanInterfacePath = port.SocketCanInterfacePath,
                    BaudRate = CanBaudRate.Baud250kb,
                    CanInterface = CanInterface.SocketCan,
                    PromiscuousTransmit = false,
                    PromiscuousReceive = false,
                };
            }

            rootPortNode.Children.Add(new PortViewModel(this, CustomerToolViewModel, portDef)
            {
                IsEnabled = isEnabled
            });


        }
    }

    private void RefreshGenerateSettings()
    {
        OnPropertyChanged(nameof(GeneratorEnabled));
        OnPropertyChanged(nameof(GeneratorBaseClass));
        OnPropertyChanged(nameof(GeneratorNamespace));
        OnPropertyChanged(nameof(GeneratorOutputFile));
    }

    #region TreeNode
    public bool IsEditable { get; set; } = false;

    public bool IsEnabled { get; set; } = false;

    public string NodeDescription
    {
        get { return $"Code Generation Setup"; }
        set { }
    }

    public MaterialIconKind Icon
    {
        get
        {
            return MaterialIconKind.Tools;
        }
    }

    public UserControl GetUserControl()
    {
        var view = new CANGenerateView();
        view.DataContext = this;
        return view;
    }

    public IEnumerable<ICanTreeNode> GetChildren() { return Enumerable.Empty<ICanTreeNode>(); }
    #endregion

}
