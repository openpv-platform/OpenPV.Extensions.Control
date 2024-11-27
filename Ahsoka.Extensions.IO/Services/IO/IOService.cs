#pragma warning disable CS1591
using Ahsoka.Core.IO.Hardware;
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.IO.Platform;
using Ahsoka.System;
using Ahsoka.System.Hardware;
using Ahsoka.Utility;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.IO;

[AhsokaService(Name)]
public class IOService : AhsokaServiceBase<IOMessageTypes.Ids>
{
    #region Fields
    readonly CancellationTokenSource cancelNotifications;
    static bool hardwareInteractionDisabled = false;
    public static bool HardwareInteractionDisabled { get => hardwareInteractionDisabled; }

    public const string Name = "IOService";
    private readonly IOServiceImplementationBase _IOImplementationBase;
    // private IOServiceSettings _settings;
    #endregion

    #region Events
    #endregion

    #region Methods
    internal static IOServiceImplementationBase GetInterop()
    {
        return SystemInfo.CurrentPlatform switch
        {
            PlatformFamily.Windows64 => new DesktopServiceImplementation(),
            PlatformFamily.OpenViewLinux => new STServiceImplementation(),
            _ => throw new PlatformNotSupportedException(),
        };
    }

    public IOService() : this(ConfigurationLoader.GetServiceConfig(Name)) { }

