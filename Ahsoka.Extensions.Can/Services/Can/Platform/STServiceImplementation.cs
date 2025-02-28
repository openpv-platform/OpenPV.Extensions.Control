using Ahsoka.Core;
using Ahsoka.Utility.SocketCAN;
using SocketCANSharp;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Platform;

[ExcludeFromCodeCoverage]
internal class STSocketCanServiceImplementation : CanServiceImplementation
{
    SocketCANInterfaceThreaded socketCAN;
    CancellationTokenSource source = null;
    Task recurringMessageHandler;

    protected override void OnClose()
    {
        socketCAN?.Stop();
        source?.Cancel();

        // Wait for Exit
        recurringMessageHandler.Wait();
    }

    protected override void OnOpen()
    {
        source = new();

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"SocketCAN Starting at {PortConfig.MessageConfiguration.Ports.First(x => x.Port == Port).CanInterfacePath}");
        socketCAN = new SocketCANInterfaceThreaded(PortConfig.MessageConfiguration.Ports.First(x => x.Port == Port).CanInterfacePath);
        socketCAN.Start();
        socketCAN.CanFrameReceived += (o, e) =>
        {
            CanMessageData messageData = new()
            {
                Dlc = (uint)e.Length,
                Data = e.Data,
                Id = e.CanId & 0x1FFFFFFF
            };

            // Handle Message (filtering etc handled here)
            FilterIncomingMessage(messageData, out bool shouldSend);

            // If Not Filtered
            if (shouldSend)
            {
                var messages = new CanMessageDataCollection() { CanPort = Port };
                messages.Messages.Add(messageData);

                // Send Message to client
                Service.NotifyCanMessages(messages);

            }
        };

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"SocketCAN Service Started on Port:{Port}");

        // Handle Recurring Messages
        recurringMessageHandler = ProcessRecurringMessages(source);
    }


    protected override void OnSendCanMessages(CanMessageDataCollection canMessageDataCollection)
    {
        if (socketCAN.IsStarted)
        {
            foreach (var canMessage in canMessageDataCollection.Messages)
            {
                if (ProcessMessage(canMessage))
                {
                    // Protect SocketCAN from Incorrectly Set Messages
                    if (canMessage.Id > 0x07FF)
                        canMessage.Id |= (uint)CanIdFlags.CAN_EFF_FLAG;

                    CanFrame frame = new(canMessage.Id, canMessage.Data);

                    socketCAN.QueueWriteMessage(frame);
                }

            }
        }
    }

    protected override void OnSendRecurringMessage(RecurringCanMessage message)
    {
        AddRecurringMessage(message);
    }
}


