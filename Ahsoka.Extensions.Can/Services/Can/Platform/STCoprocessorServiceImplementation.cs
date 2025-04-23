using Ahsoka.Core;
using Ahsoka.Core.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Hashing;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;



namespace Ahsoka.Services.Can.Platform;

[ExcludeFromCodeCoverage]
internal class STCoprocessorServiceImplementation : CanServiceImplementation
{
    //static readonly CoprocessorSocket coProcessorSocket = new();
    static readonly CanCallback callback = new CanCallback();
    static readonly FramedSerialSocket coProcessorSocket = new FramedSerialSocket(FramedSocketMessageType.CAN_Message, callback);
    static Task heartBeat = null;
    readonly AutoResetEvent isReadyHandle = new(false);
    static CancellationTokenSource heartBeatCancellationSource = new();
    int heartBeatSendCount = 0;
    int heartBeatReceiveCount = 0;

    CancellationTokenSource source = null;
    Task recurringMessageHandler;

    protected override void OnClose()
    {
        // Start MQ Broker to talk to Server
        coProcessorSocket?.Disconnect();

        // Cancel and Wait for Result.
        heartBeatCancellationSource.Cancel();
        heartBeat.Wait();
        recurringMessageHandler.Wait();
    }

    protected override void OnOpen()
    {
        // This all occurs in the start.sh script and Installer Now
        // Firmware Link should be located at Located at /lib/firmware
        // echo -n yourfilename > /sys/class/remoteproc/remoteproc0/firmware
        // echo start>/sys/class/remoteproc/remoteproc0/state

        // The Start Scrip also starts SLIP over TTY
        // slattach - p slip - L - m /dev/ttyXXXX  &

        // And Finally Maps the IP's
        // ifconfig sl0 192.168.8.2  pointtopoint 192.168.8.1 up

        // Read the IP Addresses


        string physicalPath = PortConfig.MessageConfiguration.Ports.First(x => x.Port == Port).CanInterfacePath;

        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Connecting to CAN Interface on tty {physicalPath}");

        coProcessorSocket.Connect(physicalPath, this);

        // Send Message to CoProcessor to ensure its ready to start (ServiceIsReady)
        SendServiceReadyMessage();

        // Wait for CoProcessor Message (CoProcessorReady)
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Waiting for CoProcessor Ready Message Port: {Port}");
        if (!isReadyHandle.WaitOne(5000)) // Disabled until CoProcessor App is Speaking Metmq
            throw new TimeoutException("CoProcessor did not return ready signal");

        if (heartBeat == null)
        {
            heartBeatCancellationSource = new();

            heartBeat = Task.Run(() =>
            {
                while (!heartBeatCancellationSource.IsCancellationRequested)
                {
                    lock (coProcessorSocket)
                        if (FramedSerialSocket.IsConnected)
                        {
                            // Prepare Response Message 
                            using MemoryStream headerStream = new();
                            ProtoBuf.Serializer.Serialize(headerStream, new AhsokaMessageHeader() { TransportId = CanMessageTypes.Ids.CoprocessorHeartbeat.EnumToInt() });

                            using MemoryStream dataStream = new();
                            ProtoBuf.Serializer.Serialize(dataStream, new EmptyNotification());

                            if (heartBeatSendCount != heartBeatReceiveCount)
                                AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"Heartbeat Mismatch Sent:{heartBeatSendCount} Received: {heartBeatReceiveCount}");

                            heartBeatSendCount++;
                            coProcessorSocket.SendMessage(headerStream.ToArray(), dataStream.ToArray());
                        }

                    heartBeatCancellationSource.Token.WaitHandle.WaitOne(5000);

                }
            });
        }

