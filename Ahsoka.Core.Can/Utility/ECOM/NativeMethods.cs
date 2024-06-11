using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Ahsoka.Utility.ECOM;

[ExcludeFromCodeCoverage]
class NativeMethods
{
    [DllImport("ecommlib64.dll")]
    internal static extern UInt64 CANOpen(UInt64 serial, Byte baud, ref Byte error);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte CANTransmitMessageEx(UInt64 deviceHandle, ref EFFMessage message);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte CANTransmitMessage(UInt64 deviceHandle, ref SFFMessage message);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte CANReceiveMessageEx(UInt64 deviceHandle, ref EFFMessage message);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte CANReceiveMessage(UInt64 deviceHandle, ref SFFMessage message);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte GetErrorMessage(UInt64 deviceHandle, ref ErrorMessage message);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte CloseDevice(UInt64 deviceHandle);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte CANSetupDevice(UInt64 deviceHandle, Byte setupCommand, Byte setupProperty);

    [DllImport("ecommlib64.dll")]
    internal static extern int GetQueueSize(UInt64 deviceHandle, Byte flag);

    [DllImport("ecommlib64.dll")]
    internal static extern void GetFriendlyErrorMessage(Byte error, StringBuilder buffer, Int64 bufferSize);

    [DllImport("ecommlib64.dll")]
    internal static extern UInt32 StartDeviceSearch(Byte flag);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte CloseDeviceSearch(UInt64 searchHandle);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte FindNextDevice(UInt64 searchHandle, ref DeviceInfo deviceInfo);

    [DllImport("ecommlib64.dll")]
    internal static extern Byte GetDeviceInfo(UInt64 deviceHandle, ref DeviceInfo deviceInfo);
}
