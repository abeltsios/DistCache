using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DistCache.Common.Utilities;
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
        public List<string> Servers { get; protected set; } = new List<string>();

        private HashSet<string> ValidatedHosts = new HashSet<string>();

        protected IEnumerable<IPEndPoint> ParseIPEndPoint(string hostnameAndPort)
        {
            var sp = hostnameAndPort.Split(':');
            int port;
            IPAddress address = null;
            if (sp.Length != 2)
            {
                throw new ArgumentException($@"invalid hostname and port! e.g. valid '127.0.0.1:6455'");
            }
            if (!int.TryParse(sp[1], out port) || (port & (ushort)(ushort.MaxValue)) == port)
            {
                throw new ArgumentException($"invalid port {sp[1]}");
            }
            if (!IPAddress.TryParse(sp[0], out address) || !ValidatedHosts.Contains(sp[0]) || !Dns.GetHostEntry(sp[0]).AddressList.Any())
            {
                throw new ArgumentException($"invalid port {sp[1]}");
            }
            if (address == null)
            {
                //get a random address of the given hostname
                //due to possible load balancing on dns level
                return Dns.GetHostEntry(sp[0]).AddressList.OrderBy(a => RandomProvider.Next(0, 1 << 10)).Select(ad => new IPEndPoint(ad, port)).ToList();
            }
            return new List<IPEndPoint> { new IPEndPoint(address, port) };
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
