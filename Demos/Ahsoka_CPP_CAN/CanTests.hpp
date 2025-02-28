#pragma once

#include <iostream>
#include <string>
#include "AhsokaServices.h"
#include "AhsokaRuntimeProcess.h"
#include "Generated/CANModels.generated.h"
#include <unistd.h>

class CanTests
{
    public:
       
        void RunTests(std::string pathToServiceExe)
        {

            auto process = AhsokaRuntimeProcess::StartServiceExecutable(pathToServiceExe);

            SystemServiceClient sysclient;
            sysclient.Start();

            CanServiceClient client;
            client.Start();

            TestCanViewModel();

            TestCanService(client);
           
            sysclient.RequestServiceShutdown();
            client.Stop();

            process->EndProcess();

            std::cout << "Shutdown Completed" << std::endl;
        }

        void TestCanService(CanServiceClient& client)
        {
            // Create Callbacks Lambda to pass to listen for Messages
            auto stateCallBack = [this](CanState& message)
            {
                // Wake up and notify the main thread when thte message arrives
                std::lock_guard<std::mutex> lock(waitMutex);
                state = message;
                stateReceived = true;
                condVar.notify_one();
            };

            // LSetup State Listener         
            client.RegisterStateListener(stateCallBack);

            // Create Callbacks Lambda to pass to listen for Messages
            auto messageCallBack = [this](CanMessageDataCollection& message)
            {
                // Wake up and notify the main thread when thte message arrives
                std::lock_guard<std::mutex> lock(waitMutex);
                for (auto it : message.messages())
                    messages.push_back(it);
                condVar.notify_one();
            };

            // Listen to Reply            
            client.RegisterCanListener(messageCallBack);

            // Open and Verify we recieved the Port Configuration
            client.OpenCommunicationChannel();
            auto portConfig = client.GetPortConfiguration();
            uint id = portConfig.message_configuration().messages(0).id();
            
            // Other ID for Testing.
            uint otherId = id + 1;

            // Verify we recieved the State Setup (Address Claim etc.)
            {
                std::cout << "  Waiting for State Reply" << std::endl;
                std::unique_lock<std::mutex> lock(waitMutex);
                condVar.wait(lock, [this] {return stateReceived;});
            }

            // Test Send Message Calls
            auto canMessage = CreateTestViewModel();
            canMessage.SetTestSigned(1);
            client.SendCanMessages(1, canMessage);

            canMessage.SetTestSigned(2);
            client.SendCanMessages(1, canMessage);
          
            // Build Message from Model for Recurring Message
            canMessage.SetTestSigned(3);
            auto canData = canMessage.CreateCanMessageData();

            // Build Recurring Message - Note we Swap the Can Data 
            RecurringCanMessage recurringMessage;
            recurringMessage.set_can_port(1);
            recurringMessage.set_timeout_before_update_in_ms(500);     
            recurringMessage.set_transmit_interval_in_ms(100);
            recurringMessage.mutable_message()->Swap(&canData);
            client.SendRecurringCanMessage(recurringMessage);
          
            // Wait for X Messages to Return
            {
                std::cout << "  Waiting for Message Reply" << std::endl;
                std::unique_lock<std::mutex> lock(waitMutex);
                condVar.wait(lock, [this] {return messages.size() >= 3;});
            }

            // First Message
            auto returnMessage = new TestMessage(messages[0]);
            if (returnMessage->GetTestSigned() != 1)
                std::cout << "Round Trip Message 1 Did Not Match" << std::endl;
            
            returnMessage = new TestMessage(messages[1]);
            if (returnMessage->GetTestSigned() != 2)
                std::cout << "Round Trip Message 2 Did Not Match" << std::endl;
            
            returnMessage = new TestMessage(messages[2]);
            if (returnMessage->GetTestSigned() != 3)
                std::cout << "Round Trip Message 3 Did Not Match" << std::endl;

            std::cout << "Closing Communications" << std::endl;
            client.CloseCommunicationChannel();
        }

        TestMessage CreateTestViewModel()
        {
            TestMessage model;
            model.SetTestSigned(64);
            model.SetTestUnsigned(256);
            model.SetTestEnum(Test2EnumTwo);
            model.SetTestFloat(12345.0f);
            model.SetTestDouble(54321.0f);
            return model;
        }

        void TestCanViewModel()
        {
            TestMessage model = CreateTestViewModel();

            if (model.GetTestSigned() != 64)
                std::cout << "Test Signed Did Not Match" << std::endl;
          
            if (model.GetTestUnsigned() != 256)
                std::cout << "Test Unsigned Did Not Match" << std::endl;
          
            if (model.GetTestEnum() != TestEnumValues::Test2EnumTwo)
                std::cout << "Test Enum Did Not Match" << std::endl;
                
             if (model.GetTestFloat() != 12345.0f)
                std::cout << "Test Float Did Not Match" << std::endl;

            if (model.GetTestDouble() != 54321.0f)
                std::cout << "Test Double Did Not Match" << std::endl;
               
            // Get UnScaled Value..should be half of Normal Value
            uint32_t value = model.GetRawValue<uint32_t>(TestMessage::Properties::TestUnsigned);
            if (value!= 128)
                std::cout << "Test UnScaled Failed" << std::endl;

            // Test Values Get Unset.
            model.SetTestUnsigned(0);
            if (model.GetTestUnsigned() != 0)
               std::cout << "Reset Unsigned Failed" << std::endl;
          
            auto data = model.CreateCanMessageData();
            if (data.id() != 500)
               std::cout << "ID Did Not Match" << std::endl;
            if (data.dlc() != 16)
               std::cout << "Dlc did not  Match" << std::endl;

            // Fetch the Property Object from the Metadata Factory usind the ID and Property Enum
            auto values = CanModelMetadata::CanMetadata()->GetMetadata(data.id());
            auto prop = values[TestMessage::Properties::TestSigned];
            
            // Set the Property and Verify it Returns.
            prop.SetValue<int>(data.mutable_data(), 3);
            int resultValue = prop.GetValue<int>(data.mutable_data());

            if (resultValue != 3)
               std::cout << "Metadata Generated Value did not match" << std::endl;

            resultValue = prop.GetSignalId();
            if (resultValue != 2)
               std::cout << "Metadata Signal is Incorrect" << std::endl;


            // Fetch the Property Object from the Metadata Factory usind the ID and Property Enum
            auto propInfo = CanModelMetadata::CanMetadata()->GetPropertyInfo(data.id(), 2);
        }

    private: 

        std::mutex waitMutex;
        std::condition_variable condVar;
        std::vector<CanMessageData> messages;
        CanState state;
        bool stateReceived = false;
};