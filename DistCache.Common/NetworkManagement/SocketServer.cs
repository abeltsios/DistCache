using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace DistCache.Common.NetworkManagement
{
    public class SocketServer : IDisposable
    {
        private readonly IPEndPoint _localEndPoint;
        private readonly object _lockObject = new object();
        private TcpListener _socketListener = null;
        public event EventHandler<TcpClient> ConnectionAccepted;
        private ConcurrentDictionary<Guid, TcpClient> client = new ConcurrentDictionary<Guid, TcpClient>();

        private event EventHandler<Exception> ServerSocketFail;

        public SocketServer(IPEndPoint bindTo, bool restartOnSocketFailure = true)
        {
            this._localEndPoint = new IPEndPoint(bindTo.Address, bindTo.Port);
            if (restartOnSocketFailure)
            {
                ServerSocketFail += OnServerSocketFailed;
            }
        }

        private void OnServerSocketFailed(object sender, Exception e)
        {
            Stop();
            Start();
        }

        public void Start()
        {
            if (this._socketListener == null)
            {
                lock (_lockObject)
                {
                    this._socketListener = new TcpListener(_localEndPoint);
                    this._socketListener.Start(50);
                    new Thread(AcceptorThread).Start();
                }
            }
            else throw new Exception("already inited");
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                try
                {
                    _socketListener?.Stop();
                }
                catch (Exception ex)
                {

                }
                _socketListener = null;
            }
        }

        private void AcceptorThread()
        {
            bool exceptionOccured = false;
            while (!exceptionOccured && _socketListener?.Server?.IsBound == true)
            {
                try
                {
                    TcpClient acceptedSocket = this._socketListener.AcceptTcpClient();
                    ConnectionAccepted?.Invoke(this, acceptedSocket);
                }
                catch (Exception ex)
                {
                    exceptionOccured = true;
                    ServerSocketFail?.Invoke(this, ex);
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    Stop();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SimpleSocketServer() {
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
