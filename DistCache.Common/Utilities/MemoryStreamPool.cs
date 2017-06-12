﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistCache.Common.Utilities
{
    public class MemoryStreamPool : ReusableObjectsPool<MemoryStream>
    {

        public MemoryStream Stream => FromPool;
        public MemoryStreamPool() : base(() => { return new MemoryStream(); })
        {

        }

        protected override void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                this.FromPool.SetLength(0);
                base.Dispose(disposing);
            }
        }
    }

}