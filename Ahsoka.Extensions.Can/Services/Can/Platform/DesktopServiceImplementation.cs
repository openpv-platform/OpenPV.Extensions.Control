using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Platform;

internal class DesktopServiceImplementation : CanServiceImplementation
{
    BlockingCollection<CanMessageData> messageQueue;
    CancellationTokenSource source = null;
    readonly List<Task> tasks = new();

    protected override void OnClose()
    {
        source?.Cancel();
        Task.WaitAll(tasks.ToArray()); // Wait for Exit.
    }

    protected override void OnOpen()
    {
        messageQueue = new();
        source = new();

        // Message Handler (Transmitter)
        var taskMain = Task.Run(() =>
        {
            try
            {
                while (messageQueue.TryTake(out CanMessageData messageData, -1, source.Token))
                {

                    // Handle Message (filtering etc handled here)
                    FilterIncomingMessage(messageData, out bool shouldSend);

                    // If Not Filtered
                    if (shouldSend)
                    {
                        var messages = new CanMessageDataCollection() { CanPort = Port };
                        messages.Messages.Add(messageData);

                        // Send Message
                        // "Simulate" by Echoing back to Client
                        Service.NotifyCanMessages(messages);
                    }
                }
            }
            catch (OperationCanceledException) { }
        });

        // Simulated Address Claim.
        var startTask = Task.Run(() =>
        {
            Task.Delay(1000).Wait(); // Simulate Startup Address Info
            CanState state = new() { CanPort = Port };
            foreach (var item in PortConfig.MessageConfiguration.Nodes)
                if (item.NodeType == NodeType.Self)
                    state.CurrentAddress = 255 - (uint)item.Id;
                else
                    state.NodeAddresses.Add(item.Id, 255 - (uint)item.Id);

            Service.NotifyStateUpdate(state);
        });

        // Handle Recurring Messages
        var taskRecurring = ProcessRecurringMessages(source);

        tasks.AddRange(new[] { taskMain, taskRecurring });
    }

    protected override void OnSendCanMessages(CanMessageDataCollection canMessageDataCollection)
    {
        // Kick off a producer task
        foreach (var message in canMessageDataCollection.Messages)
        {
            ProcessMessage(message);
            messageQueue.Add(message);
        }
    }

    protected override void OnSendRecurringMessage(RecurringCanMessage message)
    {
        if (message.TransmitIntervalInMs < 100)
            message.TransmitIntervalInMs = 100;

        AddRecurringMessage(message);
    }
}
