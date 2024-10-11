#include "canHandler.h"
#include "cmsis_os.h"
#include "FreeRTOS.h"
#include "semphr.h"
#include "timers.h"
#include <stdint.h>
#include <stdlib.h>
#include <stdbool.h>
#include "openamp_log.h"
#include "canMessageHandler.h"
#include "nodeList.h"

canMessageList_t* rxList[MAX_PORTS] = {0};
canMessageTimerList_t* txList[MAX_PORTS] = {0};
canMessageTimerList_t* timerList[MAX_PORTS] = {0};

SemaphoreHandle_t rxListMutex[MAX_PORTS];
SemaphoreHandle_t timerListMutex[MAX_PORTS];

bool promiscuousTx[MAX_PORTS] = {false, false};
bool promiscuousRx[MAX_PORTS] = {false, false};
bool transmittingJ1939[MAX_PORTS] = {true, true};
bool coprocessorReady = false;

void txTimerTask(void* argument);

void rxCanTask(void* argument);
void timerCallback(TimerHandle_t xTimer);

extern FDCAN_HandleTypeDef hfdcan1;
extern FDCAN_HandleTypeDef hfdcan2;
extern uint32_t rxCanMessages;
extern SemaphoreHandle_t rxCanSem[2];
extern SemaphoreHandle_t txCanSem[2];
extern xTimerHandle txCanTimer[2];
extern nodeList_t* txNode[MAX_PORTS];
#define NUM_RX_MESSAGES 10

void canRxTask(void* argument)
{
	// there is an instance of this task for every can port.
	uint32_t port = (uint32_t)argument;
	uint32_t numMessages = 0;
	bool timeout = false;

	canMessageSimple_t* canRxMessages;
	canRxMessages = malloc(sizeof(canMessageSimple_t)* NUM_RX_MESSAGES);
	if(!canRxMessages)
	{
		log_info("no memory for can buffer\r\n");
		return;
	}
	log_info("can rx for port %d started!\r\n", (int)port);
	while(1)
	{
		// wait for a message to come in from the interrupt.
		if( xSemaphoreTake( rxCanSem[port], 10 ) == pdTRUE )  // wait 10 ticks.
		{
			FDCAN_HandleTypeDef *hfdcan = (port==1) ? &hfdcan2 : &hfdcan1;
			// read message from peripheral.
			FDCAN_RxHeaderTypeDef RxHeader;
			uint8_t RxData[8];
			// Receive FDCAN1

			if(HAL_FDCAN_GetRxMessage(hfdcan, FDCAN_RX_FIFO0, &RxHeader, RxData) == HAL_OK && coprocessorReady)
			{
				uint32_t id = 0;
				bool isExtended = true;

				// need to convert to correct format and send as message.
				id = RxHeader.Identifier;
				isExtended = (FDCAN_EXTENDED_ID == RxHeader.IdType) ? 1 : 0;

				// when we get a message, check to see if it is in our rx list
				bool delete = false;
				canMessage_t* foundMsg = findCanMessage(port, RxHeader.Identifier, isExtended, promiscuousRx[port], &delete);
				// if it is in our rx List, add it to the list.
				if(foundMsg)
				{
					if(!(foundMsg->msgType == AhsokaCAN_MessageType_J1939_EXTENDED_FRAME && !transmittingJ1939[port]))
					{
						// check here to see if data has changed, only send if the data has changed.
						//add message to the list.
						canRxMessages[numMessages].port = port;
						canRxMessages[numMessages].id = id & 0x1FFFFFFF;
						canRxMessages[numMessages].dlc = RxHeader.DataLength >> 16;
						canRxMessages[numMessages].msgType = foundMsg->msgType;
						memcpy(canRxMessages[numMessages].data, RxData, canRxMessages[numMessages].dlc);
						numMessages++;
					}

					if(delete)
					{
						free(foundMsg);
					}
				}
			}
		}
		else
			timeout = true;


		//wait for the timeout or for 10 messages.
		if(numMessages == NUM_RX_MESSAGES || timeout)
		{

			// send the messages.
			if(numMessages > 0)
				handleReceiveCanMessages(canRxMessages, numMessages);
			numMessages = 0;
			timeout = false;
		}
	}
}
void txTimerTask(void* argument)
{
    uint32_t port = (uint32_t)argument;

    log_info("tx can task started\r\n");
    xTimerStart(txCanTimer[port], portMAX_DELAY);

    while(1)
    {

        // delay for time period in ticks / ms.
        //osDelay(TIMER_RESOLUTION);
        // wait for timer to expire.
        xSemaphoreTake(txCanSem[port], portMAX_DELAY);
        xSemaphoreTake(timerListMutex[port], portMAX_DELAY);
        if(timerList[port])
        {
            // reduce timer, see if it is expiring.
            // if it expires, remove and see if there are any other timers that are expiring, send CAN message, reschedule timer.
        	timerList[port]->ticksLeft--;
        	timerList[port]->timeoutTicks--;
            while(timerList[port] && timerList[port]->ticksLeft == 0)
            {
                // send message here!

                sendCan(port, timerList[port]->msg);

                if(timerList[port]->timeoutTicks > 0)
                {
                    // reschedule timer
                    canMessageTimerList_t* listNode = timerList[port];
                    timerList[port] = listNode->nextTimer;
                    listNode->nextTimer = NULL;
                    rescheduleCanMessageTimer(&timerList[port], listNode);
                }
                else
                {
                    // the message has timed out, so don't reschedule.
                	(timerList[port])->scheduled = false;
                    timerList[port] = timerList[port]->nextTimer;
                    //log_info("don't reschedule\r\n");
                }
            }

        }
        else
        {
        	// no list!
        	//log_info("no list\r\n");
        }
        xSemaphoreGive(timerListMutex[port]);
    }
}

