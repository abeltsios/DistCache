using DistCache.Common.NetworkManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistCache.Common;
using System.Net.Sockets;

namespace DistCache.Server.Protocol.Handlers
{
    public class ClientProtocolHandler : SocketHandler
    {
        public ClientProtocolHandler(SocketHandler socket) : base(socket)
        {
        }
    }
}
