#include "AhsokaServices.h"
#include "AhsokaRuntimeProcess.h"
#include <iostream>
#include <string>
#include "candemo.hpp"

#include <QApplication>

int main(int argc, char *argv[])
{
    auto process = AhsokaRuntimeProcess::StartServiceExecutable("./CommandLine/Ahsoka.CommandLine");

    // Create Client for System to allow Comunication with the PC Tools
    // could also be used for interacting with Hardware Info, Brightness and other
    // hardware features.
    AhsokaSystem::SystemServiceClient client;
    client.Start();

    // Create a CAN Client
    // Our CMAKE build will generate the code we include
    AhsokaCAN::CanServiceClient canClient;
    canClient.Start();

    AhsokaRuntime::ReleaseStartup();

    // Get Hardware Info and Print itString
    auto hardwareInfo = client.GetHardwareInformation();

    std::cout << std::endl << "Hardware Connected to (" << hardwareInfo.get()->application_name() << ") "
        << std::endl << "Device - " << hardwareInfo.get()->serial_number() << " - " << hardwareInfo.get()->part_number()
        << std::endl;

    // Run CAN Demo Logic
    CANDemo demo;
    demo.RunDemo(canClient);

    client.RequestServiceShutdown();

    process->EndProcess();

    std::cout << "Shutdown Completed" << std::endl;

    return 0;

}
