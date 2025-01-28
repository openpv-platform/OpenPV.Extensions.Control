using Ahsoka.System;
using Ahsoka.Services.Can;
using Ahsoka.Services.Can.Messages;
using System.Collections.Generic;
using System;
using System.Linq;
using ValueType = Ahsoka.Services.Can.ValueType;

namespace Ahsoka.CS.CAN;


public partial class DriverHeartbeat : CanViewModelBase
{
	public const uint CanId = 100; // CANID: 0x64
	public const int Dlc = 1;
	#region Auto Generated Properties

	public CmdValues Cmd 
	{
		get { return OnGetValue<CmdValues>();  }
		set { OnSetValue(value); } 
	}

	#endregion

	#region Auto Generated Methods
	protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.EventMetadata.GetMetadata(CanId); }
	public DriverHeartbeat() : base(CanId, Dlc)
	{
	} 
	public DriverHeartbeat(CanMessageData message) : base(message) 
	{
	}

	#endregion
}

public enum CmdValues
{
	DRIVER_HEARTBEAT_cmd_NOOP=0,
	DRIVER_HEARTBEAT_cmd_SYNC=1,
	DRIVER_HEARTBEAT_cmd_REBOOT=2
}


public partial class IoDebug : CanViewModelBase
{
	public const uint CanId = 500; // CANID: 0x1f4
	public const int Dlc = 4;
	#region Auto Generated Properties

	public uint TestUnsigned 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public TestEnumValues TestEnum 
	{
		get { return OnGetValue<TestEnumValues>();  }
		set { OnSetValue(value); } 
	}

	public int TestSigned 
	{
		get { return OnGetValue<int>();  }
		set { OnSetValue(value); } 
	}

	public uint TestFloat 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	#endregion

	#region Auto Generated Methods
	protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.EventMetadata.GetMetadata(CanId); }
	public IoDebug() : base(CanId, Dlc)
	{
	} 
	public IoDebug(CanMessageData message) : base(message) 
	{
	}

	#endregion
}

public enum TestEnumValues
{
	IO_DEBUG_test2_enum_one=1,
	IO_DEBUG_test2_enum_two=2
}


public partial class MotorCmd : CanViewModelBase
{
	public const uint CanId = 101; // CANID: 0x65
	public const int Dlc = 1;
	#region Auto Generated Properties

	public int Steer 
	{
		get { return OnGetValue<int>();  }
		set { OnSetValue(value); } 
	}

	public uint Drive 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	#endregion

	#region Auto Generated Methods
	protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.EventMetadata.GetMetadata(CanId); }
	public MotorCmd() : base(CanId, Dlc)
	{
	} 
	public MotorCmd(CanMessageData message) : base(message) 
	{
	}

	#endregion
}

public partial class MotorStatus : CanViewModelBase
{
	public const uint CanId = 400; // CANID: 0x190
	public const int Dlc = 3;
	#region Auto Generated Properties

	public uint WheelError 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint SpeedKph 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	#endregion

	#region Auto Generated Methods
	protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.EventMetadata.GetMetadata(CanId); }
	public MotorStatus() : base(CanId, Dlc)
	{
	} 
	public MotorStatus(CanMessageData message) : base(message) 
	{
	}

	#endregion
}

public partial class SensorSonars : CanViewModelBase
{
	public const uint CanId = 200; // CANID: 0xc8
	public const int Dlc = 8;
	#region Auto Generated Properties

	public uint Mux 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint ErrCount 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint Left 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint NoFiltLeft 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint Middle 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint NoFiltMiddle 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint Right 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint NoFiltRight 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint Rear 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint NoFiltRear 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	#endregion

	#region Auto Generated Methods
	protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.EventMetadata.GetMetadata(CanId); }
	public SensorSonars() : base(CanId, Dlc)
	{
	} 
	public SensorSonars(CanMessageData message) : base(message) 
	{
	}

	#endregion
}

public partial class TSC1 : CanViewModelBase
{
	public const uint CanId = 0; // CANID: 0x0
	public const int Dlc = 8;
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


	public EngineOverrideControlModeValues EngineOverrideControlMode 
	{
		get { return OnGetValue<EngineOverrideControlModeValues>();  }
		set { OnSetValue(value); } 
	}

	public EngineRequestedSpeedControlConditionsValues EngineRequestedSpeedControlConditions 
	{
		get { return OnGetValue<EngineRequestedSpeedControlConditionsValues>();  }
		set { OnSetValue(value); } 
	}

	public OverrideControlModePriorityValues OverrideControlModePriority 
	{
		get { return OnGetValue<OverrideControlModePriorityValues>();  }
		set { OnSetValue(value); } 
	}

