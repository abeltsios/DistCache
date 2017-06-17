using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DistCache.Common.Utilities
{
    public static class Executor
    {
        public static CancellationToken CancellationToken { get; } 
        private static readonly TaskFactory _taskFactory;

        static Executor()
        {
            CancellationToken = new CancellationToken(false);
            _taskFactory = new TaskFactory(CancellationToken);
        }
    }
}
