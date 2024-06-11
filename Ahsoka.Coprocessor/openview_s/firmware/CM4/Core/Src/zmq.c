#include "zmq_message_list.h"
#include "main.h"
#include "cmsis_os.h"
#include "FreeRTOS.h"
#include "semphr.h"
#include "task.h"
#include <stdint.h>
#include "decodeCanMessages.h"
#include "openamp_log.h"
#include "canMessageHandler.h"

zmq_message_list_t * zmtp_channel_recv_multipart(zmtp_channel_t *self);
uint32_t zmq_send_multipart( );
void zmq_init(void);
void dealerSocketRxTask(void* argument);
zmtp_channel_t* sendChannel;

// globals
SemaphoreHandle_t zmq_tx_mutex;


// for send, create send multi-frame, take a list of multiframe messages, and just send until there are no more in the list.

// need to protect send with mutex, or create a sender thread, and a queue of lists.

// so flow is create tcp_endpoint and connect to it.  do listen on receiver.

// create send socket, and handle the output there.    

zmq_message_list_t * zmtp_channel_recv_multipart(zmtp_channel_t *self)
{
    zmq_message_list_t* list = zmq_new_message_list();
    zmtp_msg_t* frame;
    do
    {
        frame = zmtp_channel_recv(self);
        if(frame != NULL)
        {
        	// add frame to list
        	zmq_add_message_list(list, frame);
        }
        else
        {
        	return list;
        }
    }
    while(frame->flags & ZMTP_MORE_FLAG);
    
    return list;
}

// pass in a list of frames that compromise a single message, 
uint32_t zmq_send_multipart(zmtp_channel_t *self, zmq_message_list_t* list )
{
    xSemaphoreTake(zmq_tx_mutex, portMAX_DELAY);

    do
    {
        zmtp_channel_send(self, list->msg);
        list = zmq_message_list_get_next(list);
   }while(list != NULL);

    xSemaphoreGive(zmq_tx_mutex);

    return 0;
}

extern void decodeCANCalibration(void);
// zmq init
void zmq_init(void)
{
	decodeCANCalibration();
    // create necessary structures and OS primitives.
	zmq_tx_mutex = xSemaphoreCreateMutex();
    if(zmq_tx_mutex == 0)
    {

    }

    // create thread to handle the Rx traffic.  Received messages will get passed on to handler.

    osThreadAttr_t defaultTask_attributes = { .name = "RxTask",
                                              .stack_size = 512 * 4,
                                              .priority = (osPriority_t) osPriorityNormal,};

    osThreadNew(dealerSocketRxTask, NULL, &defaultTask_attributes);
}

// zmq thread
void dealerSocketRxTask(void* argument)
{
	// create and open dealer socket.
	while(1)
	{
		zmtp_channel_t* channel = zmtp_channel_new();
		sendChannel = zmtp_channel_new();

		if(zmtp_channel_listen(channel, "tcp://192.168.8.1:6000") == 0)
		{

		}
		if(zmtp_channel_listen (sendChannel, "tcp://192.168.8.1:5500") == 0)
		{

		}
		//sendCoprocessorReady();
		while(errno != ENOTCONN)
		{
			zmq_message_list_t* list = zmtp_channel_recv_multipart(channel);
			if ((list) && (list->msg->flags & ZMTP_COMMAND_FLAG) == 0)
				canMessageHandler(list);
		}
		// we have become disconnected, so destroy channels and recreate.
    	zmtp_channel_destroy(&channel);
    	zmtp_channel_destroy(&sendChannel);
	}
    return;
}
