using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Revision;

namespace TiaGitAddIn.Tests.Revision
{
    /// <summary>
    /// Caller-supplied-bytes fake for <see cref="IGitBlobReader"/>, used to drive
    /// <see cref="TiaGitAddIn.Services.Revision.PlcRevisionProvider"/> without a real git process.
    /// Tracks call counts so tests can prove size gating happens before any read.
    /// </summary>
    internal sealed class FakeGitBlobReader : IGitBlobReader
    {
        private readonly long _size;
        private readonly byte[] _bytes;

        public FakeGitBlobReader(long size, byte[] bytes)
        {
            _size = size;
            _bytes = bytes;
        }

        public int SizeCallCount { get; private set; }

        public int ReadCount { get; private set; }

        public Task<long> GetSizeAsync(PlcRevisionSource source, string repositoryRelativePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SizeCallCount++;
            return Task.FromResult(_size);
        }

        public Task<IReadOnlyList<byte>> ReadAsync(PlcRevisionSource source, string repositoryRelativePath,
            int maximumBytes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadCount++;
            return Task.FromResult<IReadOnlyList<byte>>(_bytes);
        }
    }
}