    public IOService(ServiceConfiguration config) : base(config, new IOServiceMessages())
    {
        using var stopwatch = new AhsokaStopwatch("Create IOService");

        _IOImplementationBase = GetInterop();

        var ioInfo = IOHardwareInfoExtension.GetIOInfo(SystemInfo.HardwareInfo.PlatformFamily, SystemInfo.HardwareInfo.PlatformQualifier);

        _IOImplementationBase.Init(this, ioInfo);

        // Disable Service if Running Codesys
        string currentAppPath = SystemInfo.HardwareInfo.TargetPathInfo.GetRootPaths(RootPaths.ApplicationRoot);

        if (Environment.ProcessPath.ToLower().StartsWith(currentAppPath.ToLower()))
        {
            string appTypeFile = SystemInfo.HardwareInfo.TargetPathInfo.GetInstallerPath(InstallerPaths.ApplicationType);
            if (File.Exists(appTypeFile) && File.ReadAllText(appTypeFile) == ApplicationType.CODESYS.ToString())
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "ERROR - IO Service is not Supported on CODESYS Targets");
                throw new ApplicationException("ERROR - IO Service is not Supported on CODESYS Targets");
            }
        }

        // If Hardware Enabled Start Ignition Thread
        cancelNotifications = new CancellationTokenSource();

        UpdateCacheValue(IOServiceMessages.CurrentIgnitionState, IgnitionStates.Unknown.EnumToInt());

        Task.Factory.StartNew(() =>
        {
            try
            {
                IgnitionStates ignitionState = IgnitionStates.Unknown;  // Assume on at start-up
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, "Ignition Monitor Started");

                while (!cancelNotifications.IsCancellationRequested)
                {
                    var response = _IOImplementationBase.GetIGNPin();
                    if (ignitionState != response.State && response.State != IgnitionStates.Unknown)
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Ignition Monitor: IgnitionState is {response.State}");
                        ignitionState = response.State;

                        IOMessageTypes.Ids transportID = IOMessageTypes.Ids.IgnitionOnNotification;
                        if (ignitionState == IgnitionStates.Off)
                            transportID = IOMessageTypes.Ids.IgnitionOffNotification;


                        // Update Ignition in Data Service
                        UpdateCacheValue(IOServiceMessages.CurrentIgnitionState, ignitionState.EnumToInt());

                        SendNotification(transportID, new EmptyNotification());
                    }

                    Task.Delay(1000, cancelNotifications.Token).Wait();
                }
            }
            catch (Exception ex)
            {
                if (!(ex.InnerException is OperationCanceledException))
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Ignition notifications terminated with exception: {ex.Message}");
                    AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.StackTrace);
                }
            }
        });

        stopwatch.StopTiming("Ignition Thread Started");


    }

    protected override void OnEndPointStopped()
    {
        base.OnEndPointStopped();
        _IOImplementationBase.StopPolling();
        cancelNotifications?.Cancel();

    }

    protected override void OnHandleServiceRequest(AhsokaServiceRequest request)
    {
        switch (request.TransportId)
        {
            case IOMessageTypes.Ids.SetPollInterval:
                HandleSetPollInterval(request, request.Message as PollingInterval);
                break;

            case IOMessageTypes.Ids.GetPollInterval:
                HandleGetPollInterval(request);
                break;

            case IOMessageTypes.Ids.RetrieveDigitalOutputs:
                HandleRetrieveDigitalOutputs(request, request.Message);
                break;

            case IOMessageTypes.Ids.RetrieveDigitalInputs:
                HandleRetrieveDigitalInputs(request, request.Message);
                break;

            case IOMessageTypes.Ids.RetrieveAnalogOutputs:
                HandleRetrieveAnalogOutputs(request, request.Message);
                break;

            case IOMessageTypes.Ids.RetrieveAnalogInputs:
                HandleRetrieveAnalogInputs(request, request.Message);
                break;

            case IOMessageTypes.Ids.SetDigitalOutput:
                HandelSetDigitalOut(request, request.Message as DigitalOutput);
                break;

            case IOMessageTypes.Ids.SetAnalogOutput:
                HandelSetAnalogOut(request, request.Message as AnalogOutput);
                break;

            case IOMessageTypes.Ids.GetAnalogInput:
                HandleGetAnalogIn(request, request.Message as AnalogInput);
                break;

            case IOMessageTypes.Ids.GetDigitalInput:
                HandleGetDigitalIn(request, request.Message as DigitalInput);
                break;

            case IOMessageTypes.Ids.GetBuzzerConfig:
                HandleGetBuzzerConfig(request);
                break;

            case IOMessageTypes.Ids.SetBuzzerConfig:
                HandleSetBuzzerConfig(request, request.Message as BuzzerConfig);
                break;

            case IOMessageTypes.Ids.GetBatteryVoltage:
                HandleGetVBat(request);
                break;

            case IOMessageTypes.Ids.GetIgnitionPin:
                HandleGetIGNPin(request);
                break;

            default:
                break;
        }
    }

    private void HandleGetPollInterval(AhsokaServiceRequest messageHeader)
    {
        var response = _IOImplementationBase.GetPollingInterval();
        SendResponse(messageHeader, response);
    }

    private void HandleSetPollInterval(AhsokaServiceRequest messageHeader, PollingInterval message)
    {
        _IOImplementationBase.SetPollingInterval(message);
        SendResponse(messageHeader);

    }

    private void HandleRetrieveDigitalOutputs(AhsokaServiceRequest messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveDigitalOutputs();
        SendResponse(messageHeader, response);
    }

    private void HandleRetrieveDigitalInputs(AhsokaServiceRequest messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveDigitalInputs();
        SendResponse(messageHeader, response);
    }

    private void HandleRetrieveAnalogInputs(AhsokaServiceRequest messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveAnalogInputs();
        SendResponse(messageHeader, response);
    }

    private void HandleRetrieveAnalogOutputs(AhsokaServiceRequest messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveAnalogOutputs();
        SendResponse(messageHeader, response);
    }

    private void HandleGetDigitalIn(AhsokaServiceRequest messageHeader, DigitalInput d)
    {
        var response = _IOImplementationBase.GetDigitalInput(d);
        SendResponse(messageHeader, response);
    }

    private void HandleGetAnalogIn(AhsokaServiceRequest messageHeader, AnalogInput a)
    {
        var response = _IOImplementationBase.GetAnalogInput(a);
        SendResponse(messageHeader, response);
    }

    private void HandelSetDigitalOut(AhsokaServiceRequest messageHeader, DigitalOutput d)
    {
        var response = _IOImplementationBase.SetDigitalOut(d);
        SendResponse(messageHeader, response);
    }

    private void HandelSetAnalogOut(AhsokaServiceRequest messageHeader, AnalogOutput a)
    {
        var response = _IOImplementationBase.SetAnalogOut(a);
        SendResponse(messageHeader, response);
    }

    private void HandleGetBuzzerConfig(AhsokaServiceRequest messageHeader)
    {
        SendResponse(messageHeader, _IOImplementationBase.GetBuzzerConfig());
    }

    private void HandleSetBuzzerConfig(AhsokaServiceRequest messageHeader, BuzzerConfig buzzerConfig)
    {
        _IOImplementationBase.SetBuzzerConfig(buzzerConfig);
        SendResponse(messageHeader);
    }


    private void HandleGetVBat(AhsokaServiceRequest messageHeader)
    {
        if (HardwareInteractionDisabled)
        {
            SendResponse(messageHeader, new VoltageValue());
            return;
        }

        var response = _IOImplementationBase.GetVBat();
        SendResponse(messageHeader, response);
    }

    private void HandleGetIGNPin(AhsokaServiceRequest messageHeader)
    {
        if (HardwareInteractionDisabled)
        {
            SendResponse(messageHeader, new IgnitionState() { State = IgnitionStates.Unknown });
            return;
        }

        var response = _IOImplementationBase.GetIGNPin();
        SendResponse(messageHeader, response);
    }
    #endregion
}
