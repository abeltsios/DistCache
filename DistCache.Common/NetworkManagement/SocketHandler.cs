using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.IO.Compression;
using DistCache.Common.Utilities;
using System.Runtime.CompilerServices;

namespace DistCache.Common.NetworkManagement
{
    public abstract class SocketHandler : IDisposable
    {
        public static TcpClient CreateSocket(IPEndPoint endpoint, DistCacheConfigBase config)
        {
            TcpClient Connection = new TcpClient();

            if (Connection.ReceiveBufferSize != 1 << 15)
                Connection.ReceiveBufferSize = 1 << 15;

            if (Connection.SendBufferSize != 1 << 15)
            {
                Connection.SendBufferSize = 1 << 15;
            }
            if (Connection.ReceiveTimeout != config.SocketReadTimeout)
            {
                Connection.ReceiveTimeout = config.SocketReadTimeout;
            }

            if (Connection.SendTimeout != config.SocketWriteTimeout)
            {
                Connection.SendTimeout = config.SocketWriteTimeout;
            }
            if (Connection.Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive) as int? == 1)
            {
                Connection.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
            Connection.Connect(endpoint);

            return Connection;
        }

        internal class MessageTriplete
        {
            public byte[] Payload;

            public MessageTriplete(bool isCompressed, byte[] payload, TaskCompletionSource<bool> eventWait)
            {
                if (!isCompressed)
                {
                    this.Payload = CompressionUtilitities.Compress(payload);
                }
                else
                {
                    this.Payload = payload;
                }
                EventWait = eventWait;
            }

            public TaskCompletionSource<bool> EventWait { get; set; }
        }

        private ConcurrentQueue<MessageTriplete> _messagesToSend;

        private readonly System.Diagnostics.Stopwatch LastSocketIO = System.Diagnostics.Stopwatch.StartNew();

        //for messages
        private TcpClient _connection;
        private MemoryStream _memoryStream;
        private long _posistionReadUntil = 0;
        private readonly object lockMe;

        public event EventHandler<SocketHandler> ConnectionError;
        public DistCacheConfigBase Config { get; protected set; }

        protected SocketHandler(TcpClient tcp, DistCacheConfigBase config)
        {
            this.lockMe = new object();
            this.Config = config;
            this._connection = tcp;
            this._messagesToSend = new ConcurrentQueue<MessageTriplete>();
            this._memoryStream = new MemoryStream();
        }

        public void Initiate()
        {
            InitRead();
        }

        protected SocketHandler(SocketHandler other)
        {
            this.lockMe = other.lockMe;
            lock (this.lockMe)
            {
                this.Config = Config;
                this._connection = other._connection;
                other._connection = null;
                this._memoryStream = other._memoryStream;
                other._memoryStream = null;
                this.Config = other.Config;
                this._messagesToSend = other._messagesToSend;
                this._posistionReadUntil = other._posistionReadUntil;
            }
        }

        public void SendMessage<T>(T Message, TaskCompletionSource<bool> eventWait = null)
        {
            SendMessage(BsonUtilities.Serialise<T>(Message), false, eventWait);
        }

        private void SendMessage(byte[] playload, bool isCompressed = false, TaskCompletionSource<bool> eventWait = null)
        {
            this._messagesToSend.Enqueue(new MessageTriplete(isCompressed, playload, eventWait));
            SendData();
        }

        #region socket IO
        private void SendData()
        {
            MessageTriplete toSend = null;
            if (SocketStatus && _messagesToSend?.TryDequeue(out toSend) == true)
            {
                byte[] msg = toSend.Payload;
                bool success = false;
                using (var msh = new MemoryStreamPool())
                {
                    using (var bw = new BinaryWriter(msh.Stream, Encoding.UTF8, true))
                    {
                        bw.Write(msg.Length);
                        bw.Write(msg);
                    }
                    try
                    {
                        _connection.GetStream().Write(msh.Stream.ToArray(), 0, (int)msh.Stream.Length);
                        LastSocketIO.Restart();
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        this.ConnectionError?.Invoke(this, this);
                        return;
                    }
                    finally
                    {
                        toSend.EventWait?.TrySetResult(success);
                    }
                }
            }

        }

