using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DistCache.Common.Utilities
{
    public static class DistCacheExecutor
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
        static DistCacheExecutor()
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
            EnsureThreads(DistCacheExecutor.MaxThreadCount);
        }

        internal void EnsureThreads(int maxThreadCount)
        {
            while (CreateThread()) { }
            while (Interlocked.Read(ref _threadCount) < DistCacheExecutor.MaxThreadCount)
            {
                if(ThreadSignalQueue.TryDequeue(out ManualResetEventSlim mr))
                {
                    mr.Set();
                }
            }
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
            if (Interlocked.Read(ref _threadCount) < DistCacheExecutor.MaxThreadCount)
            {
                ManualResetEventSlim mres = new ManualResetEventSlim(true);

                var t = new Thread(JobStuff);
                Interlocked.Increment(ref _threadCount);
                t.Start(mres);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void JobStuff(object o)
        {
            using (ManualResetEventSlim mres = o as ManualResetEventSlim)
            {

                while (Interlocked.Read(ref _threadCount) <= DistCacheExecutor.MaxThreadCount)
                {
                    try
                    {
                        mres.Wait();
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    while (JobQueue.TryDequeue(out DistCacheThreadPoolJob job))
                    {
                        try
                        {
                            job.Action.Invoke();
                        }
                        catch (Exception ex)
                        {
                            //todo log
                        }
                        finally
                        {
                            job?.ResetEvent?.Set();
                        }
                    }

                    mres.Reset();
                    ThreadSignalQueue.Enqueue(mres);
                }
            }
            Interlocked.Decrement(ref _threadCount);
        }

        public void Dispose()
        {
            DistCacheExecutor.MaxThreadCount = 0;
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