void initCanHandler(uint32_t port)
{
	static bool initPort[MAX_PORTS] = {false, false};
	if(!initPort[port])
	{
		initPort[port] = true;
		// create and initialize mutexes
		rxListMutex[port] = xSemaphoreCreateMutex();
		timerListMutex[port]= xSemaphoreCreateMutex();

		// create and start timer task.
		osThreadAttr_t defaultTask_attributes = { .name = "CanTimerTask",
		                                              .stack_size = (512+256) * 4,
		                                              .priority = (osPriority_t) osPriorityHigh,};

		osThreadNew(txTimerTask, (void*)port, &defaultTask_attributes);

		osThreadAttr_t rxTask_attributes = { .name = "CanRxTask",
				                            .stack_size = 512 * 4,
				                            .priority = (osPriority_t) osPriorityHigh,};

		osThreadNew(canRxTask, (void*)port, &rxTask_attributes);
	}
    return;
}

canMessageList_t* createCanMessageList(void)
{
    canMessage_t* msg = malloc(sizeof(canMessage_t));
    canMessageList_t* node = NULL;
    if(msg)
    {
        node = malloc(sizeof(canMessageList_t));
        if(node)
        {
            node->msg = msg;
            node->next = NULL;
        }            
        else
        {
            free(msg);
        }
    } 
    else
    {
        free(msg);
    }
    return node;
}

