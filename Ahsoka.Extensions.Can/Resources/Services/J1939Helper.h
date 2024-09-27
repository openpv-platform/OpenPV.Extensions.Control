#pragma once

#include <string>
#include <functional>
#include "AhsokaServices.h"
#include "CANProtocolHelper.h"
#include "CanPropertyInfo.h"

namespace AhsokaCAN
{
    class J1939Id
    {
        public:
            J1939Id() {}

            J1939Id(uint id)
            {
                ExtractValues(id);
            }

            uint GetSourceAddress() { return sourceInfo.GetValue<uint>(id, false); }
            void SetSourceAddress(uint value) { sourceInfo.SetValue<uint>(id, value, false); }

            uint GetPDUS() { return specificInfo.GetValue<uint>(id, false); }
            void SetPDUS(uint value) { specificInfo.SetValue<uint>(id, value, false); }

            uint GetPDUF() { return formatInfo.GetValue<uint>(id, false); }
            void SetPDUF(uint value) { formatInfo.SetValue<uint>(id, value, false); }

            uint GetDataPage() { return pageInfo.GetValue<uint>(id, false); }
            void SetDataPage(uint value) { pageInfo.SetValue<uint>(id, value, false); }

            uint GetPriority() { return priorityInfo.GetValue<uint>(id, false); }
            void SetPriority(uint value) { priorityInfo.SetValue<uint>(id, value, false); }

            uint GetPGN() { return pgnInfo.GetValue<uint>(id, false); }
            void SetPGN(uint value) { pgnInfo.SetValue<uint>(id, value, false); }

            uint WriteToUint()
            {
                return (uint)id[0];
            }

            void ExtractValues(uint newId)
            {
                id[0] = newId;
            }

        private:
            CanPropertyInfo pgnInfo = CanPropertyInfo(8, 18, ByteOrder::LittleEndian, ValueType::Unsigned, 0, 0, 1, 0x3FFFF);
            CanPropertyInfo sourceInfo = CanPropertyInfo(0, 8, ByteOrder::LittleEndian, ValueType::Unsigned, 0, 0, 1, 0xFF);
            CanPropertyInfo specificInfo = CanPropertyInfo(8, 8, ByteOrder::LittleEndian, ValueType::Unsigned, 0, 0, 2, 0xFF);
            CanPropertyInfo formatInfo = CanPropertyInfo(16, 8, ByteOrder::LittleEndian, ValueType::Unsigned, 0, 0, 3, 0xFF);
            CanPropertyInfo pageInfo = CanPropertyInfo(24, 1, ByteOrder::LittleEndian, ValueType::Unsigned, 0, 0, 4, 0x01);
            CanPropertyInfo priorityInfo = CanPropertyInfo(26, 3, ByteOrder::LittleEndian, ValueType::Unsigned, 0, 0, 5, 0x07);

            uint64_t id[1] = { 0 };
    };

    class J1939Helper : public CANProtocolHelper
    {
        public:
            J1939Helper(CanMessageData* data) : CANProtocolHelper(data)
            {
                id.ExtractValues(messageData->id());
            }

            uint GetSourceAddress() { return id.GetSourceAddress(); }
            void SetSourceAddress(uint value) { id.SetSourceAddress(value); messageData->set_id(id.WriteToUint()); }

            uint GetDestinationAddress() { return id.GetPDUS(); }
            void SetDestinationAddress(uint value) { id.SetPDUS(value); messageData->set_id(id.WriteToUint()); }

        private:
            J1939Id id;
    };
}