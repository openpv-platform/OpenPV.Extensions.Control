using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Runtime.InteropServices;

namespace Ahsoka.Services.Can;

/// <summary>
/// Info for a Single Property
/// </summary>
public class CanPropertyInfo
{
    readonly double scale;
    readonly double offset;
    readonly double defaultValue;
    double minValue;
    double maxValue;
    readonly ByteOrder byteOrder;
    readonly ValueType dataType;

    /// <summary>
    ///  Signal ID for Property
    /// </summary>
    public int SignalId { get; init; }

    /// <summary>
    ///  Start Bit for Property
    /// </summary>
    public int StartBit { get; init; }

    /// <summary>
    ///  Type for Property
    /// </summary>
    public ValueType ValueType { get; init; }

    /// <summary>
    ///  Bit Length for Property
    /// </summary>
    public int BitLength { get; init; }

    /// <summary>
    /// Name of the Property
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Unique ID Used for Tracking Properties
    /// </summary>
    internal uint UniqueId { get; init; }

    /// <summary>
    /// Constructor for Properties
    /// </summary>
    /// <param name="scale"></param>
    /// <param name="offset"></param>
    /// <param name="startBit"></param>
    /// <param name="bitLength"></param>
    /// <param name="byteOrder"></param>
    /// <param name="dataType"></param>
    /// <param name="signalId"></param>
    /// <param name="defaultValue"></param>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    public CanPropertyInfo(int startBit, int bitLength, ByteOrder byteOrder, ValueType dataType, double scale, double offset, int signalId = -1, double defaultValue = 0, double minValue = double.MinValue, double maxValue = double.MaxValue , uint uniqueId = 0)
    {
        this.SignalId = signalId;
        this.StartBit = startBit;
        this.BitLength = bitLength;
        this.UniqueId = uniqueId;
        this.scale = scale;
        this.offset = offset;
        this.defaultValue = defaultValue;
        this.byteOrder = byteOrder;
        this.dataType = dataType;

        SetBounds(dataType, minValue, maxValue);
    }

    #region Utility Methods
    /// <summary>
    /// Get Value Converted from Bytes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="scaleValue"></param>
    /// <returns></returns>
    public T GetValue<T>(byte[] data, bool scaleValue = true) where T : struct
    {
        var longData = new ulong[data.Length / 8];

        // Convert Data to Uint64s
        for (int i = 0; i < longData.Length; i++)
            longData[i] = BitConverter.ToUInt64(data, i * 8);

        return this.GetValue<T>(longData, scaleValue);
    }

    /// <summary>
    /// Get Value Converted from Bytes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="scaleValue"></param>
    /// <returns></returns>
    internal T GetValue<T>(ulong[] data, bool scaleValue = true, bool getRaw = false) where T : struct
    {
        int startByte = StartBit / 8;
        int messageIndex = startByte / 8;

        if (typeof(T) == typeof(int))
        {
            return (T)(object)Convert.ToInt32(Unpack(data[messageIndex], scaleValue, getRaw));
        }
        else if (typeof(T) == typeof(uint))
        {
            return (T)(object)Convert.ToUInt32(Unpack(data[messageIndex], scaleValue, getRaw));
        }
        else if (typeof(T) == typeof(float))
        {
            return (T)(object)Convert.ToSingle(Unpack(data[messageIndex], scaleValue, getRaw));
        }
        else if (typeof(T) == typeof(double))
        {
            return (T)(object)(Unpack(data[messageIndex], scaleValue, getRaw));
        }
        else if (typeof(T).IsAssignableTo(typeof(Enum)))
        {
            return Convert.ToInt32(Unpack(data[messageIndex], scaleValue, getRaw)).IntToEnum<T>();
        }

        return default;
    }


    /// <summary>
    /// Sets data to the Data Set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="newValue"></param>
    /// <param name="scaleValue"></param>
    public void SetValue<T>(byte[] data, T newValue, bool scaleValue = true) where T : struct
    {
        var longData = new ulong[data.Length / 8];

        // Convert Data to Uint64s
        for (int i = 0; i < longData.Length; i++)
            longData[i] = BitConverter.ToUInt64(data, i * 8);

        SetValue(ref longData, newValue, scaleValue);

        // Return Data to Array
        for (int i = 0; i < longData.Length; i++)
            Array.Copy(BitConverter.GetBytes(longData[i]), 0, data, i * 8, 8);
    }


    /// <summary>
    /// Sets data to the Data Set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="newValue"></param>
    /// <param name="scaleValue"></param>
    internal void SetValue<T>(ref ulong[] data, T newValue, bool scaleValue = true) where T : struct
    {
        int startByte = StartBit / 8;
        int messageIndex = startByte / 8;

        // Clear Bits
        data[messageIndex] &= ~(BitMask() << StartBit);

        switch (newValue)
        {
            case uint u4:
                data[messageIndex] |= Pack(u4, scaleValue);
                break;

            case int s4:
                data[messageIndex] |= Pack(s4, scaleValue);
                break;

            case float f4:
                data[messageIndex] |= Pack(f4, scaleValue);
                break;

            case double f8:
                data[messageIndex] |= Pack(f8, scaleValue);
                break;

            case Enum e4:
                data[messageIndex] |= Pack(e4.EnumToInt(), scaleValue);
                break;
        }
    }

