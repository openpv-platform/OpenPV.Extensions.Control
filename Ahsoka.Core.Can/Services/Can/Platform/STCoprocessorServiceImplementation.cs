using Ahsoka.ServiceFramework;
using Ahsoka.Utility;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Platform;

[ExcludeFromCodeCoverage]
internal class STCoprocessorServiceImplementation : CanServiceImplementation
{
    static readonly CoprocessorSocket coProcessorSocket = new();
    static Task heartBeat = null;
    readonly AutoResetEvent isReadyHandle = new(false);
    static CancellationTokenSource heartBeatCancellationSource = new();
    int heartBeatSendCount = 0;
    int heartBeatReceiveCount = 0;

    protected override void OnClose()
    {
        // Start MQ Broker to talk to Server
        coProcessorSocket?.Disconnect();

        // Cancel and Wait for Result.
        heartBeatCancellationSource.Cancel();
        heartBeat.Wait();
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
        string ipAddressLocal = PortConfig.CommunicationConfiguration.LocalIpAddress;
        string ipAddressRemote = PortConfig.CommunicationConfiguration.RemoteIpAddress;
        string physicalPath = PortConfig.MessageConfiguration.Ports.First(x => x.Port == Port).CanInterfacePath;
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Connecting to CAN Interface on tty {physicalPath} with P2P Addresses {ipAddressLocal} -> {ipAddressRemote}");

        // Start MQ Broker to talk to Server
        coProcessorSocket.Connect(ipAddressLocal, ipAddressRemote, this);

        // Send Message to CoProcessor to ensure its ready to start (ServiceIsReady
        SendServiceReadyMessage();

        // Wait for CoProcessor Message (CoProcessorReady)
        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Waiting for CoProcessor Ready Message Port: {Port}");
        isReadyHandle.WaitOne(); // Disabled until CoProcessor App is Speaking Metmq

        if (heartBeat == null)
        {
            heartBeatCancellationSource = new();

            heartBeat = Task.Run(() =>
            {
                while (!heartBeatCancellationSource.IsCancellationRequested)
                {
                    lock (coProcessorSocket)
                        if (coProcessorSocket.IsConnected)
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
            if (coProcessorSocket.IsConnected)
            {
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
            if (coProcessorSocket.IsConnected)
            {
                // Prepare Response Message 
                using MemoryStream headerStream = new();
                ProtoBuf.Serializer.Serialize(headerStream, new AhsokaMessageHeader() { TransportId = CanMessageTypes.Ids.SendRecurringCanMessage.EnumToInt() });

                using MemoryStream dataStream = new();
                ProtoBuf.Serializer.Serialize(dataStream, recurringMessage);

                coProcessorSocket.SendMessage(headerStream.ToArray(), dataStream.ToArray());
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
            if (coProcessorSocket.IsConnected)
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

            for (int i = collection.Messages.Count-1; i >= 0; i--)
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

#region Coprocessor Socket 

[ExcludeFromCodeCoverage]
internal sealed class CoprocessorSocket
{
    bool isConnected = false;
    readonly List<STCoprocessorServiceImplementation> ports = new();

    NetMQPoller SocketPoller { get; set; }
    DealerSocket SendSocket { get; set; }
    DealerSocket RecieveSocket { get; set; }
    public bool IsConnected { get => isConnected; set => isConnected = value; }

    string listenAddress;
    string sendAddress;

    internal void Connect(string listenIpAddress, string sendIpAddress, STCoprocessorServiceImplementation impl)
    {        
        ports.Add(impl);

        if (IsConnected)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.High, $"CoProcessorClient is already running");
        }
        else
        {
            try
            {
                listenAddress = $"tcp://{sendIpAddress}:5500";
                sendAddress = $"tcp://{sendIpAddress}:6000";

                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Starting CoProcessorClient Sender @ {sendAddress}");

                byte[] clientId = Guid.NewGuid().ToByteArray();

                SocketPoller = new NetMQPoller();
                SendSocket = new DealerSocket();
                SendSocket.Options.Linger = TimeSpan.FromSeconds(1);
                SendSocket.Connect(sendAddress);

                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Starting CoProcessorClient Sender @ {listenAddress}");

                RecieveSocket = new DealerSocket();
                RecieveSocket.Options.Linger = TimeSpan.FromSeconds(1);
                RecieveSocket.Connect(listenAddress);

                // Handle Poller for Notifications
                SocketPoller.Add(RecieveSocket);
                RecieveSocket.ReceiveReady += (o, e) =>
                {
                    bool recievedMessage = ReceiveMessage(out byte[] headerData, out byte[] messageData);

                    if (recievedMessage)
                        foreach (var port in ports)
                        {
                            port.HandleReceiveMessage(headerData, messageData);
                        }
                };

                SocketPoller.RunAsync($"CoProcessor_NotificationListener");

                IsConnected = true;
            }
            catch { IsConnected = false; }

            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Starting CoProcessorClient with Result {IsConnected}");
        }       
    }

    internal void Disconnect()
    {
        if (IsConnected)
        {
            try
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnecting CoProcessorClient");

                try { SendSocket.Disconnect(sendAddress); } catch { }
                try { SendSocket.Disconnect(listenAddress); } catch { }

                SocketPoller.Stop();
                SocketPoller.RemoveAndDispose(SendSocket);

                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Disconnected CoProcessorClient");
            }
            catch { }
            finally { IsConnected = false; }
        }
    }

    internal void SendMessage(byte[] header, byte[] data)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Sending Coprocessor Message ");

        NetMQMessage message = new();
        message.Append(header);
        message.Append(data);

        lock (SendSocket)
            SendSocket.SendMultipartMessage(message);

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Sending Coprocessor Message Complete ");
    }

    internal bool ReceiveMessage(out byte[] header, out byte[] data, int timeOutInMs = int.MaxValue)
    {
        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Receiving Coprocessor Message");

        header = data = null;

        NetMQMessage message = new();
        if (RecieveSocket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(timeOutInMs), ref message, 2))
        {
            try
            {
                header = message.Pop().Buffer;
                data = message.Pop().Buffer;
            }
            catch
            {
                AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"Bad Coprocessor Message");
                header = data = null;
            }
        }

        AhsokaLogging.LogMessage(AhsokaVerbosity.Low, $"    Receiving Coprocessor Message Complete ");

        return header != null;
    }
}
#endregion