using Ahsoka.System;
using Ahsoka.Services.Can;
using Ahsoka.Services.Can.Messages;
using System.Collections.Generic;
using System;
using System.Linq;
using ValueType = Ahsoka.Services.Can.ValueType;

namespace Ahsoka.CS.CAN;


public partial class Service : CanViewModelBase
{
	public const uint CanId = 2818048; // CANID: 0x2b0000
	public const int Dlc = 0;
	#region Auto Generated Properties
	J1939Helper protocol;
	public J1939Helper Protocol
	{
		get
		{
			if (protocol == null)
				protocol = new J1939Helper(message);
			return protocol;
		}
	}


	#endregion

	#region Auto Generated Methods
	protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.EventMetadata.GetMetadata(CanId); }
	public Service() : base(CanId, Dlc)
	{
	} 
	public Service(CanMessageData message) : base(message) 
	{
	}

	#endregion
}

public class CanModelMetadata : ICanMetadata
{
	Dictionary<uint, Dictionary<string, CanPropertyInfo>> metadata = new();

    static CanModelMetadata eventMetadata = null;

    public Dictionary<uint, Dictionary<string, CanPropertyInfo>> GetMetadata() { return metadata; }

    public Dictionary<string, CanPropertyInfo> GetMetadata(uint canID) { return metadata[canID]; }

	public void AddMetadata(uint canID, Dictionary<string, CanPropertyInfo> properties)
	{
		metadata.Add(canID, properties);
	}

	public CanPropertyInfo GetPropertyInfo(uint canID, uint signalID)
	{
		return metadata[canID].Values.FirstOrDefault(x => x.SignalId == signalID);
	}

	public CanPropertyInfo GetPropertyInfo(uint canID, string signalName)
	{
		return metadata[canID][signalName];
	}

    public CanViewModelBase CreateObject(CanMessageData data)
    {
        switch (data.Id)
        {
			case Service.CanId:
				return new Service(data);

            default:
                return null;
        }
    }

    public static CanModelMetadata EventMetadata
    {
        get
        {
            if (eventMetadata == null)
                eventMetadata = new();
            return eventMetadata;
        }
    }

    CanModelMetadata()
    {

		// Decode Info for Service 2818048
		metadata.Add(2818048, new Dictionary<string, CanPropertyInfo>()
		{
		});

    }
}
