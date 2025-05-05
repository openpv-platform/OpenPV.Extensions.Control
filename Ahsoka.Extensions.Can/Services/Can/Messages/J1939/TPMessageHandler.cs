using Ahsoka.Core;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Messages;
internal class TPMessageHandler : J1939MessageHandlerBase
{
    enum SessionType
    {
        ReceiveCTS = 0,
        ReceiveBAM = 1,
        TransmitCTS = 2,
        TransmitBAM = 3,
    }

    Dictionary<SessionType, List<TPSession>> sessions = new();
    new readonly J1939ProtocolHandler Protocol = null;

    internal const uint childPDUF = 0xEB;

    protected TPMessageHandler(CanHandler messageHandler, J1939ProtocolHandler protocolHandler, CanServiceImplementation service)
        : base(messageHandler, protocolHandler, service, 0xEC, 0, 3)
    {
        Protocol = protocolHandler;
    }

    protected override bool IsEnabled()
    {
        if (Service.Self == null || Service.PromiscuousTransmit)
            return false;

        return Service.Self.TransportProtocol == TransportProtocol.J1939;
    }

    private static void Generate(CanPortConfiguration config)
    {
        NodeDefinition self = config.MessageConfiguration.Nodes.FirstOrDefault(x => x.NodeType == NodeType.Self);
        if (self != null && self.TransportProtocol == TransportProtocol.J1939)
        {
            var standardDefinitions = CanSystemInfo.StandardCanMessages;
            var standardMessage = standardDefinitions.Messages.First(x => x.Name == "TP_CM");
            var childMessage = standardDefinitions.Messages.First(x => x.Name == "TP_DT");
            config.MessageConfiguration.Messages.Add(standardMessage);
            config.MessageConfiguration.Messages.Add(childMessage);
        }
    }

    internal override void OnInit()
    {
        if (Enabled)
            foreach (SessionType session in Enum.GetValues(typeof(SessionType)))
                sessions.Add(session, new List<TPSession>());
        return;
    }

    internal override bool OnReceive(CanMessageData messageData)
    {
        if (!Enabled)
            return false;

        var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);
        if (j1939Id.PDUF != PDUF && j1939Id.PDUF != childPDUF)
            return false;

