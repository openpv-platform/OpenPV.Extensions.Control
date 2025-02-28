/* USER CODE BEGIN Header */
/**
 ******************************************************************************
 * @file           : main.c
 * @brief          : Main program body
 ******************************************************************************
 * @attention
 *
 * Copyright (c) 2023 STMicroelectronics.
 * All rights reserved.
 *
 * This software is licensed under terms that can be found in the LICENSE file
 * in the root directory of this software component.
 * If no LICENSE file comes with this software, it is provided AS-IS.
 *
 ******************************************************************************
 */
/* USER CODE END Header */
/* Includes ------------------------------------------------------------------*/
#include "main.h"
#include "cmsis_os.h"
#include "openamp.h"
#include "resmgr_utility.h"
#include "openamp_log.h"
#include "virt_uart.h"
#include "FreeRTOS.h"
#include "timers.h"
#include "stream_buffer.h"
#include "canHandler.h"

/* Private includes ----------------------------------------------------------*/
/* USER CODE BEGIN Includes */

/* USER CODE END Includes */

/* Private typedef -----------------------------------------------------------*/
/* USER CODE BEGIN PTD */

/* USER CODE END PTD */

/* Private define ------------------------------------------------------------*/
/* USER CODE BEGIN PD */
#define MAX_BUFFER_SIZE 512
/* USER CODE END PD */

/* Private macro -------------------------------------------------------------*/
/* USER CODE BEGIN PM */

/* USER CODE END PM */

/* Private variables ---------------------------------------------------------*/
FDCAN_HandleTypeDef hfdcan1;
FDCAN_HandleTypeDef hfdcan2;

IPCC_HandleTypeDef hipcc;

/* Definitions for defaultTask */
osThreadId_t defaultTaskHandle;
const osThreadAttr_t defaultTask_attributes = { .name = "defaultTask",
		.stack_size = 128 * 4, .priority = (osPriority_t) osPriorityNormal, };
/* USER CODE BEGIN PV */
osThreadId_t t0_TaskHandle;

VIRT_UART_HandleTypeDef huart0;
VIRT_UART_HandleTypeDef huart1;

volatile uint32_t intCounter = 0;

uint16_t VirtUart0ChannelRxSize = 0;
uint16_t VirtUart1ChannelRxSize = 0;
SemaphoreHandle_t rxCanSem[2];
SemaphoreHandle_t txCanSem[2];
TimerHandle_t txCanTimer[2];
extern void timerCallback(xTimerHandle);
extern void hdlc_init(void);

/* USER CODE END PV */

/* Private function prototypes -----------------------------------------------*/
void SystemClock_Config(void);
void PeriphCommonClock_Config(void);
static void MX_GPIO_Init(void);
static void MX_IPCC_Init(void);
static void MX_FDCAN1_Init(void);
static void MX_FDCAN2_Init(void);
int MX_OPENAMP_Init(int RPMsgRole, rpmsg_ns_bind_cb ns_bind_cb);
void StartDefaultTask(void *argument);
void t0Task(void *argument);
void VIRT_UART0_RxCpltCallback(VIRT_UART_HandleTypeDef *huart);
void VIRT_UART1_RxCpltCallback(VIRT_UART_HandleTypeDef *huart);
/* USER CODE BEGIN PFP */

/* USER CODE END PFP */

/* Private user code ---------------------------------------------------------*/
/* USER CODE BEGIN 0 */
#define CAN_ENABLE_GPIO_PORT                   GPIOA
#define CAN_ENABLE_GPIO_CLK_ENABLE()           __HAL_RCC_GPIOA_CLK_ENABLE()
#define CAN_ENABLE_GPIO_CLK_DISABLE()          __HAL_RCC_GPIOA_CLK_DISABLE()
#define CAN_ENABLE_PIN                         GPIO_PIN_13

uint8_t data_buffer[MAX_BUFFER_SIZE];

StreamBufferHandle_t streamBuffer0;
StreamBufferHandle_t streamBuffer1;


/* USER CODE END 0 */

/**
 * @brief  The application entry point.
 * @retval int
 */
