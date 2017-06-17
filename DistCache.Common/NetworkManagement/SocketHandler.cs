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
    public class SocketHandler : IDisposable
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
            public bool Compressed { get; set; }

            public byte[] Message { get; set; }

            public ManualResetEventSlim EventWait { get; set; }
        }

        private ConcurrentQueue<MessageTriplete> _messagesToSend;

        private readonly System.Diagnostics.Stopwatch LastSocketIO = System.Diagnostics.Stopwatch.StartNew();

        //for messages
        private TcpClient _connection;
        private MemoryStream _memoryStream;
        private readonly object lockMe = new object();

        public event EventHandler<SocketHandler> ConnectionError;
        public DistCacheConfigBase Config { get; protected set; }

        protected SocketHandler(TcpClient tcp, DistCacheConfigBase config)
        {
            this.Config = config;
            this._connection = tcp;
            this._messagesToSend = new ConcurrentQueue<MessageTriplete>();
            _memoryStream = new MemoryStream();
        }

        public void Initiate()
        {
            InitRead();
        }

        protected SocketHandler(SocketHandler other)
        {
            lock (this.lockMe)
            {
                lock (other.lockMe)
                {
                    this.Config = Config;
                    this._connection = other._connection;
                    other._connection = null;
                    this._memoryStream = other._memoryStream;
                    other._memoryStream = null;
                    this.Config = other.Config;
                    this._messagesToSend = other._messagesToSend;
                    other._messagesToSend = null;
                }
            }
        }

        public void SendMessage<T>(T Message, ManualResetEventSlim eventWait = null)
        {
            SendMessage(BsonUtilities.Serialise<T>(Message), false, eventWait);
        }

        protected void SendMessage(byte[] b, bool isCompressed = false, ManualResetEventSlim eventWait = null)
        {

            this._messagesToSend.Enqueue(new MessageTriplete() { Compressed = isCompressed, Message = b, EventWait = eventWait });
            new Task(() => SendData()).Start();
        }

        #region socket IO

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SendData()
        {
            if (SocketStatus)
            {
                MessageTriplete toSend = null;
                while (_messagesToSend?.TryDequeue(out toSend) == true)
                {
                    byte[] msg = toSend.Message;
                    if (!toSend.Compressed)
                    {
                        msg = CompressionUtilitities.Compress(toSend.Message);
                    }

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
                        }
                        catch (Exception ex)
                        {
                            this.ConnectionError?.Invoke(this, this);
                            return;
                        }
                        finally
                        {
                            toSend.EventWait?.Set();
                        }
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
                     buffer.Dispose();
                 });
            }
            else
            {
                throw new Exception();
            }
        }

        private void ReadData(byte[] b, int toRead)
        {
            lock (lockMe)
            {
                if (toRead == 0)
                {
                    InitRead();
                }
                _memoryStream.Write(b, 0, toRead);
                _memoryStream.Position = 0;
                using (var sr = new BinaryReader(_memoryStream, Encoding.UTF8, true))
                {
                    bool shouldHandleOthers = true;
                    while (shouldHandleOthers)
                    {

                        int? messageLength = new int?();
                        if (!messageLength.HasValue && (_memoryStream.Length - _memoryStream.Position) > 4)
                        {
                            messageLength = sr.ReadInt32();
                        }

                        if (messageLength.HasValue && (_memoryStream.Length - _memoryStream.Position) >= messageLength)
                        {
                            byte[] msg = Utilities.CompressionUtilitities.Decompress(sr.ReadBytes(messageLength.Value));

                            if (_memoryStream.Length > _memoryStream.Position)
                            {
                                byte[] remaining = sr.ReadBytes((int)(_memoryStream.Length - _memoryStream.Position));
                                _memoryStream.SetLength(0);
                                _memoryStream.Write(remaining, 0, remaining.Length);
                            }
                            else
                            {
                                _memoryStream.SetLength(0);
                            }

                            shouldHandleOthers = HandleMessages(msg);
                        }
                        else
                        {
                            _memoryStream.Position = 0;
                            InitRead();
                            break;
                        }
                    }
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
        protected virtual bool HandleMessages(byte[] message)
        {
            try
            {
                var o = BsonUtilities.Deserialise<Dictionary<object, object>>(message);
                PrintBson(o);
                Console.WriteLine(Encoding.UTF8.GetString(message));
            }
            catch (Exception ex)
            {
                Console.WriteLine("unreadable message");
            }
            return true;
        }

        private void PrintBson(Dictionary<object, object> o, string depth = "")
        {
            foreach (var k in o)
            {

                Console.WriteLine($"{depth}{k.Key}:{k.Value}");
            }
        }

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
            && LastSocketIO.ElapsedMilliseconds < Config.SocketConsideredDead
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
