#ifndef __CANMESSAGEHANDLER_H__
#define __CANMESSAGEHANDLER_H__

#include "CanService.pb.h"
#include "canHandler.h"
#include "zmq_message_list.h"
#include "encodeCanMessages.h"

extern void handleReceiveCanMessages(canMessageSimple_t* canMessages, uint32_t numMessages);
extern void canMessageHandler(zmq_message_list_t* list);
extern void sendCoprocessorReady(void);
#endif