int main(void)
{
	/* MCU Configuration--------------------------------------------------------*/



    if(NVIC_GetPendingIRQ(TIM6_IRQn))
    {
        TIM_HandleTypeDef tim;
        tim.Instance = TIM6;
        __HAL_TIM_CLEAR_IT(&tim, TIM_IT_UPDATE);
        __HAL_TIM_DISABLE_IT(&tim, TIM_IT_UPDATE);
        __HAL_TIM_DISABLE(&tim);
        NVIC_ClearPendingIRQ(TIM6_IRQn);
    }
    /* Reset of all peripherals, Initializes the Flash interface and the Systick. */
	if(HAL_Init()!= HAL_OK)
	{
		// put debug statements here
	}
	else
	{

	}


	/* IPCC initialization */
	MX_IPCC_Init();
	/* OpenAmp initialization ---------------------------------*/
	MX_OPENAMP_Init(RPMSG_REMOTE, NULL);

	log_info("Cortex-M4 boot successful with STM32Cube FW version: v%ld.%ld.%ld \r\n",
			((HAL_GetHalVersion() >> 24) & 0x000000FF),
			((HAL_GetHalVersion() >> 16) & 0x000000FF),
			((HAL_GetHalVersion() >> 8) & 0x000000FF));

	/* Resource Manager Utility initialisation ---------------------------------*/

	//MX_RESMGR_UTILITY_Init();

	/* USER CODE BEGIN SysInit */

	/* USER CODE END SysInit */

	/* Initialize all configured peripherals */

	MX_GPIO_Init();
	MX_FDCAN1_Init();
	MX_FDCAN2_Init();

	/* USER CODE BEGIN 2 */
	if (VIRT_UART_Init(&huart0) != VIRT_UART_OK) {
		log_err("VIRT_UART_Init UART0 failed.\r\n");
		Error_Handler();
	}

	if (VIRT_UART_Init(&huart1) != VIRT_UART_OK) {
		log_err("VIRT_UART_Init UART1 failed.\r\n");
		Error_Handler();
	}
	if (VIRT_UART_RegisterCallback(&huart0, VIRT_UART_RXCPLT_CB_ID,
			VIRT_UART0_RxCpltCallback) != VIRT_UART_OK) {
		Error_Handler();
	}
	if (VIRT_UART_RegisterCallback(&huart1, VIRT_UART_RXCPLT_CB_ID,
			VIRT_UART1_RxCpltCallback) != VIRT_UART_OK) {
		Error_Handler();
	}


	//MX_LWIP_Init();

	/* USER CODE END 2 */

	/* Init scheduler */
	osKernelInitialize();

	/* USER CODE BEGIN RTOS_MUTEX */
	/* add mutexes, ... */
	/* USER CODE END RTOS_MUTEX */

	/* USER CODE BEGIN RTOS_SEMAPHORES */
	/* add semaphores, ... */
	/* USER CODE END RTOS_SEMAPHORES */

	/* USER CODE BEGIN RTOS_TIMERS */
	/* start timers, add new ones, ... */
	/* USER CODE END RTOS_TIMERS */

	/* USER CODE BEGIN RTOS_QUEUES */

	// create stream buffers for virtual uarts


	streamBuffer0 = xStreamBufferCreate(1024,1);
	streamBuffer1 = xStreamBufferCreate(1024,1);
	/* USER CODE END RTOS_QUEUES */

	/* Create the thread(s) */
	/* creation of defaultTask */
	defaultTaskHandle = osThreadNew(StartDefaultTask, NULL,
			&defaultTask_attributes);


	// create a counting semaphore for each port.


	rxCanSem[0] = xSemaphoreCreateCounting( 32, 0 );
	rxCanSem[1] = xSemaphoreCreateCounting( 32, 0 );
    txCanSem[0] = xSemaphoreCreateCounting( 32, 0 );
    txCanSem[1] = xSemaphoreCreateCounting( 32, 0 );

    txCanTimer[0] = xTimerCreate ("tim0", pdMS_TO_TICKS(TIMER_RESOLUTION), pdTRUE, (void*) 0, timerCallback );
    //txCanTimer[0] = xTimerCreate ("tim0", 5, pdTRUE, (void*) 0, timerCallback );
    // set timer id to timer index
    vTimerSetTimerID(txCanTimer[0], (void *) 0);
    txCanTimer[1] = xTimerCreate ("tim1", pdMS_TO_TICKS(TIMER_RESOLUTION), pdTRUE, (void*) 0, timerCallback );
    //txCanTimer[1] = xTimerCreate ("tim1", 5, pdTRUE, (void*) 0, timerCallback );
    vTimerSetTimerID(txCanTimer[1], (void *) 1);
	FDCAN_FilterTypeDef sFilterConfig;
	/* Configure Rx filter */
	sFilterConfig.IdType = FDCAN_STANDARD_ID|FDCAN_EXTENDED_ID;
	sFilterConfig.FilterIndex = 0;
	sFilterConfig.FilterType = FDCAN_FILTER_MASK;
	sFilterConfig.FilterConfig = FDCAN_FILTER_TO_RXFIFO0;
	sFilterConfig.FilterID1 = 0x1FFFFFFF;
	sFilterConfig.FilterID2 = 0x1FFFFFFF;


	// Start FDCAN1
	HAL_FDCAN_MspInit(&hfdcan1);
	HAL_FDCAN_ConfigFilter(&hfdcan1, &sFilterConfig);
	HAL_FDCAN_Start(&hfdcan1);

	// add new message callback.
	HAL_FDCAN_ActivateNotification(&hfdcan1, FDCAN_IT_RX_FIFO0_NEW_MESSAGE, 0);
	// Not Yet
	//HAL_FDCAN_ActivateNotification(&hfdcan1, FDCAN_IT_BUS_OFF, 0);

	// Init FDCAN2
	HAL_FDCAN_MspInit(&hfdcan2);
	HAL_FDCAN_ConfigFilter(&hfdcan2, &sFilterConfig);
	// Start FDCAN2
	HAL_FDCAN_Start(&hfdcan2);
	HAL_FDCAN_ActivateNotification(&hfdcan2, FDCAN_IT_RX_FIFO0_NEW_MESSAGE, 0);
	// Not Yet TODO
	//HAL_FDCAN_ActivateNotification(&hfdcan2, FDCAN_IT_BUS_OFF, 0);


	// enable the CAN Transceiver.
    HAL_GPIO_WritePin(CAN_ENABLE_GPIO_PORT, CAN_ENABLE_PIN, GPIO_PIN_RESET);

    /* USER CODE BEGIN RTOS_THREADS */
	/* add threads, ... */
    hdlc_init();
	/* USER CODE END RTOS_THREADS */

	/* USER CODE BEGIN RTOS_EVENTS */
	/* add events, ... */
	/* USER CODE END RTOS_EVENTS */

	/* Start scheduler */
	osKernelStart();

	/* We should never get here as control is now taken by the scheduler */
	/* Infinite loop */
	/* USER CODE BEGIN WHILE */
	while (1) {
		/* USER CODE END WHILE */

		/* USER CODE BEGIN 3 */
	}
	/* USER CODE END 3 */
}