        source = new();
        recurringMessageHandler = ProcessRecurringMessages(source);
    }

    private void SendServiceReadyMessage()
    {
        // Send Startup Message to CoProcessor
        // Prepare Response Message 
        MemoryStream headerStream = new();
        ProtoBuf.Serializer.Serialize(headerStream, new AhsokaMessageHeader() { TransportId = CanMessageTypes.Ids.CanServiceIsReadyNotification.EnumToInt() });

        MemoryStream dataStream = new();
        ProtoBuf.Serializer.Serialize(dataStream, new EmptyNotification());

        coProcessorSocket.SendMessage(headerStream.ToArray(), dataStream.ToArray());
    }

    protected override void OnSendCanMessages(CanMessageDataCollection canMessageDataCollection)
    {
        lock (coProcessorSocket)
            if (FramedSerialSocket.IsConnected)
            {
                for (int i = canMessageDataCollection.Messages.Count - 1; i >= 0; i--)
                    if (!ProcessMessage(canMessageDataCollection.Messages[i]))
                        canMessageDataCollection.Messages.RemoveAt(i);

                // Prepare Response Message 
                using MemoryStream headerStream = new();
                ProtoBuf.Serializer.Serialize(headerStream, new AhsokaMessageHeader() { TransportId = CanMessageTypes.Ids.SendCanMessages.EnumToInt() });

                using MemoryStream dataStream = new();
                ProtoBuf.Serializer.Serialize(dataStream, canMessageDataCollection);

                coProcessorSocket.SendMessage(headerStream.ToArray(), dataStream.ToArray());
            }
    }

    protected override void OnSendRecurringMessage(RecurringCanMessage recurringMessage)
    {
        lock (coProcessorSocket)
            if (FramedSerialSocket.IsConnected)
            {
                if (recurringMessage.Message.Dlc > 8)
                    AddRecurringMessage(recurringMessage);
                else
                {
                    // Prepare Response Message 
                    using MemoryStream headerStream = new();
                    ProtoBuf.Serializer.Serialize(headerStream, new AhsokaMessageHeader() { TransportId = CanMessageTypes.Ids.SendRecurringCanMessage.EnumToInt() });

                    using MemoryStream dataStream = new();
                    ProtoBuf.Serializer.Serialize(dataStream, recurringMessage);

                    coProcessorSocket.SendMessage(headerStream.ToArray(), dataStream.ToArray());
                }
            }
    }

    internal override void CancelProtocolTransmissions(MessageType messageType)
    {
        if (messageType == MessageType.J1939ExtendedFrame)
            UpdateCanState(new CanState() { CurrentAddress = 254 });
    }

    internal override void UpdateCanState(CanState state)
    {
        state.CanPort = Port;
        lock (coProcessorSocket)
            if (FramedSerialSocket.IsConnected)
            {
                // Prepare Response Message 
                using MemoryStream headerStream = new();
                ProtoBuf.Serializer.Serialize(headerStream, new AhsokaMessageHeader() { TransportId = CanMessageTypes.Ids.NetworkStateChanged.EnumToInt() });


                using MemoryStream dataStream = new();
                ProtoBuf.Serializer.Serialize(dataStream, state);

                coProcessorSocket.SendMessage(headerStream.ToArray(), dataStream.ToArray());
            }
    }

    internal void HandleReceiveMessage(byte[] headerData, byte[] messageData)
    {
        // Decode Request Header
        using MemoryStream header = new(headerData);
        using MemoryStream objectData = new(messageData);

        // Decode Header
        AhsokaMessageHeader messageHeader = ProtoBuf.Serializer.Deserialize<AhsokaMessageHeader>(header);
        CanMessageTypes.Ids transportType = messageHeader.TransportId.IntToEnum<CanMessageTypes.Ids>();

        // Pass Message On
        if (transportType == CanMessageTypes.Ids.CanMessagesReceived)
        {
            object messageObject = ProtoBuf.Serializer.Deserialize<CanMessageDataCollection>(objectData);

            CanMessageDataCollection collection = messageObject as CanMessageDataCollection;

            if (collection.CanPort != Port)
                return;

            for (int i = collection.Messages.Count - 1; i >= 0; i--)
            {
                FilterIncomingMessage(collection.Messages[i], out var shouldSend);
                if (!shouldSend)
                    collection.Messages.RemoveAt(i);
            }

            Service.NotifyCanMessages(messageObject as CanMessageDataCollection);
        }
        else if (transportType == CanMessageTypes.Ids.NetworkStateChanged)
        {
            object messageObject = ProtoBuf.Serializer.Deserialize<CanState>(objectData);
            if ((messageObject as CanState).CanPort != Port)
                return;
            Service.NotifyStateUpdate(messageObject as CanState);
        }
        else if (transportType == CanMessageTypes.Ids.CoprocessorIsReadyNotification)
        {
            // Release Startup
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"CoprocessorIsReady Received Port: {Port}");
            isReadyHandle.Set();
        }
        else if (transportType == CanMessageTypes.Ids.CoprocessorHeartbeat)
        {
            heartBeatReceiveCount++;
            AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"CoprocessorHeartbeat Received Port: {Port}");
        }
    }
}

