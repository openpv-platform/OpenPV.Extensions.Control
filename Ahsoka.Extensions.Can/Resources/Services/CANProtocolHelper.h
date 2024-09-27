#pragma once

#include <string>
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaCAN
{
    class CANProtocolHelper
    {
        public:
            CANProtocolHelper(CanMessageData* data)
            {
                messageData = data;
            }

        protected:
            CanMessageData* messageData;
    };
}