/**
 * @brief System Clock Configuration
 * @retval None
 */
void SystemClock_Config(void) {
	RCC_OscInitTypeDef RCC_OscInitStruct = { 0 };
	RCC_ClkInitTypeDef RCC_ClkInitStruct = { 0 };

	/** Initializes the RCC Oscillators according to the specified parameters
	 * in the RCC_OscInitTypeDef structure.
	 */
	RCC_OscInitStruct.OscillatorType = RCC_OSCILLATORTYPE_CSI
			| RCC_OSCILLATORTYPE_HSI | RCC_OSCILLATORTYPE_LSI
			| RCC_OSCILLATORTYPE_HSE;
	RCC_OscInitStruct.HSEState = RCC_HSE_ON;
	RCC_OscInitStruct.HSIState = RCC_HSI_ON;
	RCC_OscInitStruct.HSIDivValue = RCC_HSI_DIV1;
	RCC_OscInitStruct.LSIState = RCC_LSI_ON;
	RCC_OscInitStruct.CSIState = RCC_CSI_ON;
	RCC_OscInitStruct.PLL.PLLState = RCC_PLL_NONE;
	RCC_OscInitStruct.PLL2.PLLState = RCC_PLL_ON;
	RCC_OscInitStruct.PLL2.PLLSource = RCC_PLL12SOURCE_HSE;
	RCC_OscInitStruct.PLL2.PLLM = 3;
	RCC_OscInitStruct.PLL2.PLLN = 66;
	RCC_OscInitStruct.PLL2.PLLP = 2;
	RCC_OscInitStruct.PLL2.PLLQ = 1;
	RCC_OscInitStruct.PLL2.PLLR = 1;
	RCC_OscInitStruct.PLL2.PLLFRACV = 5120;
	RCC_OscInitStruct.PLL2.PLLMODE = RCC_PLL_FRACTIONAL;
	RCC_OscInitStruct.PLL3.PLLState = RCC_PLL_ON;
	RCC_OscInitStruct.PLL3.PLLSource = RCC_PLL3SOURCE_HSE;
	RCC_OscInitStruct.PLL3.PLLM = 2;
	RCC_OscInitStruct.PLL3.PLLN = 34;
	RCC_OscInitStruct.PLL3.PLLP = 2;
	RCC_OscInitStruct.PLL3.PLLQ = 17;
	RCC_OscInitStruct.PLL3.PLLR = 37;
	RCC_OscInitStruct.PLL3.PLLRGE = RCC_PLL3IFRANGE_1;
	RCC_OscInitStruct.PLL3.PLLFRACV = 6660;
	RCC_OscInitStruct.PLL3.PLLMODE = RCC_PLL_FRACTIONAL;
	RCC_OscInitStruct.PLL4.PLLState = RCC_PLL_ON;
	RCC_OscInitStruct.PLL4.PLLSource = RCC_PLL4SOURCE_HSE;
	RCC_OscInitStruct.PLL4.PLLM = 4;
	RCC_OscInitStruct.PLL4.PLLN = 99;
	RCC_OscInitStruct.PLL4.PLLP = 6;
	RCC_OscInitStruct.PLL4.PLLQ = 8;
	RCC_OscInitStruct.PLL4.PLLR = 8;
	RCC_OscInitStruct.PLL4.PLLRGE = RCC_PLL4IFRANGE_0;
	RCC_OscInitStruct.PLL4.PLLFRACV = 0;
	RCC_OscInitStruct.PLL4.PLLMODE = RCC_PLL_INTEGER;
	if (HAL_RCC_OscConfig(&RCC_OscInitStruct) != HAL_OK) {
		Error_Handler();
	}

	/** RCC Clock Config
	 */
	RCC_ClkInitStruct.ClockType = RCC_CLOCKTYPE_HCLK | RCC_CLOCKTYPE_ACLK
			| RCC_CLOCKTYPE_PCLK1 | RCC_CLOCKTYPE_PCLK2 | RCC_CLOCKTYPE_PCLK3
			| RCC_CLOCKTYPE_PCLK4 | RCC_CLOCKTYPE_PCLK5;
	RCC_ClkInitStruct.AXISSInit.AXI_Clock = RCC_AXISSOURCE_PLL2;
	RCC_ClkInitStruct.AXISSInit.AXI_Div = RCC_AXI_DIV1;
	RCC_ClkInitStruct.MCUInit.MCU_Clock = RCC_MCUSSOURCE_PLL3;
	RCC_ClkInitStruct.MCUInit.MCU_Div = RCC_MCU_DIV1;
	RCC_ClkInitStruct.APB4_Div = RCC_APB4_DIV2;
	RCC_ClkInitStruct.APB5_Div = RCC_APB5_DIV4;
	RCC_ClkInitStruct.APB1_Div = RCC_APB1_DIV2;
	RCC_ClkInitStruct.APB2_Div = RCC_APB2_DIV2;
	RCC_ClkInitStruct.APB3_Div = RCC_APB3_DIV2;

	if (HAL_RCC_ClockConfig(&RCC_ClkInitStruct) != HAL_OK) {
		Error_Handler();
	}

	/** Set the HSE division factor for RTC clock
	 */
	__HAL_RCC_RTC_HSEDIV(1);
}

