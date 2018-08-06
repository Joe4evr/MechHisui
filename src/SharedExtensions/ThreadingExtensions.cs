using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharedExtensions
{
    internal static class ThreadingExtensions
    {
        public static async Task<SemaphoreLock> UsingLock(this SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            return new SemaphoreLock(semaphore);
        }

        public struct SemaphoreLock : IDisposable
        {
            private readonly SemaphoreSlim _sema;

            public SemaphoreLock(SemaphoreSlim sema)
            {
                _sema = sema ?? throw new ArgumentNullException();
            }

            public void Dispose()
                => _sema.Release();
        }
    }
}
