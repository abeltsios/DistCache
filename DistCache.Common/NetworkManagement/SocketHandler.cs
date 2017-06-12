﻿using System;
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

        public TcpClient Connection { get; protected set; }
        private ConcurrentQueue<MessageTriplete> _messagesToSend = new ConcurrentQueue<MessageTriplete>();

        private MemoryStreamPool _memoryStream = new MemoryStreamPool();
        private readonly System.Diagnostics.Stopwatch LastSocketIO = System.Diagnostics.Stopwatch.StartNew();
        private bool keepHandlingMessages = true;
        public event EventHandler<SocketHandler> ConnectionError;
        public DistCacheConfigBase config { get; protected set; }

        public SocketHandler(TcpClient tcp, DistCacheConfigBase config)
        {
            this.config = config;
            this.Connection = tcp;
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
                            Connection.GetStream().Write(msh.Stream.ToArray(), 0, (int)msh.Stream.Length);
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
                    using (var sr = new BinaryReader(Connection.GetStream(), Encoding.UTF8, true))
                    {
                        int? messageLength = new int?();

                        while (Connection.Connected && Connection.GetStream().CanRead && Connection.Available > 0)
                        {
                            LastSocketIO.Restart();
                            if (!messageLength.HasValue && Connection.Available > 4)
                            {
                                messageLength = sr.ReadInt32();
                            }
                            else if (messageLength.HasValue)
                            {
                                int toRead;
                                if (Connection.Available > (messageLength.Value - _memoryStream.Stream.Position))
                                {
                                    toRead = (int)(messageLength.Value - _memoryStream.Stream.Position);
                                }
                                else
                                {
                                    toRead = Connection.Available;
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
                var cp = Connection;
                Connection = null;
                cp.Close();
            }
            catch (Exception ex)
            {
                //todo log
            }
        }

        public virtual bool SocketStatus => Connection?.Connected == true
            && LastSocketIO.ElapsedMilliseconds < config.SocketConsideredDead
            && Connection?.GetStream()?.CanRead == true
            && Connection?.GetStream()?.CanWrite == true;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls


        public TcpClient PassSocket()
        {
            var c = this.Connection;
            this.Connection = null;
            return c;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (keepHandlingMessages)
                    {
                        try
                        {
                            Connection?.Close();
                        }
                        catch (Exception ex)
                        {
                            //TODO LogMe
                        }
                        Connection = null;
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
                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SocketHandler() {
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
