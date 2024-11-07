#pragma warning disable CS1591
using Ahsoka.Core.IO.Hardware;
using Ahsoka.Installer;
using Ahsoka.ServiceFramework;
using Ahsoka.Services.IO.Platform;
using Ahsoka.Services.System;
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

                        int transportID = IOMessageTypes.Ids.IgnitionOnNotification.EnumToInt();
                        if (ignitionState == IgnitionStates.Off)
                            transportID = IOMessageTypes.Ids.IgnitionOffNotification.EnumToInt();

                        var header = new AhsokaMessageHeader()
                        {
                            TransportId = transportID
                        };

                        // Update Ignition in Data Service
                        UpdateCacheValue(IOServiceMessages.CurrentIgnitionState, ignitionState.EnumToInt());

                        SendMessageInternal(header, new EmptyNotification(), true);
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

    protected override void OnEndPointDisconnected()
    {
        base.OnEndPointDisconnected();
        _IOImplementationBase.StopPolling();
        cancelNotifications?.Cancel();

    }

    protected override void OnHandleReceive(AhsokaMessageHeader messageHeader, object message)
    {
        switch (messageHeader.TransportId.IntToEnum<IOMessageTypes.Ids>())
        {
            case IOMessageTypes.Ids.SetPollInterval:
                HandleSetPollInterval(messageHeader, message as PollingInterval);
                break;

            case IOMessageTypes.Ids.GetPollInterval:
                HandleGetPollInterval(messageHeader);
                break;

            case IOMessageTypes.Ids.RetrieveDigitalOutputs:
                HandleRetrieveDigitalOutputs(messageHeader, message);
                break;

            case IOMessageTypes.Ids.RetrieveDigitalInputs:
                HandleRetrieveDigitalInputs(messageHeader, message);
                break;

            case IOMessageTypes.Ids.RetrieveAnalogOutputs:
                HandleRetrieveAnalogOutputs(messageHeader, message);
                break;

            case IOMessageTypes.Ids.RetrieveAnalogInputs:
                HandleRetrieveAnalogInputs(messageHeader, message);
                break;

            case IOMessageTypes.Ids.SetDigitalOutput:
                HandelSetDigitalOut(messageHeader, message as DigitalOutput);
                break;

            case IOMessageTypes.Ids.SetAnalogOutput:
                HandelSetAnalogOut(messageHeader, message as AnalogOutput);
                break;

            case IOMessageTypes.Ids.GetAnalogInput:
                HandleGetAnalogIn(messageHeader, message as AnalogInput);
                break;

            case IOMessageTypes.Ids.GetDigitalInput:
                HandleGetDigitalIn(messageHeader, message as DigitalInput);
                break;

            case IOMessageTypes.Ids.GetBuzzerConfig:
                HandleGetBuzzerConfig(messageHeader);
                break;

            case IOMessageTypes.Ids.SetBuzzerConfig:
                HandleSetBuzzerConfig(messageHeader, message as BuzzerConfig);
                break;

            case IOMessageTypes.Ids.GetBatteryVoltage:
                HandleGetVBat(messageHeader);
                break;

            case IOMessageTypes.Ids.GetIgnitionPin:
                HandleGetIGNPin(messageHeader);
                break;

            default:
                break;
        }
    }

    private void HandleGetPollInterval(AhsokaMessageHeader messageHeader)
    {
        var response = _IOImplementationBase.GetPollingInterval();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleSetPollInterval(AhsokaMessageHeader messageHeader, PollingInterval message)
    {
        _IOImplementationBase.SetPollingInterval(message);
        SendMessageInternal(messageHeader);

    }

    private void HandleRetrieveDigitalOutputs(AhsokaMessageHeader messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveDigitalOutputs();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleRetrieveDigitalInputs(AhsokaMessageHeader messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveDigitalInputs();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleRetrieveAnalogInputs(AhsokaMessageHeader messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveAnalogInputs();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleRetrieveAnalogOutputs(AhsokaMessageHeader messageHeader, object message)
    {
        var response = _IOImplementationBase.RetrieveAnalogOutputs();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleGetDigitalIn(AhsokaMessageHeader messageHeader, DigitalInput d)
    {
        var response = _IOImplementationBase.GetDigitalInput(d);
        SendMessageInternal(messageHeader, response);
    }

    private void HandleGetAnalogIn(AhsokaMessageHeader messageHeader, AnalogInput a)
    {
        var response = _IOImplementationBase.GetAnalogInput(a);
        SendMessageInternal(messageHeader, response);
    }

    private void HandelSetDigitalOut(AhsokaMessageHeader messageHeader, DigitalOutput d)
    {
        var response = _IOImplementationBase.SetDigitalOut(d);
        SendMessageInternal(messageHeader, response);
    }

    private void HandelSetAnalogOut(AhsokaMessageHeader messageHeader, AnalogOutput a)
    {
        var response = _IOImplementationBase.SetAnalogOut(a);
        SendMessageInternal(messageHeader, response);
    }



    private void HandleGetBuzzerConfig(AhsokaMessageHeader messageHeader)
    {
        SendMessageInternal(messageHeader, _IOImplementationBase.GetBuzzerConfig());
    }

    private void HandleSetBuzzerConfig(AhsokaMessageHeader messageHeader, BuzzerConfig buzzerConfig)
    {
        _IOImplementationBase.SetBuzzerConfig(buzzerConfig);
        SendMessageInternal(messageHeader);
    }


    private void HandleGetVBat(AhsokaMessageHeader messageHeader)
    {
        if (HardwareInteractionDisabled)
        {
            SendMessageInternal(messageHeader, new VoltageValue());
            return;
        }

        var response = _IOImplementationBase.GetVBat();
        SendMessageInternal(messageHeader, response);
    }

    private void HandleGetIGNPin(AhsokaMessageHeader messageHeader)
    {
        if (HardwareInteractionDisabled)
        {
            SendMessageInternal(messageHeader, new IgnitionState() { State = IgnitionStates.Unknown });
            return;
        }

        var response = _IOImplementationBase.GetIGNPin();
        SendMessageInternal(messageHeader, response);
    }
    #endregion
}
