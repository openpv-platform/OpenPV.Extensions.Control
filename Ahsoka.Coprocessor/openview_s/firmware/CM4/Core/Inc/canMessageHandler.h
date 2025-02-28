#ifndef __CANMESSAGEHANDLER_H__
#define __CANMESSAGEHANDLER_H__

#include "CanService.pb.h"
#include "canHandler.h"
#include "encodeCanMessages.h"
#include <stdint.h>

extern void handleReceiveCanMessages(canMessageSimple_t* canMessages, uint32_t numMessages);
extern void canMessageHandler(uint8_t* header, uint32_t headerLength, uint8_t* data, uint32_t dataLength);
extern void sendCoprocessorReady(void);
#endif

