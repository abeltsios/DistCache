using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DistCache.Common;
using DistCache.Common.NetworkManagement;
using DistCache.Client.Handlers;

namespace DistCache.Client
{
    public class DistCacheClient : IDisposable
    {
        #region connection Factory
        public static DistCacheClient Create(DistCacheClientConfig config)
        {
            TcpClient tcp = null;
            IPEndPoint toTry;
            Dictionary<IPEndPoint, bool> invalidEndPoints = new Dictionary<IPEndPoint, bool>();


            while ((toTry = config.GetOrderedServerIpEndPoint().FirstOrDefault(i => !invalidEndPoints.ContainsKey(i))) != null)
            {
                try
                {
                    tcp = SocketHandler.CreateSocket(toTry, config);
                }
                catch (Exception ex)
                {
                    tcp = null;
                    invalidEndPoints[toTry] = true;
                }
                if (tcp != null)
                    break;
            }
            if (tcp != null)
                return new DistCacheClient(tcp, config);
            throw new Exception("could not connect to any node");
        }

        #endregion

        public readonly Guid ClientId = Guid.NewGuid();

        private DistCacheClient(TcpClient tcp, DistCacheClientConfig config)
        {
            bool res;
            using (var hs = new HandShakeClientHandler(tcp, config, this.ClientId))
            {
                res = hs.VerifyConnection();
                this._tcp = hs.PassSocket();

            }
            if (!res)
            {
                throw new Exception("invalid log in or connection error");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private TcpClient _tcp;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        _tcp.Close();
                    }
                    catch (Exception)
                    {
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DistCacheClient() {
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
