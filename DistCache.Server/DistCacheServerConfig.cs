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
        public string InstanceEndPoint { get; protected set; }

        public DistCacheServerConfig() : base()
        {
        }

        public IPEndPoint GetEndpointToBind()
        {
            return ParseIPEndPoint(InstanceEndPoint).First();
            //HashSet<IPAddress> addresses = GetHostIpAddresses();
            //addresses.Add(IPAddress.Any);

            //HashSet<IPEndPoint> hs = new HashSet<IPEndPoint>(Servers.Select(ParseIPEndPoint));
            //return hs.Single(ep => addresses.Contains(ep.Address));
        }

        public List<IPEndPoint> GetClusterAddresses(bool includeCurrent = false)
        {
            var ret = new HashSet<IPEndPoint>(Servers.SelectMany(ParseIPEndPoint));
            if (!includeCurrent)
                ret.Remove(GetEndpointToBind());
            return ret.ToList();
        }
    }
}
