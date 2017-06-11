using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using DistCache.Common.NetworkManagement;
using DistCache.Common;
using DistCache.Common.Utilities;
using DistCache.Server.Protocol.Messages;

namespace DistCache.Server.Protocol.Handlers
{
    public class HandShakeHandler : SocketHandler
    {
        public enum HandShakeState
        {
            Awaiting,
            Authorised,
            NotAuthorised,
            ProtocolError
        }

        public Guid TemporaryID { get; private set; } = Guid.NewGuid();

        public HandShakeState State { get; private set; } = HandShakeState.Awaiting;
        private CacheServer _server;


        public HandShakeHandler(TcpClient tcp, CacheServer server) : base(tcp)
        {
            this._server = server;
        }

        protected override bool HandleMessages(byte[] message)
        {
            try
            {
                HandShakeMessage msg = BsonUtilities.Deserialise<HandShakeMessage>(message);
                if (ConfigProvider.Password.Equals(msg.AuthPassword))
                {
                    State = HandShakeState.Authorised;
                }
                else
                {
                    State = HandShakeState.NotAuthorised;
                }

                var waiter = new ManualResetEventSlim(false);
                this.SendMessage(new HandShakeOutcome()
                {
                    MessageType = State == HandShakeState.Authorised ? MessageTypeEnum.AuthRequestOk : MessageTypeEnum.AuthRequestError,
                    ServerGuid = State == HandShakeState.Authorised ? this._server.ServerGuid : Guid.Empty
                }, waiter);


                if (waiter.Wait(ConfigProvider.SocketWriteTimeout) && State == HandShakeState.Authorised)
                {
                    if (msg.MessageType == MessageTypeEnum.ClientAuthRequest)
                    {
                        _server.ClientConnected(this._tcp, msg.RegisteredGuid, TemporaryID);
                    }
                    else if (msg.MessageType == MessageTypeEnum.ServerAuthRequest)
                    {
                        _server.ServerConnected(this._tcp, msg.RegisteredGuid, TemporaryID);
                    }
                }
                else
                {
                    _server.UnknownConnectionFailed(this._tcp, TemporaryID);
                }
            }
            catch (Exception ex)
            {
                this.State = HandShakeState.ProtocolError;
                _server.UnknownConnectionFailed(this._tcp, TemporaryID);
            }
            return false;
        }
    }
}
