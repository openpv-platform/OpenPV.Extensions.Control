#pragma once

#include <string>
#include <functional>
#include "AhsokaServices.h"

namespace AhsokaCAN
{
    class IHasCanData
    {
        public: 
            virtual CanMessageData CreateCanMessageData() = 0;
    };
}