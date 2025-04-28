using Ahsoka.Core.Utility;
using Ahsoka.Services.Can;
using System;
using System.ComponentModel.DataAnnotations;

namespace Ahsoka.DeveloperTools;

internal class SignalViewModel : ChildViewModelBase<MessageViewModel>
{
    /// <summary>
    /// Description of Signal
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{Signal.Name} Bit:({Signal.StartBit} Len:{Signal.BitLength})";
    }


    // Allows data validation for proto objects
    public uint BitLength
    {
        get { return Signal.BitLength; }
        set
        {
            double originalMin = GetMinValue(this.ValueType, this.BitLength);
            double originalMax = GetMaxValue(this.ValueType, this.BitLength);

            double newMin = GetMinValue(this.ValueType, value);
            double newMax = GetMaxValue(this.ValueType, value);

            Signal.BitLength = value;

            if (this.MaximumValid == originalMax || this.MaximumValid > newMax)
                this.MaximumValid = newMax;
            if (this.MinimumValid == originalMin || this.MinimumValid < newMin)
                this.MinimumValid = newMin;


            OnPropertyChanged();
        }
    }


    // Allows data validation for proto objects
    public Services.Can.ValueType ValueType
    {
        get { return Signal.ValueType; }
        set
        {
            double originalMin = GetMinValue(this.ValueType, this.BitLength);
            double originalMax = GetMaxValue(this.ValueType, this.BitLength);

            double newMin = GetMinValue(value, this.BitLength);
            double newMax = GetMaxValue(value, this.BitLength);

            Signal.ValueType = value;

            if (this.MaximumValid == originalMax || this.MaximumValid > newMax)
                this.MaximumValid = newMax;
            if (this.MinimumValid == originalMin || this.MinimumValid < newMin)
                this.MinimumValid = newMin;

            OnPropertyChanged();
        }
    }

    // Allows data validation for proto objects
    [Range(byte.MinValue, byte.MaxValue)]
    public uint MuxGroupValid
    {
        get { return Signal.MuxGroup; }
        set { Signal.MuxGroup = value; OnPropertyChanged(); }
    }

    [Range(double.MinValue, double.MaxValue)]
    public double MinimumValid
    {
        get { return Signal.Minimum; }
        set
        {
            double newMin = GetMinValue(this.ValueType, this.BitLength);
            if (value < newMin)
                throw new ValidationException($"Minimum value is {newMin}");

            Signal.Minimum = value;

            OnPropertyChanged();
        }
    }

    [Range(double.MinValue, double.MaxValue)]
    public double MaximumValid
    {
        get { return Signal.Maximum; }
        set
        {
            var oldDefault = DefaultValid == MaximumValid;

            double newMax = GetMaxValue(this.ValueType, this.BitLength);
            if (value > newMax)
                throw new ValidationException($"Max value is {newMax}");

            Signal.Maximum = value;

            OnPropertyChanged();

            if (oldDefault)
                DefaultValid = Signal.Maximum;
        }
    }

    [Range(0.0, double.MaxValue)]
    public double DefaultValid
    {
        get { return Signal.DefaultValue; }
        set
        {
            double newMax = GetMaxValue(this.ValueType, this.BitLength);
            double newMin = GetMinValue(this.ValueType, this.BitLength);

            double newValue = Math.Max(value, Math.Min(value, newMax));
            if (value < newMin || value > newMax)
                throw new ValidationException($"Default must be between {newMin} and {newMax}");

            Signal.DefaultValue = value;

            OnPropertyChanged();
        }
    }

    public MessageSignalDefinition Signal { get; init; }

    public SignalViewModel(MessageViewModel viewModelParent, MessageSignalDefinition signal)
        : base(viewModelParent)
    {
        if (signal == null)
            return;

        this.Signal = signal;
    }

    private double GetMaxValue(Services.Can.ValueType valueType, uint length)
    {
        switch (valueType)
        {
            case Services.Can.ValueType.Signed:
                return Math.Pow(2, length - 1) - 1;
            case Services.Can.ValueType.Unsigned:
                return Math.Pow(2, length) - 1;
            case Services.Can.ValueType.Float:
                return float.MaxValue;
            case Services.Can.ValueType.Double:
                return double.MaxValue;
            case Services.Can.ValueType.Enum:
                return Math.Pow(2, length - 1) - 1;
            default:
                break;
        }

        return 0;
    }

    private double GetMinValue(Services.Can.ValueType valueType, uint length)
    {
        switch (valueType)
        {
            case Services.Can.ValueType.Signed:
                return -(Math.Pow(2, length - 1));
            case Services.Can.ValueType.Unsigned:
                return 0;
            case Services.Can.ValueType.Float:
                return float.MinValue;
            case Services.Can.ValueType.Double:
                return double.MinValue;
            case Services.Can.ValueType.Enum:
                return -(Math.Pow(2, length - 1));
            default:
                break;
        }
        return 0;
    }


}

public class ValidationException : Exception
{
    public ValidationException(string error) : base(error) { } //constructor

    public override string StackTrace
    {
        get { return ""; }
    }
}