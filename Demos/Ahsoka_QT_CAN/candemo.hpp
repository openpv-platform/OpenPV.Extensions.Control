#ifndef CANDEMO_H
#define CANDEMO_H

#include <chrono>
#include <thread>
#include "AhsokaServices.h"
#include <iostream>
#include <string>
#include <unistd.h>
#include "candemo.hpp"
#include "Generated/CANModels.generated.h"

class CANDemo
{
    public:

        CANDemo() {}

        void RunDemo(AhsokaCAN::CanServiceClient& canClient)
        {
            std::cout << std::endl << "Starting CAN Demo" << std::endl;

            // Create Callbacks Lambda listen for Messages
            // This is a callback that will receive any messages that
            // the service recieves.  From here we can decode the mesages
            // in their raw form OR load them into a model as we do with this MOTOR_CMD
            // if canid 101 is received.
            auto messageCallBack = [this](CanMessageDataCollection& message)
            {
                for (auto it : message.messages())
                {
                     std::cout << it.id() << " Received" << std::endl;

                     // Decode the MotorCommand
                     if (it.id() == 101)
                     {
                         DEMO_CAN::MOTOR_CMD receivedMessage(it);
                         std::cout << " Steer = " << receivedMessage.Get_steer() << std::endl;
                     }
                }
            };


            // Listen to Reply
            canClient.RegisterCanListener(messageCallBack);

            // Start Can Client Communicatican
            // this will connect to SocketCAN or the CoProcessor depending on
            // the configuration.
            std::cout << "Starting CAN Channel" << std::endl;
            canClient.OpenCommunicationChannel();

            std::cout << "CAN Channel Started" << std::endl;
            //Here we are using a simple Model Object (from the generator)
            // to set values on our MotorCommand object
            DEMO_CAN::MOTOR_CMD canMessage;
            canMessage.Set_drive(1);
            canMessage.Set_steer(1);

            // Send the Message.  The Client will take a model directly
            // and convert it to the CanMessageCollection if needed.
            std::cout << "Sending CAN Message" << std::endl;
            canClient.SendCanMessages(1, canMessage);

            // Create a new Recurring Message
            // so that we can send it in a recurring message
            DEMO_CAN::MOTOR_CMD canMessageRecurring;
            canMessageRecurring.Set_drive(1);
            canMessageRecurring.Set_steer(2);
            auto canData = canMessageRecurring.CreateCanMessageData();

            // Build Recurring Message - Note we Swap the Can Data
            // Here will send our message every 100ms with a timeout of
            RecurringCanMessage recurringMessage;
            recurringMessage.set_can_port(1);
            recurringMessage.set_timeout_before_update_in_ms(INT_MAX / 2);
            recurringMessage.set_transmit_interval_in_ms(100);
            recurringMessage.mutable_message()->Swap(&canData); // This is simply an efficient way to update our message form the existin data.
            std::cout << "Sending Recurring CAN Message" << std::endl;
            canClient.SendRecurringCanMessage(recurringMessage);

            // Create a new Recurring Message for a TSC1
            // so that we can send it in a recurring message
            // Normally we would mutate this value over time but
            // here we just set it up once and send it.
            DEMO_CAN::TSC1 canMessageTSC1;
            canMessageTSC1.SetEngine_Requested_Speed_Control_Conditions(DEMO_CAN::Stability_Optimized_for_driveline_engaged_and_or_in_lockup_condition_2__e_g___PTO_driveline_);
            canMessageTSC1.SetEngine_Requested_Speed_Speed_Limit(60);
            auto canDataTSC1 = canMessageTSC1.CreateCanMessageData();

            // Build Recurring Message - Note we Swap the Can Data
            // Here will send our message every 100ms with a timeout of
            RecurringCanMessage recurringMessageTSC1;
            recurringMessageTSC1.set_can_port(1);
            recurringMessageTSC1.set_timeout_before_update_in_ms(INT_MAX / 2);
            recurringMessageTSC1.set_transmit_interval_in_ms(10); // Note that when testing this on WIndows or Ubuntu, its governed to 100ms
            recurringMessageTSC1.mutable_message()->Swap(&canDataTSC1); // This is simply an efficient way to update our message form the existin data.
            canClient.SendRecurringCanMessage(recurringMessageTSC1);

            std::cout << "Waiting Shutdown" << std::endl;

            while(true)
                std::this_thread::sleep_for(std::chrono::seconds(1));
        }

};

#endif // CANDEMO_H
