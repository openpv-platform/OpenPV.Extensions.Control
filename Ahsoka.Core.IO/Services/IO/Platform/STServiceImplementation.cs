using Ahsoka.Services.IO.RCD;
using Ahsoka.System.Hardware;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;


namespace Ahsoka.Services.IO.Platform;

[ExcludeFromCodeCoverage]
internal class STServiceImplementation : IOServiceImplementationBase
{
    #region Fields
    private const string buzzerExportPath = "/sys/class/pwm/pwmchip4/export";
    private const string buzzerDriverPath = "/sys/class/pwm/pwmchip4/pwm0";
    bool buzzerInit = false;
    #endregion

    #region Methods
    protected override void OnHandleInit(IOHardwareInfo IOInfo,
        out IAnalogInputImplementation analogInputImplementation,
        out IAnalogOutputImplementation analogOutputImplementation,
        out IDigitalInputImplementation digitalInputImplementation,
        out IDigitalOutputImplementation digitalOutputImplementation)
    {
        analogInputImplementation = new RCDAnalogInput();
        analogOutputImplementation = new RCDAnalogOutput();
        digitalInputImplementation = new RCDDigitalInput();
        digitalOutputImplementation = new RCDDigitalOutput();
    }

    #endregion


    private void BuzzerInit()
    {
        if (!Directory.Exists(buzzerDriverPath))
        {
            Console.WriteLine("Exporting Channel - Initialzing Buzzer");
            File.WriteAllText(buzzerExportPath, "0");
        }
        buzzerInit = true;
    }

    internal override BuzzerConfig GetBuzzerConfig()
    {
        if (!buzzerInit)
            BuzzerInit();

        int volumeAdjusted = Int32.Parse(File.ReadAllText(Path.Combine(buzzerDriverPath, "duty_cycle")).Trim());
        int period = Int32.Parse(File.ReadAllText(Path.Combine(buzzerDriverPath, "period")).Trim());

        BuzzerConfig buzzerConfig = new()
        {
            IsEnabled = Int32.Parse(File.ReadAllText(Path.Combine(buzzerDriverPath, "enable")).Trim()) == 1 ? true : false,
            VolumePct = volumeAdjusted == 0 ? 0 : (int)(volumeAdjusted / (period * 0.5 * 0.01)),
            FrequencyInHz = period == 0 ? 0 : 1000000000 / period
        };

        return buzzerConfig;
    }

    internal override void SetBuzzerConfig(BuzzerConfig buzzerConfig)
    {
        if (!buzzerInit)
            BuzzerInit();

        // NOTE: Frequency is bounded within a range of [1, 5000]. Volume% is bounded within a range of [0, 100].
        buzzerConfig.FrequencyInHz = Math.Max(Math.Min(buzzerConfig.FrequencyInHz, 5000), 1);
        buzzerConfig.VolumePct = Math.Max(Math.Min(buzzerConfig.VolumePct, 100), 0);

        int period = 1000000000 / buzzerConfig.FrequencyInHz;
        int dutyCycle = (int)((period / 2.0f) * (buzzerConfig.VolumePct / 100.0f)); // Ensure < Period
        int enable = buzzerConfig.IsEnabled ? 1 : 0;

        // NOTE: duty_cycle must be lower than or equal to period. The following conditional makes sure to not write a period less than current duty_cycle.
        int currDutyCycle = Int32.Parse(File.ReadAllText(Path.Combine(buzzerDriverPath, "duty_cycle")).Trim());
        if (currDutyCycle > period)
            File.WriteAllText(Path.Combine(buzzerDriverPath, "duty_cycle"), "0");

        File.WriteAllText(Path.Combine(buzzerDriverPath, "period"), period.ToString());
        File.WriteAllText(Path.Combine(buzzerDriverPath, "duty_cycle"), dutyCycle.ToString());
        File.WriteAllText(Path.Combine(buzzerDriverPath, "enable"), enable.ToString()); // period can not be 0 when enabling
    }


    internal override VoltageValue GetVBat()
    {
        /*
            Public function to read and return the voltage value at the
            VBAT Pin. Makes use of existing RCD IO Service ADC utility.
        */
        double VBatVoltageDivider = ((56 + 5.1) / 5.1);

        var response = new VoltageValue();
        float RawValue = ADCUtils.GetVoltsRawValue(ADCInput.VBAT);

        // apply voltage divider to calculate volts at the input pin
        response.MilliVolts = RawValue * VBatVoltageDivider;
        return response;
    }

    IgnitionStates state = IgnitionStates.Unknown;
    internal override IgnitionState GetIGNPin()
    {
        double returnValue = -1;

        // Return Unknown Until System is Ready
        if (File.Exists("/sys/devices/platform/soc/48003000.adc/48003000.adc:adc@0/iio:device0/in_voltage_scale"))
        {
            /*
                Public function to read and return the voltage value at the
                Ignition (IGN) Pin. Makes use of existing RCD IO Service ADC utility.
            */
            double IGNPinVoltageDivider = ((56 + 5.1) / 5.1);

            float RawValue = ADCUtils.GetVoltsRawValue(ADCInput.IGN_PIN);

            // apply voltage divider to calculate volts at the input pin
            returnValue = RawValue * IGNPinVoltageDivider;
        }

        // Check whether IGN_PIN is grounded.  LMR50410 enable low level is >=0.95V.
        // Note that this threshold is less than the minimum AIN pull-up voltage of 2.5V,
        // and the UVLO shut-down threshold of 4.8V.
        if (returnValue is not (-1) and < 950)
        {
            state = IgnitionStates.Off;
        }
        // LMR50410 enable high level is <=1.36V.  Note that this threshold handles 6V VBAT,
        // and treats short to AIN or DIN as "on".  DIN maximum pull-up voltage is 1/2 Vbat.
        else if (returnValue > 1360)
        {
            state = IgnitionStates.On;
        }

        return new IgnitionState() { MilliVolts = returnValue, State = state };
    }
}