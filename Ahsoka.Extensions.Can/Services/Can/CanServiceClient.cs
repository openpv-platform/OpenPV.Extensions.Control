using Ahsoka.Core;
using System.Collections.Generic;

namespace Ahsoka.Services.Can;

/// <summary>
/// Services for Managing Communications on a CAN Network via an outboard Real Time CAN Gateway
/// </summary>
public class CanServiceClient : AhsokaClientBase<CanMessageTypes.Ids>
{
    /// <summary>
    /// Get Active Calibrations
    /// </summary>
    public CanApplicationConfiguration Calibrations { get; private set; }

    /// <summary>
    /// Default Constructor that uses the System Default Configuration
    /// </summary>
    public CanServiceClient(ICanMetadata metadata = null) :
        this(ConfigurationLoader.GetServiceConfig(CanService.Name), metadata)
    {

    }


    /// <summary>
    /// Constructor for use with Custom Service Configurations
    /// </summary>
    /// <param name="config"></param>
    /// <param name="metadata"></param>
    public CanServiceClient(ServiceConfiguration config, ICanMetadata metadata) :
        base(config, new CanServiceMessages())
    {
        CanExtension.Metadata = metadata;
    }

    /// <summary>
    /// Creates a Service for Use with this Client when running Local Services
    /// </summary>
    /// <returns></returns>
    protected override IAhsokaServiceEndPoint OnCreateDefaultService()
    {
        return new CanService(this.ServiceConfig);
    }

    /// <summary>
    /// Open Commuication with CAN Handler (Socket CAN or CoProcessor)
    /// </summary>
    public void OpenCommunicationChannel()
    {
        Calibrations = SendMessageWithResponse<CanApplicationConfiguration>(CanMessageTypes.Ids.OpenCommunicationChannel);
    }

    /// <summary>
    /// Open Commuication with CAN Handler (Socket CAN or CoProcessor)
    /// </summary>
    public void CloseCommunicationChannel()
    {
        SendMessageWithResponse<EmptyNotification>(CanMessageTypes.Ids.CloseCommunicationChannel);
        Calibrations = null;
    }


    /// <summary>
    ///  Send a Raw CAN Message
    /// </summary>
    /// <param name="canPort">Can Port to Send Messages On</param>
    /// <param name="messages">Messages to Send</param>
    public CanMessageResult SendCanMessages(uint canPort, params CanMessageData[] messages)
    {
        CanMessageDataCollection collection = new()
        {
            CanPort = canPort
        };
        collection.Messages.AddRange(messages);

        return SendMessageWithResponse<CanMessageResult>(CanMessageTypes.Ids.SendCanMessages, collection);
    }

    /// <summary>
    /// Send a Model Based CAN Message;
    /// </summary>
    /// <param name="canPort">Can Port to Send Messages On</param>
    /// <param name="messages"></param>
    public CanMessageResult SendCanMessages(uint canPort, params IHasCanData[] messages)
    {
        List<CanMessageData> collection = new();
        foreach (var msg in messages)
            collection.Add(msg.CreateCanMessageData());

        return SendCanMessages(canPort, collection.ToArray());
    }

    /// <summary>
    ///  Send a Raw CAN Message
    /// </summary>
    /// <param name="message"></param>
    public CanMessageResult SendRecurringCanMessage(RecurringCanMessage message)
    {
        return SendMessageWithResponse<CanMessageResult>(CanMessageTypes.Ids.SendRecurringCanMessage, message);
    }

    /// <summary>
    /// Filter the Incoming Messages for a Can Port.   To clear, send an empty filter list.
    /// </summary>
    /// <param name="filter"></param>
    public void ApplyCanFilter(ClientCanFilter filter)
    {
        SendMessageWithResponse(CanMessageTypes.Ids.ApplyMessageFilter, filter);
    }
}
