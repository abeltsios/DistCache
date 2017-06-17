using DistCache.Common.NetworkManagement;
using DistCache.Common.Utilities;
using DistCache.Common.Protocol.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Server.Protocol.Handlers
{
    public class ServerToClientProtocolHandler : SocketHandler
    {
        public ServerToClientProtocolHandler(SocketHandler other) : base(other)
        {
        }

        protected override bool HandleMessages(byte[] message)
        {
            var deserd = BsonUtilities.Deserialise<BaseMessage>(message);
            if (deserd is EchoRequest)
            {
                var req = (deserd as EchoRequest);
                Console.WriteLine("EchoRequest rec:" + req.Echo);
                SendMessage(new EchoResponse() { RequestId = req.RequestId, Echo = req.Echo });
            }
            else
            {
                throw new Exception("aaaaaaaa");
            }
            return true;
        }
    }
}
