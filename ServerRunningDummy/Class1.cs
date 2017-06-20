using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DistCache.Common;
using DistCache.Server;
using DistCache.Client;
using System.Diagnostics;

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
                int clients = 200;
                int msgs = 200;

                var ls = new List<DistCacheClient>();

                for (int k = 0; k < clients; ++k)
                {
                    var client = DistCacheClient.Create(new DistCacheClientConfig() { Password = pass });
                    ls.Add(client);
                   
                }

                Stopwatch sw = Stopwatch.StartNew();
                long cnt = 0;
                for (int i = 0; i < msgs; ++i)
                {

                    foreach (var cl in ls)
                    {
                        string s = cl.GetMessage($"{i}").Result;
                        ++cnt;
                        if (cnt % 1000 == 0)
                        {
                            Console.WriteLine($"sent {cnt + 1}");
                        }
                    }
                }
                Console.WriteLine(sw.Elapsed);
                Console.ReadLine();


            }
        }
    }
}