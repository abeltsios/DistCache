using DistCache.Common.NetworkManagement;
using DistCache.Common.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistCache.Common.Protocol.Messages;

namespace DistCache.Server.Protocol.Handlers
{
    public class ServerToServerProtocolHandler : BaseProtocolHandler
    {
        public ServerToServerProtocolHandler(SocketHandler other) : base(other)
        {
        }

        protected override void HandleRequest(BaseRequest baseRequest)
        {
            throw new NotImplementedException();
        }

        protected override void HandleResponse(BaseResponse baseResponse)
        {
            throw new NotImplementedException();
        }
    }
}
