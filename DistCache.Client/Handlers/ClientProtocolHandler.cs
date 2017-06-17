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
        private ConcurrentDictionary<Guid, ManualResetEventSlim> _messagesW = new ConcurrentDictionary<Guid, ManualResetEventSlim>();

        public ClientProtocolHandler(SocketHandler socket) : base(socket)
        {
        }

        protected override bool HandleMessages(byte[] message)
        {
            var deserd = BsonUtilities.Deserialise<BaseMessage>(message);
            Console.WriteLine($"BaseMessage recieved:{deserd.MessageType}");
            if (deserd is EchoResponse)
            {
                var req = (deserd as EchoResponse);
                Console.WriteLine("\t" + $"EchoResponse recieved:{req.MessageType}");
                _messagesW[req.RequestId].Set();
            }
            else
            {
                throw new Exception("aaaaaaaa");
            }
            return true;
        }

        internal void RegisterWaiter(Guid req, ManualResetEventSlim m)
        {
            _messagesW.TryAdd(req, m);
        }

        internal void UnregisterWaiter(Guid req)
        {
            _messagesW.TryRemove(req, out ManualResetEventSlim m);
        }
    }
}
