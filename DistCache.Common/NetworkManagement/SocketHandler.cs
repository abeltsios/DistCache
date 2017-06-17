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

        private TcpClient _connection;
        private ConcurrentQueue<MessageTriplete> _messagesToSend = new ConcurrentQueue<MessageTriplete>();

        private MemoryStreamPool _memoryStream = new MemoryStreamPool();
        private readonly System.Diagnostics.Stopwatch LastSocketIO = System.Diagnostics.Stopwatch.StartNew();
        private bool keepHandlingMessages = true;
        public event EventHandler<SocketHandler> ConnectionError;
        public DistCacheConfigBase config { get; protected set; }

        public SocketHandler(TcpClient tcp, DistCacheConfigBase config)
        {
            this.config = config;
            this._connection = tcp;
        }

        public void Start()
        {
            new Thread(ReadData).Start();
            new Thread(SendData).Start();
        }

        public void SendMessage<T>(T Message, ManualResetEventSlim eventWait = null)
        {
            SendMessage(BsonUtilities.Serialise<T>(Message), false, eventWait);
        }

        public void SendMessage(byte[] b, bool isCompressed = false, ManualResetEventSlim eventWait = null)
        {
            if (SocketStatus)
            {
                this._messagesToSend.Enqueue(new MessageTriplete() { Compressed = isCompressed, Message = b, EventWait = eventWait });
            }
            else
            {
                throw new Exception("send msg while socket dead");
            }
        }

        #region socket IO

        private void SendData()
        {
            while (keepHandlingMessages && SocketStatus)
            {

                while (keepHandlingMessages && _messagesToSend.TryDequeue(out MessageTriplete toSend))
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
                if (!_messagesToSend.Any())
                    Thread.Sleep(1);
            }
            Console.WriteLine($"{this.GetType().FullName} :: read data returned ");
        }

        private void ReadData()
        {
            try
            {
                while (SocketStatus)
                {
                    using (var sr = new BinaryReader(_connection.GetStream(), Encoding.UTF8, true))
                    {
                        int? messageLength = new int?();

                        while (_connection.Connected && _connection.GetStream().CanRead && _connection.Available > 0)
                        {
                            LastSocketIO.Restart();
                            if (!messageLength.HasValue && _connection.Available > 4)
                            {
                                messageLength = sr.ReadInt32();
                            }
                            else if (messageLength.HasValue)
                            {
                                int toRead;
                                if (_connection.Available > (messageLength.Value - _memoryStream.Stream.Position))
                                {
                                    toRead = (int)(messageLength.Value - _memoryStream.Stream.Position);
                                }
                                else
                                {
                                    toRead = _connection.Available;
                                }

                                _memoryStream.Stream.Write(sr.ReadBytes(toRead), 0, toRead);
                                if (_memoryStream.Stream.Position == messageLength.Value)
                                {
                                    if (messageLength.Value == _memoryStream.Stream.Position)
                                    {
                                        _memoryStream.Stream.Seek(0, SeekOrigin.Begin);
                                        var packet = CompressionUtilitities.Decompress(_memoryStream.Stream);
                                        _memoryStream.Stream.SetLength(0);
                                        messageLength = null;
                                        if (!HandleMessages(packet))
                                        {
                                            //exit thread
                                            keepHandlingMessages = false;
                                            return;
                                        }

                                    }

                                }
                            }
                        }
                        Thread.Sleep(1);
                    }
                }
            }
            catch (Exception ex)
            {
                this.ConnectionError?.Invoke(this, this);
            }
            finally
            {
                Console.WriteLine($"{this.GetType().FullName} :: read data returned ");
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
            && LastSocketIO.ElapsedMilliseconds < config.SocketConsideredDead
            && _connection?.GetStream()?.CanRead == true
            && _connection?.GetStream()?.CanWrite == true;



        public TcpClient PassSocket()
        {
            var c = this._connection;
            this._connection = null;
            return c;
        }

      
        public virtual void Dispose()
        {
            if (keepHandlingMessages)
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
            }

            try
            {
                this._memoryStream.Dispose();
            }
            catch (Exception ex)
            {
                //TODO LogMe
            }
            this._memoryStream = null;
            this._messagesToSend = null;
            // TODO: dispose managed state (managed objects).
        }

    }
}
