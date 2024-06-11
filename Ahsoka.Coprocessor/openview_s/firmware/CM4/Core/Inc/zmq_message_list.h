#include <stdint.h>
#include "zmtp_classes.h"

#ifndef __ZMQ_MESSAGE_LIST_H
#define __ZMQ_MESSAGE_LIST_H

// message list structure.
typedef struct zmq_message_list zmq_message_list_t;
struct zmq_message_list
{
    zmtp_msg_t* msg;
    zmq_message_list_t* next;
};
// function prototypes to manipulate lists

zmq_message_list_t* zmq_new_message_list(void);

int32_t zmq_add_message_list(zmq_message_list_t* list, zmtp_msg_t* frame);
void zmq_message_list_delete(zmq_message_list_t* list);

zmq_message_list_t* zmq_message_list_get_next(zmq_message_list_t* list);


#endif
