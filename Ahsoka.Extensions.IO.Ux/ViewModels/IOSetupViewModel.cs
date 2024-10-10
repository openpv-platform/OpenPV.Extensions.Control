using Ahsoka.DeveloperTools.Core;
using Ahsoka.DeveloperTools.Views;
using Ahsoka.Services.IO;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using Avalonia.Controls;
using Material.Icons;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Ahsoka.Extensions.IO.UX.ViewModels.Nodes;

namespace Ahsoka.DeveloperTools;

internal class IOSetupViewModel : ExtensionViewModelBase, ITreeNode
{
    #region Fields
    GroupNode<IOAnalogInputViewModel> rootAnalogInputNode = null;
    GroupNode<IOAnalogOutputViewModel> rootAnalogOutputNode = null;
    GroupNode<IODigitalInputViewModel> rootDigitalInputNode = null;
    GroupNode<IODigitalOutputViewModel> rootDigitalOutputNode = null;
    GroupNode<IOFrequencyInputViewModel> rootFrequencyInputNode = null;
    GroupNode<IOFrequencyOutputViewModel> rootFrequencyOutputNode = null;
    GroupNode<IOCurveViewModel> rootCurveNode = null;
    GroupNode<IOSetupViewModel> rootToolNode = null;

    IOAnalogInputViewModel selectedAnalogInput;
    IOAnalogOutputViewModel selectedAnalogOutput;
    IODigitalInputViewModel selectedDigitalInput;
    IODigitalOutputViewModel selectedDigitalOutput;
    IOFrequencyInputViewModel selectedFrequencyInput;
    IOFrequencyOutputViewModel selectedFrequencyOutput;
    IOCurveViewModel selectedCurve;
    IOSetupViewModel selectedGenerate;

    private IOApplicationConfiguration ioApplicationConfiguration = new();
    string configurationPath;
    string originalConfigurationText = string.Empty;
    ITreeNode selectedTreeNode = null;
    string projectFolder;
    HardwareInfo hardwareInfo;
    const string DefaultGeneratorNamespace = "Ahsoka.CS.IO";
    const string DefaultGeneratorOutputFile = "Generated/GeneratedIOObjects.cs";
    const string DefaultGeneratorBaseClass = "IOViewModelBase";
    #endregion

    #region Properties
    public ObservableCollection<ITreeNode> RootNodes
    {
        get;
        set;
    } = new();

    public ITreeNode SelectedTreeNode
    {
        get { return selectedTreeNode; }
        set
        {
            var firstItem = value?.GetChildren().FirstOrDefault();
            if (firstItem != null)
                value = firstItem;

            AnalogInputViewModel = value as IOAnalogInputViewModel;
            AnalogOutputViewModel = value as IOAnalogOutputViewModel;
            DigitalInputViewModel = value as IODigitalInputViewModel;
            DigitalOutputViewModel = value as IODigitalOutputViewModel;
            FrequencyInputViewModel = value as IOFrequencyInputViewModel;
            FrequencyOutputViewModel = value as IOFrequencyOutputViewModel;
            CurveViewModel = value as IOCurveViewModel;
            GenerateViewModel = value as IOSetupViewModel;

            selectedTreeNode = value;
            OnPropertyChanged();
        }
    }

    public IOAnalogInputViewModel AnalogInputViewModel
    {
        get => selectedAnalogInput;
        set { selectedAnalogInput = value; OnPropertyChanged(); }
    }

    public IOAnalogOutputViewModel AnalogOutputViewModel
    {
        get => selectedAnalogOutput;
        set { selectedAnalogOutput = value; OnPropertyChanged(); }
    }

    public IODigitalInputViewModel DigitalInputViewModel
    {
        get => selectedDigitalInput;
        set { selectedDigitalInput = value; OnPropertyChanged(); }
    }

    public IODigitalOutputViewModel DigitalOutputViewModel
    {
        get => selectedDigitalOutput;
        set { selectedDigitalOutput = value; OnPropertyChanged(); }
    }

    public IOFrequencyInputViewModel FrequencyInputViewModel
    {
        get => selectedFrequencyInput;
        set { selectedFrequencyInput = value; OnPropertyChanged(); }
    }

