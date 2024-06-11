#include "IHasCanData.h"

typedef std::function<void(CanMessageDataCollection&)> CanMessageReceived_t;
typedef std::function<void(CanState&)> CanStateReceived_t;
