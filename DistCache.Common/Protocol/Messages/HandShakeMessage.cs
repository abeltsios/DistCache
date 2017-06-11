using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Server.Protocol.Messages
{
    public class HandShakeMessage:BaseMessage
    {
        public HandShakeMessage()
        {
        }

        public string AuthPassword { get; set; }

        public Guid RegisteredGuid { get; set; }
    }

    public class HandShakeOutcome : BaseMessage
    {
        public HandShakeOutcome()
        {
        }

        public Guid ServerGuid { get; set; }
    }
}