    public IOFrequencyOutputViewModel FrequencyOutputViewModel
    {
        get => selectedFrequencyOutput;
        set { selectedFrequencyOutput = value; OnPropertyChanged(); }
    }

    public IOCurveViewModel CurveViewModel
    {
        get => selectedCurve;
        set { selectedCurve = value; OnPropertyChanged(); }
    }

    public IOSetupViewModel GenerateViewModel
    {
        get => selectedGenerate;
        set { selectedGenerate = value; OnPropertyChanged(); }
    }

    public bool GeneratorEnabled
    {
        get => IOApplicationConfiguration.GeneratorEnabled;
        set { IOApplicationConfiguration.GeneratorEnabled = value; OnPropertyChanged(); }
    }

    public string GeneratorBaseClass
    {
        get => IOApplicationConfiguration.GeneratorBaseClass;
        set { IOApplicationConfiguration.GeneratorBaseClass = value; OnPropertyChanged(); }
    }

    public string GeneratorNamespace
    {
        get => IOApplicationConfiguration.GeneratorNamespace;
        set { IOApplicationConfiguration.GeneratorNamespace = value; OnPropertyChanged(); }
    }

    public string GeneratorOutputFile
    {
        get => IOApplicationConfiguration.GeneratorOutputFile;
        set { IOApplicationConfiguration.GeneratorOutputFile = value; OnPropertyChanged(); }
    }

    public IOApplicationConfiguration IOApplicationConfiguration
    {
        get => ioApplicationConfiguration;
        set => ioApplicationConfiguration = value;
    }

    public ObservableCollection<IOAnalogInputViewModel> AnalogInputs => rootAnalogInputNode.Children;
    public ObservableCollection<IOAnalogOutputViewModel> AnalogOutputs => rootAnalogOutputNode.Children;
    public ObservableCollection<IODigitalInputViewModel> DigitalInputs => rootDigitalInputNode.Children;
    public ObservableCollection<IODigitalOutputViewModel> DigitalOutputs => rootDigitalOutputNode.Children;
    public ObservableCollection<IOFrequencyInputViewModel> FrequencyInputs => rootFrequencyInputNode.Children;
    public ObservableCollection<IOFrequencyOutputViewModel> FrequencyOutputs => rootFrequencyOutputNode.Children;
    public ObservableCollection<IOCurveViewModel> Curves => rootCurveNode.Children;
    #endregion

    public IOSetupViewModel()
    {

    }

    protected override UserControl OnGetView()
    {
        // Clear any previous views 
        selectedTreeNode = null;
        OnPropertyChanged(nameof(SelectedTreeNode));

        return new IOSetupView() { DataContext = this };
    }

    protected override void OnInitExtension(HardwareInfo hardwareInfo, string projectInfoFolder, string configurationFile)
    {
        this.hardwareInfo = hardwareInfo;
        projectFolder = projectInfoFolder;
        configurationPath = configurationFile;

        LoadConfiguration();
    }

    protected override string OnSave(string packageInfoFolder)
    {
        // Generate path if we have data to save but no path
        if (string.IsNullOrEmpty(configurationPath) || !configurationPath.Contains(CreateConfigurationName()))
            if (rootAnalogInputNode.Children.Count > 0 || rootAnalogOutputNode.Children.Count > 0 ||
                rootDigitalInputNode.Children.Count > 0 || rootDigitalOutputNode.Children.Count > 0 ||
                rootFrequencyInputNode.Children.Count > 0 || rootFrequencyOutputNode.Children.Count > 0 ||
                rootCurveNode.Children.Count > 0)
            {
                configurationPath = CreateConfigurationName();
            }

        ValidateConfiguration();

        if (!string.IsNullOrEmpty(configurationPath))
        {
            // Config Changed...may need to regenerate the Models.
            string data = JsonUtility.Serialize(IOApplicationConfiguration);
            if (data != originalConfigurationText)
            {
                originalConfigurationText = data;
                CustomerToolViewModel.SdkNeedsUpdate();
            }

            File.WriteAllText(Path.Combine(packageInfoFolder, configurationPath), data);
        }

        return configurationPath;
    }

    protected override bool OnHasChanges()
    {
        string newConfig = JsonUtility.Serialize(IOApplicationConfiguration);
        bool hasChanges = newConfig != originalConfigurationText;

        // Config Changed...may need to regenerate the Models.
        if (hasChanges)
            CustomerToolViewModel?.SdkNeedsUpdate();

        return hasChanges;
    }

