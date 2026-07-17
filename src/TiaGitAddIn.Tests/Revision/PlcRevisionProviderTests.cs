using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services;
using TiaGitAddIn.Services.Revision;
using Xunit;

namespace TiaGitAddIn.Tests.Revision
{
    public class PlcRevisionProviderTests
    {
        [Theory]
        [InlineData("utf8", "żółć", PlcTextEncodingKind.Utf8, false)]
        [InlineData("utf8-bom", "żółć", PlcTextEncodingKind.Utf8, true)]
        [InlineData("utf16-le", "żółć", PlcTextEncodingKind.Utf16LittleEndian, true)]
        [InlineData("utf16-be", "żółć", PlcTextEncodingKind.Utf16BigEndian, true)]
        public async Task LoadAsyncDecodesOnlySupportedStrictEncodings(
            string fixture, string expected, PlcTextEncodingKind kind, bool hasBom)
        {
            byte[] bytes = EncodingFixture.Create(fixture, expected);
            var provider = CreateProvider(bytes, maximumBytes: bytes.Length);

            using PlcRevisionLease lease = await provider.LoadAsync(
                PlcRevisionSide.Left,
                PlcRevisionSource.Commit("0123456789abcdef"),
                "Neutral/Program.bin",
                CancellationToken.None);

            Assert.Equal(bytes, lease.Revision.Bytes);
            Assert.Equal(expected, lease.Revision.Text);
            Assert.Equal(kind, lease.Revision.Encoding.Kind);
            Assert.Equal(hasBom, lease.Revision.Encoding.HasBom);
            Assert.Equal(".bin", lease.Revision.OriginalSuffix);
            Assert.DoesNotContain('�', lease.Revision.Text!);
        }

        [Fact]
        public async Task InvalidUtf8IsUndecodedBinary()
        {
            var provider = CreateProvider(new byte[] { 0xC3, 0x28 }, maximumBytes: 2);
            using PlcRevisionLease lease = await provider.LoadAsync(
                PlcRevisionSide.Right, PlcRevisionSource.Head, "Program.xml", CancellationToken.None);

            Assert.True(lease.Revision.IsBinary);
            Assert.Null(lease.Revision.Text);
            Assert.Equal(PlcTextEncodingKind.None, lease.Revision.Encoding.Kind);
            Assert.False(string.IsNullOrWhiteSpace(lease.Revision.EncodingLimitation));
        }

        [Fact]
        public async Task NPlusOneBytesFailBeforeBlobRead()
        {
            var blob = new FakeGitBlobReader(size: 5, bytes: new byte[5]);
            var provider = CreateProvider(blob, maximumBytes: 4);

            await Assert.ThrowsAsync<RevisionSizeLimitException>(() => provider.LoadAsync(
                PlcRevisionSide.Left, PlcRevisionSource.Head, "Program.xml", CancellationToken.None));

            Assert.Equal(0, blob.ReadCount);
        }

        [Fact]
        public async Task NBytesAtLimitSucceed()
        {
            byte[] bytes = { 1, 2, 3, 4 };
            var blob = new FakeGitBlobReader(size: bytes.Length, bytes: bytes);
            var provider = CreateProvider(blob, maximumBytes: bytes.Length);

            using PlcRevisionLease lease = await provider.LoadAsync(
                PlcRevisionSide.Left, PlcRevisionSource.Head, "Program.xml", CancellationToken.None);

            Assert.Equal(bytes, lease.Revision.Bytes);
            Assert.Equal(1, blob.ReadCount);
        }

        [Fact]
        public async Task LoadAsyncPropagatesCancellationWithoutWrapping()
        {
            var provider = CreateProvider(new byte[] { 1, 2, 3 }, maximumBytes: 10);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => provider.LoadAsync(
                PlcRevisionSide.Left, PlcRevisionSource.Head, "Program.xml", cts.Token));
        }

