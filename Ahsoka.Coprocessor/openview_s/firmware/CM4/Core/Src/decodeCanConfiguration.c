#include <stdint.h>
#include <stdlib.h>
#include <stdbool.h>
#include "openamp_log.h"
#include "pb_decode.h"
#include "pb_encode.h"
#include "CanService.pb.h"
#include "decodeCanConfiguration.h"
#include "nodeList.h"

bool decodeInt(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
	uint32_t data = 0;
	bool status = pb_decode_varint32(stream, &data);

	if(status)
	{
		nodeList_t* node = createNodeList();
		node->id = data;
		addNode(&decodeList, node);
	}
	return status;
}

bool decodeString(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
	uint8_t* data = *arg; // data should point at the memory
	if(stream->bytes_left > 0)
	{
		*arg = data = malloc(stream->bytes_left+1);
		data[stream->bytes_left] = 0;
	}
	else
		return true; // nothing to decode.
	if(data)
	{
		data[stream->bytes_left] = 0; // add a null terminator.
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

bool decodePort(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
	AhsokaCAN_PortDefinition message = AhsokaCAN_PortDefinition_init_zero;

	message.can_interface_path.funcs.decode = NULL;

	bool status = pb_decode(stream, AhsokaCAN_PortDefinition_fields, &message);

	if(status)
	{
		nodeList_t* port = createNodeList();
		port->id = message.port;
		addNode(&portList, port);
		setPromiscuousMode(message.port, message.promiscuous_transmit, message.promiscuous_receive);
	}
	return status;
}

bool decodeMessage(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
	AhsokaCAN_MessageDefinition message = AhsokaCAN_MessageDefinition_init_zero;

	message.name.funcs.decode = NULL;
	message.comment.funcs.decode = NULL;
	message.signals.funcs.decode = NULL;
	message.receive_nodes.funcs.decode = NULL;
	message.receive_nodes.funcs.decode = decodeInt;
	message.transmit_nodes.arg = NULL;
	message.transmit_nodes.funcs.decode = decodeInt;

	int32_t txNodes[MAX_PORTS] = {-1, -1};
	int32_t rxNodes[MAX_PORTS] = {-1, -1};

	deleteList(decodeList);
	bool status = pb_decode(stream, AhsokaCAN_MessageDefinition_fields, &message);
	if(status)
	{
		//decode node lists
		int j = 0;
		while(decodeList != NULL)
		{
			if (j < MAX_PORTS)
			{
				txNodes[j] = decodeList->id;
			}
			else
			{
				rxNodes[j % 2] = decodeList->id;
			}

			j++;
			decodeList = decodeList->next;
		}

		for(int i = 0; i < MAX_PORTS; i++)
		{
			if(txNodes[i] == -1 || rxNodes[i] == -1)
				continue;

			int32_t broadcastId = 255;
			if(broadcastAvailable[i])
			{
				broadcastId = broadcastNode[i]->id;
			}

			// we have found a message, so now, add it to the list.
			// have to check the node ID to see if this is a tx message or rx message.
			if(txNode[i]->id == txNodes[i] || txNodes[i] == broadcastId)
			{
				// this is a transmit message
				// for transmit, create a timer message so that we can have 2 refs to the objects in
				// the list.  This makes it easier when the periodic messages get reset to update the data,
				// we don't have to crawl the timer list every time.
				canMessageTimerList_t* node = createCanMessageTimer();
				if(node == NULL)
					return false;

				// initialize message values.
				node->msg->id = message.id;
				node->msg->msgType = message.message_type;
				node->msg->overrideDestination = message.override_source_address;
				node->msg->overrideSource = message.override_source_address;
				node->msg->rate = message.rate;
				node->msg->timeout = message.timeout_ms;
				node->msg->transmitNodeId = txNodes[i];
				node->msg->receiverNodeId = rxNodes[i];

				if(message.has_roll_count)
				{
					node->msg->rollCountLength = message.roll_count_length;
					node->msg->rollCountBitPos = message.roll_count_bit;
				}
				else
				{
					// length of 0 will prevent roll count
					node->msg->rollCountLength = 0;
					node->msg->rollCountBitPos = 0;
				}
				node->msg->rollCount = 0;
				node->msg->dlc = message.dlc;

				node->msg->crc = getChecksumFunc(message.crc_type);
				node->msg->crcPos = message.crc_bit;

				// TODO: data may be more than 8 bytes. what if we are sending extended frame messages.
				// may not want to allow recurring extended frame messages, because of the large amount of
				// data that would require.
				memset(&node->msg->data[0], 0, node->msg->dlc);

				// don't schedule the timer, won't start until we get a message.
				addCanMessageTxList(&txList[i], node);
			}
			if(txNode[i]->id != txNodes[i] || txNodes[i] == broadcastId)
			{
				// this is a receive message
				// right now, ignore the filter parameter, unclear if it is accept or reject filter.
				canMessageList_t* node = createCanMessageList();
				// need to remove the 0x80000000 if the message is extended.
				if(message.message_type == AhsokaCAN_MessageType_J1939_EXTENDED_FRAME)
				{
					node->msg->id = (message.id & 0x7fffffff) | (txNodes[i] & 0xff);
				}
				else if(message.message_type == AhsokaCAN_MessageType_RAW_EXTENDED_FRAME)
				{
					node->msg->id = (message.id & 0x7fffffff);
				}
				else
				{
					node->msg->id = message.id;
				}
				node->msg->msgType = message.message_type;
				node->msg->rate = message.rate;
				node->msg->timeout = message.timeout_ms;
				node->msg->transmitNodeId = txNodes[i];
				node->msg->receiverNodeId = rxNodes[i];
				if(message.has_roll_count)
				{
					node->msg->rollCountLength = message.roll_count_length;
					node->msg->rollCountBitPos = message.roll_count_bit;
				}
				else
				{
					// length of 0 will prevent roll count
					node->msg->rollCountLength = 0;
					node->msg->rollCountBitPos = 0;
				}
				node->msg->rollCount = 0;
				node->msg->dlc = message.dlc;
				node->msg->crc = NULL;  // for now.
				node->msg->crcPos = 0;
				node->msg->overrideDestination = message.override_source_address;
				node->msg->overrideSource = message.override_source_address;
				addCanMessageList(&rxList[i], node);
			}
		}
	}
	return status;
}

bool decodeNode(pb_istream_t *stream, const pb_field_iter_t *field, void **arg)
{
	AhsokaCAN_NodeDefinition message = AhsokaCAN_NodeDefinition_init_zero;
	uint8_t* name = NULL;
	message.name.funcs.decode = NULL;
	message.name.arg = name;
	message.comment.funcs.decode = NULL;
	uint8_t* comment = NULL;
	message.comment.arg = comment;
	message.ports.arg = NULL;
	message.ports.funcs.decode = decodeInt;

	deleteList(decodeList);
	bool status = pb_decode(stream, AhsokaCAN_NodeDefinition_fields, &message);

	if(status)
	{ 
        // create a new node, and set the transmitter node.
        nodeList_t* node = createNodeList();
        node->id = message.id;
        node->staticAddress = true;

        //iterate over ports node is available on
        while(decodeList != NULL)
		{
        	if(message.j1939_info.address_type == AhsokaCAN_NodeAddressType_STATIC)
			{
				node->address = message.j1939_info.address_value_one;
			}
        	else
        	{
        		node->staticAddress = false;
        	}

			// add node to list of nodes here!
			if(message.node_type == AhsokaCAN_NodeType_SELF)
			{
				// this is the transmitter node
				txNode[decodeList->id] = node;
			}
			else if(message.node_type == AhsokaCAN_NodeType_ANY)
			{
				// this is the broadcast node
				broadcastNode[decodeList->id] = node;
				broadcastAvailable[decodeList->id] = true;
				addNode(&rxNodes[decodeList->id], node);
			}
			else
			{
				// this is a receiver node.
				addNode(&rxNodes[decodeList->id], node);
			}

        	decodeList = decodeList->next;
		}
	}
	return status;
}

void decodeCANCalibration(void)
{
	AhsokaCAN_CanApplicationConfiguration msg = AhsokaCAN_CanApplicationConfiguration_init_zero;

    // need to decode ports before decoding calibration nodes.
	msg.can_port_configuration.message_configuration.ports.arg = NULL;
	msg.can_port_configuration.message_configuration.ports.funcs.decode = decodePort;

	uint8_t* localIpAddress = NULL;
	uint8_t* remoteIpAddress = NULL;
	msg.can_port_configuration.communication_configuration.local_ip_address.arg = localIpAddress;
	msg.can_port_configuration.communication_configuration.local_ip_address.funcs.decode = decodeString;
	msg.can_port_configuration.communication_configuration.remote_ip_address.arg = remoteIpAddress;
	msg.can_port_configuration.communication_configuration.remote_ip_address.funcs.decode = decodeString;

	uint8_t* buffer = (uint8_t*)0x800;
	pb_istream_t stream = pb_istream_from_buffer(buffer, (64*1024)-0x800);
	pb_decode(&stream, AhsokaCAN_CanApplicationConfiguration_fields, &msg);

	localIpAddress = msg.can_port_configuration.communication_configuration.local_ip_address.arg;
	remoteIpAddress = msg.can_port_configuration.communication_configuration.remote_ip_address.arg;

    // need to decode nodes before decoding calibration messages.
	msg.can_port_configuration.message_configuration.ports.funcs.decode = NULL;
	msg.can_port_configuration.message_configuration.nodes.arg = NULL;
	msg.can_port_configuration.message_configuration.nodes.funcs.decode = decodeNode;

	buffer = (uint8_t*)0x800;
	stream = pb_istream_from_buffer(buffer, (64*1024)-0x800);
	pb_decode(&stream, AhsokaCAN_CanApplicationConfiguration_fields, &msg);

	// status will be false here
	msg.can_port_configuration.message_configuration.nodes.funcs.decode = NULL;
	msg.can_port_configuration.message_configuration.messages.arg = NULL;
	msg.can_port_configuration.message_configuration.messages.funcs.decode = decodeMessage;

	buffer = (uint8_t*)0x800;
	stream = pb_istream_from_buffer(buffer, (64*1024)-0x800);
	pb_decode(&stream, AhsokaCAN_CanApplicationConfiguration_fields, &msg);

	nodeList_t* list = portList;
	while(list != NULL)
	{
		initCanHandler(list->id);
		list = list->next;
	}
	deleteList(portList);
}
