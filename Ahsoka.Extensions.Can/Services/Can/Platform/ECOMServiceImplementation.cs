using Ahsoka.ServiceFramework;
using Ahsoka.Services.Can.Messages;
using Ahsoka.Utility.ECOM;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Platform;

[ExcludeFromCodeCoverage]
internal class ECOMServiceImplementation : CanServiceImplementation
{
    UInt64 ecomHandle;
    CancellationTokenSource source = null;
    Task recurringMessageHandler;
    bool isConnected = false;

    private Task readSFFBackgroundTask;
    private Task readEFFBackgroundTask;

    protected override void OnClose()
    {
        if (!isConnected)
            return;

        ECOMLibrary.CloseDevice(ecomHandle);
        source?.Cancel();

        // Wait for Exit
        recurringMessageHandler.Wait();
        readEFFBackgroundTask.Wait();
        readSFFBackgroundTask.Wait();

        isConnected = false;
    }

    protected override void OnOpen()
    {
        if (!isConnected)
        {
            byte returnError = 0;

            ecomHandle = ECOMLibrary.CANOpen(0, ECOMLibrary.CAN_BAUD_250K, ref returnError);
            if (ecomHandle == UIntPtr.Zero)
            {
                isConnected = false;
                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Connection to ECOM dongle failed");
                return;
            }

            var result = ECOMLibrary.CANSetupDevice(ecomHandle, ECOMLibrary.CAN_CMD_TRANSMIT, ECOMLibrary.CAN_PROPERTY_ASYNC);

            source = new();
            recurringMessageHandler = ProcessRecurringMessages(source);

            readEFFBackgroundTask = Task.Run(ReceiveEFFBackgroundThread);
            readSFFBackgroundTask = Task.Run(ReceiveSFFBackgroundThread);           

            isConnected = true;
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Connection to ECOM completed successfully");
        }
    }

    private void HandleMessageReceived(CanMessageDataCollection e)
    {
        FilterIncomingMessage(e.Messages.First(), out bool shouldSend);
        if (shouldSend)
        {
            e.CanPort = Port;
            Service.NotifyCanMessages(e);
        }
        e.Messages.Clear();
    }

    protected override void OnSendCanMessages(CanMessageDataCollection canMessageDataCollection)
    {
        if (isConnected)
        {
            foreach (var canMessage in canMessageDataCollection.Messages)
            {
                if (ProcessMessage(canMessage))
                {
                    byte returnError;
                    if (canMessage.Id >= 0x80000000) // Extended Frame
                    {
                        var ecomMessage = new EFFMessage
                        {
                            ID = canMessage.Id,
                            DataLength = (byte)canMessage.Dlc,
                            data1 = canMessage.Data[0],
                            data2 = canMessage.Data[1],
                            data3 = canMessage.Data[2],
                            data4 = canMessage.Data[3],
                            data5 = canMessage.Data[4],
                            data6 = canMessage.Data[5],
                            data7 = canMessage.Data[6],
                            data8 = canMessage.Data[7]
                        };
                        returnError = ECOMLibrary.CANTransmitMessageEx(ecomHandle, ref ecomMessage);
                    }
                    else
                    {
                        var ecomMessage = new SFFMessage();
                        var idBytes = BitConverter.GetBytes(canMessage.Id);
                        ecomMessage.IDH = idBytes[1];
                        ecomMessage.IDL = idBytes[0];
                        ecomMessage.DataLength = (byte)canMessage.Dlc;
                        ecomMessage.data1 = canMessage.Data[0];
                        ecomMessage.data2 = canMessage.Data[1];
                        ecomMessage.data3 = canMessage.Data[2];
                        ecomMessage.data4 = canMessage.Data[3];
                        ecomMessage.data5 = canMessage.Data[4];
                        ecomMessage.data6 = canMessage.Data[5];
                        ecomMessage.data7 = canMessage.Data[6];
                        ecomMessage.data8 = canMessage.Data[7];
                        returnError = ECOMLibrary.CANTransmitMessage(ecomHandle, ref ecomMessage);
                    }

                    if (returnError != ECOMLibrary.ECI_NO_ERROR)
                    {
                        StringBuilder errMsg = new(400);
                        ECOMLibrary.GetFriendlyErrorMessage(returnError, errMsg, 400);
                        AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Message failed to send with error: {errMsg}");
                    }
                }           
            }
        }
    }

    protected override void OnSendRecurringMessage(RecurringCanMessage message)
    {
        AddRecurringMessage(message);
    }

    private void ReceiveEFFBackgroundThread()
    {
        RunBackgroundThread(true);
    }

    private void ReceiveSFFBackgroundThread()
    {
        RunBackgroundThread(false);
    }

    private void RunBackgroundThread(bool extended)
    {
        CanMessageDataCollection msgs = new();

        while (true)
        {
            if (source.Token.IsCancellationRequested)
                return;

            if (TryReceive(extended, ref msgs))
                HandleMessageReceived(msgs);
        }
    }

    private bool TryReceive(bool extended, ref CanMessageDataCollection msgs)
    {
        Byte returnError;

        var rxSMessage = new SFFMessage();
        var rxEMessage = new EFFMessage();

        var queueFlag = extended ? ECOMLibrary.CAN_GET_EFF_SIZE : ECOMLibrary.CAN_GET_SFF_SIZE;
        var messageCount = ECOMLibrary.GetQueueSize(ecomHandle, queueFlag);
        if (messageCount == 0)
        {
            Thread.Sleep(20);
            return false;
        }

        returnError = extended ? ECOMLibrary.CANReceiveMessageEx(ecomHandle, ref rxEMessage) : ECOMLibrary.CANReceiveMessage(ecomHandle, ref rxSMessage);
        if (returnError != ECOMLibrary.ECI_NO_ERROR)
        {
            StringBuilder errMsg = new(400);
            ECOMLibrary.GetFriendlyErrorMessage(returnError, errMsg, 400);
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Message received failed with error: {errMsg}");
            return false;
        }

        var msg = new CanMessageData();
        var bytes = new byte[8];
        if (extended)
        {
            msg.Id = rxEMessage.ID;
            bytes[0] = rxEMessage.data1;
            bytes[1] = rxEMessage.data2;
            bytes[2] = rxEMessage.data3;
            bytes[3] = rxEMessage.data4;
            bytes[4] = rxEMessage.data5;
            bytes[5] = rxEMessage.data6;
            bytes[6] = rxEMessage.data7;
            bytes[7] = rxEMessage.data8;
        }
        else
        {
            msg.Id = BitConverter.ToUInt32(new byte[4] { rxSMessage.IDL, rxSMessage.IDH, 0, 0 });
            bytes[0] = rxSMessage.data1;
            bytes[1] = rxSMessage.data2;
            bytes[2] = rxSMessage.data3;
            bytes[3] = rxSMessage.data4;
            bytes[4] = rxSMessage.data5;
            bytes[5] = rxSMessage.data6;
            bytes[6] = rxSMessage.data7;
            bytes[7] = rxSMessage.data8;
        }
        msg.Dlc = 8;
        msg.Data = bytes;
        msgs.Messages.Add(msg);
        
        return true;
    }
}
