#include "AhsokaServices.h"
#include <iostream>
#include <string>
#include "candemo.hpp"

#include <QApplication>

int main(int argc, char *argv[])
{

    // Create Client for System to allow Comunication with the PC Tools
    // could also be used for interacting with Hardware Info, Brightness and other
    // hardware features.
    AhsokaSystem::SystemServiceClient client;

    // Create a CAN Client
    // Our CMAKE build will generate the code we include
    AhsokaCAN::CanServiceClient canClient;

    auto runtime = AhsokaRuntime::CreateBuilder()
        .AddClient(&client)
        .AddClient(&canClient)
        .SetServiceExecutable("./CommandLine/Ahsoka.CommandLine") // Path to Ahsoka Service CommandLine
        .StartWithExternalServices();

    AhsokaRuntime::ReleaseStartup();

    // Get Hardware Info and Print itString
    auto hardwareInfo = client.GetHardwareInformation();

    std::cout << std::endl << "Hardware Connected to (" << hardwareInfo.get()->application_name() << ") "
        << std::endl << "Device - " << hardwareInfo.get()->serial_number() << " - " << hardwareInfo.get()->part_number()
        << std::endl;

    // Run CAN Demo Logic
    CANDemo demo;
    demo.RunDemo(canClient);

    runtime.RequestShutdown();
    runtime.WaitForShutdown();

    std::cout << "Shutdown Completed" << std::endl;

    return 0;

}