canMessageTimerList_t* createCanMessageTimer(void)
{
    canMessageTimerList_t* newListNode = NULL;
    canMessage_t* msg = malloc(sizeof(canMessage_t));
    if(msg)
    {
        memset(msg, 0, sizeof(canMessage_t));
        // malloc space for the node;
        newListNode = malloc(sizeof(canMessageTimerList_t));
        if(newListNode)
        {
            // initialize every thing.
            newListNode->msg = msg;
            newListNode->ticksLeft = 0;
            newListNode->timeoutTicks = 0;
            newListNode->nextTimer = NULL;
            newListNode->nextMsg = NULL;
            newListNode->scheduled = false;
        }
        else
        {
            // going to return NULL, free the memory for the message.
            free(msg);
        }
    } 
    return newListNode;
}
void rescheduleCanMessageTimer(canMessageTimerList_t** list, canMessageTimerList_t* node)
{
	// this function is used from the timer thread, so don't need to take semaphores

	    node->ticksLeft = node->msg->rate / TIMER_RESOLUTION;
	    node->scheduled = true;

	    if (*list == NULL)
	    {
	        // list is empty, easy
	        *list = node;
	    }
	    else
	    {
	        canMessageTimerList_t* temp = *list;  // start walking down the list.
	        canMessageTimerList_t* prev = NULL;
	        bool insert = false;
	        while (temp)
	        {
	            if (node->ticksLeft <= temp->ticksLeft)
	            {
	                temp->ticksLeft -= node->ticksLeft;
	                temp->timeoutTicks -= node->ticksLeft;
	                // insert the timer in front of temp
	                if (prev == NULL)
	                {
	                    // at front of list.
	                    node->nextTimer = *list;
	                    *list = node;


	                }
	                else
	                {
	                    // insert the node.
	                    prev->nextTimer = node;
	                    node->nextTimer = temp;
	                }
	                insert = true;
	                break; // found the insertion point.
	            }
	            else
	            {
	                node->ticksLeft -= temp->ticksLeft;
	                node->timeoutTicks -= temp->ticksLeft;
	            }
	            prev = temp;
	            temp = temp->nextTimer;
	        }
	        if (!insert)
	        {
	            prev->nextTimer = node; // put at end of list.
	        }
	    }
}
void updateCanMessageTimerValues( canMessageTimerList_t* node, AhsokaCAN_RecurringCanMessage message)
{
	xSemaphoreTake(timerListMutex[message.can_port], portMAX_DELAY);
	node->msg->rate = message.transmit_interval_in_ms;
	node->msg->timeout = message.timeout_before_update_in_ms;
	memcpy(node->msg->data, message.message.data.arg, node->msg->dlc);
	xSemaphoreGive(timerListMutex[message.can_port]);
	free(message.message.data.arg);
}
void scheduleCanMessageTimer(uint32_t port, canMessageTimerList_t* node)
{
	// this message is only called if the findCanTxMessage is successful, if it isn't successful, there is no reason to schedule.
    xSemaphoreTake(timerListMutex[port], portMAX_DELAY);

    node->timeoutTicks = node->msg->timeout / TIMER_RESOLUTION;
    if(!node->scheduled)
    {
        rescheduleCanMessageTimer(&timerList[port], node);
    }
    xSemaphoreGive(timerListMutex[port]);


    return;
}

canMessageTimerList_t* findCanTxMessage(uint32_t port, canMessageTimerList_t* list, uint32_t id, uint8_t msgType, bool* delete)
{
	canMessageTimerList_t* foundNode = NULL;

	if(promiscuousTx[port])
	{
		foundNode = createCanMessageTimer();
		foundNode->msg->id = id;
		foundNode->msg->msgType = AhsokaCAN_MessageType_RAW_EXTENDED_FRAME;
		foundNode->msg->overrideDestination = 1;
		foundNode->msg->overrideSource = 1;
		foundNode->msg->crc = 0;
		foundNode->msg->dlc = 8;
		foundNode->msg->rollCountLength = 0;
		*delete = true;
		return foundNode;
	}

	canMessageTimerList_t* listStart = list;

	while(list != NULL)
	{
		if( ((list->msg->id & 0x1FFFFFFF) == (id & 0x1FFFFFFF)) && (list->msg->msgType != AhsokaCAN_MessageType_J1939_EXTENDED_FRAME) )
		{
			foundNode = list;
			foundNode->msg->id = id;
			return foundNode;
		}
		list = list->nextMsg;
	}

    list = listStart;
	while(list != NULL)
	{
		int32_t mask = (list->msg->id & 0x00FF0000) >= PDU2_THRESHOLD ? 0x3FFFF00 : 0x3FF0000;
		if( ((list->msg->id & mask) == (id & mask)) && (list->msg->msgType == AhsokaCAN_MessageType_J1939_EXTENDED_FRAME) )
		{
			bool knownDestination = findNode(port, list->msg->receiverNodeId)->address != -1;

			bool available = true;
			if(list->msg->overrideDestination)
			{
				if( ((list->msg->id & 0x00FF0000) < PDU2_THRESHOLD) && !knownDestination )
					available &= false;
			}

			if(available)
			{
				foundNode = list;
				foundNode->msg->id = id;
				return foundNode;
			}
		}
		list = list->nextMsg;
	}

    return foundNode; 
}
// called when parsing config
void addCanMessageTxList(canMessageTimerList_t** list, canMessageTimerList_t* node)
{
	//canMessageTimerList_t* list = *list_in;
    // find the end of the list
    if(*list == NULL)
    {
    	// list is empty
        *list = node;
    }
    else
    {
    	canMessageTimerList_t* tempList = *list;

        while((tempList)->nextMsg != NULL)
        {
        	tempList = (tempList)->nextMsg;
        }

        (tempList)->nextMsg = node;
    }
    return;
}

