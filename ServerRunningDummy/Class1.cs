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
                var lso = new List<Task>();
                for (int k = 0; k < 2; ++k)
                {
                    int it = k;
                    var o = new Task(() =>
                      {
                          using (var client = DistCacheClient.Create(new DistCacheClientConfig() { Password = pass }))
                          {
                              for (int i = 0; i < 100; ++i)
                              {
                                  client.GetMessage($"{it}_{i}");
                              }
                          }
                      });
                    o.Start();
                    lso.Add(o);
                }
                Task.WaitAll(lso.ToArray());
            }
        }
    }
}
