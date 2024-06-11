#include "lwip.h"
#include "lwip/init.h"
#include "lwip/netif.h"
#include "lwip/tcpip.h"
#include "netif/slipif.h"
#include "main.h"
#include "cmsis_os.h"
#include "lwip/sio.h"
#include "virt_uart.h"
#include "FreeRTOS.h"
#include "stream_buffer.h"

// This file is the glue between the slipif and the virtual uart.  
extern VIRT_UART_HandleTypeDef huart0;
extern VIRT_UART_HandleTypeDef huart1;

extern StreamBufferHandle_t streamBuffer0;
extern StreamBufferHandle_t streamBuffer1;
#if 1
u32_t sys_now(void)
{
	//return xTaskGetTickCount();
    return HAL_GetTick();
}
u32_t sys_jiffies(void)
{
	return HAL_GetTick();
}
#endif
VIRT_UART_HandleTypeDef* getUartHandle(uint32_t uartNum)
{
	return ((uartNum == 1) ? &huart0 : &huart1);
}


uint32_t getUartNum(VIRT_UART_HandleTypeDef* handle)
{
	return ((handle == &huart0) ? 1:2);
}

sio_fd_t sio_open(u8_t devnum)
{
	uint32_t num = devnum;
	return (sio_fd_t)num+1;
}


void sio_send(u8_t c, sio_fd_t fd)
{
	//VIRT_UART_Transmit(getUartHandle((uint32_t)fd), &c, 1);
	xStreamBufferSend(streamBuffer1, &c, 1, portMAX_DELAY);
}

u32_t sio_read(sio_fd_t fd, u8_t *data, u32_t len)
{
	// this is a blocking read, should block until we read len number!
	uint32_t count = 0;
	uint32_t length = len;
	while(length)
	{
		count = xStreamBufferReceive(((int32_t)fd == 1 ? streamBuffer0 : streamBuffer1), data, length, portMAX_DELAY);
		length = length - count;
		data += count;

	}

	return len;
}
