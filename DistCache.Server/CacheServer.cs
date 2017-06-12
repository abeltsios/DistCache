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
        private ConcurrentDictionary<Guid, HandShakeServerHandler> authPendingClients = new ConcurrentDictionary<Guid, HandShakeServerHandler>();
        private ConcurrentDictionary<Guid, SocketHandler> Clients = new ConcurrentDictionary<Guid, SocketHandler>();
        private ConcurrentDictionary<Guid, SocketHandler> Servers = new ConcurrentDictionary<Guid, SocketHandler>();
        public int ConnectedClientsCount => Clients.Count;
        public int ConnectedServersCount => Clients.Count;
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
                    h.Shutdown();
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
                Clients.TryAdd(registeredGuid, new SocketHandler(client.PassSocket(), Config));
                Clients[registeredGuid].Start();
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CacheServer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
