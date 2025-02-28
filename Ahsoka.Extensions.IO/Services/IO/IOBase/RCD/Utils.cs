using Ahsoka.Core;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

/*
    Utility Functions / Defines that are common and reused by the IO Pins
*/

namespace Ahsoka.Services.IO.RCD;

internal enum ADCInput
{
    REV_TAG = 0,
    SUPPLY_5V = 1,
    VBAT = 2,
    IGN_PIN = 3,
    AIN1 = 4,
    AIN2 = 5,
    DIN1 = 6,
    DIN2 = 7,
}

[ExcludeFromCodeCoverage]
internal static class MuxSelectUtils
{
    static GpioController muxControllerA;
    static GpioController muxControllerB;
    static GpioController muxControllerC;

    static MuxSelectUtils()
    {
        muxControllerA = new(PinNumberingScheme.Logical, new LibGpiodDriver(MuxChipA));
        muxControllerB = new(PinNumberingScheme.Logical, new LibGpiodDriver(MuxChipB));
        muxControllerC = new(PinNumberingScheme.Logical, new LibGpiodDriver(MuxChipC));
    }

    public const int MuxChipA = 4;
    public const int MuxChipB = 5;
    public const int MuxChipC = 4;

    public const int MuxPinA = 7;
    public const int MuxPinB = 8;
    public const int MuxPinC = 10;

    // Pre defined dictionary containing the bitmask for each mux select
    public static readonly Dictionary<ADCInput, PinValue[]> BitMask = new()
    {
        {ADCInput.REV_TAG,   new PinValue[] {PinValue.Low, PinValue.Low, PinValue.Low} },    // A=0  B=0  C=0
        {ADCInput.SUPPLY_5V, new PinValue[] {PinValue.High, PinValue.Low, PinValue.Low} },   // A=1  B=0  C=0
        {ADCInput.VBAT,      new PinValue[] {PinValue.Low, PinValue.High, PinValue.Low} },   // A=0  B=1  C=0
        {ADCInput.IGN_PIN,   new PinValue[] {PinValue.High, PinValue.High, PinValue.Low} },  // A=1  B=1  C=0
        {ADCInput.AIN2,      new PinValue[] {PinValue.High, PinValue.Low, PinValue.High} },  // A=1  B=0  C=1
        {ADCInput.AIN1,      new PinValue[] {PinValue.Low, PinValue.Low, PinValue.High} },   // A=0  B=0  C=1
        {ADCInput.DIN1,      new PinValue[] {PinValue.Low, PinValue.High, PinValue.High} },  // A=0  B=1  C=1
        {ADCInput.DIN2,      new PinValue[] {PinValue.High, PinValue.High, PinValue.High} }, // A=1  B=1  C=1
    };

    private static bool SetMux(GpioController muxController, int MuxPin, PinValue pv, bool throwOnError)
    {
        try
        {
            muxController.OpenPin(MuxPin, PinMode.Output);
            muxController.Write(MuxPin, pv);
            muxController.ClosePin(MuxPin);
            return true;
        }
        catch (IOException ex)
        {
            if (throwOnError)
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Retry Failed Reading IO Value {ex.ToString()}");
                throw;
            }
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Error Reading IO Value - Attempting Retry");
            return false;
        };
    }

    public static void SetMuxSelect(ADCInput index)
    {
        // Set Mux Select A
        bool result = SetMux(muxControllerA, MuxPinA, BitMask[index][0], false);
        if (!result)
            result = SetMux(muxControllerA, MuxPinA, BitMask[index][0], true); // Retry

        // Set Mux Select B
        result = SetMux(muxControllerB, MuxPinB, BitMask[index][1], false);
        if (!result)
            result = SetMux(muxControllerB, MuxPinB, BitMask[index][1], true); // Retry

        // Set Mux Select C
        result = SetMux(muxControllerC, MuxPinC, BitMask[index][2], false);
        if (!result)
            result = SetMux(muxControllerC, MuxPinC, BitMask[index][2], true); // Retry
    }
}

[ExcludeFromCodeCoverage]
static class ADCUtils
{
    /*
        Utility class to facilitate reading from the RCD's Analog-to-Digital Converter.
        
        The GetVoltsRawValue() function will parse the float value stored in the ADC's
        Input Voltage file and apply a ST provided formula for calculating the volts at the
        Micro processor pin. That value is returned and it is up to the calling function
        to then apply any circuit specific (ex. voltage dividers) calculations to get
        the resulting voltage reading at the physical pin.
    */

    static readonly string _voltsScalePath = @"/sys/devices/platform/soc/48003000.adc/48003000.adc:adc@0/iio:device0/in_voltage_scale";
    static readonly string _voltsOffsetPath = @"/sys/devices/platform/soc/48003000.adc/48003000.adc:adc@0/iio:device0/in_voltage_offset";
    static readonly string _voltsRawPath = @"/sys/devices/platform/soc/48003000.adc/48003000.adc:adc@0/iio:device0/in_voltage5_raw";

    static readonly object _syncRoot = new();

    static float _voltsScaleValue = 0;
    static float _voltsOffsetValue = -1;

    /// <summary>
    /// Retrieves an ADC sample for the specified ADC mux channel using the low-level IO service.
    /// </summary>
    /// <param name="channel">ADC channel number [0..7]</param>
    /// <returns>millivolts</returns>
    /// <exception cref="Exception"></exception>
    public static float GetVoltsRawValue(ADCInput channel)
    {
        float VoltsRawValue = 0;


        if (_voltsScaleValue == 0 && File.Exists(_voltsScalePath))
            _voltsScaleValue = float.Parse(File.ReadAllText(_voltsScalePath));

        if (_voltsOffsetValue == -1 && File.Exists(_voltsOffsetPath))
            _voltsOffsetValue = float.Parse(File.ReadAllText(_voltsOffsetPath));

        lock (_syncRoot)
        {
            MuxSelectUtils.SetMuxSelect(channel);

            // If Ignition or VBAT
            if (channel is ADCInput.IGN_PIN or ADCInput.VBAT)
            {
                // Maximum mux switching time (74HCT4851): 17ns
                // ADC sampling time: 10us (configured in kernel device tree)
                // AIN_MUX has a 330pF cap
                // Maximum input resistance: 56k for VBAT and IGN_PIN
                // The switching and sampling times are negligible.  Wait 3x longest possible tau
                // to charge or discharge the capacitor at the mux output.
                Thread.Sleep((int)Math.Ceiling(56000 * 0.000000330 * 3 * 1000));  // Milliseconds
            }
            else
            {
                Thread.Sleep(1); // .55ms
            }

            // Get Volts raw
            if (File.Exists(_voltsRawPath))
                VoltsRawValue = float.Parse(File.ReadAllText(_voltsRawPath));
        }

        // Calculate Volts at MicroProcessor
        float MpVolts = (VoltsRawValue + _voltsOffsetValue) * _voltsScaleValue;

        return MpVolts;
    }
}
