using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using DistCache.Common;

namespace DistCache.Server
{
    public class DistCacheServerConfig : DistCacheConfigBase
    {

        public DistCacheServerConfig() : base()
        {
            this.Servers = new List<string>() { "0.0.0.0:9856" };
        }

        public IPEndPoint GetEndpointToBind()
        {
            HashSet<IPAddress> addresses = GetHostIpAddresses();
            addresses.Add(IPAddress.Any);

            HashSet<IPEndPoint> hs = new HashSet<IPEndPoint>(Servers.Select(ParseIPEndPoint));
            return hs.Single(ep => addresses.Contains(ep.Address));
        }

      

    }
}
