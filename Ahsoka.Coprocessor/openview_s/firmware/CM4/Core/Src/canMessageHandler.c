#include <stdint.h>
#include <stdbool.h>
#include "openamp_log.h"
#include "pb_encode.h"
#include "CanService.pb.h"
#include "encodeCanMessages.h"
#include "decodeCanMessages.h"

static void handleHeartbeat(void);
static void sendCoprocessorReady(void);

#define  HEADER_BUFFER_SIZE 40
#define  DATA_BUFFER_SIZE 1024

static uint8_t headerBuffer[HEADER_BUFFER_SIZE];
static uint8_t dataBuffer[DATA_BUFFER_SIZE];

extern void sendAhsokaMessage(uint8_t* header, uint32_t headerLength, uint8_t* data, uint32_t dataLength);



void canMessageHandler(uint8_t* header, uint32_t headerLength, uint8_t* data, uint32_t dataLength)
{
    
    AhsokaCAN_CanMessageTypes_Ids id = decodeCanMessageType(header, headerLength);
    // remove type
    // process message
    switch(id)
    {
        
        case AhsokaCAN_CanMessageTypes_Ids_OPEN_COMMUNICATION_CHANNEL:
            //log_info("open com channel\r\n");
            // don't really know what to do here yet, the channel is always open    
            break;

        case AhsokaCAN_CanMessageTypes_Ids_CLOSE_COMMUNICATION_CHANNEL:
            //log_info("close com channel\r\n");
            // see above comment, channel is always open, should we enable/disable CAN?
            break;

        case AhsokaCAN_CanMessageTypes_Ids_NETWORK_STATE_CHANGED:
            //log_info("network state changed\r\n");
            decodeNetworkStateChanged(data, dataLength);
            break;

        case AhsokaCAN_CanMessageTypes_Ids_SEND_CAN_MESSAGES:
            //log_info("send can message\r\n");
            // send single can message
            decodeSendCANMessages(data, dataLength);
            break;

        case AhsokaCAN_CanMessageTypes_Ids_SEND_RECURRING_CAN_MESSAGE:
           // log_info("send recurring can message\r\n");
            // add recurring can message to list of recurring messages
            decodeSendRecurringCANMessage(data, dataLength);
            break;

        case AhsokaCAN_CanMessageTypes_Ids_APPLY_MESSAGE_FILTER:
            //log_info("apply message filter\r\n");
            // add filter to list of messages to look for.
            decodeAddCANFilter(data, dataLength);
            break;

        case AhsokaCAN_CanMessageTypes_Ids_COPROCESSOR_HEARTBEAT:
        	handleHeartbeat();
            //log_info("heartbeat\r\n");
            break;
        case AhsokaCAN_CanMessageTypes_Ids_CAN_SERVICE_IS_READY_NOTIFICATION:
        	sendCoprocessorReady();
        	break;

        case AhsokaCAN_CanMessageTypes_Ids_COPROCESSOR_IS_READY_NOTIFICATION:
        case AhsokaCAN_CanMessageTypes_Ids_CAN_MESSAGES_RECEIVED:
        case AhsokaCAN_CanMessageTypes_Ids_NONE:
        default:
            // these messages either can't be received, or the value is invalid.
            log_info("none or bad CAN Message type:%d\r\n",id);
            break;
    }
}
void sendCoprocessorReady(void)
{
	uint32_t count = 0;
	//static bool readySent = false;

	// then if we haven't sent one before, send a coprocessor ready message.
	//if(!readySent)
	{
		//readySent = true;
		// now serialize and send the ready message.

		count = encodeCanMessageType(AhsokaCAN_CanMessageTypes_Ids_COPROCESSOR_IS_READY_NOTIFICATION, headerBuffer, HEADER_BUFFER_SIZE);

		if(count > 0)
			sendAhsokaMessage(headerBuffer, count, NULL, 0);

		decodeCoprocessorReady();
	}

}
static void handleHeartbeat(void)
{
	uint32_t count = 0;
    // for a heartbeat, first send a heartbeat message
    count = encodeCanMessageType(AhsokaCAN_CanMessageTypes_Ids_COPROCESSOR_HEARTBEAT, headerBuffer, HEADER_BUFFER_SIZE);
    if(count > 0)
    	sendAhsokaMessage(headerBuffer, count, NULL, 0);
}

void handleReceiveCanMessages(canMessageSimple_t* canMessages, uint32_t numMessages)
{
	uint32_t headerLength;
	uint32_t dataLength;

	//log_info("handle rx message\r\n");
	// for a heartbeat, first send a heartbeat message
	headerLength = encodeCanMessageType(AhsokaCAN_CanMessageTypes_Ids_CAN_MESSAGES_RECEIVED, headerBuffer, HEADER_BUFFER_SIZE);

	// get and empty frame and add it to the list for a heartbeat.
	dataLength = encodeCanMessageCollection(canMessages, numMessages, dataBuffer, DATA_BUFFER_SIZE);

	// send the message, send takes care of deleting the message memory!
	sendAhsokaMessage(headerBuffer, headerLength, dataBuffer, dataLength);

}
