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

namespace DistCache.Client.Protocol.Handlers
{
    public class ClientProtocolHandler : SocketHandler
    {
        private ConcurrentDictionary<Guid, TaskCompletionSource<object>> _messagesW = new ConcurrentDictionary<Guid, TaskCompletionSource<object>>();

        public ClientProtocolHandler(SocketHandler socket) : base(socket)
        {
        }

        protected override bool HandleMessages(byte[] message)
        {
            var deserd = BsonUtilities.Deserialise<BaseMessage>(message);
            var req = (deserd as BaseResponse);
            _messagesW[req.RequestId].SetResult(req);
            return true;
        }

        internal void RegisterWaiter(Guid req, TaskCompletionSource<object> m)
        {
            _messagesW.TryAdd(req, m);
        }

        internal void UnregisterWaiter(Guid req)
        {
            _messagesW.TryRemove(req, out TaskCompletionSource<object> m);
        }
    }
}