        [Fact]
        public void MissingReturnsLeaseWithNoWorkingFile()
        {
            var provider = CreateProvider(Array.Empty<byte>(), maximumBytes: 1);

            using PlcRevisionLease lease = provider.Missing(
                PlcRevisionSide.Right, PlcRevisionSource.WorkingTree, "Program.xml", PlcRevisionMissingReason.Deleted);

            Assert.True(lease.Revision.IsMissing);
            Assert.Equal(PlcRevisionMissingReason.Deleted, lease.Revision.MissingReason);
            Assert.Null(lease.WorkingFilePath);
        }

        [Theory]
        [InlineData("C:\\evil\\Program.xml")]
        [InlineData("\\\\server\\share\\Program.xml")]
        [InlineData("../secrets.xml")]
        [InlineData("Blocks/../../secrets.xml")]
        [InlineData("Program.xml\0hidden")]
        public async Task GitBlobReaderRejectsMaliciousPaths(string maliciousPath)
        {
            var reader = new GitBlobReader(
                new ThrowingGitProcessRunner(), new ThrowingGitBinaryProcessRunner(), "git", CreateTestRoot());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                reader.GetSizeAsync(PlcRevisionSource.Head, maliciousPath, CancellationToken.None));
        }

        [Theory]
        [InlineData("not-hex-zz")]
        [InlineData("abc123")]
        [InlineData("")]
        [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef0")]
        public async Task GitBlobReaderRejectsMaliciousRevisions(string hash)
        {
            var reader = new GitBlobReader(
                new ThrowingGitProcessRunner(), new ThrowingGitBinaryProcessRunner(), "git", CreateTestRoot());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                reader.GetSizeAsync(PlcRevisionSource.Commit(hash), "Program.xml", CancellationToken.None));
        }

        [Fact]
        public async Task GitBlobReaderReadsWorkingTreeSizeAndContentDirectlyFromDisk()
        {
            string root = CreateTestRoot();
            Directory.CreateDirectory(root);
            const string relativePath = "Program.xml";
            byte[] expectedBytes = Encoding.UTF8.GetBytes("<Document>working tree content</Document>");
            File.WriteAllBytes(Path.Combine(root, relativePath), expectedBytes);

            var reader = new GitBlobReader(
                new ThrowingGitProcessRunner(), new ThrowingGitBinaryProcessRunner(), "git", root);

            long size = await reader.GetSizeAsync(PlcRevisionSource.WorkingTree, relativePath, CancellationToken.None);
            IReadOnlyList<byte> bytes = await reader.ReadAsync(
                PlcRevisionSource.WorkingTree, relativePath, expectedBytes.Length, CancellationToken.None);

            Assert.Equal(expectedBytes.Length, size);
            Assert.Equal(expectedBytes, bytes);
        }

        [Fact]
        public async Task GitBlobReaderRejectsWorkingTreeContentExceedingTheSizeLimitBeforeAFullRead()
        {
            string root = CreateTestRoot();
            Directory.CreateDirectory(root);
            const string relativePath = "Program.xml";
            File.WriteAllBytes(Path.Combine(root, relativePath), new byte[16]);

            var reader = new GitBlobReader(
                new ThrowingGitProcessRunner(), new ThrowingGitBinaryProcessRunner(), "git", root);

            await Assert.ThrowsAsync<RevisionSizeLimitException>(() =>
                reader.ReadAsync(PlcRevisionSource.WorkingTree, relativePath, maximumBytes: 4, CancellationToken.None));
        }

        [Fact]
        public async Task GitBlobReaderRejectsAMissingWorkingTreeFileAsAHardFailure()
        {
            string root = CreateTestRoot();
            Directory.CreateDirectory(root);

            var reader = new GitBlobReader(
                new ThrowingGitProcessRunner(), new ThrowingGitBinaryProcessRunner(), "git", root);

            await Assert.ThrowsAsync<RevisionLoadException>(() =>
                reader.GetSizeAsync(PlcRevisionSource.WorkingTree, "DoesNotExist.xml", CancellationToken.None));
        }

        [Theory]
        [InlineData("C:\\evil\\Program.xml")]
        [InlineData("\\\\server\\share\\Program.xml")]
        [InlineData("../secrets.xml")]
        [InlineData("Blocks/../../secrets.xml")]
        [InlineData("Program.xml\0hidden")]
        public async Task GitBlobReaderRejectsMaliciousPathsForWorkingTreeSourceToo(string maliciousPath)
        {
            var reader = new GitBlobReader(
                new ThrowingGitProcessRunner(), new ThrowingGitBinaryProcessRunner(), "git", CreateTestRoot());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                reader.GetSizeAsync(PlcRevisionSource.WorkingTree, maliciousPath, CancellationToken.None));
        }

        [Fact]
        public async Task LoadAsyncReadsAWorkingTreeRevisionEndToEnd()
        {
            string root = CreateTestRoot();
            Directory.CreateDirectory(root);
            const string relativePath = "Program.xml";
            byte[] expectedBytes = Encoding.UTF8.GetBytes("hello working tree");
            File.WriteAllBytes(Path.Combine(root, relativePath), expectedBytes);

            var blobReader = new GitBlobReader(
                new ThrowingGitProcessRunner(), new ThrowingGitBinaryProcessRunner(), "git", root);
            var provider = new PlcRevisionProvider(
                blobReader, new PlcRevisionProviderOptions(expectedBytes.Length, CreateTestRoot()));

            using PlcRevisionLease lease = await provider.LoadAsync(
                PlcRevisionSide.Right, PlcRevisionSource.WorkingTree, relativePath, CancellationToken.None);

            Assert.Equal(expectedBytes, lease.Revision.Bytes);
            Assert.Equal("hello working tree", lease.Revision.Text);
        }

        [Fact]
        public async Task LoadAsyncRejectsAnOversizedWorkingTreeRevisionBeforeReadingItsContent()
        {
            string root = CreateTestRoot();
            Directory.CreateDirectory(root);
            const string relativePath = "Program.xml";
            File.WriteAllBytes(Path.Combine(root, relativePath), new byte[16]);

            var blobReader = new GitBlobReader(
                new ThrowingGitProcessRunner(), new ThrowingGitBinaryProcessRunner(), "git", root);
            var provider = new PlcRevisionProvider(blobReader, new PlcRevisionProviderOptions(4, CreateTestRoot()));

            await Assert.ThrowsAsync<RevisionSizeLimitException>(() => provider.LoadAsync(
                PlcRevisionSide.Right, PlcRevisionSource.WorkingTree, relativePath, CancellationToken.None));
        }

        private static PlcRevisionProvider CreateProvider(byte[] bytes, int maximumBytes)
            => CreateProvider(new FakeGitBlobReader(size: bytes.Length, bytes: bytes), maximumBytes);

        private static PlcRevisionProvider CreateProvider(FakeGitBlobReader blob, int maximumBytes)
            => new PlcRevisionProvider(blob, new PlcRevisionProviderOptions(maximumBytes, CreateTestRoot()));

        private static string CreateTestRoot()
            => Path.Combine(Path.GetTempPath(), "TiaGitAddInTests", Guid.NewGuid().ToString("N"));

        private sealed class ThrowingGitProcessRunner : IGitProcessRunner
        {
            public Task<GitProcessResult> RunAsync(string gitExecutablePath, string workingDirectory,
                IReadOnlyList<string> arguments, CancellationToken cancellationToken)
                => throw new InvalidOperationException("Validation must reject the input before any process is invoked.");
        }

        private sealed class ThrowingGitBinaryProcessRunner : IGitBinaryProcessRunner
        {
            public Task<GitBinaryProcessResult> RunBinaryAsync(string gitExecutablePath, string workingDirectory,
                IReadOnlyList<string> arguments, int maximumStandardOutputBytes, CancellationToken cancellationToken)
                => throw new InvalidOperationException("Validation must reject the input before any process is invoked.");
        }
    }
}
