//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Threading;
//using System.Diagnostics;
//using DistCache.Common.Utilities;
//using Microsoft.VisualStudio.TestTools.UnitTesting;

//namespace DistCache.Tests
//{
//    [TestClass]
//    public class DistCacheThreadPoolTest
//    {
//        [TestMethod]
//        public void DistCacheThreadTestCaseOne()
//        {
//            int sleepTime = 300;
//            int jobCount = 1500;
//            DistCacheExecutor.MaxThreadCount = 500;

//            Stopwatch sw = Stopwatch.StartNew();

//            List<ManualResetEventSlim> ls = new List<ManualResetEventSlim>();
//            for (int i = 0; i < jobCount; ++i)
//            {
//                int x = i;
//                var s = new ManualResetEventSlim(false);
//                DistCacheExecutor.QueueJob(() =>
//                {
//                    Thread.Sleep(sleepTime);
//                }, s);
//                ls.Add(s);
//            }

//            foreach (var s in ls)
//            {
//                s.Wait();
//            }
//            sw.Stop();
//            var l = sw.ElapsedMilliseconds;
//            var res = decimal.Divide(sleepTime * jobCount, DistCacheExecutor.MaxThreadCount);
//            Assert.IsTrue(l < res * 1.1m);
//            DistCacheExecutor.MaxThreadCount = 0;

//        }
//    }
//}
