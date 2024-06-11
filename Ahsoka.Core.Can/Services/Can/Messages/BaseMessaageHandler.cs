namespace Ahsoka.Services.Can.Messages;
internal abstract class BaseMessageHandler
{
    protected CanHandler MessageHandler { get; init; }
    protected BaseProtocolHandler Protocol { get; init; }
    protected CanServiceImplementation Service { get; init; }
    internal bool Enabled { get; init; }

    protected BaseMessageHandler(CanHandler messageHandler, BaseProtocolHandler protocolHandler, CanServiceImplementation service)
    {
        this.MessageHandler = messageHandler;
        this.Protocol = protocolHandler;
        this.Service = service;
        this.Enabled = IsEnabled();

        OnInit();
    }

    protected abstract bool IsEnabled();

    protected abstract void OnInit();

    internal abstract bool OnSend(SendInformation sendInfo, out CanMessageResult result);

    internal abstract bool OnReceive(CanMessageData messageData);
}