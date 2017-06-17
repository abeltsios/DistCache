using System;
using System.Net;
using System.Linq;
using DistCache.Client;
using DistCache.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DistCache.Tests
{
    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        public void TestServerBindIp()
        {
            Assert.IsNotNull(new DistCacheServerConfig().GetEndpointToBind());
        }

        [TestMethod]
        public void TestClientConeectionIpEndpoint()
        {
            var o = new DistCacheClientConfig();
            o.Servers.Clear();
            o.Servers.Add("123.123.123.4:123");

            Assert.IsNotNull(o.GetOrderedServerIpEndPoint());
            Assert.AreEqual(
                new IPEndPoint(IPAddress.Parse("123.123.123.4"), 123),
                o.GetOrderedServerIpEndPoint().First());
        }
    }
}
