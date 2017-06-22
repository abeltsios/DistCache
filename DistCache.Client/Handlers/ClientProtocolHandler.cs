using DistCache.Common.NetworkManagement;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DistCache.Common.Protocol.Messages;
using DistCache.Common.Utilities;
using System.Net.Sockets;
using DistCache.Common.Protocol;

namespace DistCache.Client.Protocol.Handlers
{
    public class ClientProtocolHandler : BaseProtocolHandler
    {

        public ClientProtocolHandler(SocketHandler socket) : base(socket)
        {
        }

        protected override void HandleRequest(BaseRequest baseRequest)
        {
            switch (baseRequest.MessageSubtype)
            {
                default:
                    {
                        throw new Exception("Unknown message type");
                    }
            }
        }

        protected override void HandleResponse(BaseResponse baseResponse)
        {
            if (_tasksAwaitingResponse.TryGetValue(baseResponse.RequestId, out TaskCompletionDisposableSource toSet))
            {
                toSet.TrySetResult(baseResponse);
            }
            else
            {

            }
        }
    }
}
