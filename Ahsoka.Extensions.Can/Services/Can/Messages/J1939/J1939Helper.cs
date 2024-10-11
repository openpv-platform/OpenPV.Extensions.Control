using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahsoka.Services.Can.Messages
{
    public class J1939Helper : CANProtocolHelper
    {
        J1939PropertyDefinitions.Id id;

        public J1939Helper(CanMessageData data) : base(data)
        {
            id = new J1939PropertyDefinitions.Id(data.Id);
        }

        public uint PGN { get { return id.PGN; } }

        public uint Priority
        {
            get { return id.Priority; }
            set { id.Priority = value; data.Id = id.WriteToUint(); }
        }

        public uint SourceAddress
        {
            get { return id.SourceAddress; }
            set { id.SourceAddress = value; data.Id = id.WriteToUint(); }
        }

        public uint DestinationAddress
        {
            get { return id.PDUS; }
            set { id.PDUS = value; data.Id = id.WriteToUint(); }
        }
    }
}
