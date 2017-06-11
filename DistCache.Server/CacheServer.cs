using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using DistCache.Common.NetworkManagement;
using DistCache.Server.Protocol.Handlers;

namespace DistCache.Server
{
    public class CacheServer
    {
        private SocketServer _socketServer;
        private HashSet<IPEndPoint> _otherServers;
        public Guid ServerGuid { get; private set; } = Guid.NewGuid();
        public DistCacheServerConfig _config { get; private set; }

        public CacheServer(DistCacheServerConfig config)
        {
            this._config = config;
            this._socketServer = new SocketServer(_config.GetEndpointToBind());
            this._otherServers = new HashSet<IPEndPoint>(_config.GetClusterAddresses());
            this._socketServer.Start();
            this._socketServer.ConnectionAccepted += ConnectionAccepted;
        }



        private void ConnectionAccepted(object sender, TcpClient socket)
        {
            new HandShakeHandler(socket, this);
        }

        public void ClientConnected(TcpClient newclient, Guid registeredGuid, Guid tempGuid)
        {

        }

        public void ServerConnected(TcpClient newclient, Guid registeredGuid, Guid tempGuid)
        {

        }

        internal void UnknownConnectionFailed(TcpClient tcp, Guid temporaryID)
        {
            throw new NotImplementedException();
        }
    }
}
