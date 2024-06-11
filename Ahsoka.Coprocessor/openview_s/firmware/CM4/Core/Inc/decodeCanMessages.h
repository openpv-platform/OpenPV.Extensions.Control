#ifndef __DECODECANMESSAGES_H__
#define __DECODECANMESSAGES_H__

#include "CanService.pb.h"



extern AhsokaCAN_CanMessageTypes_Ids decodeCanMessageType(uint8_t* buffer, uint32_t length);

extern void decodeSendCANMessages(uint8_t* buffer, uint32_t length);
extern void decodeSendRecurringCANMessage(uint8_t* buffer, uint32_t length);
extern void decodeAddCANFilter(uint8_t* buffer, uint32_t length);
extern void decodeNetworkStateChanged(uint8_t* buffer, uint32_t length);
extern void decodeCoprocessorReady();
#endif

