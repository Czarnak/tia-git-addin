using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Models;

namespace TiaGitAddIn.Services
{
    public sealed class GitService : IGitService
    {
        private const string PrettyCommitFormat = "%H%x1f%an%x1f%aI%x1f%s%x1f%P";
        private readonly IGitProcessRunner runner;
        private readonly OperationSerializer serializer;
        private readonly string gitExecutablePath;
        private readonly string repositoryRoot;

        public GitService(
            IGitProcessRunner runner,
            OperationSerializer serializer,
            string gitExecutablePath,
            string repositoryRoot)
        {
            this.runner = runner;
            this.serializer = serializer;
            this.gitExecutablePath = gitExecutablePath;
            this.repositoryRoot = repositoryRoot;
        }

        public async Task<GitStatus> GetStatusAsync(CancellationToken ct = default)
        {
            GitProcessResult result = await RunAsync(
                new[] { "status", "--porcelain=v1", "-b" },
                ct).ConfigureAwait(false);

            EnsureSuccess(result, "Unable to read Git status.");
            return GitOutputParser.ParseStatus(result.StandardOutput);
        }

        public async Task<OperationResult> StageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                return OperationResult.Ok("No files to stage.");
            }

            foreach (string path in filePaths)
            {
                ValidationResult validation = PathValidator.Validate(path);
                if (!validation.IsValid)
                {
                    return OperationResult.Fail(validation.ErrorMessage);
                }
            }

            List<string> args = new List<string> { "add", "--" };
            args.AddRange(filePaths);

            GitProcessResult result = await RunExclusiveAsync(args, ct).ConfigureAwait(false);

            return ToOperationResult(result, "Files staged.", "Unable to stage files.");
        }

        public async Task<OperationResult> UnstageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default)
        {
            if (filePaths == null || filePaths.Count == 0)
            {
                return OperationResult.Ok("No files to unstage.");
            }

            foreach (string path in filePaths)
            {
                ValidationResult validation = PathValidator.Validate(path);
                if (!validation.IsValid)
                {
                    return OperationResult.Fail(validation.ErrorMessage);
                }
            }

            List<string> args = new List<string> { "restore", "--staged", "--" };
            args.AddRange(filePaths);

            GitProcessResult result = await RunExclusiveAsync(args, ct).ConfigureAwait(false);

            return ToOperationResult(result, "Files unstaged.", "Unable to unstage files.");
        }

        public async Task<OperationResult> CommitAsync(string message, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return OperationResult.Fail("Commit message is required.");
            }

            GitProcessResult result = await RunExclusiveAsync(
                new[] { "commit", "-m", message },
                ct).ConfigureAwait(false);

            return ToOperationResult(result, "Commit created.", "Unable to create commit.");
        }

        public async Task<IReadOnlyList<CommitInfo>> GetCommitLogAsync(
            int maxCount,
            CancellationToken ct = default)
        {
            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount), "Commit count must be positive.");
            }

            GitProcessResult result = await RunAsync(
                new[] { "log", "--date=iso-strict", "--pretty=format:" + PrettyCommitFormat, "-n", maxCount.ToString() },
                ct).ConfigureAwait(false);

            EnsureSuccess(result, "Unable to read commit log.");
            return GitOutputParser.ParseCommitLog(result.StandardOutput).ToList();
        }

        public async Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(CancellationToken ct = default)
        {
            GitProcessResult result = await RunAsync(
                new[] { "branch", "--format=%(HEAD)%x1f%(refname:short)%x1f%(upstream:short)" },
                ct).ConfigureAwait(false);

            EnsureSuccess(result, "Unable to read branches.");
            return GitOutputParser.ParseBranches(result.StandardOutput).ToList();
        }

        public async Task<OperationResult> CheckoutBranchAsync(string branchName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(branchName))
            {
                return OperationResult.Fail("Branch name is required.");
            }

            GitProcessResult result = await RunExclusiveAsync(
                new[] { "checkout", branchName },
                ct).ConfigureAwait(false);

            return ToOperationResult(result, "Branch checked out.", "Unable to check out branch.");
        }

        public async Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(CancellationToken ct = default)
        {
            GitProcessResult result = await RunAsync(
                new[] { "remote", "-v" },
                ct).ConfigureAwait(false);

            EnsureSuccess(result, "Unable to read remotes.");
            return GitOutputParser.ParseRemotes(result.StandardOutput).ToList();
        }

        private async Task<GitProcessResult> RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken ct)
        {
            return await runner.RunAsync(
                gitExecutablePath,
                repositoryRoot,
                arguments,
                ct).ConfigureAwait(false);
        }

        private async Task<GitProcessResult> RunExclusiveAsync(
            IReadOnlyList<string> arguments,
            CancellationToken ct)
        {
            using (await serializer.AcquireAsync(ct).ConfigureAwait(false))
            {
                return await RunAsync(arguments, ct).ConfigureAwait(false);
            }
        }

        private static OperationResult ToOperationResult(
            GitProcessResult result,
            string successMessage,
            string failureMessage)
        {
            return result.IsSuccess
                ? OperationResult.Ok(successMessage)
                : OperationResult.Fail(failureMessage, result.StandardError);
        }

        private static void EnsureSuccess(GitProcessResult result, string message)
        {
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(message + " " + result.StandardError);
            }
        }
    }
}
