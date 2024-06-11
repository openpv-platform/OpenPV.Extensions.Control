#ifndef __ENCODECANMESSAGES_H__
#define __ENCODECANMESSAGES_H__

#include "CanService.pb.h"
#include "zmq_message_list.h"

typedef struct canMessageSimple
{
	uint32_t port;  // might not need?
	uint32_t id;
	uint32_t dlc;
	uint32_t msgType;
	uint8_t data[8];
}canMessageSimple_t;

typedef struct canMessageSimpleCollection
{
	canMessageSimple_t* ptr;
	uint32_t count;
}canMessageSimpleCollection_t;

typedef struct canMessageData
{
	uint8_t* data;
	uint32_t length;
}canMessageData_t;

extern zmtp_msg_t* encodeCanMessageType(AhsokaCAN_CanMessageTypes_Ids type);
extern zmtp_msg_t* encodeCanMessageCollection(canMessageSimple_t* canMessages, uint32_t numMessages);

#endif

