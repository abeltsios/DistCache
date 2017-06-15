using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistCache.Common.Utilities
{
    public static class DistCacheThreadPool
    {
        public static int MaxThreadCount
        {
            get { return _maxThreadCount; }
            set
            {
                _maxThreadCount = value;
                pool.EnsureThreads(_maxThreadCount);
            }
        }
        private static int _maxThreadCount;

        private static DistCacheThreadPoolInternal pool = new DistCacheThreadPoolInternal();
        static DistCacheThreadPool()
        {

        }

        public static void QueueJob(Action action, ManualResetEventSlim resetEvent = null)
        {
            pool.Enqueue(new DistCacheThreadPoolJob() { Action = action, ResetEvent = resetEvent });
        }
    }

    internal class DistCacheThreadPoolJob
    {
        internal ManualResetEventSlim ResetEvent;

        internal Action Action;
    }
    //todo disposable clear all mres
    //set to dispose
    internal class DistCacheThreadPoolInternal : IDisposable
    {
        //jobs waiting
        private ConcurrentQueue<DistCacheThreadPoolJob> JobQueue = new ConcurrentQueue<DistCacheThreadPoolJob>();
        //threads waiting
        private ConcurrentQueue<ManualResetEventSlim> ThreadSignalQueue = new ConcurrentQueue<ManualResetEventSlim>();

        private long _threadCount = 0;
        internal DistCacheThreadPoolInternal()
        {
            EnsureThreads(DistCacheThreadPool.MaxThreadCount);
        }

        internal void EnsureThreads(int maxThreadCount)
        {
            while (CreateThread()) { }
        }

        internal void Enqueue(DistCacheThreadPoolJob job)
        {
            this.JobQueue.Enqueue(job);
            if (ThreadSignalQueue.TryDequeue(out ManualResetEventSlim r))
            {
                r.Set();
            }
        }

        private bool CreateThread()
        {
            if (Interlocked.Read(ref _threadCount) < DistCacheThreadPool.MaxThreadCount)
            {
                ManualResetEventSlim mres = new ManualResetEventSlim(true);
                new Thread(JobStuff).Start(mres);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void JobStuff(object o)
        {
            ManualResetEventSlim mres = o as ManualResetEventSlim;
            Interlocked.Increment(ref _threadCount);
            while (Interlocked.Read(ref _threadCount) <= DistCacheThreadPool.MaxThreadCount)
            {
                mres.Wait();

                while (JobQueue.TryDequeue(out DistCacheThreadPoolJob job))
                {
                    job.Action.Invoke();
                    job?.ResetEvent?.Set();
                }

                mres.Reset();
                ThreadSignalQueue.Enqueue(mres);
            }
            mres.Dispose();
            Interlocked.Decrement(ref _threadCount);
        }

        public void Dispose()
        {
            DistCacheThreadPool.MaxThreadCount = 0;
            while (JobQueue.Any())
            {
                Thread.Sleep(10);
            }
            while (ThreadSignalQueue.TryDequeue(out ManualResetEventSlim r))
            {
                r.Set();
            }
        }
    }
}
