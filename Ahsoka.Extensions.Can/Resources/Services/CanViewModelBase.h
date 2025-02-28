#pragma once
#include <string>
#include <functional>
#include <memory>
#include "AhsokaServices.h"
#include "CanPropertyInfo.h"
#include "IHasCanData.h"

using namespace std;
using namespace AhsokaCAN;

namespace AhsokaCAN
{
    class CanViewModelBase : public IHasCanData
    {
        public: 

            uint GetId() { return message.id(); } 

            CanViewModelBase(uint canID, int dlc)
            {
                message.set_dlc((uint)dlc);
                message.set_id(canID);
                
                dlc += dlc % 8;

                 // Create Data Buffer
                data = new std::vector<uint64_t>( std::ceil( dlc / 8.0f));
                fill(data->begin(),data->end(),ULLONG_MAX);
            }

            CanViewModelBase(CanMessageData messageData)
            {
                message = messageData;

                // Fill Data Buffer
                int count = std::ceil(messageData.data().length() / 8.0f);
                data = new std::vector<uint64_t>(count);

                memcpy((void*)(data->data()),
                       messageData.data().c_str(),
                       messageData.data().length());
            }

            CanViewModelBase(const CanViewModelBase& messageData)
            {
                message.set_id(messageData.message.id());
                message.set_dlc(messageData.message.dlc());

                int count = std::ceil( messageData.message.data().length() /  8.0f);
                data = new std::vector<uint64_t>(count);

                memcpy((void*)(data->data()), messageData.message.data().c_str(), data->size() * 8);
            }

            CanMessageData CreateCanMessageData()
            {
                // Copy Data into Message and return.
                CanMessageData returnData;
                returnData.set_id(message.id());
                returnData.set_dlc(message.dlc());
                returnData.set_data((void*)(&data->front()), data->size() * 8);

                return returnData;
            }
            
            template <typename T>
            T GetRawValue(int memberProperty) 
            {
                return OnGetValue<T>(memberProperty, false);
            }

        protected:

            template <typename T>
            void OnSetValue(T newValue, int memberProperty) 
            {
                auto md = GetMetadata();
                auto info = md.find(memberProperty);
                if (info != md.end())
                {
                    T baseValue = info->second.GetValue<T>(data->data(), true, true);
                    if (baseValue != newValue)
                        info->second.SetValue<T>(data->data(), newValue);
                }
            }

            template <typename T>
            T OnGetValue(int memberProperty, bool scaledValue = true)
            {
                T returnValue;
                auto md = GetMetadata();
                auto info = md.find(memberProperty);
                if (info != md.end())
                   returnValue = info->second.GetValue<T>(data->data(), scaledValue);

                return returnValue;
            }

            virtual std::map<int, CanPropertyInfo>& GetMetadata() = 0;

        protected:

            CanMessageData message;

        private:

            std::vector<uint64_t>* data;
    };
}

