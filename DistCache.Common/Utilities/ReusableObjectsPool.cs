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
        protected delegate bool PoolableObjectCheck(T toCheck);

        public static int MaxPoolSize { get; protected set; } = 1000;

        protected T FromPool { get; private set; }
        protected PoolableObjectCheck OnReturnToPoolCheck { get; private set; }

        private static readonly bool ShouldDispose = (typeof(T) is IDisposable);

        protected ReusableObjectsPool(PoolableObjectFactory factory = null, PoolableObjectCheck check = null)
        {
            this.FromPool = GetFromPool(factory);
            this.OnReturnToPoolCheck = check;
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

        protected static void ReturnToPool(T toPool, PoolableObjectCheck extraCheck=null)
        {
            if (ObjectPool.Count < MaxPoolSize && (extraCheck == null || extraCheck.Invoke(toPool)))
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