/**
 * @brief Peripherals Common Clock Configuration
 * @retval None
 */
void PeriphCommonClock_Config(void) {
	RCC_PeriphCLKInitTypeDef PeriphClkInit = { 0 };

	/** Initializes the common periph clock
	 */
	PeriphClkInit.PeriphClockSelection = RCC_PERIPHCLK_CKPER;
	PeriphClkInit.CkperClockSelection = RCC_CKPERCLKSOURCE_HSE;
	if (HAL_RCCEx_PeriphCLKConfig(&PeriphClkInit) != HAL_OK) {
		Error_Handler();
	}
}

/**
 * @brief FDCAN1 Initialization Function
 * @param None
 * @retval None
 */
static void MX_FDCAN1_Init(void) {

	if (ResMgr_Request(RESMGR_ID_FDCAN1, RESMGR_FLAGS_ACCESS_NORMAL |
	RESMGR_FLAGS_CPU2, 0, NULL) != RESMGR_OK) {
		/* USER CODE BEGIN RESMGR_UTILITY_FDCAN1 */
		Error_Handler();
		/* USER CODE END RESMGR_UTILITY_FDCAN1 */
	}
	/* USER CODE BEGIN FDCAN1_Init 0 */

	/* USER CODE END FDCAN1_Init 0 */

	/* USER CODE BEGIN FDCAN1_Init 1 */

	/* USER CODE END FDCAN1_Init 1 */
	hfdcan1.Instance = FDCAN1;
	hfdcan1.Init.FrameFormat = FDCAN_FRAME_CLASSIC;
	hfdcan1.Init.Mode = FDCAN_MODE_NORMAL;
	hfdcan1.Init.AutoRetransmission = DISABLE;
	hfdcan1.Init.TransmitPause = DISABLE;
	hfdcan1.Init.ProtocolException = DISABLE;
	hfdcan1.Init.NominalPrescaler = 4;
	hfdcan1.Init.NominalSyncJumpWidth = 1;
	hfdcan1.Init.NominalTimeSeg1 = 11;
	hfdcan1.Init.NominalTimeSeg2 = 12;
	hfdcan1.Init.DataPrescaler = 1;
	hfdcan1.Init.DataSyncJumpWidth = 1;
	hfdcan1.Init.DataTimeSeg1 = 1;
	hfdcan1.Init.DataTimeSeg2 = 1;
	hfdcan1.Init.MessageRAMOffset = 0;
	hfdcan1.Init.StdFiltersNbr = 0;
	hfdcan1.Init.ExtFiltersNbr = 0;
	hfdcan1.Init.RxFifo0ElmtsNbr = 32;
	hfdcan1.Init.RxFifo0ElmtSize = FDCAN_DATA_BYTES_8;
	hfdcan1.Init.RxFifo1ElmtsNbr = 0;
	hfdcan1.Init.RxFifo1ElmtSize = FDCAN_DATA_BYTES_8;
	hfdcan1.Init.RxBuffersNbr = 0;
	hfdcan1.Init.RxBufferSize = FDCAN_DATA_BYTES_8;
	hfdcan1.Init.TxEventsNbr = 0;
	hfdcan1.Init.TxBuffersNbr = 0;
	hfdcan1.Init.TxFifoQueueElmtsNbr = 32;
	hfdcan1.Init.TxFifoQueueMode = FDCAN_TX_FIFO_OPERATION;
	hfdcan1.Init.TxElmtSize = FDCAN_DATA_BYTES_8;
	if (HAL_FDCAN_Init(&hfdcan1) != HAL_OK) {
		Error_Handler();
	}
	/* USER CODE BEGIN FDCAN1_Init 2 */

	/* USER CODE END FDCAN1_Init 2 */

}

