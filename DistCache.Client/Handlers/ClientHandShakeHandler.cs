using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using DistCache.Common.NetworkManagement;
using DistCache.Common;
using DistCache.Common.Utilities;
using DistCache.Common.Protocol.Messages;

namespace DistCache.Client.Protocol.Handlers
{
    public class ClientHandShakeHandler : SocketHandler
    {
        private ManualResetEventSlim waitForLogin = new ManualResetEventSlim(false);
        private MessageTypeEnum? state = null;
        private Guid clientUID;

        public ClientHandShakeHandler(TcpClient tcp, DistCacheConfigBase config, Guid clientUID) : base(tcp, config)
        {
            this.clientUID = clientUID;
        }

        protected override bool HandleMessages(byte[] message)
        {
            var ans = BsonUtilities.Deserialise<HandShakeOutcome>(message);
            this.state = ans.MessageType;
            Console.WriteLine($"HandShakeOutcome recieved:{state}");
            waitForLogin.Set();
            return false;
        }

        public bool VerifyConnection()
        {
            try
            {
                SendMessage(new HandShakeRequest()
                {
                    AuthPassword = Config.Password,
                    MessageType = MessageTypeEnum.ClientAuthRequest,
                    RegisteredGuid = clientUID
                });

                if (waitForLogin.Wait(Config.SocketReadTimeout) && state == MessageTypeEnum.AuthRequestOk)
                {
                    return true;
                }
                return false;
            }
            finally
            {
                waitForLogin.Dispose();
            }
        }
    }
}
