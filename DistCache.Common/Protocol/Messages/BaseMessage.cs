using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Protocol.Messages
{
    public class BaseMessage
    {
        public MessageTypeEnum MessageType { get; set; }
    }

    public class BaseRequest : BaseMessage
    {

        public BaseRequest() : base()
        {
            this.MessageType = MessageTypeEnum.Request;
        }
        public Guid RequestId { get; set; }
    }

    public class EchoRequest : BaseRequest
    {
        public string Echo { get; set; }
    }

    public class BaseResponse : BaseMessage
    {

        public BaseResponse() : base()
        {
            this.MessageType = MessageTypeEnum.Reponse;
        }
        public Guid RequestId { get; set; }
    }

    public class EchoResponse : BaseResponse
    {
        public string Echo { get; set; }
    }
}
