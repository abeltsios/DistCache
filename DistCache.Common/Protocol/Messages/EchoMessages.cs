using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Protocol.Messages
{
    public class EchoRequest : BaseRequest
    {
        public EchoRequest() : base()
        {
            this.MessageSubtype = MessageSubTypeEnum.Echo;
        }
        public string Echo { get; set; }

        public override BaseResponse CreateResponse()
        {
            return new EchoResponse()
            {
                Echo = this.Echo,
                RequestId = this.RequestId
            };
        }
    }

    public class EchoResponse : BaseResponse
    {
        public EchoResponse() : base()
        {
            this.MessageSubtype = MessageSubTypeEnum.Echo;
        }
        public string Echo { get; set; }
    }
}
