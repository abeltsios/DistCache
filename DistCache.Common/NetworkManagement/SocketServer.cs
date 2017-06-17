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
        private readonly bool _restartOnSocketFailure;
        public event EventHandler<TcpClient> ConnectionAccepted;

        private event EventHandler<Exception> ServerSocketFail;

        public SocketServer(IPEndPoint bindTo, bool restartOnSocketFailure = true)
        {
            this._restartOnSocketFailure = restartOnSocketFailure;
            this._localEndPoint = new IPEndPoint(bindTo.Address, bindTo.Port);
            this._socketListener = new TcpListener(_localEndPoint);
            this._socketListener.Start(32);
            ServerSocketFail += OnServerSocketFailed;
        }

        private void OnServerSocketFailed(object sender, Exception e)
        {
            Stop();
            if (_restartOnSocketFailure)
            {
                AcceptSocket();
            }
        }

        public void AcceptSocket()
        {
            if (this._socketListener != null)
            {
                this._socketListener.AcceptTcpClientAsync().ContinueWith((task) =>
                {
                    if (task.IsFaulted)
                    {
                        ServerSocketFail.Invoke(task, task.Exception);
                    }
                    else
                    {
                        ConnectionAccepted?.Invoke(this, task.Result);
                        AcceptSocket();
                    }
                });
            }
        }

        public void Stop()
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

        public void Dispose()
        {
            Stop();
        }
    }
}
