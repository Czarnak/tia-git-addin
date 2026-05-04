using System;
using System.Threading;
using System.Threading.Tasks;

namespace TiaGitAddIn.Services
{
    public sealed class OperationSerializer
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public bool IsBusy => semaphore.CurrentCount == 0;

        public async Task<IDisposable> AcquireAsync(
            CancellationToken cancellationToken,
            bool waitForTurn = true)
        {
            bool acquired = waitForTurn
                ? await WaitAsync(cancellationToken).ConfigureAwait(false)
                : semaphore.Wait(0);

            if (!acquired)
            {
                throw new GitOperationInProgressException();
            }

            return new Lease(semaphore);
        }

        private async Task<bool> WaitAsync(CancellationToken cancellationToken)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }

        private sealed class Lease : IDisposable
        {
            private readonly SemaphoreSlim semaphore;
            private bool disposed;

            public Lease(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

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
