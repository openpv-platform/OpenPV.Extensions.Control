#include <stdint.h>
#include <stdlib.h>
#include <stdbool.h>
#include "openamp_log.h"
#include "pb_decode.h"
#include "pb_encode.h"
#include "CanService.pb.h"
#include "CanConfiguration.pb.h"
#include "decodeCanMessages.h"
#include "nodeList.h"
#include "Services.pb.h"
#include "cmsis_os.h"
#include "FreeRTOS.h"
#include "semphr.h"

extern SemaphoreHandle_t timerListMutex[MAX_PORTS];
// decode message type


AhsokaCAN_CanMessageTypes_Ids decodeCanMessageType(uint8_t* buffer, uint32_t length)
{
	AhsokaServiceFramework_AhsokaMessageHeader message = AhsokaServiceFramework_AhsokaMessageHeader_init_default;
    
    // create a stream from buffer
    pb_istream_t stream = pb_istream_from_buffer(buffer, length);

    bool status = pb_decode(&stream, AhsokaServiceFramework_AhsokaMessageHeader_fields, &message);
    if(!status)
    {
        log_info("error decoding message\r\n");
    }
    return message.transport_id;
}
bool decodeDataField(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
	uint8_t* data = *arg; // data should point at the memory that we just allocated

	if(stream->bytes_left > 0)
		*arg = data = malloc(stream->bytes_left);
	else
		return true; // nothing to decode.
	if(data)
	{
		if(!pb_read(stream, data, stream->bytes_left))
		{
			// free data, since we are going to return false.
			free(data);
			return false;
		}
	}
	else
	{
		return false;
	}
	return true;
}

bool decodeCanMessageCallback(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
	// decode and send CAN message for each message.
	AhsokaCAN_CanMessageData message = AhsokaCAN_CanMessageData_init_default;

	uint8_t* data = NULL;
	message.data.arg = data;
	message.data.funcs.decode = decodeDataField;
	uint32_t port = (uint32_t)*arg;
    bool status = pb_decode(stream, AhsokaCAN_CanMessageData_fields, &message);

	if(status)
	{
		// send the can message here!
		data = message.data.arg;
		// need to implement CAN message send that will handle sending multipacket messages if needed.
		// need to decide who deletes the malloc'd data memory, the CAN send or here?
		canMessageTimerList_t* node;
		xSemaphoreTake(timerListMutex[port], portMAX_DELAY);
		bool delete = false;
		node = findCanTxMessage(port, txList[port], message.id, true, &delete);
		if(node)
		{
			// update data!
			memcpy(node->msg->data, data,node->msg->dlc);
			// found a message in the list, send it now!
			sendCan(port, node->msg);

		}
		if(delete)
		{
			free(node->msg);
			free(node);
		}
		xSemaphoreGive(timerListMutex[port]);
		free(data);
	}
	else
	{
		log_info("error decoding CAN message:%d\r\n", status);
		return false;
	}
	return true;
}
void decodeSendCANMessages(uint8_t* buffer, uint32_t length)
{
    AhsokaCAN_CanMessageDataCollection message = AhsokaCAN_CanMessageDataCollection_init_default;

    message.messages.arg = NULL;
    message.messages.funcs.decode = NULL;
    // decode once with NULL to get the port, so that we can decode it first.
    pb_istream_t stream = pb_istream_from_buffer(buffer, length);

    bool status = pb_decode(&stream, AhsokaCAN_CanMessageDataCollection_fields, &message);
    if(!status)
    {
    	log_info("error decoding sendCANMessages \r\n");
    }
    // now that we know the port, use that as the argument for the decode messages, so that
    // we can send directly from the decode call.
    uint32_t port = message.can_port;

    message.messages.arg = (void*)port;
    message.messages.funcs.decode = decodeCanMessageCallback;

    stream = pb_istream_from_buffer(buffer, length);

	status = pb_decode(&stream, AhsokaCAN_CanMessageDataCollection_fields, &message);
	if(!status)
	{
		log_info("error decoding sendCANMessages \r\n");
	}

}

void decodeSendRecurringCANMessage(uint8_t* buffer, uint32_t length)
{
	AhsokaCAN_RecurringCanMessage message = AhsokaCAN_RecurringCanMessage_init_default;
	uint32_t port;
	message.message.data.arg = &port;
	message.message.data.funcs.decode = decodeDataField;
	uint8_t* data = NULL;
	message.message.data.arg = &data;

	pb_istream_t stream = pb_istream_from_buffer(buffer, length);

	bool status = pb_decode(&stream, AhsokaCAN_RecurringCanMessage_fields, &message);
	if(status)
	{

		// now need to check to see if this message is in the tx list and if it is, start sending it.
		canMessageTimerList_t* node;
		bool delete = false;
		node = findCanTxMessage(message.can_port, txList[message.can_port], message.message.id,false, &delete);
		if(node)
		{
			// update the values!
			updateCanMessageTimerValues(node, message);
			// we found a message that matches in the tx list, so now schedule the message.
			scheduleCanMessageTimer(message.can_port, node);
		}
	}
}

bool decodeCANFilter(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
//	uint32_t port = (uint32_t)arg;
	while(stream->bytes_left)
	{
		uint64_t value;
		// this might be a fixed32 value?
		if(!pb_decode_varint(stream, &value))
			return false;
		// add filter call goes here!
		// value contains the id to add to list  of filtered.
	}
	return true;
}
void decodeAddCANFilter(uint8_t* buffer, uint32_t length)
{
	AhsokaCAN_ClientCanFilter message = AhsokaCAN_ClientCanFilter_init_default;

	// get port first
	message.can_id_list.arg = NULL;
	message.can_id_list.funcs.decode = NULL;

	pb_istream_t stream = pb_istream_from_buffer(buffer, length);

	bool status = pb_decode(&stream, AhsokaCAN_ClientCanFilter_fields, &message);
	if(!status)
		return;
	uint32_t port = message.can_port;

	message.can_id_list.arg = (void*)port;
	message.can_id_list.funcs.decode = decodeCANFilter;

	stream = pb_istream_from_buffer(buffer, length);

	status = pb_decode(&stream, AhsokaCAN_ClientCanFilter_fields, &message);
}

bool decodeNodeAddress(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
	AhsokaCAN_CanState_NodeAddressesEntry entry = AhsokaCAN_CanState_NodeAddressesEntry_init_default;

	bool status = pb_decode(stream, AhsokaCAN_CanState_NodeAddressesEntry_fields, &entry);
	if(!status)
		return false;

	nodeList_t* node = searchNode(entry.key);
	if (node != NULL)
		node->address = entry.value;

	return true;
}

void decodeNetworkStateChanged(uint8_t* buffer, uint32_t length)
{
	AhsokaCAN_CanState message = AhsokaCAN_CanState_init_default;
	message.node_addresses.funcs.decode = decodeNodeAddress;
	message.node_addresses.arg = NULL;

	pb_istream_t stream = pb_istream_from_buffer(buffer, length);

	bool status = pb_decode(&stream, AhsokaCAN_CanState_fields, &message);
	if(!status)
		return;

	uint32_t port = message.can_port;
	uint32_t address = message.current_address;

	txNode[port]->address = address;
	if(address == 254)
		setTransmittingJ1939(port, false);
}

void decodeCoprocessorReady()
{
	setCoprocessorReady(true);
}

