#ifndef __CANHANDLER_H__
#define __CANHANDLER_H__

#include "CanService.pb.h"

#define TIMER_RESOLUTION  1
#define MAX_PORTS 2
#define PDU2_THRESHOLD 0xF00000

typedef void (*crcFunc)(uint32_t, uint8_t*, uint8_t, uint32_t);
// This structure is used by both transmit and receive messages.
typedef struct canMessage_type
{
    uint32_t id;

    uint32_t rate;
    uint32_t timeout;
    uint32_t rollCountLength; // set to 0 if there is no roll count.
    uint32_t rollCount;
    uint8_t data[8];        // populated when the send message comes in.
    crcFunc crc;              // set to NULL if there is no checksum
    uint8_t crcPos;         // set to 0 if there is no checksum.
    uint8_t msgType;
    uint8_t overrideSource;
    uint8_t overrideDestination;
    uint8_t dlc;
    uint8_t rollCountBitPos;  // set to 0 if there is no roll count
    uint32_t transmitNodeId;
    uint32_t receiverNodeId;
}canMessage_t;

typedef struct canMessageList canMessageList_t;
struct canMessageList
{
    canMessage_t* msg;
    canMessageList_t* next;
};

typedef struct canTimerMessageList canMessageTimerList_t;
struct canTimerMessageList
{
    canMessage_t* msg;
    uint32_t ticksLeft;
    uint32_t timeoutTicks;
    bool scheduled;
    canMessageTimerList_t* nextTimer;  // this struct is used for both the tx list and the timer list,
    canMessageTimerList_t* nextMsg;    // this allows us to not to have to traverse the whole list of timers, when we want to update 
                                       // the timeout timer.
};

// create a linked list of transmit messages, and a list of receive messages.
// each list will need to be protected by a mutex.
extern canMessageTimerList_t* txList[MAX_PORTS];
extern canMessageTimerList_t* timerList[MAX_PORTS];
extern canMessageList_t* rxList[MAX_PORTS];

// initialize mutexes and create timer thread. 
extern void initCanHandler(uint32_t port);
extern canMessageTimerList_t* createCanMessageTimer(void);
extern void rescheduleCanMessageTimer(canMessageTimerList_t** list, canMessageTimerList_t* node);
extern void scheduleCanMessageTimer(uint32_t port, canMessageTimerList_t* node);
extern canMessageTimerList_t* findCanTxMessage(uint32_t port, canMessageTimerList_t* list, uint32_t id, uint8_t msgType, uint8_t dlc, bool* delete);
extern void addCanMessageTxList(canMessageTimerList_t** list, canMessageTimerList_t* node);


extern canMessage_t* findCanMessage(uint32_t port, uint32_t id, bool isExtended, bool promiscuous, bool* delete);
extern void addCanMessageList(canMessageList_t** list, canMessageList_t* node);
extern canMessageList_t* createCanMessageList(void);
extern void updateCanMessageTimerValues( canMessageTimerList_t* node, AhsokaCAN_RecurringCanMessage message);

extern void setPromiscuousMode(uint32_t port, bool txMode, bool rxMode);
extern void setTransmittingJ1939(uint32_t port, bool transmitting);
extern void setCoprocessorReady(bool ready);

// checksum functions
extern void tsc1Checksum(uint32_t id, uint8_t* data, uint8_t length, uint32_t bit);
extern void checksum(uint32_t id, uint8_t* data, uint8_t length, uint32_t bit);
extern void crc8(uint32_t id, uint8_t* data, uint8_t length, uint32_t bit);

extern crcFunc getChecksumFunc(uint32_t type);


extern void sendCan(uint32_t port, canMessage_t* msg);

#endif

