using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServerRunningDummy
{
    public class MainApp
    {
        public static void Main(string[] args)
        {
            using (var srv = new DistCache.Server.CacheServer(new DistCache.Server.DistCacheServerConfig()
            {
                Password = "asdasd"
            }, false))
            {
                using (var cli = DistCache.Client.DistCacheClient.Create(new DistCache.Client.DistCacheClientConfig() { Password = "asdasd" }))
                {
                }
                Thread.Sleep(10);
            }
        }
    }
}
