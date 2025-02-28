#pragma once
#include <string>
#include <functional>
#include <memory>
#include <math.h>
#include <cfloat>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaCAN;

namespace AhsokaCAN
{
    class CanPropertyInfo
    {
        public: 

            CanPropertyInfo( ){}
            CanPropertyInfo(const CanPropertyInfo& copy )
            {
                scale = copy.scale;
                offset = copy.offset;
                startBit = copy.startBit;
                bitLength = copy.bitLength;
                byteOrder = copy.byteOrder;
                dataType = copy.dataType;
                signalId = copy.signalId;
                defaultValue = copy.defaultValue;
                minValue = copy.minValue;
                maxValue = copy.maxValue;
            }

            CanPropertyInfo(int astartBit, int abitLength, ByteOrder abyteOrder, ValueType adataType, double ascale, double aoffset, int signal = -1, double defaultVal = 0, double min = DBL_MIN, double max = DBL_MAX)
            {
                scale = ascale;
                offset = aoffset;
                startBit = astartBit;
                bitLength = abitLength;
                byteOrder = abyteOrder;
                dataType = adataType;
                signalId = signal;
                defaultValue = defaultVal;
                SetBounds(adataType, min, max);
            }
        
            template <typename T>
            void SetValue(string * data, T newValue, bool scaleValue = true) 
            {
                uint64_t longData[data->size() / 8];

                memcpy((void*)&longData, (void*)data->c_str(), data->size());

                SetValue<T>(longData, newValue, scaleValue);

                memcpy((void*)data->c_str(),(void*) &longData , data->size());
            }

            template <typename T>
            T GetValue(string* data, bool scaleValue = true, bool getRaw = false)
            {
                uint64_t longData[data->size() / 8];
                
                memcpy(&longData, data->c_str(), data->size());

                return GetValue<T>(longData, scaleValue, getRaw);
            }

            template <typename T>
            void SetValue(uint64_t data[], T newValue, bool scaleValue = true) 
            {
                int startByte = startBit / 8;
                int messageIndex = startByte / 8;

                // Clear Bits
                data[messageIndex] &= ~(BitMask() << startBit);

                // Set New Value
                data[messageIndex] |= Pack(newValue, scaleValue);
            }

            template <typename T>
            T GetValue(uint64_t data[], bool scaleValue = true, bool getRaw = false)
            {
                int startByte = startBit / 8;
                int messageIndex = startByte / 8;

                T retVal;

                if (dataType == ValueType::SIGNED)
                    return (T)Unpack(data[messageIndex], scaleValue, getRaw);
                else if (dataType == ValueType::UNSIGNED)
                    return (T)Unpack(data[messageIndex], scaleValue, getRaw);
                else if (dataType == ValueType::FLOAT)
                    return (T)Unpack(data[messageIndex], scaleValue, getRaw);
                else if (dataType == ValueType::DOUBLE)
                    return (T)Unpack(data[messageIndex], scaleValue, getRaw);
                else if (dataType == ValueType::ENUM)
                    return (T)(Unpack(data[messageIndex], scaleValue, getRaw));
                
                return retVal;
            }
        
            uint64_t Pack(double value, bool scaleValue = true)
            {
                long iVal;
                uint64_t bitMask = BitMask();

                // Ensure value lies within bounds
                auto rawValue = std::max(minValue, std::min(maxValue, value));

                // Apply scaling
                rawValue = scaleValue ? (rawValue - offset) / scale : rawValue;

                // Convert to Byte[8]
                if (dataType == ValueType::FLOAT)
                    iVal =   (long)(float)rawValue; 
                else if (dataType == ValueType::DOUBLE)
                    iVal =  (long)(double)rawValue; 
                else
                    iVal = (long)std::round(rawValue);                

                // Pack signal
                if (byteOrder == ByteOrder::ORDER_LITTLE_ENDIAN) // Little endian 
                     return (((uint64_t)iVal & bitMask) << startBit);
                else // Big endian
                    return MirrorMsg(((uint64_t)iVal & bitMask) << GetStartBitLE());
            }

            double Unpack(uint64_t data, bool scaleValue = true, bool getRaw = false)
            {
                long iVal;
                double retVal = 0;
                uint64_t bitMask = BitMask();

                // Unpack signal
                if (byteOrder == ByteOrder::ORDER_LITTLE_ENDIAN) // Little endian 
                    iVal = (long)((data >> startBit) & bitMask);
                else // Big endian 
                    iVal = (long)((MirrorMsg(data) >> GetStartBitLE()) & bitMask);

                if (dataType == ValueType::FLOAT)
                    retVal = (float)iVal;
                else if (dataType == ValueType::DOUBLE)
                    retVal = (double)iVal;
                else 
                    retVal = iVal;

                // All FF's
                if ((unsigned long)iVal == bitMask && !getRaw)
                    return defaultValue;

                // Apply scaling
                if (scaleValue)
                    return iVal * scale + offset;                                

                // Ensure value lies within bounds
                retVal = std::max(minValue, std::min(maxValue, retVal));
                
                return retVal;
            }

            uint64_t BitMask()
            {
                return (ULLONG_MAX >> (64 - bitLength));
            }

            uint8_t GetStartBitLE()
            {
                uint8_t startByte = (uint8_t)(startBit / 8);
                return (uint8_t)(64 - (bitLength + 8 * startByte + (8 * (startByte + 1) - (startBit + 1)) % 8));
            }

            uint64_t MirrorMsg(uint64_t msg)
            {
                uint64_t swapped =  ((0x00000000000000FF) & (msg >> 56)
                                | (0x000000000000FF00) & (msg >> 40)
                                | (0x0000000000FF0000) & (msg >> 24)
                                | (0x00000000FF000000) & (msg >> 8)
                                | (0x000000FF00000000) & (msg << 8)
                                | (0x0000FF0000000000) & (msg << 24)
                                | (0x00FF000000000000) & (msg << 40)
                                | (0xFF00000000000000) & (msg << 56));

                return swapped;
            }

            void SetBounds(ValueType dataType, double min, double max) 
            {
                if (min == 0 && max == 0)
                {
                    min = DBL_MIN; 
                    max = DBL_MAX;
                }

                if (dataType == ValueType::FLOAT)
                {
                    minValue = std::max(min, (double)FLT_MIN);
                    maxValue = std::min(max, (double)FLT_MAX);
                }
                else if (dataType == ValueType::DOUBLE)
                {
                    minValue = std::max(min, DBL_MIN);
                    maxValue = std::min(max, DBL_MAX);
                }
                else if (dataType == ValueType::UNSIGNED || dataType == ValueType::ENUM)
                {
                    minValue = std::max(min, (double)0);
                    maxValue = std::min(max, (double)UINT32_MAX);
                }
                else if (dataType == ValueType::SIGNED)    
                {
                    minValue = std::max(min, (double)INT_MIN);
                    maxValue = std::min(max, (double)INT_MAX);
                } 
            }

            int GetSignalId() { return signalId; }
            ValueType GetDataType() { return dataType; }      
            int GetStartBit() const { return startBit; }
            int GetBitLength() const { return bitLength; }

        private: 
            
            int signalId;
            double scale;
            double offset; 
            double defaultValue;
            double minValue;
            double maxValue;
            int startBit;
            int bitLength;
            ByteOrder byteOrder;
            ValueType dataType;

    };
}