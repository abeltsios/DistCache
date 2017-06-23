using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Utilities
{
    public class ByteArrayBufferPool : ReusableObjectsPool<byte[]>
    {
        private const int ArraySize = 1 << 15; // 32kb
        static ByteArrayBufferPool()
        {
            //16mb for io buffer
            MaxPoolSize = (16 * (1 << 20)) / (1 << 15);
        }
        public byte[] ByteArray => FromPool;
        public ByteArrayBufferPool() : base(() => { return new byte[ArraySize]; })
        {

        }
    }
}
