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
                int clients = 100;
                int msgs = 1000;

                var ls = new List<DistCacheClient>();

                for (int k = 0; k < clients; ++k)
                {
                    var client = DistCacheClient.Create(new DistCacheClientConfig() { Password = pass });
                    ls.Add(client);
                    if (k % 1000 == 0)
                    {
                        Console.WriteLine($"connected {k + 1}");
                    }
                }

                Stopwatch sw = Stopwatch.StartNew();
                long cnt = 0;
                for (int i = 0; i < msgs; ++i)
                {
                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        foreach (var cl in ls)
                        {
                            string s = cl.GetMessage($"{i}").Result;
                            Interlocked.Increment(ref cnt);
                        }
                    });

                }
                while (true)
                {
                    Console.ReadLine();
                    long cn = Interlocked.Read(ref DistCacheClient.MsgCount);
                    Console.WriteLine($"sent {cn}/{cnt+1}");
                    Console.WriteLine(decimal.Divide(sw.ElapsedMilliseconds, cn+1));
                    Console.WriteLine(sw.Elapsed);
                }

            }
        }
    }
}