using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using DistCache.Common.NetworkManagement;
using DistCache.Common;
using DistCache.Server;
using DistCache.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DistCache.Common.Protocol.Messages;
using System.Threading.Tasks;

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

            using (var srv = new CacheServer(serverConfig))
            {
                var ls = new List<DistCacheClient>();
                var client = DistCacheClient.Create(new DistCacheClientConfig() { Password = pass });
                Thread.Sleep(1000);
                Assert.IsTrue(srv.ConnectedClientsCount == 1);
            }

        }

        [TestMethod]
        public void HandShakeAcceptMoar()
        {
            var dic = new Dictionary<string, string>();

            string pass = DistCacheConfigBase.GenerateRandomPassword();

            var serverConfig = new DistCacheServerConfig()
            {
                Password = pass
            };

            int toAdd = 16;
            int iters = 2;

            using (var srv = new CacheServer(serverConfig))
            {
                var clientscon = new ConcurrentBag<DistCacheClient>();

                for (int k = 0; k < iters; ++k)
                {

                    List<Task<DistCacheClient>> clients = new List<Task<DistCacheClient>>();
                    var ls = new List<DistCacheClient>();

                    for (int i = 0; i < toAdd; ++i)
                    {
                        clients.Add(new Task<DistCacheClient>(() =>
                        {

                            var o = DistCacheClient.Create(new DistCacheClientConfig() { Password = pass });
                            clientscon.Add(o);
                            return o;

                        }));
                        clients.Last().Start();
                    }

                    Task.WaitAll(clients.ToArray());
                }
                while (srv.PendingClientsCount > 0)
                {
                    Thread.Sleep(500);
                }
                Assert.AreEqual(toAdd * iters, srv.ConnectedClientsCount);
            }
        }

    }
}

