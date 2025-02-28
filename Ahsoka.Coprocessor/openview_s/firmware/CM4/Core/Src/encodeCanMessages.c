#include <stdint.h>
#include <stdbool.h>
#include "openamp_log.h"
#include "pb_encode.h"
#include "CanService.pb.h"
#include "Services.pb.h"
#include "encodeCanMessages.h"


uint32_t encodeCanMessageType(AhsokaCAN_CanMessageTypes_Ids type, uint8_t *data, uint32_t max_length)
{
	uint32_t size = 0;
	AhsokaServiceFramework_AhsokaMessageHeader message = AhsokaServiceFramework_AhsokaMessageHeader_init_default;


	message.client_id.arg = NULL;
	message.client_id.funcs.encode = NULL;
	message.client_message_id = 0;
	message.transport_id = type;

    pb_ostream_t sizeStream = {0};

	bool status = pb_encode(&sizeStream, AhsokaServiceFramework_AhsokaMessageHeader_fields, &message);

    // check to see if encoded size will fit.

    if(status && (sizeStream.bytes_written <= max_length))
    {
        // create stream from buffer
        pb_ostream_t stream = pb_ostream_from_buffer(data, sizeStream.bytes_written);
        status = pb_encode(&stream, AhsokaServiceFramework_AhsokaMessageHeader_fields, &message);
        if(!status)
        {
            log_info("encoding failed\r\n");
            size = 0;
        }
        else
        {
        	size = stream.bytes_written;
        }
    } 
    return size;
}

static bool write_data(pb_ostream_t *stream, const pb_field_iter_t *field, void * const *arg)
{
	canMessageData_t* canData = *arg;

    if (!pb_encode_tag_for_field(stream, field))
        return false;

    return pb_encode_string(stream, (uint8_t*)canData->data, canData->length);
}

static bool encodeCanMessage(pb_ostream_t *stream, const pb_field_iter_t *field, void * const *arg)
{
	AhsokaCAN_CanMessageData message = AhsokaCAN_CanMessageData_init_default;
	canMessageSimpleCollection_t* collection = *arg;
	canMessageSimple_t* canMessage = collection->ptr;
	uint32_t numCanMessages = collection->count;

	while(numCanMessages)
	{
		//if(canMessage->msgType)
		//	canMessage->id |= 0x80000000;
		message.dlc = canMessage->dlc;
		message.id = canMessage->id;
		message.data.funcs.encode = write_data;
		canMessageData_t canData;
		canData.data = canMessage->data;
		canData.length = canMessage->dlc;
		message.data.arg = &canData;
		// do the encode of field tag

		if (!pb_encode_tag_for_field(stream, field))
		        return false;

		// encode the data, this might need to be a submessage encode?
		if(!pb_encode_submessage(stream, AhsokaCAN_CanMessageData_fields, &message))
		{
			return false;
		}

		numCanMessages--;
		canMessage++;
	}


	return true;
}
// this will encode an array of simple can messages (canMessageSimple_t), the number of messages is passed in numMessages.

uint32_t encodeCanMessageCollection(canMessageSimple_t* canMessages, uint32_t numMessages, uint8_t *data, uint32_t max_length)
{
	uint32_t size = 0;

	AhsokaCAN_CanMessageDataCollection message = AhsokaCAN_CanMessageDataCollection_init_default;
	message.can_port = canMessages->port;
	message.messages.funcs.encode = encodeCanMessage;
	canMessageSimpleCollection_t collection;
	collection.ptr = canMessages;
	collection.count = numMessages;

	message.messages.arg = &collection;


	pb_ostream_t sizeStream = {0};
	bool status = pb_encode(&sizeStream, AhsokaCAN_CanMessageDataCollection_fields, &message);

	if((status) && (sizeStream.bytes_written <= max_length))
	{
		pb_ostream_t stream = pb_ostream_from_buffer(data, sizeStream.bytes_written);
		status = pb_encode(&stream, AhsokaCAN_CanMessageDataCollection_fields, &message);

		if(!status)
		{
			log_info("encode error\r\n");
			size = 0;
		}
		else
		{
			size = stream.bytes_written;
		}
	}

	return size;
}
