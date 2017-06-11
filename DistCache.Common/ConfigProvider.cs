using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;

namespace DistCache.Common
{
    public class ConfigProvider
    {
        public const int SocketReadTimeout = 60000;
        public const int SocketWriteTimeout = 5000;
        public const int SocketConsideredDead = 60000;
        public const string Password = "sdflkhgvsdklr42qio7ryfqsdbvq43u95ytqweiufhcq3874q5b8ro72`b1r58egtrOX74TR2I3YGFydt63IQ74";
    }


    public class DistCacheConfigBase
    {
        public int SocketReadTimeout { get; protected set; } = ConfigProvider.SocketReadTimeout;
        public int SocketWriteTimeout { get; protected set; } = ConfigProvider.SocketWriteTimeout;
        public int SocketConsideredDead { get; protected set; } = ConfigProvider.SocketConsideredDead;
        public string Password { get; protected set; } = ConfigProvider.Password;
        public  List<string> Servers { get; protected set; } = new List<string>();

        protected IPEndPoint ParseIPEndPoint(string s)
        {
            return new IPEndPoint(IPAddress.Parse(s.Split(':')[0]), int.Parse(s.Split(':')[1]));
        }

        public static HashSet<IPAddress> GetHostIpAddresses()
        {
            HashSet<IPAddress> addresses = new HashSet<IPAddress>(NetworkInterface.GetAllNetworkInterfaces()
                   .Where(iface => iface.OperationalStatus == OperationalStatus.Up) //filter active
                   .Select(iface => iface.GetIPProperties()
                       .UnicastAddresses
                       .Where(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork || ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6))
                   .SelectMany(ua => ua).Select(ua => ua.Address));
            return addresses;
        }
    }
}
