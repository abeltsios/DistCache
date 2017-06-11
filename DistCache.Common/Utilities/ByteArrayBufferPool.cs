using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Utilities
{
    public class ByteArrayBufferPool : ReusableObjectsPool<byte[]>
    {
        public byte[] Stream => FromPool;
        public ByteArrayBufferPool() : base(() => { return new byte[4 * 1 << 10]; })
        {

        }
    }
}