    private ulong Pack(double value, bool scaleValue = true)
    {
        long iVal;
        ulong bitMask = BitMask();

        // Ensure value lies within bounds
        var rawValue = Math.Max(minValue, Math.Min(maxValue, value));

        // Apply scaling
        rawValue = scaleValue ? (rawValue - offset) / scale : rawValue;

        // Convert to Byte[8]
        if (dataType == ValueType.Float)
            iVal = FloatConverter.AsInteger((float)rawValue);
        else if (dataType == ValueType.Double)
            iVal = DoubleConverter.AsInteger(rawValue);
        else
            iVal = (long)Math.Round(rawValue);

        // Pack signal
        if (byteOrder == ByteOrder.LittleEndian) // Little endian 
            return (((ulong)iVal & bitMask) << StartBit);
        else // Big endian
            return MirrorMsg(((ulong)iVal & bitMask) << GetStartBitLE());
    }

    private double Unpack(ulong data, bool scaleValue = true, bool getRaw = false)
    {
        double returnValue = 0;
        long iVal;
        ulong bitMask = BitMask();

        // Unpack signal
        if (byteOrder == ByteOrder.LittleEndian) // Little endian 
            iVal = (long)((data >> StartBit) & bitMask);
        else // Big endian 
            iVal = (long)((MirrorMsg(data) >> GetStartBitLE()) & bitMask);

        if (dataType == ValueType.Float)
            returnValue = FloatConverter.AsFloatingPoint((int)iVal);
        else if (dataType == ValueType.Double)
            returnValue = DoubleConverter.AsFloatingPoint(iVal);
        else
            returnValue = iVal;

        // All FF's 
        if ((ulong)iVal == bitMask && !getRaw)
            return defaultValue;

        // Apply scaling
        if (scaleValue)
            returnValue = returnValue * scale + offset;

        // Ensure value lies within bounds
        returnValue = Math.Max(minValue, Math.Min(maxValue, returnValue));
        
        return returnValue;
    }

    private ulong BitMask()
    {
        return (ulong.MaxValue >> (64 - BitLength));
    }

    private static ulong MirrorMsg(ulong msg)
    {
        ulong swapped = ((0x00000000000000FF) & (msg >> 56)
                         | (0x000000000000FF00) & (msg >> 40)
                         | (0x0000000000FF0000) & (msg >> 24)
                         | (0x00000000FF000000) & (msg >> 8)
                         | (0x000000FF00000000) & (msg << 8)
                         | (0x0000FF0000000000) & (msg << 24)
                         | (0x00FF000000000000) & (msg << 40)
                         | (0xFF00000000000000) & (msg << 56));

        return swapped;
    }

    private Byte GetStartBitLE()
    {
        Byte startByte = (Byte)(StartBit / 8);
        return (Byte)(64 - (BitLength + 8 * startByte + (8 * (startByte + 1) - (StartBit + 1)) % 8));
    }

    private void SetBounds(ValueType dataType, double min, double max)
    {        
        if (min == 0 && max == 0)
        {
            min = double.MinValue; 
            max = double.MaxValue;
        }

        if (dataType == ValueType.Float)
        {
            minValue = Math.Max(min, float.MinValue);
            maxValue = Math.Min(max, float.MaxValue);
        }
        else if (dataType == ValueType.Double)
        {
            minValue = Math.Max(min, double.MinValue);
            maxValue = Math.Min(max, double.MaxValue);
        }
        else if (dataType == ValueType.Unsigned || dataType == ValueType.Enum)
        {
            minValue = Math.Max(min, uint.MinValue);
            maxValue = Math.Min(max, uint.MaxValue);
        }
        else if (dataType == ValueType.Signed)
        {
            minValue = Math.Max(min, int.MinValue);
            maxValue = Math.Min(max, int.MaxValue);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal class FloatConverter
    {
        [FieldOffset(0)] public int Integer;
        [FieldOffset(0)] public float Float;

        public static int AsInteger(float value)
        {
            return new FloatConverter() { Float = value }.Integer;
        }

        public static float AsFloatingPoint(int value)
        {
            return new FloatConverter() { Integer = value }.Float;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal class DoubleConverter
    {
        [FieldOffset(0)] public long Integer;
        [FieldOffset(0)] public double Float;

        public static long AsInteger(double value)
        {
            return new DoubleConverter() { Float = value }.Integer;
        }

        public static double AsFloatingPoint(long value)
        {
            return new DoubleConverter() { Integer = value }.Float;
        }
    }
    #endregion
}


