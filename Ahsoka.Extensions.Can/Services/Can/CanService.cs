using Ahsoka.Installer.Components;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.Can.Platform;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using static NetMQ.NetMQSelector;

namespace Ahsoka.Services.Can;

/// <summary>
/// Service for Interacting with CAN via the CAN Service Implementation (typically MCU)
/// </summary>
[AhsokaService(Name)]
public class CanService : AhsokaServiceBase<CanMessageTypes.Ids>
{
    #region Fields
    /// <summary>
    /// Configuration Name for the Can Service
    /// </summary>
    public const string Name = "CanService";
    readonly CanApplicationConfiguration calibration;
    readonly Dictionary<uint, CanServiceImplementation> portHandlers = new();
    #endregion

    #region Methods

    internal static CanServiceImplementation GetInterop(CanInterface interfaceType)
    {
        switch (SystemInfo.CurrentPlatform)
        {
            case PlatformFamily.Windows64:
                if (interfaceType == CanInterface.ECOMWindows)
                    return new ECOMServiceImplementation();
                else
                    return new DesktopServiceImplementation();

            case PlatformFamily.MacosArm64:
                return new DesktopServiceImplementation();

            case PlatformFamily.Ubuntu64:
                return new DesktopServiceImplementation();

            case PlatformFamily.OpenViewLinux:
                if (interfaceType == CanInterface.Coprocessor)
                    return new STCoprocessorServiceImplementation();
                else
                    return new STSocketCanServiceImplementation();

            default:
                throw new PlatformNotSupportedException();
        }
    }

    /// <summary>
    /// Can Service Constructor using Default Service Configuration
    /// </summary>
    public CanService() :
        this(ConfigurationLoader.GetServiceConfig(Name))
    {

    }

    /// <summary>
    /// Can Service Constructor accepting a customer Service Configuration
    /// </summary>
    /// <param name="config"></param>
    public CanService(ServiceConfiguration config) :
        base(config, new CanServiceMessages())
    {
        // Load Configurations
        string coprocessorPath = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.CoProcessorApplicationPath);
        string configPath = Path.Combine(coprocessorPath, CanInstallerComponent.applicationConfiguration);
        if (!File.Exists(configPath))
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CAN Configuration not found at {configPath}");
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Attempting to load JSON Configuration from program output folder");
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Note: You can include a CANServiceConfiguration.json in your program output for use on windows ");

            // Attempt to load local .json file if available
            string configInfo = "";
            foreach (var path in Directory.EnumerateFiles(".", $"CANServiceConfiguration.json"))
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Found / Loading Configuration File @ {path}");
                configInfo = path;
                break;
            }                
            
            calibration = CanMetadataTools.GenerateApplicationConfig(SystemInfo.HardwareInfo, configInfo, false);
        }
        else
        {
            // Load Calibration and Return to Client
            using var fs = new FileStream(configPath, FileMode.Open);
            fs.Seek(0, SeekOrigin.Begin);
            calibration = ProtoBuf.Serializer.Deserialize<CanApplicationConfiguration>(fs);
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CAN Configuration Loaded From {configPath}");
        }
    }

    /// <summary>
    /// Handle Recieved Messages to pass to the Service Implementation
    /// </summary>
    /// <param name="request"></param>
    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case CanMessageTypes.Ids.OpenCommunicationChannel:
                HandleOpenChannel(request);
                break;

            case CanMessageTypes.Ids.CloseCommunicationChannel:

                HandleCloseChannel(request);
                break;

            case CanMessageTypes.Ids.SendCanMessages:

                HandleSendCanMessages(request, request.Message as CanMessageDataCollection);
                break;

            case CanMessageTypes.Ids.SendRecurringCanMessage:

                SendRecurringMessage(request, request.Message as RecurringCanMessage);
                break;

            case CanMessageTypes.Ids.ApplyMessageFilter:

                HandleFilterCanMessages(request, request.Message as ClientCanFilter);
                break;

            default:
                break;
        }
    }

    private void HandleFilterCanMessages(AhsokaServiceRequest messageHeader, ClientCanFilter clientCanFilter)
    {
        if (portHandlers.TryGetValue(clientCanFilter.CanPort, out CanServiceImplementation impl))
            impl.SetClientMessageFilter(clientCanFilter);

        SendResponse(messageHeader);
    }

    private void HandleOpenChannel(AhsokaServiceRequest messageHeader)
    {
        // Load Handlers
        foreach (var item in calibration.CanPortConfiguration.MessageConfiguration.Ports)
        {
            try
            {
                var serviceHandler = GetInterop(item.CanInterface);
                serviceHandler.Open(this, calibration.CanPortConfiguration, item.Port);
                portHandlers[item.Port] = serviceHandler;
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"CAN Ready for Port {item.Port}");
            }
            catch (Exception ex)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Failed to Open {item.Port} - {ex.Message}");
            }
        }

        SendResponse(messageHeader, calibration);
    }

    private void HandleCloseChannel(AhsokaServiceRequest messageHeader)
    {
        foreach (var item in portHandlers)
            item.Value.Close();

        portHandlers.Clear();

        SendResponse(messageHeader);
    }

    private void HandleSendCanMessages(AhsokaServiceRequest messageHeader, CanMessageDataCollection canMessageDataCollection)
    {
        var status = new CanMessageResult();
        if (portHandlers.TryGetValue(canMessageDataCollection.CanPort, out CanServiceImplementation impl))
            status = impl.HandleSendCanRequest(canMessageDataCollection);

        SendResponse(messageHeader, status);
    }

    private void SendRecurringMessage(AhsokaServiceRequest messageHeader, RecurringCanMessage canMessageDataCollection)
    {
        var status = new CanMessageResult();
        if (portHandlers.TryGetValue(canMessageDataCollection.CanPort, out CanServiceImplementation impl))
            status = impl.HandleSendRecurringRequest(canMessageDataCollection);

        SendResponse(messageHeader, status);
    }

    internal void NotifyStateUpdate(CanState stateInfo)
    {
        SendNotification(CanMessageTypes.Ids.NetworkStateChanged, stateInfo);
    }

    internal void NotifyCanMessages(CanMessageDataCollection messages)
    {
        SendNotification(CanMessageTypes.Ids.CanMessagesReceived, messages);
    }

    #endregion
}


