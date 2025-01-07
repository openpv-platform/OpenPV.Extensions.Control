using System;
using System.Runtime.CompilerServices;

namespace Ahsoka.Services.Can.Messages;
internal class J1939PropertyDefinitions
{
    public const UInt32 BroadcastAddress = 255;
    public const UInt32 NullAddress = 254;

    public static void ParseAddresses(string addresses, out uint min, out uint max)
    {
        var addressValues = addresses.Split(",");
        min = uint.Parse(addressValues[0]);
        max = uint.Parse(addressValues[1]);
        if (min > max)
            max = min;
    }

    public class Name
    {
        private CanPropertyInfo identityInfo = new(0, 21, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo manufacturerInfo = new(21, 11, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo ECUInfo = new(32, 3, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo functionInstanceInfo = new(35, 5, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo functionInfo = new(40, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo vehicleInfo = new(49, 7, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo vehicleInstanceInfo = new(56, 4, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo industryInfo = new(60, 3, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);

        public J1939NodeDefinition Definition { get; set; } = new();

        public Name() { }

        public Name(J1939NodeDefinition definition)
        {
            this.Definition = definition;
        }

        public Name(ulong name)
        {
            ExtractValues(name);
        }

        public void ExtractValues(ulong name)
        {
            var input = new ulong[] { name };

            Definition.ManufacturerCode = manufacturerInfo.GetValue<uint>(input, false);
            Definition.ECUinstance = ECUInfo.GetValue<uint>(input, false);
            Definition.FunctionInstance = functionInstanceInfo.GetValue<uint>(input, false);
            Definition.Function = functionInfo.GetValue<uint>(input, false);
            Definition.VehicleSystem = vehicleInfo.GetValue<uint>(input, false);
            Definition.VehicleSystemInstance = vehicleInstanceInfo.GetValue<uint>(input, false);
            Definition.IndustryGroup = industryInfo.GetValue<uint>(input, false);

            Definition.Name = name;
        }

        public ulong WriteToUlong(UInt32 identityNumber)
        {
            var name = new ulong[] { 0 };

            identityInfo.SetValue(ref name, identityNumber, false);
            manufacturerInfo.SetValue(ref name, Definition.ManufacturerCode, false);
            ECUInfo.SetValue(ref name, Definition.ECUinstance, false);
            functionInstanceInfo.SetValue(ref name, Definition.FunctionInstance, false);
            functionInfo.SetValue(ref name, Definition.Function, false);
            vehicleInfo.SetValue(ref name, Definition.VehicleSystem, false);
            vehicleInstanceInfo.SetValue(ref name, Definition.VehicleSystemInstance, false);
            industryInfo.SetValue(ref name, Definition.IndustryGroup, false);

            return name[0];
        }
    }

    public class Id
    {
        ulong[] id = { 0 };
        CanPropertyInfo pgnInfo = new(8, 18, ByteOrder.LittleEndian, ValueType.Unsigned, 0, 0, 1, 0x3FFFF);
        CanPropertyInfo sourceInfo = new(0, 8, ByteOrder.LittleEndian, ValueType.Unsigned, 0, 0, 1, 0xFF);
        CanPropertyInfo specificInfo = new(8, 8, ByteOrder.LittleEndian, ValueType.Unsigned, 0, 0, 2, 0xFF);
        CanPropertyInfo formatInfo = new(16, 8, ByteOrder.LittleEndian, ValueType.Unsigned, 0, 0, 3, 0xFF);
        CanPropertyInfo pageInfo = new(24, 1, ByteOrder.LittleEndian, ValueType.Unsigned, 0, 0, 4, 0x01);
        CanPropertyInfo priorityInfo = new(26, 3, ByteOrder.LittleEndian, ValueType.Unsigned, 0, 0, 5, 0x07);

        public uint SourceAddress { get { return sourceInfo.GetValue<uint>(id, false); } set { sourceInfo.SetValue(ref id, value, false); } }
        public uint PDUS { get { return specificInfo.GetValue<uint>(id, false); } set { specificInfo.SetValue(ref id, value, false); } }
        public uint PDUF { get { return formatInfo.GetValue<uint>(id, false); } set { formatInfo.SetValue(ref id, value, false); } }
        public uint DataPage { get { return pageInfo.GetValue<uint>(id, false); } set { pageInfo.SetValue(ref id, value, false); } }
        public uint Priority { get { return priorityInfo.GetValue<uint>(id, false); } set { priorityInfo.SetValue(ref id, value, false); } }
        public uint PGN { get { return pgnInfo.GetValue<uint>(id, false); } set { pgnInfo.SetValue(ref id, value, false); } }

        public Id() { }

        public Id(uint id)
        {
            ExtractValues(id);
        }

        public uint WriteToUint()
        {
            return (uint)id[0];
        }

        public void ExtractValues(uint newId)
        {
            id[0] =  newId;
        }
    }

    public class TPCM
    {
        ulong[] data = { 0 };
        //RTS
        private CanPropertyInfo controlInfo = new(0, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo messageInfo = new(8, 16, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo packetInfo = new(24, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo responseInfo = new(32, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0, -1, 0xFF);
        private CanPropertyInfo pgnInfo = new(40, 24, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);

        //CTS
        private CanPropertyInfo sendInfo = new(8, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo sequenceInfo = new(16, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo reservedCTS = new(24, 16, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);

        //BAM
        private CanPropertyInfo reservedBAM = new(32, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);

        //Abort
        private CanPropertyInfo reasonInfo = new(8, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo roleInfo = new(16, 2, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo reservedAbort = new(18, 22, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);

        public CMControl ControlByte { get { return controlInfo.GetValue<CMControl>(data, false); } set { controlInfo.SetValue(ref data, value, false); } }
        public uint MessageSize { get { return messageInfo.GetValue<uint>(data, false); } set { messageInfo.SetValue(ref data, value, false); } }
        public uint NumPackets { get { return packetInfo.GetValue<uint>(data, false); } set { packetInfo.SetValue(ref data, value, false); } }
        public uint PacketsPerCTS { get { return responseInfo.GetValue<uint>(data, false); } set { responseInfo.SetValue(ref data, value, false); } }
        public uint PGN { get { return pgnInfo.GetValue<uint>(data, false); } set { pgnInfo.SetValue(ref data, value, false); } }

        public uint PacketsRequested { get { return sendInfo.GetValue<uint>(data, false); } set { sendInfo.SetValue(ref data, value, false); } }
        public uint PacketStart { get { return sequenceInfo.GetValue<uint>(data, false); } set { sequenceInfo.SetValue(ref data, value, false); } }

        public uint AbortReason { get { return reasonInfo.GetValue<uint>(data, false); } set { reasonInfo.SetValue(ref data, value, false); } }
        public uint AbortRole { get { return roleInfo.GetValue<uint>(data, false); } set { roleInfo.SetValue(ref data, value, false); } }

        public TPCM() { }

        public TPCM(ulong data)
        {
            ExtractValues(data);
        }

        public ulong WriteToUint()
        {
            if (ControlByte == CMControl.CTS)
            {
                reservedCTS.SetValue(ref data, 0xFFFF, false);
            }
            else if (ControlByte == CMControl.BAM)
            {
                reservedBAM.SetValue(ref data, 0xFF, false);
            }
            else if (ControlByte == CMControl.Abort)
            {
                reservedAbort.SetValue(ref data, 0xFFFFFF, false);
            }

            return data[0];
        }

        public void ExtractValues(ulong newData)
        {
            data[0] = newData;
        }


    }

    public enum CMControl
    {
        RTS = 16,
        CTS = 17,
        EndOfMsgACK = 19,
        Abort = 255,
        BAM = 32
    }
}