canMessage_t* findCanMessage(uint32_t port, uint32_t id, bool isExtended, bool promiscuous, bool* delete)
{
    canMessage_t* foundMsg = NULL;
    canMessageList_t* list = rxList[port];

    if(promiscuous)
	{
		foundMsg = malloc(sizeof(canMessage_t));
		foundMsg->msgType = isExtended ? AhsokaCAN_MessageType_RAW_EXTENDED_FRAME : AhsokaCAN_MessageType_RAW_STANDARD_FRAME;
		*delete = true;
		return foundMsg;
	}

    canMessageTimerList_t* listStart = list;

	while(list != NULL)
	{
		if( ((list->msg->id & 0x1FFFFFFF) == (id & 0x1FFFFFFF)) && (list->msg->msgType != AhsokaCAN_MessageType_J1939_EXTENDED_FRAME) )
		{
			foundMsg = list->msg;
			return foundMsg;
		}
		list = list->next;
	}

	list = listStart;
	while(list != NULL)
	{
		int32_t mask = (list->msg->id & 0x00FF0000) >= PDU2_THRESHOLD ? 0x3FFFF00 : 0x3FF0000;
		if( ((list->msg->id & mask) == (id & mask)) && isExtended && (list->msg->msgType == AhsokaCAN_MessageType_J1939_EXTENDED_FRAME) )
		{
    		int32_t source = id & 0xFF;
    		int32_t destination = (id >> 8) & 0xFF;

    		bool available = true;
    		if(list->msg->overrideDestination)
    		{
    			if( ((list->msg->id & 0x00FF0000) < PDU2_THRESHOLD) && !(destination == txNode[port]->address || list->msg->receiverNodeId == 255) )
					available &= false;
    		}

    		if(list->msg->overrideSource)
    		{
    			if(!(source == findNode(port, list->msg->transmitNodeId)->address || list->msg->transmitNodeId == 255))
    				available &= false;
    		}

			if(available)
			{
				foundMsg = list->msg;
				return foundMsg;
			}
		}
		list = list->next;
	}
    return foundMsg; 
}

//called when parsing config
void addCanMessageList(canMessageList_t** list, canMessageList_t* node)
{
    // find the end of the list
    if(*list == NULL)
    {
        // list is empty
        *list = node;
    }
    else
    {
    	canMessageList_t* temp = *list;

        while(temp->next != NULL)
            temp = temp->next;
        temp->next = node;
    }
    return;
}

void setPromiscuousMode(uint32_t port, bool txMode, bool rxMode)
{
	promiscuousTx[port] = txMode;
	promiscuousRx[port] = rxMode;
}

void setTransmittingJ1939(uint32_t port, bool transmitting)
{
	transmittingJ1939[port] = transmitting;
}

void setCoprocessorReady(bool ready)
{
	coprocessorReady = ready;
}