/**
 * @brief FDCAN2 Initialization Function
 * @param None
 * @retval None
 */
static void MX_FDCAN2_Init(void) {

	if (ResMgr_Request(RESMGR_ID_FDCAN2, RESMGR_FLAGS_ACCESS_NORMAL |
	RESMGR_FLAGS_CPU2, 0, NULL) != RESMGR_OK) {
		/* USER CODE BEGIN RESMGR_UTILITY_FDCAN2 */
		Error_Handler();
		/* USER CODE END RESMGR_UTILITY_FDCAN2 */
	}
	/* USER CODE BEGIN FDCAN2_Init 0 */

	/* USER CODE END FDCAN2_Init 0 */

	/* USER CODE BEGIN FDCAN2_Init 1 */

	/* USER CODE END FDCAN2_Init 1 */
	hfdcan2.Instance = FDCAN2;
	hfdcan2.Init.FrameFormat = FDCAN_FRAME_CLASSIC;
	hfdcan2.Init.Mode = FDCAN_MODE_NORMAL;
	hfdcan2.Init.AutoRetransmission = DISABLE;
	hfdcan2.Init.TransmitPause = DISABLE;
	hfdcan2.Init.ProtocolException = DISABLE;
	hfdcan2.Init.NominalPrescaler = 4;
	hfdcan2.Init.NominalSyncJumpWidth = 1;
	hfdcan2.Init.NominalTimeSeg1 = 11;
	hfdcan2.Init.NominalTimeSeg2 = 12;
	hfdcan2.Init.DataPrescaler = 1;
	hfdcan2.Init.DataSyncJumpWidth = 1;
	hfdcan2.Init.DataTimeSeg1 = 1;
	hfdcan2.Init.DataTimeSeg2 = 1;
	hfdcan2.Init.MessageRAMOffset = 0;
	hfdcan2.Init.StdFiltersNbr = 0;
	hfdcan2.Init.ExtFiltersNbr = 0;
	hfdcan2.Init.RxFifo0ElmtsNbr = 32;
	hfdcan2.Init.RxFifo0ElmtSize = FDCAN_DATA_BYTES_8;
	hfdcan2.Init.RxFifo1ElmtsNbr = 0;
	hfdcan2.Init.RxFifo1ElmtSize = FDCAN_DATA_BYTES_8;
	hfdcan2.Init.RxBuffersNbr = 0;
	hfdcan2.Init.RxBufferSize = FDCAN_DATA_BYTES_8;
	hfdcan2.Init.TxEventsNbr = 0;
	hfdcan2.Init.TxBuffersNbr = 0;
	hfdcan2.Init.TxFifoQueueElmtsNbr = 32;
	hfdcan2.Init.TxFifoQueueMode = FDCAN_TX_FIFO_OPERATION;
	hfdcan2.Init.TxElmtSize = FDCAN_DATA_BYTES_8;
	if (HAL_FDCAN_Init(&hfdcan2) != HAL_OK) {
		Error_Handler();
	}
	/* USER CODE BEGIN FDCAN2_Init 2 */

	/* USER CODE END FDCAN2_Init 2 */

}

