using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using DistCache.Common.NetworkManagement;
using DistCache.Common;
using DistCache.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DistCache.Server.Protocol.Messages;

namespace DistCache.Tests
{
    [TestClass]
    public class HandShakeTest
    {
        [TestMethod]
        public void HandShakeAccept()
        {
            var dic = new Dictionary<string, string>();

            for (int i = 0; i < 10; ++i)
                dic[Guid.NewGuid().ToString()] = Guid.NewGuid().ToString();


            var config = new DistCacheServerConfig();

            var srv = new CacheServer(config);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            var s = new TcpClient();

            s.Connect(IPAddress.Loopback, 5800);
            using (var sh = new SocketHandler(s))
            {
                var hsm = new HandShakeMessage()
                {
                    AuthPassword = ConfigProvider.Password,
                    MessageType = MessageTypeEnum.ClientAuthRequest,
                    RegisteredGuid = Guid.NewGuid()
                };
                sh.SendMessage(hsm);
                Thread.Sleep(TimeSpan.FromDays(1));

            }
        }
    }
}
