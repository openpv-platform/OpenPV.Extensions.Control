#include "nodeList.h"
#include <stdint.h>
#include <stdlib.h>
#include "openamp_log.h"

nodeList_t* txNode[MAX_PORTS] = {0};
nodeList_t* rxNodes[MAX_PORTS] = {0};
nodeList_t* broadcastNode[MAX_PORTS] = {0};
nodeList_t* decodeList = 0;
nodeList_t* portList = 0;
bool broadcastAvailable[MAX_PORTS] = {false, false};

nodeList_t* createNodeList(void)
{
    nodeList_t* node = malloc(sizeof(nodeList_t));
    if(node)
    {
        node->next = NULL;
        node->id = -1;
    }
    return node;
}

nodeList_t* findNode(uint32_t port, uint32_t id)
{
	nodeList_t* list = rxNodes[port];
    while(list != NULL && (id != list->id))
    {
        list = list->next;
    }

    return list;
}

nodeList_t* searchNode(uint32_t id)
{
	for(int i = 0; i < MAX_PORTS; i++)
	{
		nodeList_t* list = rxNodes[i];
		while(list != NULL)
		{
			if(id == list->id)
				return list;
			list = list->next;
		}
	}

    return NULL;
}

void addNode(nodeList_t** list, nodeList_t* node)
{
    // if list is empty this node starts the list.
	if(*list == NULL)
	{
		*list = node;
	}
	else
    {
		nodeList_t* tempList = *list;
		while((tempList)->next != NULL)
		{
			tempList = (tempList)->next;
		}

		(tempList)->next = node;
    }
}

void deleteList(nodeList_t* list)
{
	if(list == NULL)
		return;

	if(list->next != NULL)
		deleteList(list->next);

	free(list);
}