        private void InitRead()
        {
            if (SocketStatus)
            {
                var buffer = new ByteArrayBufferPool();
                _connection.GetStream().ReadAsync(buffer.ByteArray, 0, buffer.ByteArray.Length).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        this.ConnectionError?.Invoke(this, this);
                    }
                    else if (task.IsCompleted)
                    {
                        LastSocketIO.Restart();
                        ReadData(buffer.ByteArray, task.Result);
                    }
                    else
                    {

                    }
                    buffer.Dispose();
                });
            }
            else
            {
                throw new Exception();
            }
        }

        private bool _shouldKeepHandlingMessages = true;

        private void ReadData(byte[] b, int toRead)
        {
            //lock (lockMe)
            {
                if (toRead >= 0 && _shouldKeepHandlingMessages)
                {
                    //message pattern is 
                    // A:B (A is 4bytes:int: byte size of B, B is gzipped message)
                    //append read data to the end
                    _memoryStream.Position = _memoryStream.Length;
                    _memoryStream.Write(b, 0, toRead);
                    //back to the flagged beginning of the message
                    _memoryStream.Position = _posistionReadUntil;
                    using (var sr = new BinaryReader(_memoryStream, Encoding.UTF8, true))
                    {
                        //if we should read data
                        //and data in buffer are enough
                        //at least to check packet size
                        while (_shouldKeepHandlingMessages && (_memoryStream.Length - _memoryStream.Position) > 4)
                        {
                            //read incoming message size
                            int messageLength = sr.ReadInt32();
                            //if we have enough data to fully consume message
                            if ((_memoryStream.Length - _memoryStream.Position) >= messageLength)
                            {
                                //read and decompress packet
                                byte[] msg = Utilities.CompressionUtilitities.Decompress(sr.ReadBytes(messageLength));
                                //flag read data position
                                _posistionReadUntil = _memoryStream.Position;
                                _shouldKeepHandlingMessages = HandleMessages(msg);
                                if (!_shouldKeepHandlingMessages)
                                {
                                    //we shouldn't handle any more messages
                                    //or consume any packets
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }

                    }
                }
                if (SocketStatus)
                    //if the socket is passed to another handler
                    //and we would be in a non resolvable state
                    lock (lockMe)
                    {
                        //lock and slice the memory stream
                        if (SocketStatus)
                        {
                            using (var br = new BinaryReader(_memoryStream, Encoding.UTF8, true))
                            {
                                _memoryStream.Position = _posistionReadUntil;
                                var tmpSlice = br.ReadBytes((int)(_memoryStream.Length - _posistionReadUntil));
                                _memoryStream.SetLength(0); //position resets
                                _memoryStream.Write(tmpSlice, 0, tmpSlice.Length);
                                _memoryStream.Position = _posistionReadUntil = 0;
                            }
                        }
                    }

                if (_shouldKeepHandlingMessages)
                {
                    InitRead();
                }
            }
        }



        #endregion

        /// <summary>
        /// If should proceed handling messages by the current class
        /// or we should stop because the socket will be passed
        /// in an other handler
        /// keep handling -> true;
        /// stop handling -> false
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected abstract bool HandleMessages(byte[] message);

        public void Shutdown()
        {
            try
            {
                var cp = _connection;
                _connection = null;
                cp?.Close();
            }
            catch (Exception ex)
            {
                //todo log
            }
        }

        public virtual bool SocketStatus => _connection?.Connected == true
            //&& LastSocketIO.ElapsedMilliseconds < Config.SocketConsideredDead
            && _connection?.GetStream()?.CanRead == true
            && _connection?.GetStream()?.CanWrite == true;



        public void Dispose()
        {
            lock (lockMe)
            {
                try
                {
                    _connection?.Close();
                }
                catch (Exception ex)
                {
                    //TODO LogMe
                }
                _connection = null;

                try
                {
                    _memoryStream?.Dispose();
                }
                catch (Exception ex)
                {
                    //TODO LogMe
                }
                _memoryStream = null;

                _messagesToSend = null;
            }
            // TODO: dispose managed state (managed objects).
        }

    }
}