    protected override void OnClose()
    {
        IOApplicationConfiguration = null;
        configurationPath = null;
        SelectedTreeNode = null;
    }

    internal async void SetConfigurationDirectory()
    {
        string currentPath = "";
        if (configurationPath != null)
            currentPath = Path.Combine(projectFolder, configurationPath);

        var path = await CustomerToolViewModel.FileLocatorAsync("Pick IO Configuration", "json", "IO Configuration", currentPath);
        if (path != null && File.Exists(path))
        {
            var targetPath = Path.Combine(projectFolder, CreateConfigurationName());
            if (path != targetPath)
                File.Copy(path, targetPath, true);

            configurationPath = CreateConfigurationName();
            LoadConfiguration();
        }
    }

    private string CreateConfigurationName()
    {
        return $"IOServiceConfiguration.json";
    }

    private void LoadConfiguration()
    {
        if (configurationPath != null)
        {
            string pathToConfig = Path.Combine(projectFolder, configurationPath);
            if (File.Exists(pathToConfig))
            {
                try
                {
                    originalConfigurationText = File.ReadAllText(pathToConfig);

                    IOApplicationConfiguration = ConfigurationFileLoader.LoadFile<IOApplicationConfiguration>(pathToConfig);

                    IOApplicationConfiguration.GeneratorNamespace = string.IsNullOrEmpty(IOApplicationConfiguration.GeneratorNamespace) ? DefaultGeneratorNamespace : IOApplicationConfiguration.GeneratorNamespace;
                    IOApplicationConfiguration.GeneratorOutputFile = string.IsNullOrEmpty(IOApplicationConfiguration.GeneratorOutputFile) ? DefaultGeneratorOutputFile : IOApplicationConfiguration.GeneratorOutputFile;
                    IOApplicationConfiguration.GeneratorBaseClass = string.IsNullOrEmpty(IOApplicationConfiguration.GeneratorBaseClass) ? DefaultGeneratorBaseClass : IOApplicationConfiguration.GeneratorBaseClass;
                }
                catch
                {
                    CreateDefaultConfiguration();
                }
            }
            else
            {
                CreateDefaultConfiguration();
            }
        }
        else
        {
            CreateDefaultConfiguration();
        }

        RootNodes.Clear();
        rootAnalogInputNode = new() { NodeDescription = "Analog Inputs", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootAnalogOutputNode = new() { NodeDescription = "Analog Outputs", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootDigitalInputNode = new() { NodeDescription = "Digital Inputs", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootDigitalOutputNode = new() { NodeDescription = "Digital Outputs", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootFrequencyInputNode = new() { NodeDescription = "Frequency Inputs", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootFrequencyOutputNode = new() { NodeDescription = "Frequency Outputs", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootCurveNode = new() { NodeDescription = "Curves", Icon = MaterialIconKind.Folder, IsExpanded = true };
        rootToolNode = new() { NodeDescription = "Tools", Icon = MaterialIconKind.Toolbox, IsExpanded = true };

        // Add analog inputs to root node
        foreach (var item in IOApplicationConfiguration.IOConfiguration.AnalogInputs)
            rootAnalogInputNode.Children.Add(new IOAnalogInputViewModel(this, CustomerToolViewModel, item));

        // Add analog outputs to root node
        foreach (var item in IOApplicationConfiguration.IOConfiguration.AnalogOutputs)
            rootAnalogOutputNode.Children.Add(new IOAnalogOutputViewModel(this, CustomerToolViewModel, item));

        // Add digital inputs to root node
        foreach (var item in IOApplicationConfiguration.IOConfiguration.DigitalInputs)
            rootDigitalInputNode.Children.Add(new IODigitalInputViewModel(this, CustomerToolViewModel, item));

        // Add digital outputs to root node
        foreach (var item in IOApplicationConfiguration.IOConfiguration.DigitalOutputs)
            rootDigitalOutputNode.Children.Add(new IODigitalOutputViewModel(this, CustomerToolViewModel, item));

        // Add frequency inputs to root node
        foreach (var item in IOApplicationConfiguration.IOConfiguration.FrequencyInputs)
            rootFrequencyInputNode.Children.Add(new IOFrequencyInputViewModel(this, CustomerToolViewModel, item));

        // Add frequency outputs to root node
        foreach (var item in IOApplicationConfiguration.IOConfiguration.FrequencyOutputs)
            rootFrequencyOutputNode.Children.Add(new IOFrequencyOutputViewModel(this, CustomerToolViewModel, item));

        // Add curves to root node
        foreach (var item in IOApplicationConfiguration.IOConfiguration.Curves)
            rootCurveNode.Children.Add(new IOCurveViewModel(this, CustomerToolViewModel, item));

        // Add tools to root node
        rootToolNode.Children.Add(this);

        // Add root nodes to tree in order
        RootNodes.Add(rootAnalogInputNode);
        RootNodes.Add(rootAnalogOutputNode);
        RootNodes.Add(rootDigitalInputNode);
        RootNodes.Add(rootDigitalOutputNode);
        RootNodes.Add(rootFrequencyInputNode);
        RootNodes.Add(rootFrequencyOutputNode);
        RootNodes.Add(rootCurveNode);
        RootNodes.Add(rootToolNode);

        // Set current version
        IOApplicationConfiguration.Version = VersionUtility.GetAppVersionString();

        SelectedTreeNode = null;
    }

    private void CreateDefaultConfiguration()
    {
        IOApplicationConfiguration = new();

        IOApplicationConfiguration.GeneratorNamespace = DefaultGeneratorNamespace;
        IOApplicationConfiguration.GeneratorOutputFile = DefaultGeneratorOutputFile;
        IOApplicationConfiguration.GeneratorBaseClass = DefaultGeneratorBaseClass;

        IOApplicationConfiguration.IOConfiguration = new();
    }

    internal void AddAnalogInput()
    {
        IOAnalogInputViewModel vm = new(this, CustomerToolViewModel);
        rootAnalogInputNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal void AddAnalogOutput()
    {
        IOAnalogOutputViewModel vm = new(this, CustomerToolViewModel);
        rootAnalogOutputNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal void AddDigitalInput()
    {
        IODigitalInputViewModel vm = new(this, CustomerToolViewModel);
        rootDigitalInputNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal void AddDigitalOutput()
    {
        IODigitalOutputViewModel vm = new(this, CustomerToolViewModel);
        rootDigitalOutputNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal void AddFrequencyInput()
    {
        IOFrequencyInputViewModel vm = new(this, CustomerToolViewModel);
        rootFrequencyInputNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal void AddFrequencyOutput()
    {
        IOFrequencyOutputViewModel vm = new(this, CustomerToolViewModel);
        rootFrequencyOutputNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal void AddCurve()
    {
        IOCurveViewModel vm = new(this, CustomerToolViewModel);
        rootCurveNode.Children.Add(vm);
        SelectedTreeNode = vm;
    }

    internal async void RemoveItem()
    {
        if (SelectedTreeNode is IOAnalogInputViewModel aiNode)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Analog Input", "Are you sure you wish to remove the selected analog input?", "Yes", "Cancel");
            if (!continueWork)
                return;

            IOApplicationConfiguration.IOConfiguration.AnalogInputs.Remove(aiNode.PortConfiguration);
            rootAnalogInputNode.Children.Remove(aiNode);
            SelectedTreeNode = null;

            // Need to renumber the ports in case there is a hole in the channel numbers now
            for (int i = 0; i < rootAnalogInputNode.Children.Count; i++)
            {
                var item = rootAnalogInputNode.Children[i];
                item.PortConfiguration.ChannelNum = (uint)i;
                item.RefreshNodeDescription();
            }
        }
        else if (SelectedTreeNode is IOAnalogOutputViewModel aoNode)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Analog Output", "Are you sure you wish to remove the selected analog output?", "Yes", "Cancel");
            if (!continueWork)
                return;

            IOApplicationConfiguration.IOConfiguration.AnalogOutputs.Remove(aoNode.PortConfiguration);
            rootAnalogOutputNode.Children.Remove(aoNode);
            SelectedTreeNode = null;

            // Need to renumber the ports in case there is a hole in the channel numbers now
            for (int i = 0; i < rootAnalogOutputNode.Children.Count; i++)
            {
                var item = rootAnalogOutputNode.Children[i];
                item.PortConfiguration.ChannelNum = (uint)i;
                item.RefreshNodeDescription();
            }
        }
        else if (SelectedTreeNode is IODigitalInputViewModel diNode)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Digital Input", "Are you sure you wish to remove the selected digital input?", "Yes", "Cancel");
            if (!continueWork)
                return;

            IOApplicationConfiguration.IOConfiguration.DigitalInputs.Remove(diNode.PortConfiguration);
            rootDigitalInputNode.Children.Remove(diNode);
            SelectedTreeNode = null;

            // Need to renumber the ports in case there is a hole in the channel numbers now
            for (int i = 0; i < rootDigitalInputNode.Children.Count; i++)
            {
                var item = rootDigitalInputNode.Children[i];
                item.PortConfiguration.ChannelNum = (uint)i;
                item.RefreshNodeDescription();
            }
        }
        else if (SelectedTreeNode is IODigitalOutputViewModel doNode)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Digital Output", "Are you sure you wish to remove the selected digital output?", "Yes", "Cancel");
            if (!continueWork)
                return;

            IOApplicationConfiguration.IOConfiguration.DigitalOutputs.Remove(doNode.PortConfiguration);
            rootDigitalOutputNode.Children.Remove(doNode);
            SelectedTreeNode = null;

            // Need to renumber the ports in case there is a hole in the channel numbers now
            for (int i = 0; i < rootDigitalOutputNode.Children.Count; i++)
            {
                var item = rootDigitalOutputNode.Children[i];
                item.PortConfiguration.ChannelNum = (uint)i;
                item.RefreshNodeDescription();
            }
        }
        else if (SelectedTreeNode is IOFrequencyInputViewModel fiNode)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Frequency Input", "Are you sure you wish to remove the selected frequency input?", "Yes", "Cancel");
            if (!continueWork)
                return;

            IOApplicationConfiguration.IOConfiguration.FrequencyInputs.Remove(fiNode.PortConfiguration);
            rootFrequencyInputNode.Children.Remove(fiNode);
            SelectedTreeNode = null;

            // Need to renumber the ports in case there is a hole in the channel numbers now
            for (int i = 0; i < rootFrequencyInputNode.Children.Count; i++)
            {
                var item = rootFrequencyInputNode.Children[i];
                item.PortConfiguration.ChannelNum = (uint)i;
                item.RefreshNodeDescription();
            }
        }
        else if (SelectedTreeNode is IOFrequencyOutputViewModel foNode)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Frequency Output", "Are you sure you wish to remove the selected frequency output?", "Yes", "Cancel");
            if (!continueWork)
                return;

            IOApplicationConfiguration.IOConfiguration.FrequencyOutputs.Remove(foNode.PortConfiguration);
            rootFrequencyOutputNode.Children.Remove(foNode);
            SelectedTreeNode = null;

            // Need to renumber the ports in case there is a hole in the channel numbers now
            for (int i = 0; i < rootFrequencyOutputNode.Children.Count; i++)
            {
                var item = rootFrequencyOutputNode.Children[i];
                item.PortConfiguration.ChannelNum = (uint)i;
                item.RefreshNodeDescription();
            }
        }
        else if (SelectedTreeNode is IOCurveViewModel curveNode)
        {
            var continueWork = await CustomerToolViewModel.ShowDialog("Remove Curve", "Are you sure you wish to remove the selected curve?", "Yes", "Cancel");
            if (!continueWork)
                return;

            IOApplicationConfiguration.IOConfiguration.Curves.Remove(curveNode.CurveDefinition);
            rootCurveNode.Children.Remove(curveNode);
            SelectedTreeNode = null;
        }
    }

    private void ValidateConfiguration()
    {
        // TODO: what to validate?
    }

    #region ITreeNode
    public bool IsEditable { get; } = false;

    public bool IsEnabled { get; set; } = false;

    public string NodeDescription
    {
        get { return $"Code Generation Setup"; }
        set { }
    }

    public MaterialIconKind Icon
    {
        get => MaterialIconKind.Tools;
    }

    public IEnumerable<ITreeNode> GetChildren() { return Enumerable.Empty<ITreeNode>(); }
    #endregion
}