	public uint EngineRequestedSpeedSpeedLimit 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint EngineRequestedTorqueTorqueLimit 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public Tsc1TransmissionRateValues Tsc1TransmissionRate 
	{
		get { return OnGetValue<Tsc1TransmissionRateValues>();  }
		set { OnSetValue(value); } 
	}

	public Tsc1ControlPurposeValues Tsc1ControlPurpose 
	{
		get { return OnGetValue<Tsc1ControlPurposeValues>();  }
		set { OnSetValue(value); } 
	}

	public EngineRequestedTorqueHighResolutionValues EngineRequestedTorqueHighResolution 
	{
		get { return OnGetValue<EngineRequestedTorqueHighResolutionValues>();  }
		set { OnSetValue(value); } 
	}

	public uint MessageCounter 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	public uint MessageChecksum 
	{
		get { return OnGetValue<uint>();  }
		set { OnSetValue(value); } 
	}

	#endregion

	#region Auto Generated Methods
	protected override Dictionary<string, CanPropertyInfo> GetMetadata() { return CanModelMetadata.EventMetadata.GetMetadata(CanId); }
	public TSC1() : base(CanId, Dlc)
	{
	} 
	public TSC1(CanMessageData message) : base(message) 
	{
	}

	#endregion
}

public enum EngineOverrideControlModeValues
{
	Override_disabled___Disable_any_existing_control_commanded_by_the_source_of_this_command_=0,
	Speed_control___Govern_speed_to_the_included__desired_speed__value_=1,
	Torque_control___Control_torque_to_the_included__desired_torque__value_=2,
	Speed_torque_limit_control___Limit_speed_and_or_torque_based_on_the_included_limit_values__The_speed_limit_governor_is_a_droop_governor_where_the_speed_limit_value_defines_the_speed_at_the_maximum_torque_available_during_this_oper=3
}

public enum EngineRequestedSpeedControlConditionsValues
{
	Transient_Optimized_for_driveline_disengaged_and_non_lockup_conditions=0,
	Stability_Optimized_for_driveline_disengaged_and_non_lockup_conditions=1,
	Stability_Optimized_for_driveline_engaged_and_or_in_lockup_condition_1__e_g___vehicle_driveline_=2,
	Stability_Optimized_for_driveline_engaged_and_or_in_lockup_condition_2__e_g___PTO_driveline_=3
}

public enum OverrideControlModePriorityValues
{
	Highest_priority=0,
	High_priority=1,
	Medium_priority=2,
	Low_priority=3
}

public enum Tsc1TransmissionRateValues
{
	_1000_ms_transmission_rate=0,
	_750_ms_transmission_rate=1,
	_500_ms_transmission_rate=2,
	_250_ms_transmission_rate=3,
	_100_ms_transmission_rate=4,
	_50_ms_transmission_rate=5,
	_20_ms_transmission_rate=6,
	Use_standard_TSC1_transmission_rates_of_10_ms_to_engine=7
}

public enum Tsc1ControlPurposeValues
{
	P1___Accelerator_Pedal_Operator_Selection=0,
	P2___Cruise_Control=1,
	P3___PTO_Governor=2,
	P4___Road_Speed_Governor=3,
	P5___Engine_Protection=4,
	P32___Temporary_Power_Train_Control__Original_use_of_TSC1_Command_=31
}

public enum EngineRequestedTorqueHighResolutionValues
{
	_0_000_=0,
	_0_125_=1,
	_0_875_=7
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
			case DriverHeartbeat.CanId:
				return new DriverHeartbeat(data);
			case IoDebug.CanId:
				return new IoDebug(data);
			case MotorCmd.CanId:
				return new MotorCmd(data);
			case MotorStatus.CanId:
				return new MotorStatus(data);
			case SensorSonars.CanId:
				return new SensorSonars(data);
			case TSC1.CanId:
				return new TSC1(data);

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

		// Decode Info for DriverHeartbeat 100
		metadata.Add(100, new Dictionary<string, CanPropertyInfo>()
		{
			{nameof(DriverHeartbeat.Cmd), new( 0, 8, ByteOrder.LittleEndian, ValueType.Enum, 1, 0, 0, 0, 0, 0, 1243343334)},
		});

		// Decode Info for IoDebug 500
		metadata.Add(500, new Dictionary<string, CanPropertyInfo>()
		{
			{nameof(IoDebug.TestUnsigned), new( 0, 8, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 0, 0, 0, 0, 2538080523)},
			{nameof(IoDebug.TestEnum), new( 8, 8, ByteOrder.LittleEndian, ValueType.Enum, 1, 0, 0, 0, 0, 0, 1922947961)},
			{nameof(IoDebug.TestSigned), new( 16, 8, ByteOrder.LittleEndian, ValueType.Signed, 1, 0, 0, 0, 0, 0, 4165418399)},
			{nameof(IoDebug.TestFloat), new( 24, 8, ByteOrder.LittleEndian, ValueType.Unsigned, 0.5, 0, 0, 0, 0, 0, 4292783587)},
		});

