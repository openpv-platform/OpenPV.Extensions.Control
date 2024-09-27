using Ahsoka.ServiceFramework;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Messages;
internal class TPMessageHandler : J1939MessageHandlerBase
{
    static readonly List<TPSession> sessions = new();
    new readonly J1939ProtocolHandler Protocol = null;

    internal const uint childPDUF = 0xEB;

    protected TPMessageHandler(CanHandler messageHandler, J1939ProtocolHandler protocolHandler, CanServiceImplementation service)
        : base(messageHandler, protocolHandler, service, 0xEC, 0, 3)
    { 
        Protocol = protocolHandler;
    }

    protected override bool IsEnabled()
    {
        if (Service.Self == null)
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

    protected override void OnInit()
    {
        return;
    }

    internal override bool OnReceive(CanMessageData messageData)
    {
        if (!Enabled)
            return false;

        var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);
        if (j1939Id.PDUF != PDUF && j1939Id.PDUF != childPDUF)
            return false;

        foreach (var session in sessions)
        {
            if (session.HandleMessageReceive(messageData))
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

            if (sessions.Where(x => !x.Transmitting).Count() < 2 &&
                !sessions.Where(x => !x.Transmitting && x.DestinationAddress == j1939Id.PDUS).Any() &&
                Protocol.InAvailableMessages(RTSId.WriteToUint(), true))
            {
                sessions.Add(new TPSession(this, Protocol, Service));
                sessions.Last().StartReceiveSession(messageData);
            }
            else
            {
                CanMessageData response = new()
                {
                    Dlc = 8,
                    Id = CreateMessageId(Protocol.CanState.CurrentAddress, j1939Id.SourceAddress)
                };
                var abort = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.Abort, AbortReason = 1, AbortRole = 1, PGN = (messageData.Id >> 8) & 0x3FFFF };
                response.Data = BitConverter.GetBytes(RTS.WriteToUint());

                var messageCollection = new CanMessageDataCollection
                {
                    CanPort = Service.Port
                };
                messageCollection.Messages.Add(response);
                Service.SendCanMessages(messageCollection);
            }
        }