/**
 * @brief IPCC Initialization Function
 * @param None
 * @retval None
 */
static void MX_IPCC_Init(void) {

	/* USER CODE BEGIN IPCC_Init 0 */

	/* USER CODE END IPCC_Init 0 */

	/* USER CODE BEGIN IPCC_Init 1 */

	/* USER CODE END IPCC_Init 1 */
	hipcc.Instance = IPCC;
	if (HAL_IPCC_Init(&hipcc) != HAL_OK) {
		Error_Handler();
	}
	/* USER CODE BEGIN IPCC_Init 2 */

	/* USER CODE END IPCC_Init 2 */

}

/**
 * @brief GPIO Initialization Function
 * @param None
 * @retval None
 */
static void MX_GPIO_Init(void) {
	GPIO_InitTypeDef  gpio_init_structure;
	/* GPIO Ports Clock Enable */
	__HAL_RCC_GPIOH_CLK_ENABLE();
	__HAL_RCC_GPIOB_CLK_ENABLE();
	__HAL_RCC_GPIOA_CLK_ENABLE();

	gpio_init_structure.Pin = CAN_ENABLE_PIN;
	gpio_init_structure.Mode = GPIO_MODE_OUTPUT_PP;
	gpio_init_structure.Pull = GPIO_PULLUP;
	gpio_init_structure.Speed = GPIO_SPEED_FREQ_VERY_HIGH;
	//BSP_ENTER_CRITICAL_SECTION(CAN_ENABLE_GPIO_PORT);
	HAL_GPIO_Init(CAN_ENABLE_GPIO_PORT, &gpio_init_structure);
	//BSP_EXIT_CRITICAL_SECTION(CAN_ENABLE_GPIO_PORT);
	HAL_GPIO_WritePin(CAN_ENABLE_GPIO_PORT, CAN_ENABLE_PIN, GPIO_PIN_SET);

}

