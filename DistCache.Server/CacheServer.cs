using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        public DistCacheServerConfig Config { get; private set; }
        private ConcurrentDictionary<Guid, HandShakeServerHandler> authPendingClients = new ConcurrentDictionary<Guid, HandShakeServerHandler>();
        private ConcurrentDictionary<Guid, SocketHandler> Clients = new ConcurrentDictionary<Guid, SocketHandler>();
        private ConcurrentDictionary<Guid, SocketHandler> Servers = new ConcurrentDictionary<Guid, SocketHandler>();
        public int ConnectedClientsCount => Clients.Count;
        public int ConnectedServersCount => Clients.Count;
        public int ConnectedTotalCount => Clients.Count + Servers.Count;


        private Action maintenaceActions;

        public CacheServer(DistCacheServerConfig config)
        {
            this.Config = config;
            this._socketServer = new SocketServer(this.Config.GetEndpointToBind());
            this._otherServers = new HashSet<IPEndPoint>(this.Config.GetClusterAddresses());
            this._socketServer.Start();
            this._socketServer.ConnectionAccepted += ConnectionAccepted;

            maintenaceActions += () =>
            {
                foreach (var kvp in authPendingClients.ToList())
                {
                    try
                    {
                        if (!kvp.Value.SocketStatus)
                        {
                            kvp.Value.Shutdown();
                            if (authPendingClients.TryRemove(kvp.Key, out HandShakeServerHandler handler))
                            {
                                handler.Dispose();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        //todo log
                    }
                }
            };
        }



        private void ConnectionAccepted(object sender, TcpClient socket)
        {
            var tempGuid = Guid.NewGuid();
            authPendingClients[tempGuid] = new HandShakeServerHandler(socket, this, tempGuid);
        }



        public void ClientConnected(Guid registeredGuid, Guid tempGuid)
        {
            if (authPendingClients.TryRemove(tempGuid, out HandShakeServerHandler client))
            {
                //this is a new valid client
                Clients.TryAdd(registeredGuid, new SocketHandler(client.Connection, Config));
            }
            else
            {
                //todo log
            }
        }

        public void ServerConnected(TcpClient newclient, Guid registeredGuid, Guid tempGuid)
        {
            if (authPendingClients.TryRemove(tempGuid, out HandShakeServerHandler client))
            {
                //this is a new valid client
            }
            else
            {
                //todo log
            }
        }

        internal void UnknownConnectionFailed(TcpClient tcp, Guid? temporaryID)
        {
            throw new NotImplementedException();
        }
    }
}
