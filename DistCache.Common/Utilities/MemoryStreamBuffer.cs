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
    public class MemoryStreamPool : IDisposable
    {
        public const int MaxPool = 150;

        public MemoryStream Stream { get; private set; }

        public MemoryStreamPool()
        {
            this.Stream = GetFromPool();
        }


        private static ConcurrentQueue<MemoryStream> SteamPool = new ConcurrentQueue<MemoryStream>();

        public static MemoryStream GetFromPool()
        {
            if (SteamPool.TryDequeue(out MemoryStream result))
            {
                return result;
            }
            else
            {
                return new MemoryStream();
            }
        }

        public static void ReturnToPool(MemoryStream toPool)
        {
            if (SteamPool.Count < MaxPool && toPool.CanRead && toPool.CanSeek && toPool.CanWrite)
            {
                toPool.SetLength(0);
                SteamPool.Enqueue(toPool);
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    var reference = this.Stream;
                    this.Stream = null;
                    ReturnToPool(reference);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
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
