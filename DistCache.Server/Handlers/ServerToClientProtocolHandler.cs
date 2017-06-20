using DistCache.Common.NetworkManagement;
using DistCache.Common.Utilities;
using DistCache.Common.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistCache.Common.Protocol;

namespace DistCache.Server.Protocol.Handlers
{
    public class ServerToClientProtocolHandler : BaseProtocolHandler
    {
        public ServerToClientProtocolHandler(SocketHandler other) : base(other)
        {
        }

        protected override void HandleRequest(BaseRequest baseRequest)
        {
            switch (baseRequest.MessageSubtype)
            {
                case MessageSubTypeEnum.Echo:
                    {
                        SendMessage(baseRequest.CreateResponse());
                        break;
                    }
                default:
                    {
                        throw new Exception("Unknown message type");
                    }
            }
        }

        protected override void HandleResponse(BaseResponse baseResponse)
        {
            throw new Exception("should be here for now");
        }
    }
}
