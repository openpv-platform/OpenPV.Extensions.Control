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
    readonly CancellationTokenSource cts = new();

    // Write thread fields
    private readonly ConcurrentQueue<CanFrame> writeMessageQueue = new();
    private readonly ManualResetEventSlim mres = new();
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
        mres.Dispose();
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

        // Create a deep copy of the message to be stored in the queue to avoid any accidental sharing of object
        // references.
        CanFrame msgCopy = new()
        {
            CanId = msg.CanId,
            Length = msg.Length,
            Data = (byte[])msg.Data.Clone(),
        };

        writeMessageQueue.Enqueue(msgCopy);

        // Release the semaphore to allow the write thread to continue operating.
        mres.Set();
        return true;
    }

    private void WriteSocketBackgroundThread()
    {
        CanFrame msg;

        while (true)
        {
            // We are going to wait here, with no cancellation token passed through. This is because we want to make
            // sure this thread doesn't terminate before all the messages that are in the write queue have been
            // successfully written out to the socket.
            // mres.Wait();
            // Wait until the semaphore is released.
            try
            {
                mres.Wait(cts.Token);
            }
            catch (OperationCanceledException)
            {
                if (writeMessageQueue.IsEmpty)
                {
                    Console.WriteLine("Done sending the messages.");
                    return;
                }
            }

            // Make sure that we only run the loop if there are items in the queue. Rerun the dequeue step until we are
            // able to successfully pull an item.
            while (!writeMessageQueue.IsEmpty)
            {
                // Dequeue the next item, if we fail to dequeue the item then sleep for at least 1 ms and then try again
                while (!writeMessageQueue.TryDequeue(out msg)) { Thread.Sleep(1); }

                // Keep trying to write a message to the socket until we are successful. If we aren't able to write the
                // message out, sleep for a bit and then try to continue writing the message out.
                while (!TryWriteMessage(msg)) { Thread.Sleep(10); }
            }

            // Once we are done with the loop sending out the messages in the queue, reset the Manual Reset Event to
            // wait until there are more messages in the queue.
            mres.Reset();
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

        // writeBackgroundTask = Task.Run(WriteSocketBackgroundThread, cts.Token);
        writeBackgroundTask = Task.Run(WriteSocketBackgroundThread);
        // readBackgroundTask  = Task.Run(ReadSocketBackgroundThread, cts.Token);
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
