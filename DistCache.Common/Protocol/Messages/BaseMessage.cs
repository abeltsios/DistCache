using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Server.Protocol.Messages
{
    public class BaseMessage
    {
        public MessageTypeEnum MessageType { get; set; }
    }
}
