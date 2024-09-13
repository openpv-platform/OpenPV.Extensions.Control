    
    // Custom Versions of Client Methods
    void SendCanMessages(uint canPort, IHasCanData& message);
    void OpenCommunicationChannel();
    void CloseCommunicationChannel();
    
    // Custom Extension methods
    CanPortConfiguration& GetPortConfiguration();

    void RegisterCanListener(CanMessageReceived_t callback);
	void RegisterStateListener(CanStateReceived_t callback);

private:
	
    unique_ptr<CanApplicationConfiguration> calibration;
    CanMessageReceived_t messageReceived;
    CanStateReceived_t stateReceived;