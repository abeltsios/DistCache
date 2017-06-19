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

        public static int MaxPoolSize { get; set; } = 1000;

        protected T FromPool { get; private set; }

        private static readonly bool ShouldDispose = (typeof(T) is IDisposable);

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
            if (ObjectPool.Count < MaxPoolSize)
            {
                ObjectPool.Enqueue(toPool);
            }
            else if (ShouldDispose)
            {
                (toPool as IDisposable)?.Dispose();
            }
        }

        #region IDisposable Support

        public virtual void Dispose()
        {
            var reference = this.FromPool;
            this.FromPool = null;
            ReturnToPool(reference);
        }
        #endregion
    }


}
