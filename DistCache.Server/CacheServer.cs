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


        public CacheServer(int port, IEnumerable<IPEndPoint> others) : this(new IPEndPoint(IPAddress.Any, port), others)
        {
        }

        public CacheServer(IPEndPoint bindEndPoint, IEnumerable<IPEndPoint> others)
        {
            this._socketServer = new SocketServer(bindEndPoint);
            this._otherServers = new HashSet<IPEndPoint>(others ?? new List<IPEndPoint>());
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
