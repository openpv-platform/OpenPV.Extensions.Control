#include <stdint.h>
#include <stdbool.h>
#include "openamp_log.h"
#include "pb_encode.h"
#include "CanService.pb.h"
#include "encodeCanMessages.h"
#include "decodeCanMessages.h"
#include "zmq_message_list.h"

extern zmtp_channel_t* sendChannel;
extern uint32_t zmq_send_multipart(zmtp_channel_t *self, zmq_message_list_t* list );

static void handleHeartbeat(void);
static void sendCoprocessorReady(void);

static void sendCANMessages(zmq_message_list_t* list);
static void sendRecurringCANMessage(zmq_message_list_t* list);
static void addCANFilter(zmq_message_list_t* list);
static void networkStateChanged(zmq_message_list_t* list);

void canMessageHandler(zmq_message_list_t* list)
{
    
    AhsokaCAN_CanMessageTypes_Ids id = decodeCanMessageType(list->msg->data, list->msg->size);
    // remove type
    list = zmq_message_list_get_next(list);
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
            networkStateChanged(list);
            break;

        case AhsokaCAN_CanMessageTypes_Ids_SEND_CAN_MESSAGES:
            //log_info("send can message\r\n");
            // send single can message
            sendCANMessages(list);
            break;

        case AhsokaCAN_CanMessageTypes_Ids_SEND_RECURRING_CAN_MESSAGE:
           // log_info("send recurring can message\r\n");
            // add recurring can message to list of recurring messages
            sendRecurringCANMessage(list);
            break;

        case AhsokaCAN_CanMessageTypes_Ids_APPLY_MESSAGE_FILTER:
            //log_info("apply message filter\r\n");
            // add filter to list of messages to look for.
            addCANFilter(list);
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
    // all done with the list, so delete it.
    zmq_message_list_delete(list);

}
void sendCoprocessorReady(void)
{
	//static bool readySent = false;

	// then if we haven't sent one before, send a coprocessor ready message.
	//if(!readySent)
	{
		//readySent = true;
		// now serialize and send the ready message.
		zmq_message_list_t* list = zmq_new_message_list();
		zmtp_msg_t* frame = encodeCanMessageType(AhsokaCAN_CanMessageTypes_Ids_COPROCESSOR_IS_READY_NOTIFICATION);
		zmq_add_message_list(list, frame);

		zmtp_msg_t* emptyFrame = zmtp_msg_new(0, 0);
		zmq_add_message_list(list, emptyFrame);
		// send the message
		zmq_send_multipart(sendChannel, list);

		decodeCoprocessorReady();
	}
}
static void handleHeartbeat(void)
{
    // for a heartbeat, first send a heartbeat message
    zmq_message_list_t* list = zmq_new_message_list();
    zmtp_msg_t* frame = encodeCanMessageType(AhsokaCAN_CanMessageTypes_Ids_COPROCESSOR_HEARTBEAT);
    zmq_add_message_list(list, frame);
    // get and empty frame and add it to the list for a heartbeat.
    zmtp_msg_t* emptyFrame = zmtp_msg_new(0, 0);
    zmq_add_message_list(list, emptyFrame);
    // send the message, send takes care of deleting the message memory!
    zmq_send_multipart(sendChannel, list);    
    // for old version, send ready when we get the heartbeat.
    //sendCoprocessorReady();
}

static void sendCANMessages(zmq_message_list_t* list)
{
    // message is a collection of can messages to send.
    decodeSendCANMessages(list->msg->data, list->msg->size);
}

static void sendRecurringCANMessage(zmq_message_list_t* list)
{
    decodeSendRecurringCANMessage(list->msg->data, list->msg->size);
}

static void addCANFilter(zmq_message_list_t* list)
{
    decodeAddCANFilter(list->msg->data, list->msg->size);
}

static void networkStateChanged(zmq_message_list_t* list)
{
	decodeNetworkStateChanged(list->msg->data, list->msg->size);
}

void handleReceiveCanMessages(canMessageSimple_t* canMessages, uint32_t numMessages)
{
	//log_info("handle rx message\r\n");
	// for a heartbeat, first send a heartbeat message
	zmq_message_list_t* list = zmq_new_message_list();
	zmtp_msg_t* frame = encodeCanMessageType(AhsokaCAN_CanMessageTypes_Ids_CAN_MESSAGES_RECEIVED);
	zmq_add_message_list(list, frame);
	// get and empty frame and add it to the list for a heartbeat.
	frame = encodeCanMessageCollection(canMessages, numMessages);
	if(frame == NULL)
	{
		frame = zmtp_msg_new(0, 0);
	}
	zmq_add_message_list(list, frame);
	// send the message, send takes care of deleting the message memory!
	zmq_send_multipart(sendChannel, list);
}
