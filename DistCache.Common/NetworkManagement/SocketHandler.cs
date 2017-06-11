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
        internal class MessageTriplete
        {
            public bool Compressed { get; set; }

            public byte[] Message { get; set; }

            public ManualResetEventSlim EventWait { get; set; }
        }

        protected TcpClient _tcp;
        private ConcurrentQueue<MessageTriplete> _messagesToSend = new ConcurrentQueue<MessageTriplete>();

        private MemoryStreamPool _memoryStream = new MemoryStreamPool();
        private readonly System.Diagnostics.Stopwatch LastSocketIO = System.Diagnostics.Stopwatch.StartNew();
        private bool keepHandlingMessages = true;
        public event EventHandler<SocketHandler> ConnectionError;

        public SocketHandler(TcpClient tcp)
        {
            this._tcp = tcp;
            _tcp.ReceiveBufferSize = 1 << 15;
            _tcp.SendBufferSize = 1 << 15;
            _tcp.ReceiveTimeout = ConfigProvider.SocketReadTimeout;
            _tcp.SendTimeout = ConfigProvider.SocketWriteTimeout;
            _tcp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
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
                            _tcp.GetStream().Write(msh.Stream.ToArray(), 0, (int)msh.Stream.Length);
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



        }

        private void ReadData()
        {
            using (var sr = new BinaryReader(_tcp.GetStream(), Encoding.UTF8))
            {
                try
                {
                    while (SocketStatus)
                    {
                        int? messageLength = new int?();

                        while (_tcp.Connected && _tcp.GetStream().CanRead && _tcp.Available > 0)
                        {
                            LastSocketIO.Restart();
                            if (!messageLength.HasValue && _tcp.Available > 4)
                            {
                                messageLength = sr.ReadInt32();
                            }
                            else if (messageLength.HasValue)
                            {
                                int toRead;
                                if (_tcp.Available > (messageLength.Value - _memoryStream.Stream.Position))
                                {
                                    toRead = (int)(messageLength.Value - _memoryStream.Stream.Position);
                                }
                                else
                                {
                                    toRead = _tcp.Available;
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
                catch (Exception ex)
                {
                    this.ConnectionError?.Invoke(this, this);
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

                Console.WriteLine ($"{depth}{k.Key}:{k.Value}");
            }
        }

        public virtual bool SocketStatus => _tcp?.Connected == true
            && LastSocketIO.ElapsedMilliseconds < ConfigProvider.SocketConsideredDead
            && _tcp?.GetStream()?.CanRead == true
            && _tcp?.GetStream()?.CanWrite == true;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

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
                            _tcp.Close();
                        }
                        catch (Exception ex)
                        {
                            //TODO LogMe
                        }
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
