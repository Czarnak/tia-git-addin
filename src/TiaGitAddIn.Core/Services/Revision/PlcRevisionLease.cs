using System;
using System.IO;
using System.Linq;
using System.Threading;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Revision
{
    /// <summary>
    /// Owns a loaded <see cref="PlcRevision"/>'s scoped temporary working file for the lease's lifetime.
    /// Every present revision gets its own <c>&lt;temporaryRoot&gt;/&lt;Guid:N&gt;/</c> directory so two
    /// concurrently loaded leases for the same path never collide. Missing revisions create nothing on
    /// disk. <see cref="Dispose"/> attempts cleanup exactly once, retrying transient I/O errors before
    /// giving up.
    /// </summary>
    public sealed class PlcRevisionLease : IDisposable
    {
        private static readonly int[] CleanupRetryDelaysMilliseconds = { 20, 50, 100 };

        private int _disposeState;

        private PlcRevisionLease(PlcRevision revision, string? leaseDirectory, string? workingFilePath)
        {
            Revision = revision;
            LeaseDirectory = leaseDirectory;
            WorkingFilePath = workingFilePath;
        }

        public PlcRevision Revision { get; }
        public string? LeaseDirectory { get; }
        public string? WorkingFilePath { get; }
        public int DisposeCountForTests { get; private set; }

        public static PlcRevisionLease Create(PlcRevision revision, string temporaryRoot)
        {
            if (revision == null) throw new ArgumentNullException(nameof(revision));
            if (string.IsNullOrWhiteSpace(temporaryRoot))
            {
                throw new ArgumentException("Temporary root is required.", nameof(temporaryRoot));
            }

            if (revision.IsMissing)
            {
                return new PlcRevisionLease(revision, null, null);
            }

            string leaseDirectory = Path.Combine(temporaryRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(leaseDirectory);

            string suffix = revision.OriginalSuffix ?? string.Empty;
            string workingFilePath = Path.Combine(leaseDirectory, "revision" + suffix);

            byte[] bytes = revision.Bytes as byte[] ?? revision.Bytes.ToArray();
            using (FileStream stream = new FileStream(workingFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            return new PlcRevisionLease(revision, leaseDirectory, workingFilePath);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposeState, 1) != 0)
            {
                return;
            }

            DisposeCountForTests++;

            if (LeaseDirectory == null)
            {
                return;
            }

            CleanupDirectoryWithRetry(LeaseDirectory);
        }

        private static void CleanupDirectoryWithRetry(string directory)
        {
            int totalAttempts = CleanupRetryDelaysMilliseconds.Length + 1;
            for (int attempt = 0; attempt < totalAttempts; attempt++)
            {
                try
                {
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }

                    return;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    bool isLastAttempt = attempt == CleanupRetryDelaysMilliseconds.Length;
                    if (isLastAttempt)
                    {
                        throw new RevisionCleanupException(
                            $"Failed to clean up comparison revision lease '{RedactedLeaseId(directory)}'.", ex);
                    }

                    Thread.Sleep(CleanupRetryDelaysMilliseconds[attempt]);
                }
            }
        }

        private static string RedactedLeaseId(string leaseDirectory)
            => Path.GetFileName(leaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
}
