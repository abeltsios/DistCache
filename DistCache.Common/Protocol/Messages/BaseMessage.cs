using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Protocol.Messages
{
    public enum MessageTypeEnum
    {
        ClientAuthRequest = 1,
        ServerAuthRequest,
        AuthRequestOk,
        AuthRequestError,
        KeepAlive,
        Request,
        Reponse,
    }

    public enum MessageSubTypeEnum
    {
        Echo = 1,
        PingPong
    }

    public abstract class BaseMessage
    {
        public MessageTypeEnum MessageType { get; set; }
    }

    public abstract class BaseRequest : BaseMessage
    {

        public BaseRequest() : base()
        {
            this.MessageType = MessageTypeEnum.Request;
        }
        public Guid RequestId { get; set; }
        public MessageSubTypeEnum MessageSubtype { get; set; }

        public abstract BaseResponse CreateResponse();
    }

    public abstract class BaseResponse : BaseMessage
    {
        public BaseResponse() : base()
        {
            this.MessageType = MessageTypeEnum.Reponse;
        }
        public MessageSubTypeEnum MessageSubtype { get; set; }
        public Guid RequestId { get; set; }
    }
}
