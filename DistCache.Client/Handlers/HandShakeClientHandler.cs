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

namespace DistCache.Client.Handlers
{
    public class HandShakeClientHandler : SocketHandler
    {
        private ManualResetEventSlim waitForLogin = new ManualResetEventSlim(false);
        private MessageTypeEnum? state = null;
        public HandShakeClientHandler(TcpClient tcp, DistCacheConfigBase config,Guid clientUID) : base(tcp, config)
        {
            
            SendMessage(new HandShakeRequest()
            {
                AuthPassword = config.Password,
                MessageType = MessageTypeEnum.ClientAuthRequest,
                RegisteredGuid = clientUID
            });
        }

        protected override bool HandleMessages(byte[] message)
        {
            var ans = BsonUtilities.Deserialise<HandShakeOutcome>(message);
            this.state = ans.MessageType;
            if( state == MessageTypeEnum.AuthRequestOk)
            {
                Shutdown();
            }
            waitForLogin.Set();
            return false;
        }

        public bool VerifyConnection()
        {
            if(waitForLogin.Wait(config.SocketReadTimeout) && state == MessageTypeEnum.AuthRequestOk)
            {
                return true;
            }
            return false;
        }
    }
}