#region Framed Serial Socket


public enum FramedSocketMessageType
{
    CAN_Message,
    IO_Message
}
public interface IReceiveCallback
{

    void OnReceive(byte[] header, byte[] data);

}

internal class CanCallback : IReceiveCallback
{
    static readonly List<STCoprocessorServiceImplementation> ports = new();

    static public void AddImplementation(STCoprocessorServiceImplementation impl)
    {
        ports.Add(impl);
    }
    public void OnReceive(byte[] header, byte[] data)
    {
        foreach (var port in ports)
        {
            port.HandleReceiveMessage(header, data);
        }
    }
}

[ExcludeFromCodeCoverage]
public sealed class FramedSerialSocket
{
    enum HDLCStates
    {
        ESC_Received,
        Normal_Received
    }
    const byte FLAG_BYTE = 0x7E;
    const byte ESC_BYTE = 0x7D;
    const byte ESC_MODIFIER = 0x20;
    const int MIN_LENGTH = 4;
    private static HDLCStates State;
    private static bool isConnected;
    private static readonly List<byte> SerialBuffer = new();
    private static SerialPort serialPort;


    public static bool IsConnected { get => isConnected; set => isConnected = value; }
    // the port name should come from the config?

    private static readonly List<IReceiveCallback> callbacks = [];
    private readonly object lockObject = new();
    private readonly object writeLock = new();
    private readonly FramedSocketMessageType messageType;
    private static UInt32 hashErrors;


    // Still need to swap internal queue to a memory stream, for performance
    // and need to figure out a way to set the port name on the fly, and set the 
    // client message type.

    public FramedSerialSocket(FramedSocketMessageType type, IReceiveCallback handler)
    {
        messageType = type;
        // add the callback interface to the list of handlers, with the enum as the index.
        callbacks.Insert((int)messageType, handler);
    }
    private FramedSerialSocket()
    {

    }

    private static void Write(byte[] rawData)
    {
        byte[] cookedData = EncodeData(rawData);
        serialPort.Write(cookedData, 0, cookedData.Length);
    }
    private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        // this is an event callback.
        SerialPort sp = (SerialPort)sender;
        int bytesToRead = sp.BytesToRead;
        byte[] buffer = new byte[bytesToRead];
        sp.Read(buffer, 0, bytesToRead);

