using Ahsoka.Core.IO.Hardware;
using Ahsoka.ServiceFramework;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ahsoka.Services.IO;

internal abstract class IOServiceImplementationBase
{
    #region Fields
    private IOService _ioService;
    internal Timer timer;
    int currentTimerValue = 500;

    private AnalogInputList analogInList;
    private AnalogOutputList analogOutList;
    private DigitalInputList digitalInList;
    private DigitalOutputList digitalOutList;

    private IAnalogInputImplementation analogInputImplementation;
    private IAnalogOutputImplementation analogOutputImplementation;
    private IDigitalInputImplementation digitalInputImplementation;
    private IDigitalOutputImplementation digitalOutputImplementation;

    ConcurrentDictionary<int,GetInputResponse> latestAnalogValues = new();
    ConcurrentDictionary<int, GetInputResponse> latestDigitalValues = new();
    #endregion

    public void Init(IOService service, IOHardwareInfoExtension IOInfo)
    {
        // Initialize internal members
        _ioService = service;
        analogInList = new AnalogInputList();
        analogOutList = new AnalogOutputList();
        digitalInList = new DigitalInputList();
        digitalOutList = new DigitalOutputList();

        // Load each IO Pin 
        foreach (int pin in IOInfo.AnalogInputs)
        {
            analogInList.AnalogInputs.Add(new() { Pin = pin });
            latestAnalogValues.TryAdd(pin, new());
            _ioService.UpdateCacheValue(IOServiceMessages.AnalogInput_ + pin.ToString(), 0);
        }

        foreach (int pin in IOInfo.AnalogOutputs)
        {
            analogOutList.AnalogOutputs.Add(new() { Pin = pin });
            _ioService.UpdateCacheValue(IOServiceMessages.AnalogOutput_ + pin.ToString(), 0);
        }

        foreach (int pin in IOInfo.DigitalInputs)
        {
            digitalInList.DigitalInputs.Add(new() { Pin = pin });
            latestDigitalValues.TryAdd(pin, new());
            _ioService.UpdateCacheValue(IOServiceMessages.DigitalInput_ + pin.ToString(), 0);
        }

        foreach (int pin in IOInfo.DigitalOutputs)
        {
            digitalOutList.DigitalOutputs.Add(new() { Pin = pin });
            _ioService.UpdateCacheValue(IOServiceMessages.DigitalOutput_ + pin.ToString(), 0);
        }

        OnHandleInit(IOInfo, out analogInputImplementation, out analogOutputImplementation, out digitalInputImplementation, out digitalOutputImplementation);

        LoadLatestValues(this);

        // Start Poll Timer
        timer = new Timer(LoadLatestValues, this, currentTimerValue, currentTimerValue);
    }