		// Decode Info for MotorCmd 101
		metadata.Add(101, new Dictionary<string, CanPropertyInfo>()
		{
			{nameof(MotorCmd.Steer), new( 0, 4, ByteOrder.LittleEndian, ValueType.Signed, 1, -5, 0, 0, -5, 5, 4239993765)},
			{nameof(MotorCmd.Drive), new( 4, 4, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 0, 0, 0, 9, 442808169)},
		});

		// Decode Info for MotorStatus 400
		metadata.Add(400, new Dictionary<string, CanPropertyInfo>()
		{
			{nameof(MotorStatus.WheelError), new( 0, 1, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 0, 0, 0, 0, 2480137974)},
			{nameof(MotorStatus.SpeedKph), new( 8, 16, ByteOrder.LittleEndian, ValueType.Unsigned, 0.001, 0, 0, 0, 0, 0, 1066340515)},
		});

		// Decode Info for SensorSonars 200
		metadata.Add(200, new Dictionary<string, CanPropertyInfo>()
		{
			{nameof(SensorSonars.Mux), new( 0, 4, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 0, 0, 0, 0, 4195371219)},
			{nameof(SensorSonars.ErrCount), new( 4, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 0, 0, 0, 0, 1612209030)},
			{nameof(SensorSonars.Left), new( 16, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 0.1, 0, 0, 0, 0, 0, 131997842)},
			{nameof(SensorSonars.NoFiltLeft), new( 16, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 0.1, 0, 0, 0, 0, 0, 202713697)},
			{nameof(SensorSonars.Middle), new( 28, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 0.1, 0, 0, 0, 0, 0, 3387452885)},
			{nameof(SensorSonars.NoFiltMiddle), new( 28, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 0.1, 0, 0, 0, 0, 0, 1883030579)},
			{nameof(SensorSonars.Right), new( 40, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 0.1, 0, 0, 0, 0, 0, 4191157421)},
			{nameof(SensorSonars.NoFiltRight), new( 40, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 0.1, 0, 0, 0, 0, 0, 187021029)},
			{nameof(SensorSonars.Rear), new( 52, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 0.1, 0, 0, 0, 0, 0, 2845092881)},
			{nameof(SensorSonars.NoFiltRear), new( 52, 12, ByteOrder.LittleEndian, ValueType.Unsigned, 0.1, 0, 0, 0, 0, 0, 1365644182)},
		});

		// Decode Info for TSC1 0
		metadata.Add(0, new Dictionary<string, CanPropertyInfo>()
		{
			{nameof(TSC1.EngineOverrideControlMode), new( 0, 2, ByteOrder.LittleEndian, ValueType.Enum, 1, 0, 695, 0, 0, 3, 975961898)},
			{nameof(TSC1.EngineRequestedSpeedControlConditions), new( 2, 2, ByteOrder.LittleEndian, ValueType.Enum, 1, 0, 696, 0, 0, 3, 2846823130)},
			{nameof(TSC1.OverrideControlModePriority), new( 4, 2, ByteOrder.LittleEndian, ValueType.Enum, 1, 0, 897, 0, 0, 3, 648919367)},
			{nameof(TSC1.EngineRequestedSpeedSpeedLimit), new( 8, 16, ByteOrder.LittleEndian, ValueType.Unsigned, 0.125, 0, 898, 0, 0, 8031.88, 4226213315)},
			{nameof(TSC1.EngineRequestedTorqueTorqueLimit), new( 24, 8, ByteOrder.LittleEndian, ValueType.Unsigned, 1, -125, 518, 0, -125, 125, 2378174033)},
			{nameof(TSC1.Tsc1TransmissionRate), new( 32, 3, ByteOrder.LittleEndian, ValueType.Enum, 1, 0, 3349, 0, 0, 7, 3962202020)},
			{nameof(TSC1.Tsc1ControlPurpose), new( 35, 5, ByteOrder.LittleEndian, ValueType.Enum, 1, 0, 3350, 0, 0, 31, 3870894311)},
			{nameof(TSC1.EngineRequestedTorqueHighResolution), new( 40, 4, ByteOrder.LittleEndian, ValueType.Enum, 0.125, 0, 4191, 0, 0, 0.875, 400440828)},
			{nameof(TSC1.MessageCounter), new( 56, 4, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 4206, 0, 0, 15, 2738406898)},
			{nameof(TSC1.MessageChecksum), new( 60, 4, ByteOrder.LittleEndian, ValueType.Unsigned, 1, 0, 4207, 0, 0, 15, 1671931877)},
		});

    }
}
