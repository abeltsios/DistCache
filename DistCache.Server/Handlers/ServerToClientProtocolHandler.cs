using DistCache.Common.NetworkManagement;
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
    }
}