    private void LoadLatestValues(object sender)
    {
        lock (_ioService)
        {
            try
            {
                // Load each IO Pin 
                foreach (var item in latestAnalogValues.Keys)
                {
                    var newValue = analogInputImplementation.ReadVolts(item);
                    if (newValue.Ret == ReturnCode.Success)
                        _ioService.UpdateCacheValue(IOServiceMessages.AnalogInput_ + item.ToString(), newValue.Value);

                    latestAnalogValues[item] = newValue;
                }

                foreach (var item in latestDigitalValues.Keys)
                {
                    var newValue = digitalInputImplementation.ReadVolts(item);
                    if (newValue.Ret == ReturnCode.Success)
                        _ioService.UpdateCacheValue(IOServiceMessages.DigitalInput_ + item.ToString(), newValue.Value);

                    latestDigitalValues[item] = newValue;
                }
            }
            catch (IOException ex) 
            { 
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, ex.ToString()); 
            }
        }
    }

    protected abstract void OnHandleInit(IOHardwareInfoExtension IOInfo,
        out IAnalogInputImplementation analogInputImplementation, 
        out IAnalogOutputImplementation analogOutputImplementation, 
        out IDigitalInputImplementation digitalInputImplementation, 
        out IDigitalOutputImplementation digitalOutputImplementation);

    // Functions to Retrieve IO Pins
    public DigitalInputList RetrieveDigitalInputs()
    {
        return digitalInList;
    }

    public DigitalOutputList RetrieveDigitalOutputs()
    {
        return digitalOutList;
    }

    public AnalogInputList RetrieveAnalogInputs()
    {
        return analogInList;
    }

    public AnalogOutputList RetrieveAnalogOutputs()
    {
        return analogOutList;
    }

    public SetOutputResponse SetDigitalOut(DigitalOutput d)
    {
        if (!digitalOutList.DigitalOutputs.Any(x => x.Pin == d.Pin))
            return new() { ErrorDescription = "Output Pin Not Found!" };

        lock (_ioService)
        {
            var returnValue = digitalOutputImplementation.SetOutput(d.Pin, d.State);

            // Notify Data Service of New Value.
            _ioService.UpdateCacheValue(IOServiceMessages.DigitalOutput_ + d.Pin.ToString(), (int)d.State);

            return returnValue;
        }
    }

    public SetOutputResponse SetAnalogOut(AnalogOutput a)
    {
        if (!analogOutList.AnalogOutputs.Any(x => x.Pin == a.Pin))
            return new() { ErrorDescription = "Output Pin Not Found!" };

        lock (_ioService)
        {
            var returnValue = analogOutputImplementation.SetOutput(a.Pin, a.MilliVolts);

            // Notify Data Service of New Value.
            _ioService.UpdateCacheValue(IOServiceMessages.AnalogOutput_ + a.Pin.ToString(), a.MilliVolts);

            return returnValue;
        }
    }

    public GetInputResponse GetAnalogInput(AnalogInput aIn)
    {
        if (!analogInList.AnalogInputs.Any(x => x.Pin == aIn.Pin))
            return new() { ErrorDescription = "Output Pin Not Found!" };

        // Return Current Value if timer is running
        // if not, call the input read directly
        lock (_ioService)
        {
            if (currentTimerValue == int.MaxValue)
            {
                var returnValue = analogInputImplementation.ReadVolts(aIn.Pin);
                _ioService.UpdateCacheValue(IOServiceMessages.AnalogInput_ + aIn.Pin.ToString(), returnValue.Value);
                return returnValue;
            }
            else
                return latestAnalogValues[aIn.Pin];
        }
    }

    public GetInputResponse GetDigitalInput(DigitalInput dIn)
    {
        if (!digitalInList.DigitalInputs.Any(x => x.Pin == dIn.Pin))
            return new() { ErrorDescription = "Output Pin Not Found!" };

        // Return Current Value if timer is running
        // if not, call the input read directly
        lock (_ioService)
        {
            if (currentTimerValue == int.MaxValue)
            {
                var returnValue = digitalInputImplementation.ReadVolts(dIn.Pin);
                _ioService.UpdateCacheValue(IOServiceMessages.DigitalInput_ + dIn.Pin.ToString(), returnValue.Value);
                return returnValue;
            }
            else
                return latestDigitalValues[dIn.Pin];
        }
    }

    internal void SetPollingInterval(PollingInterval message)
    {
        currentTimerValue = message.PollingIntervalMs;
        if (currentTimerValue != int.MaxValue)
            timer.Change(currentTimerValue, currentTimerValue);
        else
            timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    internal PollingInterval GetPollingInterval()
    {
        return new() { PollingIntervalMs = currentTimerValue };
    }

    internal void StopPolling()
    {
        timer.Dispose();
    }


    internal abstract BuzzerConfig GetBuzzerConfig();

    internal abstract void SetBuzzerConfig(BuzzerConfig buzzerConfig);

    internal abstract VoltageValue GetVBat();

    internal abstract IgnitionState GetIGNPin();

}

internal interface IDigitalOutputImplementation
{
    SetOutputResponse SetOutput(int pin, PinState state);
}

internal interface IDigitalInputImplementation
{
    GetInputResponse ReadVolts(int pin);
}

internal interface IAnalogOutputImplementation
{
    SetOutputResponse SetOutput(int pin, double millivolts);
}

internal interface IAnalogInputImplementation
{
    GetInputResponse ReadVolts(int pin);
}

