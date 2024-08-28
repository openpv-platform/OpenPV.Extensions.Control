namespace Ahsoka.Services.Can.Messages;
internal abstract class J1939MessageHandlerBase : BaseMessageHandler
{
    protected uint PDUF { get; init; }
    protected uint DataPage { get; init; }
    protected uint Priority { get; init; }

    protected J1939MessageHandlerBase(CanHandler messageHandler, J1939ProtocolHandler protocolHandler, CanServiceImplementation service, uint pduf, uint dataPage, uint priority)
        : base(messageHandler, protocolHandler, service)
    {
        this.PDUF = pduf;
        this.DataPage = dataPage;
        this.Priority = priority;
    }

    protected override bool IsEnabled()
    {
        if (Service.Self == null)
            return false;

        return Service.Self.TransportProtocol == TransportProtocol.J1939;
    }

    internal uint CreateMessageId(uint sourceAddress, uint destinationAddress)
    {
        var id = new J1939Helper.Id
        {
            Priority = Priority,
            DataPage = DataPage,
            PDUF = PDUF,
            PDUS = destinationAddress,
            SourceAddress = sourceAddress
        };
        return id.WriteToUint();
    }
}
