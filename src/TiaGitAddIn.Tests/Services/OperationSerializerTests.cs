using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public sealed class OperationSerializerTests
    {
        [Fact]
        public async Task AcquireAsyncRejectsConcurrentOperationWhenWaitIsDisabled()
        {
            OperationSerializer serializer = new OperationSerializer();

            using (await serializer.AcquireAsync(CancellationToken.None))
            {
                await Assert.ThrowsAsync<GitOperationInProgressException>(
                    () => serializer.AcquireAsync(CancellationToken.None, waitForTurn: false));
            }
        }

        [Fact]
        public async Task DisposeReleasesOperation()
        {
            OperationSerializer serializer = new OperationSerializer();

            (await serializer.AcquireAsync(CancellationToken.None)).Dispose();

            using (await serializer.AcquireAsync(CancellationToken.None, waitForTurn: false))
            {
                Assert.True(serializer.IsBusy);
            }
        }
    }
}
