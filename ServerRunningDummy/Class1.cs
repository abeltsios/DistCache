using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
                int clients = 10;
                int msgs = 20000;

                var ls = new ConcurrentBag<DistCacheClient>();

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
                        string s = cl.GetMessage($"{Interlocked.Increment(ref cnt)}").Result;
                    }
                }
                string ss = "";
                while (ss != "q")
                {
                    Console.WriteLine($"got {Interlocked.Read(ref cnt)} {sw.Elapsed}");
                    ss = Console.ReadLine();
                }
            }
        }
    }
}