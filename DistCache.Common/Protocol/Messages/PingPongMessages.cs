using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Protocol.Messages
{
    public class PingMessage:BaseRequest
    {
        public PingMessage():base()
        {
            MessageType = MessageTypeEnum.Request;
        }

        public override BaseResponse CreateResponse()
        {
            throw new NotImplementedException();
        }
    }
}
