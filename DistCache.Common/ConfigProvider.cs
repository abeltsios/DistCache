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
    public class DistCacheConfigBase
    {
        public static string GenerateRandomPassword()
        {
            return string.Join("", new int[] { 1, 2, 3, 4, 5, 6 }.Select(i => Guid.NewGuid().ToString()).ToArray());
        }
        public int SocketReadTimeout { get; set; } = 60000;
        public int SocketWriteTimeout { get; set; } = 10000;
        public int SocketConsideredDead { get; set; } = 60000;
        public string Password { get; set; }
        public List<string> Servers { get; set; } = new List<string>();

        private HashSet<string> ValidatedHosts = new HashSet<string>();

        protected IEnumerable<IPEndPoint> ParseIPEndPoint(string hostnameAndPort)
        {
            var sp = hostnameAndPort.Split(':');
            if (sp.Length != 2)
            {
                throw new ArgumentException($@"invalid hostname and port! e.g. valid '127.0.0.1:6455'");
            }
            if (!int.TryParse(sp[1], out int port) || (port & (ushort)(ushort.MaxValue)) != port)
            {
                throw new ArgumentException($"invalid port {sp[1]}");
            }

            IPAddress address = null;
            try
            {
                address = IPAddress.Parse(sp[0]);
            }
            catch (Exception e)
            {

            }

            if (address == null && !Dns.GetHostEntry(sp[0]).AddressList.Any())
            {
                throw new ArgumentException($"invalid host {sp[1]}");
            }

            if (address == null)
            {
                //get a random address of the given hostname
                //due to possible load balancing on dns level
                ValidatedHosts.Add(sp[0]);
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