/* USER CODE BEGIN 4 */
void VIRT_UART0_RxCpltCallback(VIRT_UART_HandleTypeDef *huart) {

	/* copy received msg in a variable to sent it back to master processor in main infinite loop*/
	VirtUart0ChannelRxSize =
			huart->RxXferSize < MAX_BUFFER_SIZE ?
					huart->RxXferSize : MAX_BUFFER_SIZE - 1;
	xStreamBufferSend(streamBuffer0, huart->pRxBuffPtr, VirtUart0ChannelRxSize, portMAX_DELAY);

}

void VIRT_UART1_RxCpltCallback(VIRT_UART_HandleTypeDef *huart)
{

	/* copy received msg in a variable to sent it back to master processor in main infinite loop*/
	VirtUart1ChannelRxSize =
			huart->RxXferSize < MAX_BUFFER_SIZE ?
					huart->RxXferSize : MAX_BUFFER_SIZE - 1;
	//xStreamBufferSend(streamBuffer1, huart->pRxBuffPtr, VirtUart1ChannelRxSize, portMAX_DELAY);
}
/* USER CODE END 4 */

/* USER CODE BEGIN Header_StartDefaultTask */
/**
 * @brief  Function implementing the defaultTask thread.
 * @param  argument: Not used
 * @retval None
 */
/* USER CODE END Header_StartDefaultTask */
void StartDefaultTask(void *argument) {
	/* USER CODE BEGIN 5 */
	/* Infinite loop */
	for (;;) {

		//poll the OpenAMP mailbox here, this is gross, and should get re-worked into doing this during an interrupt for performance reasons.
		OPENAMP_check_for_message();

		uint32_t count = xStreamBufferReceive(streamBuffer1, data_buffer, MAX_BUFFER_SIZE, 0);
		if(count)
		{
			VIRT_UART_Transmit(&huart0, data_buffer, count);
		}
		osDelay(1);
	}
	/* USER CODE END 5 */
}

/**
 * @brief  Period elapsed callback in non blocking mode
 * @note   This function is called  when TIM6 interrupt took place, inside
 * HAL_TIM_IRQHandler(). It makes a direct call to HAL_IncTick() to increment
 * a global variable "uwTick" used as application time base.
 * @param  htim : TIM handle
 * @retval None
 */
void HAL_TIM_PeriodElapsedCallback(TIM_HandleTypeDef *htim) {
	/* USER CODE BEGIN Callback 0 */
	intCounter++;
	/* USER CODE END Callback 0 */
	if (htim->Instance == TIM6) {
		HAL_IncTick();
	}
	/* USER CODE BEGIN Callback 1 */

	/* USER CODE END Callback 1 */
}

uint32_t rxCanMessages = 0;

void HAL_FDCAN_RxFifo0Callback(FDCAN_HandleTypeDef *hfdcan, uint32_t RxFifo0ITs)
{


    if(RxFifo0ITs & FDCAN_IR_RF0N)
    {
    	uint32_t portNum = (hfdcan == &hfdcan1) ? 0 : 1;

    	BaseType_t xHigherPriorityTaskWoken = pdFALSE;

    	xSemaphoreGiveFromISR( rxCanSem[portNum], &xHigherPriorityTaskWoken );

        rxCanMessages++;
    }
}

/**
 * @brief  This function is executed in case of error occurrence.
 * @retval None
 */
void Error_Handler(void) {
	/* USER CODE BEGIN Error_Handler_Debug */
	/* User can add his own implementation to report the HAL error return state */

	__disable_irq();
	while (1) {
	}
	/* USER CODE END Error_Handler_Debug */
}

#ifdef  USE_FULL_ASSERT
/**
  * @brief  Reports the name of the source file and the source line number
  *         where the assert_param error has occurred.
  * @param  file: pointer to the source file name
  * @param  line: assert_param error line source number
  * @retval None
  */
void assert_failed(uint8_t *file, uint32_t line)
{
  /* USER CODE BEGIN 6 */
  /* User can add his own implementation to report the file name and line number,
     ex: printf("Wrong parameters value: file %s on line %d\r\n", file, line) */
  /* USER CODE END 6 */
}
#endif /* USE_FULL_ASSERT */
