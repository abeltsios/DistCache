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
    public static class CompressionUtilitities
    {

        public static byte[] Compress(byte[] data)
        {
            using (var ms = new MemoryStreamPool())
            {
                using (var gz = new GZipStream(ms.Stream, CompressionMode.Compress, true))
                {
                    gz.Write(data, 0, data.Length);
                }
                return ms.Stream.ToArray();
            }
        }

        public static byte[] Decompress(MemoryStream compressed)
        {
            using (GZipStream stream = new GZipStream(compressed, CompressionMode.Decompress, true))
            {
                using (var bytearrayBuffer = new ByteArrayBufferPool())
                {
                    byte[] buffer = bytearrayBuffer.Stream;
                    using (var ms = new MemoryStreamPool())
                    {
                        int count = 0;
                        do
                        {
                            count = stream.Read(buffer, 0, buffer.Length);
                            if (count > 0)
                            {
                                ms.Stream.Write(buffer, 0, count);
                            }
                        }
                        while (count > 0);
                        return ms.Stream.ToArray();
                    }
                }
            }
        }

    }
}