        return true;
    }

    internal override bool OnSend(SendInformation sendInfo, out CanMessageResult result)
    {
        result = null;
        if (!Enabled || (sendInfo.messageData != null && sendInfo.messageData.Dlc <= 8))
            return false;

        AhsokaLogging.LogMessage(AhsokaVerbosity.High, "TP Send");

        var j1939Id = new J1939PropertyDefinitions.Id(sendInfo.messageData.Id);
        if (sessions.Where(x => x.Transmitting).Count() == 2)
            result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"No space for additional Multi-packet sessions" };
        else if (sessions.Where(x => x.Transmitting && x.DestinationAddress == J1939PropertyDefinitions.BroadcastAddress).Count() == 1)
            result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"Can only send one broadcast message at a time" };
        else if (sessions.Where(x => x.Transmitting && x.DestinationAddress == j1939Id.PDUS).Count() == 1)
            result = new CanMessageResult() { Status = MessageStatus.Error, Message = $"Message to this address already in progress" };
        else
        {
            sessions.Add(new TPSession(this, Protocol, Service));
            sessions.Last().StartTransmitSession(sendInfo.messageData);
            result = new CanMessageResult() { Status = MessageStatus.Success, Message = $"Multi-packet transmit session started" };
        }

        return true;
    }

    private void EndSession(TPSession session)
    {
        sessions.Remove(session);
    }

    private class TPSession
    {
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
        int numberOfPackets;

        readonly List<CanMessageData> receivedMessages = new();
        readonly ManualResetEventSlim recievedEvent = new(false);
        readonly ManualResetEventSlim TPEvent = new(false);

        internal uint DestinationAddress { get; private set; } //for originator messages
        internal uint SourceAddress { get; private set; } //for originator messages
        internal bool Transmitting { get; private set; }

        internal TPSession(TPMessageHandler handler, J1939ProtocolHandler protocol, CanServiceImplementation service)
        {
            this.handler = handler;
            this.protocol = protocol;
            this.service = service;

            messageCollection = new CanMessageDataCollection
            {
                CanPort = service.Port
            };
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

        internal void StartTransmitSession(CanMessageData messageData)
        {
            Task.Run(() =>
            {
                numberOfPackets = (int)(messageData.Dlc / 7 + (messageData.Dlc % 7 > 0 ? 1 : 0));

                var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);
                var PGN = (messageData.Id >> 8) & 0x3FFFF;
                DestinationAddress = j1939Id.PDUS;
                SourceAddress = protocol.CanState.CurrentAddress;
                Transmitting = true;
                if (j1939Id.PDUS == J1939PropertyDefinitions.BroadcastAddress)
                {
                    var response = new CanMessageData
                    {
                        Id = handler.CreateMessageId(SourceAddress, J1939PropertyDefinitions.BroadcastAddress)
                    };
                    var TPCM = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.BAM, MessageSize = messageData.Dlc, NumPackets = (uint)numberOfPackets, PGN = PGN };
                    response.Dlc = 8;
                    response.Data = BitConverter.GetBytes(TPCM.WriteToUint());
                    messageCollection.Messages.Add(response);
                    service.SendCanMessages(messageCollection);

                    TPEvent.Wait(Tr / 2);

                    response.Id = (handler.CreateMessageId(SourceAddress, J1939PropertyDefinitions.BroadcastAddress) & 0xFF00FFFF) | TPMessageHandler.childPDUF << 16;
                    SendPackets(messageData, response, 1, (uint)numberOfPackets);
                }
                else
                {
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

                                response.Id = (handler.CreateMessageId(SourceAddress, DestinationAddress) & 0xFF00FFFF) | TPMessageHandler.childPDUF << 16;
                                SendPackets(messageData, response, CTS.PacketStart, CTS.PacketsRequested);
                                delay = T3;
                            }
                            else if (CTS.ControlByte is J1939PropertyDefinitions.CMControl.EndOfMsgACK or J1939PropertyDefinitions.CMControl.Abort)
                            {
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
                            transmissionFinished = true;
                        }
                    }
                }
            }).ContinueWith((result) =>
            {
                handler.EndSession(this);
            });
        }

        internal void StartReceiveSession(CanMessageData messageData)
        {
            Task.Run(() =>
            {
                var j1939Id = new J1939PropertyDefinitions.Id(messageData.Id);
                var PGN = (messageData.Id >> 8) & 0x3FFFF;
                DestinationAddress = j1939Id.PDUS;
                SourceAddress = j1939Id.SourceAddress;
                Transmitting = false;
                var sequenceNumber = 1;
                if (j1939Id.PDUS == J1939PropertyDefinitions.BroadcastAddress)
                {
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
                                    else break;
                                }
                                receivedMessages.Clear();
                                recievedEvent.Reset();
                            }
                        }
                        else break;
                    }

                    messageCollection.Messages.Clear();
                    messageCollection.Messages.Add(message);
                    service.Service.NotifyCanMessages(messageCollection);
                }
                else if (j1939Id.PDUS == protocol.CanState.CurrentAddress)
                {
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

                            while (sequenceNumber < nextPacket)
                            {
                                errorOccured = false;
                                if (recievedEvent.Wait(delay))
                                {
                                    lock (receivedMessages)
                                    {
                                        foreach (var dt in receivedMessages)
                                        {
                                            if (dt.Data[0] == sequenceNumber)
                                            {
                                                var bytesToCopy = sequenceNumber == RTS.NumPackets ? message.Dlc % 7 : 7;
                                                Array.Copy(dt.Data, 1, message.Data, 7 * (sequenceNumber - 1), bytesToCopy);
                                                sequenceNumber++;
                                                delay = T1;
                                            }
                                            else
                                            {
                                                errorOccured = true;
                                                break;
                                            }
                                        }
                                        receivedMessages.Clear();
                                        recievedEvent.Reset();
                                    }

                                }
                                else errorOccured = true;

                                if (errorOccured)
                                    if (++numRetransmits > 2)
                                    {
                                        var abort = new J1939PropertyDefinitions.TPCM() { ControlByte = J1939PropertyDefinitions.CMControl.Abort, AbortReason = 5, AbortRole = 1, PGN = PGN };
                                        response.Data = BitConverter.GetBytes(abort.WriteToUint());
                                        messageCollection.Messages.Clear();
                                        messageCollection.Messages.Add(response);
                                        service.SendCanMessages(messageCollection);
                                        receiveFinished = true;
                                    }
                                    else break;
                            }
                        }
                    }
                }
            }).ContinueWith((result) =>
            {
                handler.EndSession(this);
            });
        }

        private void SendPackets(CanMessageData messageData, CanMessageData packet, uint sequenceNumber, uint numPacketsToSend)
        {
            for (uint i = sequenceNumber; i < sequenceNumber + numPacketsToSend; i++)
            {
                messageCollection.Messages.Clear();
                packet.Data = new byte[] { (byte)i, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                var bytesToCopy = i == numberOfPackets ? messageData.Dlc % 7 : 7;
                Array.Copy(messageData.Data, 7 * (i - 1), packet.Data, 1, bytesToCopy);
                messageCollection.Messages.Add(packet);
                service.SendCanMessages(messageCollection);

                TPEvent.Wait(Tr / 2);
            }
        }
    }
}