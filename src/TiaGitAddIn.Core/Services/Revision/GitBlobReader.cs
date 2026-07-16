using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Revision
{
    /// <summary>
    /// Reads git blob size and content via <c>git cat-file</c>, going through the text runner for the
    /// cheap <c>-s</c> size query and the binary runner for the actual blob bytes. Every repository-relative
    /// path and every revision is validated before any process argument is built; nothing is ever
    /// concatenated into a shell command (<c>UseShellExecute=false</c> throughout the process seam).
    /// </summary>
    public sealed class GitBlobReader : IGitBlobReader
    {
        private static readonly Regex HexRevisionPattern = new Regex("^[0-9a-fA-F]{7,64}$", RegexOptions.Compiled);

        private readonly IGitProcessRunner _textRunner;
        private readonly IGitBinaryProcessRunner _binaryRunner;
        private readonly string _gitExecutablePath;
        private readonly string _repositoryRoot;

        public GitBlobReader(IGitProcessRunner textRunner, IGitBinaryProcessRunner binaryRunner,
            string gitExecutablePath, string repositoryRoot)
        {
            _textRunner = textRunner ?? throw new ArgumentNullException(nameof(textRunner));
            _binaryRunner = binaryRunner ?? throw new ArgumentNullException(nameof(binaryRunner));
            _gitExecutablePath = string.IsNullOrWhiteSpace(gitExecutablePath)
                ? throw new ArgumentException("Git executable path is required.", nameof(gitExecutablePath))
                : gitExecutablePath;
            _repositoryRoot = string.IsNullOrWhiteSpace(repositoryRoot)
                ? throw new ArgumentException("Repository root is required.", nameof(repositoryRoot))
                : Path.GetFullPath(repositoryRoot);
        }

        public async Task<long> GetSizeAsync(PlcRevisionSource source, string repositoryRelativePath,
            CancellationToken cancellationToken)
        {
            string objectExpression = BuildObjectExpression(source, repositoryRelativePath);
            cancellationToken.ThrowIfCancellationRequested();

            GitProcessResult result = await _textRunner.RunAsync(
                _gitExecutablePath, _repositoryRoot,
                new[] { "cat-file", "-s", objectExpression },
                cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new RevisionLoadException("Failed to read the size of the requested revision.");
            }

            if (!long.TryParse(result.StandardOutput.Trim(), out long size))
            {
                throw new RevisionLoadException("Git returned an unexpected response for the revision size.");
            }

            return size;
        }

        public async Task<IReadOnlyList<byte>> ReadAsync(PlcRevisionSource source, string repositoryRelativePath,
            int maximumBytes, CancellationToken cancellationToken)
        {
            string objectExpression = BuildObjectExpression(source, repositoryRelativePath);
            cancellationToken.ThrowIfCancellationRequested();

            GitBinaryProcessResult result = await _binaryRunner.RunBinaryAsync(
                _gitExecutablePath, _repositoryRoot,
                new[] { "cat-file", "blob", objectExpression },
                maximumBytes,
                cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new RevisionLoadException("Failed to read the content of the requested revision.");
            }

            return result.StandardOutput;
        }

        private string BuildObjectExpression(PlcRevisionSource source, string repositoryRelativePath)
        {
            string path = ValidatePath(repositoryRelativePath);
            string revision = ValidateRevision(source);
            return $"{revision}:{path}";
        }

        private string ValidatePath(string? repositoryRelativePath)
        {
            if (string.IsNullOrEmpty(repositoryRelativePath))
            {
                throw new ArgumentException("Repository-relative path is required.", nameof(repositoryRelativePath));
            }

            string path = repositoryRelativePath!;

            if (path.IndexOf('\0') >= 0)
            {
                throw new ArgumentException("Path must not contain a NUL character.", nameof(repositoryRelativePath));
            }

            if (Path.IsPathRooted(path) || IsDriveOrUncQualified(path))
            {
                throw new ArgumentException("Path must be repository-relative, not rooted.", nameof(repositoryRelativePath));
            }

            string normalized = path.Replace('\\', '/');
            string[] segments = normalized.Split('/');
            foreach (string segment in segments)
            {
                if (segment.Length == 0 || segment == "." || segment == "..")
                {
                    throw new ArgumentException(
                        "Path must not contain empty, '.' or '..' segments.", nameof(repositoryRelativePath));
                }
            }

            string combined = Path.GetFullPath(Path.Combine(_repositoryRoot, normalized));
            string rootWithSeparator = _repositoryRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? _repositoryRoot
                : _repositoryRoot + Path.DirectorySeparatorChar;

            if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Path escapes the repository root.", nameof(repositoryRelativePath));
            }

            return normalized;
        }

        private static bool IsDriveOrUncQualified(string path)
            => path.StartsWith("\\\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal);

        private static string ValidateRevision(PlcRevisionSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            switch (source.Kind)
            {
                case PlcRevisionSourceKind.Head:
                    return "HEAD";
                case PlcRevisionSourceKind.Commit:
                    return ValidateHex(source.CommitHash);
                case PlcRevisionSourceKind.ParentOfCommit:
                    return ValidateHex(source.CommitHash) + "^";
                default:
                    throw new ArgumentException(
                        $"Revision source kind '{source.Kind}' cannot be read as a git blob.", nameof(source));
            }
        }

        private static string ValidateHex(string? hash)
        {
            if (string.IsNullOrEmpty(hash) || !HexRevisionPattern.IsMatch(hash))
            {
                throw new ArgumentException("Revision must be 7-64 hexadecimal characters.", nameof(hash));
            }

            return hash!;
        }
    }
}
