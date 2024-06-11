#ifndef __NODELIST_H__
#define  __NODELIST_H__

#include "CanService.pb.h"
#include "CanConfiguration.pb.h"
#include "canHandler.h"
#include "nodeList.h"


// create a list of nodes, that aren't the transmit node.
typedef struct nodeList nodeList_t;
struct nodeList
{
    int32_t id;
    uint32_t address;
    nodeList_t* next;
};

extern nodeList_t* txNode[MAX_PORTS];
extern nodeList_t* rxNodes[MAX_PORTS];
extern nodeList_t* broadcastNode[MAX_PORTS];
extern bool broadcastAvailable[MAX_PORTS];

// list of ints used in configuration decoding
extern nodeList_t* decodeList;
extern nodeList_t* portList;

extern nodeList_t* createNodeList(void);
extern nodeList_t* findNode(uint32_t port, uint32_t id);
extern nodeList_t* searchNode(uint32_t id);
extern void addNode(nodeList_t** list, nodeList_t* node);
extern void deleteList(nodeList_t* list);

#endif

