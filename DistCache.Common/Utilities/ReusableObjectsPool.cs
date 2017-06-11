using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace DistCache.Common.Utilities
{
    public class ReusableObjectsPool<T> : IDisposable where T : class
    {
        protected delegate T PoolableObjectFactory();

        protected const int MaxMemoryStreamPoolSize = 100;

        protected T FromPool { get; private set; }

        protected ReusableObjectsPool(PoolableObjectFactory factory = null)
        {
            this.FromPool = GetFromPool(factory);
        }

        private static ConcurrentQueue<T> ObjectPool = new ConcurrentQueue<T>();

        protected static T GetFromPool(PoolableObjectFactory factory = null)
        {
            if (ObjectPool.TryDequeue(out T result))
            {
                return result;
            }
            else
            {
                if (factory == null)
                {
                    return default(T);
                }
                return factory.Invoke();
            }
        }

        protected static void ReturnToPool(T toPool)
        {
            if (ObjectPool.Count < MaxMemoryStreamPoolSize)
            {
                ObjectPool.Enqueue(toPool);
            }
        }

        #region IDisposable Support
        protected bool DisposedValue { get; private set; } = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    var reference = this.FromPool;
                    this.FromPool = null;
                    ReturnToPool(reference);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                DisposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MemoryStreamPool() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }


}
