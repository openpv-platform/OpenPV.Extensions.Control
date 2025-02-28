using Ahsoka.Core;
using static Ahsoka.Services.Can.SocketMessageEncoding;

namespace Ahsoka.Services.Can
{
    public class CanClientSocket : IAhsokaClientSocket
    {

        IAhsokaClientEndPoint endPoint;
        CanServiceClient canClient;
        bool shouldDisconnect = false;
        uint port;
        uint id;
        uint mask;

        public bool IsConnected { get; private set; }

        public CanClientSocket(uint port, uint id, uint mask = 0x1FFFFFFF)
        {
            this.port = port;
            this.id = id;
            this.mask = mask;
            canClient = AhsokaRuntime.Default.GetClient("CanClient") as CanServiceClient;
            if (canClient == null)
            {
                shouldDisconnect = true;
                canClient = new CanServiceClient();
            }
        }

        bool IAhsokaClientSocket.Connect(IAhsokaClientEndPoint endPoint)
        {
            this.endPoint = endPoint as IAhsokaClientEndPoint;
            if (canClient.Calibrations == null)
                canClient.OpenCommunicationChannel();

            canClient.NotificationReceived += CanClient_NotificationReceived;
            IsConnected = true;
            return IsConnected;
        }

        void IAhsokaClientSocket.Disconnect()
        {
            if (shouldDisconnect && canClient.Calibrations != null)
                canClient.CloseCommunicationChannel();
            IsConnected = false;
        }

        private void CanClient_NotificationReceived(object sender, AhsokaClientBase<CanMessageTypes.Ids>.AhsokaNotificationArgs e)
        {
            if (e.TransportId == CanMessageTypes.Ids.CanMessagesReceived &&
            e.NotificationObject is CanMessageDataCollection message)
            {
                foreach (var item in message.Messages)
                {
                    if ((item.Id & mask) == (id & mask))
                    {
                        MessageFromCAN(item, out AhsokaClientMessage received, out bool isNotification);

                        if (received.Header.EndpointId == endPoint.EndPointId || received.Header.ClientId == endPoint.GetClientId())
                        {
                            if (isNotification)
                                endPoint.HandleNotification(received);
                            else
                                endPoint.HandleResponse(received);
                        }
                    }
                }
            }
        }

        public void SendMessage(AhsokaClientMessage message)
        {
            message.Header.ClientId = endPoint.GetClientId();
            message.Header.EndpointId = endPoint.EndPointId;
            var canMessage = MessageToCAN(message, id, false);
            canClient.SendCanMessages(port, canMessage);
        }
    }
}
