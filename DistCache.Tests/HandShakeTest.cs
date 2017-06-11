using System;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using DistCache.Common.NetworkManagement;
using DistCache.Common;
using DistCache.Server;
using DistCache.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DistCache.Common.Protocol.Messages;

namespace DistCache.Tests
{
    [TestClass]
    public class HandShakeTest
    {
        [TestMethod]
        public void HandShakeAccept()
        {
            var dic = new Dictionary<string, string>();

            string pass = DistCacheConfigBase.GenerateRandomPassword();

            var serverConfig = new DistCacheServerConfig()
            {

                Password = pass
            };
            int i = 0;
            var srv = new CacheServer(serverConfig);
            var ls = new List<DistCacheClient>();
            while (++i < 100)
            {
                var client = DistCacheClient.Create(new DistCacheClientConfig() { Password = pass });
            }
            Thread.Sleep(1000);
            Assert.IsTrue(srv.ConnectedClientsCount == 100);
            foreach (var c in ls)
            {
                try
                {
                    c.Dispose();
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
