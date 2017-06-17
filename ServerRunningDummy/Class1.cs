using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DistCache.Common;
using DistCache.Server;
using DistCache.Client;

namespace ServerRunningDummy
{
    public class MainApp
    {
        public static void Main(string[] args)
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
            }
        }
    }
}
