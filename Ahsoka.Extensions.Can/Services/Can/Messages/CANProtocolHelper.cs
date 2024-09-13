using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
