using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Services;
using TiaGitAddIn.Services.Revision;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    /// <summary>
    /// Focused adapter tests for <see cref="GitProcessRunner"/>'s raw-byte reading path
    /// (<see cref="IGitBinaryProcessRunner"/>). The Siemens <c>AddInProcess</c> type is a thin wrapper
    /// around <see cref="System.Diagnostics.Process"/>, so rather than spinning up a real OS process for
    /// every scenario (slow, environment-dependent), the size-limit/cancellation/argument-separation
    /// behavior is exercised directly against the internal <c>ReadBoundedAsync</c> and
    /// <c>BuildArgumentString</c> members, exposed to this assembly via <c>InternalsVisibleTo</c>.
    /// </summary>
    public class GitProcessRunnerTests
    {
        [Fact]
        public async Task ReadBoundedAsyncReturnsExactBytesAtTheLimit()
        {
            byte[] data = { 1, 2, 3, 4 };
            using var stream = new MemoryStream(data);

            byte[] result = await GitProcessRunner.ReadBoundedAsync(stream, maximumBytes: 4, CancellationToken.None);

            Assert.Equal(data, result);
        }

        [Fact]
        public async Task ReadBoundedAsyncThrowsWhenExceedingTheLimitByOneByte()
        {
            byte[] data = { 1, 2, 3, 4, 5 };
            using var stream = new MemoryStream(data);

            RevisionSizeLimitException exception = await Assert.ThrowsAsync<RevisionSizeLimitException>(
                () => GitProcessRunner.ReadBoundedAsync(stream, maximumBytes: 4, CancellationToken.None));

            Assert.DoesNotContain("1", exception.Message);
            Assert.DoesNotContain("\x02", exception.Message);
        }

        [Fact]
        public async Task ReadBoundedAsyncStopsShortlyAfterTheLimitInsteadOfDrainingTheWholeStream()
        {
            byte[] data = new byte[1000];
            var stream = new SingleByteChunkStream(data);

            await Assert.ThrowsAsync<RevisionSizeLimitException>(
                () => GitProcessRunner.ReadBoundedAsync(stream, maximumBytes: 5, CancellationToken.None));

            Assert.True(stream.ReadCallCount <= 10,
                $"Expected reading to stop shortly after the limit was exceeded, but Read was called {stream.ReadCallCount} times.");
        }

        [Fact]
        public async Task ReadBoundedAsyncPropagatesCancellationRatherThanWrappingIt()
        {
            using var stream = new MemoryStream(new byte[10]);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Stream.ReadAsync's default cancellation path throws TaskCanceledException, a subclass of
            // OperationCanceledException; ThrowsAnyAsync accepts the subclass while still proving the
            // cancellation was propagated rather than swallowed or wrapped in something unrelated.
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => GitProcessRunner.ReadBoundedAsync(stream, maximumBytes: 100, cts.Token));
        }

        [Fact]
        public void BuildArgumentStringKeepsAMultiWordArgumentAsASingleQuotedToken()
        {
            string commandLine = GitProcessRunner.BuildArgumentString(
                new[] { "cat-file", "blob", "HEAD:path with spaces/Program.xml" });

            Assert.Equal("cat-file blob \"HEAD:path with spaces/Program.xml\"", commandLine);
        }

        [Fact]
        public void BuildArgumentStringKeepsEachArrayElementAsADiscreteArgument()
        {
            string commandLine = GitProcessRunner.BuildArgumentString(new[] { "cat-file", "-s", "HEAD:a b.xml" });

            // Three logical arguments must survive as three tokens: unquoted flags stay bare,
            // the path containing a space is quoted as a single token rather than split in two.
            Assert.Equal("cat-file -s \"HEAD:a b.xml\"", commandLine);
        }

        /// <summary>Stream that always returns at most one byte per read, forcing many small reads.</summary>
        private sealed class SingleByteChunkStream : Stream
        {
            private readonly byte[] _data;
            private int _position;

            public SingleByteChunkStream(byte[] data) => _data = data;

            public int ReadCallCount { get; private set; }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ReadCallCount++;
                if (_position >= _data.Length)
                {
                    return 0;
                }

                buffer[offset] = _data[_position];
                _position++;
                return 1;
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
                => Task.FromResult(Read(buffer, offset, count));

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