        lock (sessions)
        {
            foreach (var sessionType in sessions)
            {
                if (sessionType.Value.Count > 0)
                    if (sessionType.Value.First().HandleMessageReceive(messageData))
                        return true;
            }

            if (j1939Id.PDUF == PDUF)
            {
                var RTS = new J1939PropertyDefinitions.TPCM(BitConverter.ToUInt64(messageData.Data));

                var RTSId = new J1939PropertyDefinitions.Id(RTS.PGN << 8)
                {
                    SourceAddress = j1939Id.SourceAddress,
                    PDUS = j1939Id.PDUS,
                };

                if (RTS.ControlByte is J1939PropertyDefinitions.CMControl.RTS or J1939PropertyDefinitions.CMControl.BAM)
                {

                    SessionType sessionType = j1939Id.PDUS == J1939PropertyDefinitions.BroadcastAddress ? SessionType.ReceiveBAM : SessionType.ReceiveCTS;
                    if (sessions[sessionType].Count() == 0 &&
                        Protocol.InAvailableMessages(RTSId.WriteToUint(), true))
                    {
                        sessions[sessionType].Add(new TPSession(this, Protocol, Service, sessionType, messageData));
                        sessions[sessionType].First().StartTask();
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Multi-packet receive session started");
                    }
                    else if (j1939Id.PDUS == Protocol.CanState.CurrentAddress)
                    {
                        CanMessageData response = new()
                        {
                            Dlc = 8,
                            Id = CreateMessageId(Protocol.CanState.CurrentAddress, j1939Id.SourceAddress)
                        };
                        var abort = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.Abort, AbortReason = 1, AbortRole = 1, PGN = (messageData.Id >> 8) & 0x3FFFF };
                        response.Data = BitConverter.GetBytes(abort.WriteToUint());

                        var messageCollection = new CanMessageDataCollection
                        {
                            CanPort = Service.Port
                        };
                        messageCollection.Messages.Add(response);
                        Service.SendCanMessages(messageCollection);
                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"Multi-packet receive session aborted, already receiving");
                    }

                }
            }
        }


        return true;
    }

    internal override bool OnSend(SendInformation sendInfo, out CanMessageResult result)
    {
        try
        {
            result = null;
            if (!Enabled || (sendInfo.messageData != null && sendInfo.messageData.Dlc <= 8))
                return false;

            lock (sessions)
            {
                var j1939Id = new J1939PropertyDefinitions.Id(sendInfo.messageData.Id);
                SessionType sessionType = j1939Id.PDUS == J1939PropertyDefinitions.BroadcastAddress ? SessionType.TransmitBAM : SessionType.TransmitCTS;
                sessions[sessionType].Add(new TPSession(this, Protocol, Service, sessionType, sendInfo.messageData));

                //tech debt, must return error so message is not sent normally
                if (sessions[sessionType].Count() > 1)
                    result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"Message Queued, another message with of type {sessionType} is being transmitted" };
                else
                {
                    sessions[sessionType].First().StartTask();
                    result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"Multi-packet transmit session started" };
                }
            }

            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"{result.Message}");
        }
        catch (Exception ex)
        {
            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP send failed with message: {ex.Message}");
            result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"TP send failed with message: {ex.Message}" };
        }

        return true;
    }

    private void EndSession(TPSession session)
    {
        lock (sessions)
        {
            var sessionList = sessions[session.SessionType];
            sessionList.Remove(session);

            if (sessionList.Count > 0)
            {
                Thread.Sleep(10);
                sessionList.First().StartTask();
            }
        }
    }

    private class TPSession
    {
        const int Tb = 10;  //timeout min before sending BAM
        const int Tr = 200; //timeout max before sending next packet
        const int Th = 500; //timeout max between "hold connection" CTS
        const int T1 = 750; //timeout after receipt of last packet, more expected
        const int T2 = 1250; //timeout after a CTS transmission, originator failure
        const int T3 = 1250; //timeout due to lack of CTS or EndACK, responder failure
        const int T4 = 1050; //timeout from lack of "hold connection" CTS

        readonly TPMessageHandler handler;
        readonly CanServiceImplementation service;
        readonly J1939ProtocolHandler protocol;
        readonly CanMessageDataCollection messageCollection;

        Task sessionTask;
        int numberOfPackets;

        readonly List<CanMessageData> receivedMessages = new();
        readonly ManualResetEventSlim recievedEvent = new(false);
        readonly ManualResetEventSlim TPEvent = new(false);

        internal uint DestinationAddress { get; private set; } //for originator messages
        internal uint SourceAddress { get; private set; } //for originator messages
        internal SessionType SessionType { get; private set; }
        internal bool Transmitting { get { return SessionType > SessionType.ReceiveBAM; } }

        internal TPSession(TPMessageHandler handler, J1939ProtocolHandler protocol, CanServiceImplementation service, SessionType sessionType, CanMessageData messageData)
        {
            this.handler = handler;
            this.protocol = protocol;
            this.service = service;
            this.SessionType = sessionType;

            messageCollection = new CanMessageDataCollection
            {
                CanPort = service.Port
            };

            if (Transmitting)
                CreateTransmitSession(messageData);
            else
                CreateReceiveSession(messageData);

        }

        internal bool HandleMessageReceive(CanMessageData messageData)
        {
            var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);
            if ((Transmitting && j1939Id.PDUS == SourceAddress && j1939Id.SourceAddress == DestinationAddress) ||
                (!Transmitting && j1939Id.PDUS == DestinationAddress && j1939Id.SourceAddress == SourceAddress))
            {
                lock (receivedMessages)
                {
                    receivedMessages.Add(messageData);
                    recievedEvent.Set();
                    return true;
                }
            }

            return false;
        }

        internal void StartTask()
        {
            sessionTask.Start();
            sessionTask.ContinueWith(shutdown => handler.EndSession(this));
        }

        private void CreateTransmitSession(CanMessageData messageData)
        {
            numberOfPackets = (int)(messageData.Data.Length / 7 + (messageData.Dlc % 7 > 0 ? 1 : 0));

            var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);
            var PGN = (messageData.Id >> 8) & 0x3FFFF;
            DestinationAddress = j1939Id.PDUS;
            SourceAddress = protocol.CanState.CurrentAddress;

            sessionTask = new Task(() =>
            {

                if (SessionType == SessionType.TransmitBAM)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Transmit BAM Started");
                    var response = new CanMessageData
                    {
                        Id = handler.CreateMessageId(SourceAddress, J1939PropertyDefinitions.BroadcastAddress)
                    };
                    var TPCM = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.BAM, MessageSize = messageData.Dlc, NumPackets = (uint)numberOfPackets, PGN = PGN };
                    response.Dlc = 8;
                    response.Data = BitConverter.GetBytes(TPCM.WriteToUint());
                    messageCollection.Messages.Add(response);
                    service.SendCanMessages(messageCollection);

                    TPEvent.Wait(Tb);

                    response.Id = (handler.CreateMessageId(SourceAddress, J1939PropertyDefinitions.BroadcastAddress) & 0xFF00FFFF) | TPMessageHandler.childPDUF << 16;
                    SendPackets(messageData, response.Id, 1, (uint)numberOfPackets, Tb);
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Transmit BAM Completed");
                }
                else if (SessionType == SessionType.TransmitCTS)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Transmit Started");
                    var response = new CanMessageData
                    {
                        Id = handler.CreateMessageId(SourceAddress, DestinationAddress)
                    };
                    var RTS = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.RTS, MessageSize = messageData.Dlc, NumPackets = (uint)numberOfPackets, PacketsPerCTS = 0xFF, PGN = PGN };
                    response.Dlc = 8;
                    response.Data = BitConverter.GetBytes(RTS.WriteToUint());
                    messageCollection.Messages.Add(response);
                    service.SendCanMessages(messageCollection);

                    var delay = T3;
                    var transmissionFinished = false;
                    while (!transmissionFinished)
                    {
                        if (recievedEvent.Wait(delay))
                        {
                            var CTS = new J1939PropertyDefinitions.TPCM();
                            lock (receivedMessages)
                            {
                                CTS.ExtractValues(BitConverter.ToUInt64(receivedMessages.First().Data));
                                receivedMessages.Clear();
                                recievedEvent.Reset();
                            }

                            if (CTS.ControlByte == J1939PropertyDefinitions.CMControl.CTS)
                            {
                                if (CTS.PacketsRequested == 0)
                                {
                                    delay = T4;
                                    continue;
                                }
                                else if (CTS.PacketsRequested + CTS.PacketStart >= numberOfPackets + 1)
                                {
                                    CTS.PacketsRequested = (uint)(numberOfPackets - CTS.PacketStart) + 1;
                                }

                                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Transmit CTS Received sending {CTS.PacketsRequested} packets starting at {CTS.PacketStart}");
                                response.Id = (handler.CreateMessageId(SourceAddress, DestinationAddress) & 0xFF00FFFF) | TPMessageHandler.childPDUF << 16;
                                SendPackets(messageData, response.Id, CTS.PacketStart, CTS.PacketsRequested);
                                delay = T3;
                            }
                            else if (CTS.ControlByte is J1939PropertyDefinitions.CMControl.EndOfMsgACK)
                            {
                                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Transmit Completed");
                                transmissionFinished = true;
                            }
                            else if (CTS.ControlByte is J1939PropertyDefinitions.CMControl.Abort)
                            {
                                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Transmit received Abort Signal");
                                transmissionFinished = true;
                            }
                        }
                        else
                        {
                            response.Id = handler.CreateMessageId(SourceAddress, DestinationAddress);
                            var abort = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.Abort, AbortReason = 3, AbortRole = 0, PGN = PGN };
                            response.Data = BitConverter.GetBytes(abort.WriteToUint());
                            messageCollection.Messages.Clear();
                            messageCollection.Messages.Add(response);
                            service.SendCanMessages(messageCollection);
                            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Transmit Timeout");
                            transmissionFinished = true;
                        }
                    }
                }
            });
        }

        private void CreateReceiveSession(CanMessageData messageData)
        {
            var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);
            var PGN = (messageData.Id >> 8) & 0x3FFFF;
            DestinationAddress = j1939Id.PDUS;
            SourceAddress = j1939Id.SourceAddress;

            sessionTask = new Task(() =>
            {
                var sequenceNumber = 1;
                if (SessionType == SessionType.ReceiveBAM)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive BAM Started");
                    var BAM = new J1939PropertyDefinitions.TPCM(BitConverter.ToUInt64(messageData.Data));

                    var message = new CanMessageData();
                    var messageId = new J1939PropertyDefinitions.Id(BAM.PGN << 8)
                    {
                        SourceAddress = SourceAddress,
                        Priority = 3
                    };
                    message.Id = messageId.WriteToUint();
                    message.Dlc = BAM.MessageSize;
                    message.Data = new byte[message.Dlc];

                    while (sequenceNumber <= BAM.NumPackets)
                    {
                        if (recievedEvent.Wait(T1))
                        {
                            lock (receivedMessages)
                            {
                                foreach (var dt in receivedMessages)
                                {
                                    if (dt.Data[0] == sequenceNumber)
                                    {
                                        var bytesToCopy = sequenceNumber == BAM.NumPackets ? message.Dlc % 7 : 7;
                                        Array.Copy(dt.Data, 1, message.Data, 7 * (sequenceNumber - 1), bytesToCopy);
                                        sequenceNumber++;
                                    }
                                }
                                receivedMessages.Clear();
                                recievedEvent.Reset();
                            }
                        }
                        else
                        {
                            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive BAM Timed Out");
                            return;
                        }
                    }

                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive BAM Completed");
                    messageCollection.Messages.Clear();
                    messageCollection.Messages.Add(message);
                    service.Service.NotifyCanMessages(messageCollection);
                }
                else if (SessionType == SessionType.ReceiveCTS)
                {
                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive Started");
                    var RTS = new J1939PropertyDefinitions.TPCM(BitConverter.ToUInt64(messageData.Data));
                    if (RTS.PacketsPerCTS > RTS.NumPackets)
                        RTS.PacketsPerCTS = RTS.NumPackets;

                    var message = new CanMessageData();
                    var messageId = new J1939PropertyDefinitions.Id(RTS.PGN << 8)
                    {
                        SourceAddress = SourceAddress,
                        Priority = 3
                    };
                    message.Id = messageId.WriteToUint();
                    message.Dlc = RTS.MessageSize;
                    message.Data = new byte[message.Dlc];

                    var receiveFinished = false;
                    var nextPacket = 1;
                    var numRetransmits = 0;
                    var errorOccured = false;
                    CanMessageData response = new()
                    {
                        Dlc = 8,
                        Id = handler.CreateMessageId(protocol.CanState.CurrentAddress, SourceAddress)
                    };
                    while (!receiveFinished)
                    {
                        if (sequenceNumber > RTS.NumPackets)
                        {
                            var endACK = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.EndOfMsgACK, MessageSize = RTS.MessageSize, NumPackets = RTS.NumPackets, PGN = PGN };
                            response.Data = BitConverter.GetBytes(endACK.WriteToUint());
                            messageCollection.Messages.Clear();
                            messageCollection.Messages.Add(response);
                            service.SendCanMessages(messageCollection);

                            messageCollection.Messages.Clear();
                            messageCollection.Messages.Add(message);
                            service.Service.NotifyCanMessages(messageCollection);
                            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive Completed");
                            receiveFinished = true;
                        }
                        else
                        {
                            var packetsRequested = errorOccured ? 1 : RTS.PacketsPerCTS;
                            nextPacket = (int)(Math.Min(sequenceNumber + packetsRequested, RTS.NumPackets + 1));
                            var CTS = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.CTS, PacketsRequested = packetsRequested, PacketStart = (uint)sequenceNumber, PGN = PGN };
                            response.Data = BitConverter.GetBytes(CTS.WriteToUint());
                            messageCollection.Messages.Clear();
                            messageCollection.Messages.Add(response);
                            lock (receivedMessages)
                                receivedMessages.Clear();
                            service.SendCanMessages(messageCollection);
                            var delay = T2;

                            AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive requesting {packetsRequested} packets starting from {sequenceNumber}");
                            while ((sequenceNumber < nextPacket) && !receiveFinished)
                            {
                                errorOccured = false;
                                if (recievedEvent.Wait(delay))
                                {
                                    lock (receivedMessages)
                                    {
                                        foreach (var dt in receivedMessages)
                                        {
                                            J1939PropertyDefinitions.Id dtId = new(dt.Id);
                                            if (dtId.PDUF == handler.PDUF)
                                            {
                                                var TPCM = new J1939PropertyDefinitions.TPCM(BitConverter.ToUInt64(dt.Data));
                                                if (TPCM.ControlByte == J1939PropertyDefinitions.CMControl.Abort)
                                                {
                                                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Received Abort Message");
                                                    errorOccured = true;
                                                    numRetransmits = 3;
                                                    break;
                                                }
                                            }
                                            else if (dt.Data[0] == sequenceNumber)
                                            {
                                                var bytesToCopy = sequenceNumber == RTS.NumPackets ? message.Dlc % 7 : 7;
                                                Array.Copy(dt.Data, 1, message.Data, 7 * (sequenceNumber - 1), bytesToCopy);
                                                sequenceNumber++;
                                                delay = T1;
                                            }
                                            else
                                            {
                                                AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive Out of Order");
                                                errorOccured = true;
                                                break;
                                            }
                                        }
                                        receivedMessages.Clear();
                                        recievedEvent.Reset();
                                    }

                                }
                                else
                                {
                                    AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive Timeout");
                                    errorOccured = true;
                                }

                                if (errorOccured)
                                {
                                    if (++numRetransmits > 2)
                                    {
                                        AhsokaLogging.LogMessage(AhsokaVerbosity.Medium, $"TP Receive Aborted");
                                        var abort = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.Abort, AbortReason = 5, AbortRole = 1, PGN = PGN };
                                        response.Data = BitConverter.GetBytes(abort.WriteToUint());
                                        messageCollection.Messages.Clear();
                                        messageCollection.Messages.Add(response);
                                        service.SendCanMessages(messageCollection);
                                        receiveFinished = true;
                                    }
                                    break;
                                }

                            }
                        }
                    }
                }
            });
        }

        private void SendPackets(CanMessageData messageData, uint packetId, uint sequenceNumber, uint numPacketsToSend, int delay = 0)
        {
            messageCollection.Messages.Clear();
            for (uint i = sequenceNumber; i < sequenceNumber + numPacketsToSend; i++)
            {
                CanMessageData packet = new()
                {
                    Id = packetId,
                    Dlc = 8,
                    Data = [(byte)i, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]
                };
                var bytesToCopy = i == numberOfPackets ? messageData.Dlc % 7 : 7;
                Array.Copy(messageData.Data, 7 * (i - 1), packet.Data, 1, bytesToCopy);
                messageCollection.Messages.Add(packet);

                //transmit groups of six messages, helps smooth large transmissions
                if (delay > 0 || (i - sequenceNumber + 1) % 6 == 0)
                {
                    service.SendCanMessages(messageCollection);
                    messageCollection.Messages.Clear();
                    TPEvent.Wait(delay);
                }
            }
            if (messageCollection.Messages.Count > 0)
                service.SendCanMessages(messageCollection);

            TPEvent.Wait(Tr / 2);
        }
    }

}