using Ahsoka.Core.Utility;
using Ahsoka.Services.Can;

namespace Ahsoka.Utility;

internal class CanSystemInfo
{
    static object syncRoot = new object();

    static CanClientConfiguration canClientCalibration;

    /// <summary>
    /// Contains Definitions for automaitcally includes CAN nodes and messages
    /// </summary>
    public static CanClientConfiguration StandardCanMessages
    {
        get
        {
            lock (syncRoot)
                canClientCalibration ??= LoadCanMessages();
            return canClientCalibration;
        }
    }

    private static CanClientConfiguration LoadCanMessages()
    {
        return JsonUtility.Deserialize<CanClientConfiguration>(Properties.CANResources.StandardCanDefinitions);
    }

}
