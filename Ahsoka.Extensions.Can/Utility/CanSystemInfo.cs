using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahsoka.Services.Can;

namespace Ahsoka.Utility;

internal class CanSystemInfo
{
    static object  syncRoot = new object();

    static CanClientCalibration canClientCalibration;
    
    /// <summary>
    /// Contains Definitions for automaitcally includes CAN nodes and messages
    /// </summary>
    public static CanClientCalibration StandardCanMessages
    {
        get
        {
            lock (syncRoot)
                canClientCalibration ??= LoadCanMessages();
            return canClientCalibration;
        }
    }

    private static CanClientCalibration LoadCanMessages()
    {
        return JsonUtility.Deserialize<CanClientCalibration>(Properties.CANResources.StandardCanDefinitions);
    }

}
