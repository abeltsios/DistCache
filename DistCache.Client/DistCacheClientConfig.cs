using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using DistCache.Common;
using DistCache.Common.Utilities;

namespace DistCache.Client
{
    public class DistCacheClientConfig : DistCacheConfigBase
    {
        public DistCacheClientConfig()
        {
            this.Servers = new List<string>() { "127.0.0.1:9856" };
        }

        public IEnumerable<IPEndPoint> GetOrderedServerIpEndPoint()
        {
            var hostIps = GetHostIpAddresses();

            var local = Servers.SelectMany(ParseIPEndPoint).FirstOrDefault(ipep => hostIps.Contains(ipep.Address));
            var ls = Servers.SelectMany(ParseIPEndPoint).ToList();
            ls = ls.OrderBy(ipep => ipep == local ? 0 : RandomProvider.Next(1,1<<10)).ToList();
            return ls;
        }
    }
}
