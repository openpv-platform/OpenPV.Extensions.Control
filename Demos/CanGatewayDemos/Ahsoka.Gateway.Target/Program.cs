using Ahsoka.Core;
using Ahsoka.Core.Dispatch;
using Ahsoka.Services.Can;
using Ahsoka.Services.System;
using System;

namespace Ahsoka.CS.CAN;

public class Program
{
    static readonly CanUI basicUI = new();

    public static void Main()
    {
        //Enables Multi-Packet Logging
        AhsokaLogging.LoggingVerbosity = AhsokaVerbosity.Medium;
        
        // Create and start the System Service without client
        SystemService system = new();
        system.Start();

        // Create Can Service and Enable Gateway. This service
        // will now forward any Service messages received on a 
        // can port to the correct service.
        CanService service = new();
        service.Start();
        // Message Id must correspond to a configured message class
        service.EnableGateway(1, 2818048, 0x1FF0000);

        // Open the Channel After the System has Started the Services
        CanServiceClient canClient = new();
        Dispatcher.Default.InvokeDispatcher(IntializeCAN, EventArgs.Empty, canClient);

        // Start a Basic UI.
        basicUI.StartAndRun(Dispatcher.Default);

        // Execute Shutdown
        ApplicationContext.Exit();

        return;
    }


    private static void IntializeCAN(object sender, EventArgs args)
    {
        CanServiceClient canClient = sender as CanServiceClient;
        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "Starting CAN Service");

        // Open Communication with the CoProcessor or SocketCAN
        canClient.OpenCommunicationChannel();
    }
}
