using DistCache.Common.NetworkManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Server.Protocol.Handlers
{
    public class ServerToServerProtocolHandler : SocketHandler
    {
        public ServerToServerProtocolHandler(SocketHandler other) : base(other)
        {
        }
    }
}
