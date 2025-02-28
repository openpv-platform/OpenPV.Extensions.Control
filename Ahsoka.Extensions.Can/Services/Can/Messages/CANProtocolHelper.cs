namespace Ahsoka.Services.Can.Messages
{
    public class CANProtocolHelper
    {
        protected CanMessageData data;
        public CANProtocolHelper(CanMessageData data)
        {
            this.data = data;
        }
    }
}
