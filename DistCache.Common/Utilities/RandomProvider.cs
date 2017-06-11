using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Threading;

namespace DistCache.Common.Utilities
{
    public static class RandomProvider
    {
        private static ThreadLocal<Random> rand = new ThreadLocal<Random>(() => new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0)), false);

        public static int Next(int min, int max)
        {
            return rand.Value.Next(min, max);
        }
    }
}
