using System;
using System.IO;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Revision;
using TiaGitAddIn.Tests.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Revision
{
    public class PlcRevisionLeaseTests
    {
        [Fact]
        public void ConcurrentLeasesAreUniquePreserveSuffixAndDeleteTheirOwnScope()
        {
            string root = CreateTestRoot();
            PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, "text", "Blocks/Program.scl");
            PlcRevisionLease first = PlcRevisionLease.Create(revision, root);
            PlcRevisionLease second = PlcRevisionLease.Create(revision, root);

            Assert.NotEqual(first.WorkingFilePath, second.WorkingFilePath);
            Assert.Equal(".scl", Path.GetExtension(first.WorkingFilePath));
            Assert.True(File.Exists(first.WorkingFilePath));
            Assert.True(File.Exists(second.WorkingFilePath));

            string firstDirectory = first.LeaseDirectory!;
            string secondDirectory = second.LeaseDirectory!;
            first.Dispose();
            Assert.False(Directory.Exists(firstDirectory));
            Assert.True(Directory.Exists(secondDirectory));
            second.Dispose();
            Assert.False(Directory.Exists(secondDirectory));
        }

        [Fact]
        public void MissingLeaseCreatesNoTemporaryFileAndDisposeIsIdempotent()
        {
            PlcRevision revision = ComparisonTestData.MissingRevision(PlcRevisionSide.Left);
            PlcRevisionLease lease = PlcRevisionLease.Create(revision, CreateTestRoot());
            Assert.Null(lease.WorkingFilePath);
            lease.Dispose();
            lease.Dispose();
            Assert.Equal(1, lease.DisposeCountForTests);
        }

        [Fact]
        public void PresentLeaseWritesExactOriginalBytes()
        {
            byte[] bytes = { 0, 1, 2, 3, 0xFF, 0xEF, 0xBB, 0xBF };
            PlcRevision revision = PlcRevision.Present(PlcRevisionSide.Right, PlcRevisionSource.Head,
                "Program.bin", bytes, PlcTextEncoding.None, null, true, "binary content");
            using PlcRevisionLease lease = PlcRevisionLease.Create(revision, CreateTestRoot());

            byte[] written = File.ReadAllBytes(lease.WorkingFilePath!);
            Assert.Equal(bytes, written);
        }

        [Fact]
        public void DisposeRetriesThenThrowsRedactedCleanupExceptionWhenCleanupKeepsFailing()
        {
            string root = CreateTestRoot();
            PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, "text", "Blocks/Program.scl");
            PlcRevisionLease lease = PlcRevisionLease.Create(revision, root);
            string leaseDirectory = lease.LeaseDirectory!;

            using (new FileStream(lease.WorkingFilePath!, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                DateTime start = DateTime.UtcNow;
                RevisionCleanupException exception = Assert.Throws<RevisionCleanupException>(() => lease.Dispose());
                TimeSpan elapsed = DateTime.UtcNow - start;

                // 20 + 50 + 100 ms of retry backoff must actually have elapsed.
                Assert.True(elapsed >= TimeSpan.FromMilliseconds(150), $"Expected retries to take >= 150ms, took {elapsed}.");
                Assert.DoesNotContain(leaseDirectory, exception.Message, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(root, exception.Message, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(1, lease.DisposeCountForTests);
            }

            if (Directory.Exists(leaseDirectory))
            {
                Directory.Delete(leaseDirectory, recursive: true);
            }
        }

        private static string CreateTestRoot()
            => Path.Combine(Path.GetTempPath(), "TiaGitAddInTests", Guid.NewGuid().ToString("N"));
    }
}
