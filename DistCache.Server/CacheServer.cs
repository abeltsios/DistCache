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
    public class CacheServer : IDisposable
    {
        private SocketServer _socketServer;
        private HashSet<IPEndPoint> _otherServers;
        public Guid ServerGuid { get; private set; } = Guid.NewGuid();
        public DistCacheServerConfig Config { get; private set; }
        private ConcurrentDictionary<Guid, ServerHandShakeHandler> authPendingClients = new ConcurrentDictionary<Guid, ServerHandShakeHandler>();
        private ConcurrentDictionary<Guid, SocketHandler> Clients = new ConcurrentDictionary<Guid, SocketHandler>();
        private ConcurrentDictionary<Guid, SocketHandler> Servers = new ConcurrentDictionary<Guid, SocketHandler>();
        public int ConnectedClientsCount => Clients.Count;
        public int PendingClientsCount => authPendingClients.Count;
        public int ConnectedServersCount => Servers.Count;
        public int ConnectedTotalCount => Clients.Count + Servers.Count;

        public void Shutdown()
        {
            try
            {
                _socketServer.Stop();
            }
            catch (Exception e)
            {

            }

            _socketServer = null;

            var cons = new List<SocketHandler>(authPendingClients.Values);
            cons.AddRange(Clients.Values);
            cons.AddRange(Servers.Values);
            foreach (var h in cons)
            {
                try
                {
                    h.Dispose();
                }
                catch (Exception ex)
                {
                }
            }
        }

        private Action maintenaceActions;

        public CacheServer(DistCacheServerConfig config, bool restartOnFailure = true)
        {
            this.Config = config;
            this._socketServer = new SocketServer(this.Config.GetEndpointToBind(), restartOnFailure);
            this._otherServers = new HashSet<IPEndPoint>(this.Config.GetClusterAddresses());
            this._socketServer.AcceptSocket();
            this._socketServer.ConnectionAccepted += ConnectionAccepted;

            maintenaceActions += () =>
            {
                //foreach (var kvp in authPendingClients.ToList())
                //{
                //    try
                //    {
                //        if (!kvp.Value.SocketStatus)
                //        {
                //            kvp.Value.Shutdown();
                //            if (authPendingClients.TryRemove(kvp.Key, out ServerHandShakeHandler handler))
                //            {
                //                handler.Dispose();
                //            }
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        //todo log
                //    }
                //}
            };
        }

        private void ConnectionAccepted(object sender, TcpClient socket)
        {
            var tempGuid = Guid.NewGuid();
            var newClient = new ServerHandShakeHandler(socket, this, tempGuid);
            authPendingClients[tempGuid] = newClient;
            newClient.Initiate();
        }

        public void ClientConnected(Guid registeredGuid, Guid tempGuid)
        {
            if (authPendingClients.TryRemove(tempGuid, out ServerHandShakeHandler client))
            {
                var h = new ServerToClientProtocolHandler(client);
                //this is a new valid client
                Clients.TryAdd(registeredGuid, h);
                h.Initiate();
            }
            else
            {
                //todo log
            }
        }

        public void ServerConnected(Guid registeredGuid, Guid tempGuid)
        {
            if (authPendingClients.TryRemove(tempGuid, out ServerHandShakeHandler client))
            {
                //this is a new valid client
            }
            else
            {
                //todo log
            }
        }

        internal void ConnectionFailed(SocketHandler sockHandler, Guid? temporaryID)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            Shutdown();

        }
    }
}
