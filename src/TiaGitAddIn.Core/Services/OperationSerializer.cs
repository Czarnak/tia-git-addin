using System;
using System.Threading;
using System.Threading.Tasks;

namespace TiaGitAddIn.Services
{
    public sealed class OperationSerializer
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);

        public bool IsBusy => semaphore.CurrentCount == 0;

        public async Task<IDisposable> AcquireAsync(
            CancellationToken cancellationToken,
            bool waitForTurn = true)
        {
            if (waitForTurn)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (!semaphore.Wait(0))
            {
                throw new GitOperationInProgressException();
            }

            return new Lease(semaphore);
        }

        private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                semaphore.Release();
                disposed = true;
            }
        }
    }
}
