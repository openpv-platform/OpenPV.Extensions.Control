#include "zmq_message_list.h"

// create a new message list
zmq_message_list_t* zmq_new_message_list(void)
{
    zmq_message_list_t* list;

    list = malloc(sizeof(zmq_message_list_t));
    if(list != NULL)
    {
        list->msg = NULL;
        list->next = NULL;
    }
    return list;
}

// add a message to the end of the list
int32_t zmq_add_message_list(zmq_message_list_t* list, zmtp_msg_t* frame)
{
    
    if(list->msg == NULL)
        list->msg = frame;
    else
    {
        while(list->next != NULL)
            list = list->next;
        // found end of list.
        zmq_message_list_t* node = zmq_new_message_list(); 
        if(node != NULL)
        {
            node->msg = frame;
            list->next = node;
        }
        else 
            return -1;
    }
    return 0;
}

// delete the message list
void zmq_message_list_delete(zmq_message_list_t* list)
{
	if(list == NULL)
	{
		return;
	}
    while(list->next != NULL)
    {
        // remove and delete top of message
        list = zmq_message_list_get_next(list);
    }
    // now check to see if we need to delete the last one
    if(list->msg != NULL)
    {
        // delete the msg
        zmtp_msg_destroy(&list->msg);
        free(list);
    }
}

// get message from the front of the list, set list to point to next
zmq_message_list_t* zmq_message_list_get_next(zmq_message_list_t* list)
{
   zmq_message_list_t* temp = list;
   list = list->next;
   // disconnect this node, so that only it gets deleted.
   temp->next = NULL;
   zmq_message_list_delete(temp);
   return list;
}