void sendCan(uint32_t port, canMessage_t* msg)
{
	if(msg->msgType == AhsokaCAN_MessageType_J1939_EXTENDED_FRAME && !transmittingJ1939[port])
		return;

	FDCAN_TxHeaderTypeDef header;
    header.DataLength = (msg->dlc << 16);


    if(msg->msgType == AhsokaCAN_MessageType_J1939_EXTENDED_FRAME)
    {
    	if(msg->overrideSource == 1)
    	    msg->id |= txNode[port]->address;

		if(msg->overrideDestination == 1 && (( msg->id & 0x00FF0000) < PDU2_THRESHOLD))
			 msg->id |= findNode(port, msg->receiverNodeId)->address << 8;
    }
    header.Identifier = msg->id;

    header.IdType = (msg->msgType != AhsokaCAN_MessageType_RAW_STANDARD_FRAME) ? FDCAN_EXTENDED_ID : FDCAN_STANDARD_ID;
    header.TxFrameType = FDCAN_DATA_FRAME;
    header.FDFormat = FDCAN_CLASSIC_CAN;
    header.BitRateSwitch = FDCAN_BRS_OFF;
	header.ErrorStateIndicator = FDCAN_ESI_ACTIVE;
	header.MessageMarker = 1;
	header.TxEventFifoControl = FDCAN_NO_TX_EVENTS;

	FDCAN_HandleTypeDef *canHandle;
	if(port == 1)
		canHandle = &hfdcan2;
	else
		canHandle = &hfdcan1;

	if(msg->rollCountLength > 0)
	{
		uint64_t mask __attribute__((aligned(8))) = (1 << msg->rollCountLength) - 1;
		msg->rollCount &= mask;
		mask = mask << msg->rollCountBitPos;
		uint64_t* ptr __attribute__((aligned(8))) = (uint64_t*)msg->data;
		*ptr = *ptr & ~mask;
		uint64_t temp __attribute__((aligned(8))) = msg->rollCount;
		*ptr = *ptr | (temp << msg->rollCountBitPos);
		msg->rollCount++;
	}
	if(msg->crc)
	{
		// do crc calculation.
		// since CRC function knows length of CRC, it will do the insertion at the bit location.

		(msg->crc)(msg->id, msg->data, msg->dlc, msg->crcPos);
		//tsc1Checksum(msg->id, msg->data, msg->dlc, msg->crcPos);
	}
	HAL_FDCAN_AddMessageToTxFifoQ(canHandle, &header, msg->data);
    return;
}

// might want to move these to their own functions
void tsc1Checksum(uint32_t id, uint8_t* data, uint8_t length, uint32_t bit)
{
	uint32_t sum = 0;

	for(int i = 0; i < 7; i++)
	{
		sum += data[i];
	}

	sum += (data[7] & 0x0f);
	sum += (unsigned char)(id & 0x000000FF);
	sum += (unsigned char)((id >> 8) & 0x000000FF);
	sum += (unsigned char)((id >> 16) & 0x000000FF);
	sum += (unsigned char)((id >> 24) & 0x000000FF);
	sum = (((sum >> 6) & 0x03) + (sum >>3) +  sum) & 0x07;
#if 0
	uint64_t* ptr = (uint64_t*)data;
	*ptr &= 0xfffffffffffffffUL;
	*ptr |= (((uint64_t)sum) << 60);
#else
	data[7] &= 0x0f;
	data[7] |= (sum << 4);
#endif
}
void checksum(uint32_t id, uint8_t* data, uint8_t length, uint32_t bit)
{
	// assume length of 8 bits
	uint32_t mask  = (1 << bit) - 1;
	uint32_t sum = 0;

	for(int i = 0; i < length-1; ++i)
	{
		sum += data[i];
	}
	uint32_t index;
	uint32_t rem;
	index = bit/8;
	rem = bit - (index * 8);
	data[index] &= ~(mask << rem);
	data[index] |= (sum << rem);
}

crcFunc getChecksumFunc(uint32_t type)
{
	switch(type)
	{
		case AhsokaCAN_CrcType_CheckSum:
			return &checksum;
			break;
		case AhsokaCAN_CrcType_TSC1:
			return &tsc1Checksum;
			break;
		default:
			return NULL;
			break;

	}
}


void timerCallback(xTimerHandle xTimer)
{
    void* timerId;
    uint32_t timerIndex;
    // get the timer index from handle,
    timerId = pvTimerGetTimerID(xTimer);
    timerIndex = (uint32_t)timerId;
    // verhoog the semaphore
    if(xSemaphoreGive(txCanSem[timerIndex]) != pdPASS)
        log_info("semaphore failed to give\n");
}
