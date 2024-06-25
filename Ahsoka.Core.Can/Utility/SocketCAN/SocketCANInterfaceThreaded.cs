using Ahsoka.ServiceFramework;
using SocketCANSharp;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Utility.SocketCAN;

/// <summary>
/// Threaded SocketCAN Interface which supports Queuing of Frames.
/// </summary>
[ExcludeFromCodeCoverage]
internal class SocketCANInterfaceThreaded : SocketCANInterfaceBase
{
    #region Fields
    bool hitWatermarkAdd = false;
    bool hitWatermarkSend = false;
    int watermark = 1000;
    readonly CancellationTokenSource cts = new();

    // Write thread fields
    private readonly BlockingCollection<CanFrame> writeMessageQueue = new();
    private Task writeBackgroundTask;

    // Read thread fields
    /// <summary>
    /// Notification when a CAN Frame has been received.
    /// </summary>
    public event EventHandler<CanFrame> CanFrameReceived;
    private Task readBackgroundTask;
    #endregion

    #region Constructors
    ///<InheritDoc/>
    public SocketCANInterfaceThreaded(string interfaceNameArg) : base(interfaceNameArg) { }

    ///<InheritDoc/>
    public SocketCANInterfaceThreaded(string interfaceNameArg, TimeSpan readTimeoutArg) : base(interfaceNameArg, readTimeoutArg) { }

    ///<InheritDoc/>
    public new void Dispose()
    {
        base.Dispose();

        // Dispose the things that we are using in this class.
        readBackgroundTask.Dispose();
        writeBackgroundTask.Dispose();
        cts.Dispose();
    }
    #endregion

    #region WriteMessageFunctions
    /// <summary>
    /// Add a CAN message to the queue of messages that are going to be sent out.
    /// 
    /// If the queue was able to successfully be cleared, this function will return true, if there are pending
    /// messages in the queue at the end of this message then it will return false.
    /// </summary>
    /// <param name="msg">The CAN message that is going to be added to the queue.</param>
    /// <returns></returns>
    public Boolean QueueWriteMessage(CanFrame msg)
    {
        if (!IsStarted)
        {
            throw new InvalidOperationException("The socket hasn't been started yet.");
        }

        // Don't accept any more messages if we can't send them
        if (writeMessageQueue.Count > watermark)
        {
            if (!hitWatermarkAdd)
                hitWatermarkAdd = true;

            return false;
        }

        // Restore Watermark Indicator
        if (hitWatermarkSend)
            hitWatermarkAdd = false;

        // Create a deep copy of the message to be stored in the queue to avoid any accidental sharing of object
        // references.
        CanFrame msgCopy = new()
        {
            CanId = msg.CanId,
            Length = msg.Length,
            Data = (byte[])msg.Data.Clone(),
        };

        writeMessageQueue.TryAdd(msgCopy);

        return true;
    }

    private void WriteSocketBackgroundThread()
    {
        CanFrame msg;

        while (!cts.IsCancellationRequested)
        {
            // Dequeue the next item
            if (writeMessageQueue.TryTake(out msg, 1000, cts.Token))
            {
                // Keep trying to write a message to the socket until we are successful.
                bool result = TryWriteMessage(msg);
                if (!result)
                {
                    while (!cts.IsCancellationRequested &&
                        !result &&
                        writeMessageQueue.Count < watermark) // We Have Room to Keep Trying
                    {
                        Thread.Sleep(1);
                        result = TryWriteMessage(msg);
                    }

                    if (!result && !hitWatermarkSend)
                    {
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "SocketCAN Interface - Hit Watermark - Failed to Send CAN Message, Dropping Message");
                        hitWatermarkSend = true;
                    }
                }
                else if (hitWatermarkSend)
                {
                    hitWatermarkSend = false;
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, "SocketCAN Interface - CAN Transmission Restored");
                }
            }
        }
    }
    #endregion

    #region ReadMessageFunctions
    /// <summary>
    /// This task function should only be initialized whenever there are values in the EventHandler. No point in
    /// running the task whenever there is nothing to receive the messages.
    /// </summary>
    private void ReadSocketBackgroundThread()
    {
        CanFrame msg = new();

        while (true)
        {
            // Make sure that we check to see if the thread has been cancelled before we go on and read from the socket.
            if (cts.Token.IsCancellationRequested)
            {
                return;
            }

            if (TryReadMessage(ref msg))
            {
                // We are going to call all the event functions that have been registers for this class whenever we
                // receive a CAN message that matches the filter that has been defined.
                CanFrameReceived?.Invoke(this, msg);
            }
        }
    }
    #endregion

    #region Start and Stop Functions
    /// <summary>
    /// Open the Socket and Start the Reader and Writer Threads
    /// </summary>
    public new void Start()
    {
        base.Start();

        writeBackgroundTask = Task.Run(WriteSocketBackgroundThread);
        readBackgroundTask = Task.Run(ReadSocketBackgroundThread);
    }

    /// <summary>
    /// Stop the Reader / Writer Threads and Close the CAN Socket
    /// </summary>
    public new void Stop()
    {
        cts.Cancel();

        writeBackgroundTask.Wait();
        readBackgroundTask.Wait();

        base.Stop();
    }
    #endregion
}
