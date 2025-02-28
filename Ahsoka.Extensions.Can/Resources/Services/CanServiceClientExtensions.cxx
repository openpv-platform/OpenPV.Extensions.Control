void CanServiceClient::OpenCommunicationChannel()
{
    auto callback = [this](CanMessageTypes::Ids transportId, AhsokaServiceFramework::AhsokaMessageType_t& message)
        {
            if (transportId == CanMessageTypes_Ids_CAN_MESSAGES_RECEIVED && messageReceived != 0)
                messageReceived((CanMessageDataCollection&)message);

            if (transportId == CanMessageTypes_Ids_NETWORK_STATE_CHANGED && stateReceived != 0)
                stateReceived((CanState&)message);
        };

    NotifyMessageReceived(callback);
    calibration = SendMessageWithResponse<CanApplicationConfiguration>(CanMessageTypes_Ids_OPEN_COMMUNICATION_CHANNEL);
}

void CanServiceClient::CloseCommunicationChannel()
{
    SendMessageWithResponse(CanMessageTypes_Ids_CLOSE_COMMUNICATION_CHANNEL);
    calibration = 0;
}

void CanServiceClient::SendCanMessages(uint canPort, IHasCanData& message)
{
    CanMessageDataCollection collection;
    collection.set_can_port(canPort);
    collection.mutable_messages()->Add(message.CreateCanMessageData());

    SendMessageWithResponse(CanMessageTypes_Ids_SEND_CAN_MESSAGES, &collection);
}

CanPortConfiguration& CanServiceClient::GetPortConfiguration()
{
    auto mapConfig = calibration->mutable_can_port_configuration();
    return *mapConfig;
}

void CanServiceClient::RegisterCanListener(CanMessageReceived_t callback)
{
    messageReceived = callback;
}

void CanServiceClient::RegisterStateListener(CanStateReceived_t callback)
{
    stateReceived = callback;
}