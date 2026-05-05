using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Logging;

namespace TiaGitAddIn.Services
{
    public sealed class GitFileExtractor : IGitFileExtractor
    {
        private readonly IGitProcessRunner processRunner;
        private readonly string gitExecutablePath;
        private readonly string repositoryRoot;
        private readonly IAddInLogger? logger;

        public GitFileExtractor(IGitProcessRunner processRunner, string gitExecutablePath, string repositoryRoot, IAddInLogger? logger = null)
        {
            this.processRunner = processRunner;
            this.gitExecutablePath = gitExecutablePath;
            this.repositoryRoot = repositoryRoot;
            this.logger = logger;
        }

        public async Task<string> ExtractFileAsync(string? commitHash, string filePath, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(commitHash))
            {
                var localPath = Path.Combine(repositoryRoot, filePath);
                logger?.Info($"GitFileExtractor: Using local file for working tree diff: {localPath}");
                return localPath;
            }

            logger?.Info($"GitFileExtractor: Extracting {filePath} at {commitHash}");
            var result = await processRunner.RunAsync(
                gitExecutablePath,
                repositoryRoot,
                new[] { "show", $"{commitHash}:{filePath}" },
                ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                logger?.Error($"GitFileExtractor: Failed to extract {filePath} at {commitHash}: {result.StandardError}", new Exception(result.StandardError));
                throw new InvalidOperationException($"Failed to extract file {filePath} at {commitHash}: {result.StandardError}");
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");
            File.WriteAllText(tempPath, result.StandardOutput);
            logger?.Info($"GitFileExtractor: Extracted to temp file {tempPath}");
            return tempPath;
        }

        public async Task<string?> ExtractParentFileAsync(string? commitHash, string filePath, CancellationToken ct)
        {
            string gitRef = string.IsNullOrEmpty(commitHash) ? "HEAD" : $"{commitHash}^";
            
            logger?.Info($"GitFileExtractor: Extracting parent {filePath} at {gitRef}");

            var result = await processRunner.RunAsync(
                gitExecutablePath,
                repositoryRoot,
                new[] { "show", $"{gitRef}:{filePath}" },
                ct).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                logger?.Info($"GitFileExtractor: Parent file {filePath} not found at {gitRef} (likely newly added). Exit code: {result.ExitCode}, Stderr: {result.StandardError}");
                return null;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xml");
            File.WriteAllText(tempPath, result.StandardOutput);
            logger?.Info($"GitFileExtractor: Extracted parent to temp file {tempPath}");
            return tempPath;
        }
    }
}