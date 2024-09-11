using System;
using System.Runtime.CompilerServices;

namespace Ahsoka.Services.Can;
internal class J1939Helper
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
        private CanPropertyInfo pgnInfo = new(8, 18, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0, 1, 0x3FFFF);
        private CanPropertyInfo sourceInfo = new(0, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0, 1, 0xFF);
        private CanPropertyInfo specificInfo = new(8, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0, 2, 0xFF);
        private CanPropertyInfo formatInfo = new(16, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0, 3, 0xFF);
        private CanPropertyInfo pageInfo = new(24, 1, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0, 4, 0x01);
        private CanPropertyInfo priorityInfo = new(26, 3, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0, 5, 0x07);

        public uint SourceAddress { get; set; }
        public uint PDUS { get; set; }
        public uint PDUF { get; set; }
        public uint DataPage { get; set; }
        public uint Priority { get; set; }
        public uint PGN { get; set; }

        public Id() { }

        public Id(uint id)
        {
            ExtractValues(id);
        }

        public uint WritePGNToUint()
        {
            var id = new ulong[] { WriteToUint() };
            pgnInfo.SetValue(ref id, PGN, false);
            ExtractValues((uint)id[0]);
            return (uint)id[0];
        }

        public uint WriteToUint()
        {
            var id = new ulong[] { 0 };

            sourceInfo.SetValue(ref id, SourceAddress, false);
            specificInfo.SetValue(ref id, PDUS, false);
            formatInfo.SetValue(ref id, PDUF, false);
            pageInfo.SetValue(ref id, DataPage, false);
            priorityInfo.SetValue(ref id, Priority, false);
            return (uint)id[0];
        }

        public void ExtractValues(uint id)
        {
            var input = new ulong[] { id };
            SourceAddress = sourceInfo.GetValue<uint>(input, false);
            PDUS = specificInfo.GetValue<uint>(input, false);
            PDUF = formatInfo.GetValue<uint>(input, false);
            DataPage = pageInfo.GetValue<uint>(input, false);
            Priority = priorityInfo.GetValue<uint>(input, false);
            PGN = pgnInfo.GetValue<uint>(input, false);
        }
    }

    public class TPCM
    {
        //RTS
        private CanPropertyInfo controlInfo = new(0, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo messageInfo = new(8, 16, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo packetInfo = new(24, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
        private CanPropertyInfo responseInfo = new(32, 8, ByteOrder.LittleEndian, Services.Can.ValueType.Unsigned, 0, 0);
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

        public CMControl ControlByte { get; set; }
        public uint MessageSize { get; set; }
        public uint NumPackets { get; set; }
        public uint PacketsPerCTS { get; set; }
        public uint PGN { get; set; }

        public uint PacketsRequested { get; set; }
        public uint PacketStart { get; set; }

        public uint AbortReason { get; set; }
        public uint AbortRole { get; set; }

        public TPCM() { }

        public TPCM(ulong data)
        {
            ExtractValues(data);
        }

        public ulong WriteToUint()
        {
            var data = new ulong[] { 0 };

            controlInfo.SetValue(ref data, ControlByte, false);
            if (ControlByte is CMControl.RTS or CMControl.EndOfMsgACK)
            {
                messageInfo.SetValue(ref data, MessageSize, false);
                packetInfo.SetValue(ref data, NumPackets, false);
                responseInfo.SetValue(ref data, PacketsPerCTS, false);
            }
            else if (ControlByte == CMControl.CTS)
            {
                sendInfo.SetValue(ref data, PacketsRequested, false);
                sequenceInfo.SetValue(ref data, PacketStart, false);
                reservedCTS.SetValue(ref data, 0xFFFF, false);
            }
            else if (ControlByte == CMControl.BAM)
            {
                messageInfo.SetValue(ref data, MessageSize, false);
                packetInfo.SetValue(ref data, NumPackets, false);
                reservedBAM.SetValue(ref data, 0xFF, false);
            }
            else if (ControlByte == CMControl.Abort)
            {
                reasonInfo.SetValue(ref data, AbortReason, false);
                roleInfo.SetValue(ref data, AbortRole, false);
                reservedAbort.SetValue(ref data, 0xFFFFFF, false);
            }
            pgnInfo.SetValue(ref data, PGN, false);

            return data[0];
        }

        public void ExtractValues(ulong data)
        {
            var input = new ulong[] { data };

            ControlByte = (CMControl)controlInfo.GetValue<uint>(input, false);
            if (ControlByte is CMControl.RTS or CMControl.EndOfMsgACK)
            {
                MessageSize = messageInfo.GetValue<uint>(input, false);
                NumPackets = packetInfo.GetValue<uint>(input, false);
                PacketsPerCTS = responseInfo.GetValue<uint>(input, false);
            }
            else if (ControlByte == CMControl.CTS)
            {
                PacketsRequested = sendInfo.GetValue<uint>(input, false);
                PacketStart = sequenceInfo.GetValue<uint>(input, false);
            }
            else if (ControlByte == CMControl.BAM)
            {
                MessageSize = messageInfo.GetValue<uint>(input, false);
                NumPackets = packetInfo.GetValue<uint>(input, false);
            }
            else if (ControlByte == CMControl.Abort)
            {
                AbortReason = reasonInfo.GetValue<uint>(input, false);
                AbortRole = roleInfo.GetValue<uint>(input, false);
            }
            PGN = pgnInfo.GetValue<uint>(input, false);
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