        // Process the received binary data
        ProcessData(buffer);
    }

    // This needs to be refactored into its own class, the whole serial port thing might be its own class.
    private static void ProcessData(byte[] bytes)
    {
        // decode the message
        // 0x7E is flag, flags start and end transmission
        // 0x7D is an escape sequence, following byte has bit 5 inverted.
        // if 0x7D 0x7E (not bit 5 inverted) is received it is an abort sequence.
        // closing flag may also be starting flag.
        // we are going to use the last 2 bytes as a check sequence.


        foreach (byte b in bytes)
        {
            if (b == FLAG_BYTE)
            {
                // we received a flag, so message is final, or start.
                if (State == HDLCStates.ESC_Received)
                {
                    // this is an abort.
                    SerialBuffer.Clear();
                }
                else
                {
                    // check length of message, if greater then min message
                    // process message.
                    if (SerialBuffer.Count > MIN_LENGTH)
                    {
                        // 4 is the minimum length.
                        // do frame check sequence.  

                        // buffer needs to be 4 less to account for CRC.
                        byte[] buffer = SerialBuffer.ToArray().SkipLast(4).ToArray();
                        var crc = new Crc32();
                        crc.Reset();
                        crc.Append(buffer);
                        // last 4 are CRC32
                        byte[] hash = SerialBuffer.ToArray().TakeLast(4).ToArray();

                        if (crc.GetCurrentHash().SequenceEqual(hash))
                        {
                            // need to look at the buffer, to get the correct message, and then stick it on the list.
                            UInt16 clientType = BitConverter.ToUInt16(buffer, 0);
                            UInt16 headerLength = BitConverter.ToUInt16(buffer, 2);

                            byte[] header = new byte[headerLength];
                            byte[] data = new byte[buffer.Length - (headerLength + 4)];
                            Array.Copy(buffer, 4, header, 0, headerLength);
                            Array.Copy(buffer, 4 + headerLength, data, 0, buffer.Length - (headerLength + 4));
                            callbacks[clientType].OnReceive(header, data);
                        }
                        else
                        {
                            hashErrors++;
                        }
                    }
                    SerialBuffer.Clear();    // start over.
                }

                State = HDLCStates.Normal_Received;
            }
            else if (b == ESC_BYTE)
            {
                if (State == HDLCStates.ESC_Received)
                {
                    // this is an illeagal sequence, just abort.
                    SerialBuffer.Clear();
                }
                State = HDLCStates.ESC_Received;
            }
            else
            {
                // if escape recieved, escape the character, else enqueue it.
                if (State == HDLCStates.ESC_Received)
                {
                    // this is an illeagal sequence, just abort.
                    int temp = b ^ ESC_MODIFIER;
                    // enqueue the temp result
                    SerialBuffer.Add((byte)temp);
                }
                else
                {
                    SerialBuffer.Add(b);
                }
                // enqueue the received byte

                State = HDLCStates.Normal_Received;
            }
        }
    }

    private static byte[] EncodeData(byte[] bytes)
    {
        List<byte> myList = [];
        var crc = new Crc32();

        crc.Append(bytes);
        byte[] hash = crc.GetCurrentHash();

        myList.Add(FLAG_BYTE);   // add the flags

        foreach (byte b in bytes.Concat(hash))
        {
            // check to see if we need to escape the value.
            if (b == FLAG_BYTE || b == ESC_BYTE)
            {
                myList.Add(0x7D);
                myList.Add(item: (byte)(b ^ ESC_MODIFIER));
            }
            else
            {
                myList.Add(b);
            }
        }
        myList.Add(FLAG_BYTE); // closing flag

        return [.. myList];
    }

    internal void Connect(string PortName, STCoprocessorServiceImplementation impl)
    {

        lock (lockObject)
        {
            if (!IsConnected)
            {
                CanCallback.AddImplementation(impl);
                serialPort = new SerialPort(PortName, 9600, Parity.None, 8, StopBits.One);
                serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                // need to add a handler for receive errors.
                serialPort.Open();
                IsConnected = true;
            }
        }
    }

    public void Disconnect()
    {
        lock (lockObject)
        {
            if (!IsConnected)
            {
                isConnected = false;
            }
        }
    }

    public void SendMessage(byte[] header, byte[] data)
    {
        if (IsConnected)
        {
            byte[] messageType = BitConverter.GetBytes((UInt16)this.messageType);

            byte[] headerLength = BitConverter.GetBytes((UInt16)header.Length);
            byte[] message = new byte[header.Length + data.Length + messageType.Length + headerLength.Length];

            Array.Copy(messageType, 0, message, 0, messageType.Length);
            Array.Copy(headerLength, 0, message, messageType.Length, headerLength.Length);
            Array.Copy(header, 0, message, headerLength.Length + messageType.Length, header.Length);
            Array.Copy(data, 0, message, headerLength.Length + header.Length + messageType.Length, data.Length);
            // write will need a mutex if this is called from multiple places.
            lock (writeLock)
            {
                FramedSerialSocket.Write(message);
            }
        }
    }
}
#endregion