using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Protocol.Messages
{
    public class HandShakeRequest:BaseMessage
    {
        public HandShakeRequest()
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
