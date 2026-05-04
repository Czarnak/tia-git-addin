using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI.ViewModels;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class StatusViewModelTests
    {
        [Fact]
        public async Task RefreshAsyncPublishesBranchSummaryAndChangedFiles()
        {
            FakeGitService gitService = new FakeGitService(
                new GitStatus
                {
                    CurrentBranch = "main",
                    TrackingBranch = "origin/main",
                    AheadBy = 1,
                    Entries = new[]
                    {
                        new FileStatusEntry
                        {
                            FilePath = "Blocks/Main.scl",
                            WorkTreeStatus = FileStatus.Modified
                        },
                        new FileStatusEntry
                        {
                            FilePath = "Tags/Plant.xml",
                            IndexStatus = FileStatus.Added
                        }
                    }
                });

            StatusViewModel viewModel = new StatusViewModel(gitService);

            await viewModel.RefreshAsync();

            Assert.Equal("main", viewModel.CurrentBranch);
            Assert.Equal("origin/main, ahead 1", viewModel.TrackingSummary);
            Assert.Equal("2 changed files", viewModel.StatusSummary);
            Assert.Equal(2, viewModel.Entries.Count);
            Assert.Contains(viewModel.Entries, entry =>
                entry.FilePath == "Blocks/Main.scl" &&
                entry.StatusText == "Modified" &&
                entry.Area == "Working tree");
            Assert.Contains(viewModel.Entries, entry =>
                entry.FilePath == "Tags/Plant.xml" &&
                entry.StatusText == "Added" &&
                entry.Area == "Staged");
        }

        [Fact]
        public async Task StageSelectedAsyncStagesFileAndRefreshesStatus()
        {
            FakeGitService gitService = new FakeGitService(
                new GitStatus
                {
                    CurrentBranch = "main",
                    Entries = new[]
                    {
                        new FileStatusEntry
                        {
                            FilePath = "Blocks/Main.scl",
                            WorkTreeStatus = FileStatus.Modified
                        }
                    }
                },
                new GitStatus { CurrentBranch = "main" });
            StatusViewModel viewModel = new StatusViewModel(gitService);
            await viewModel.RefreshAsync();
            viewModel.SelectedEntry = viewModel.Entries.Single();

            await viewModel.StageSelectedAsync();

            Assert.Equal(new[] { "Blocks/Main.scl" }, gitService.StagedPaths);
            Assert.Empty(viewModel.Entries);
            Assert.Equal("File staged.", viewModel.LastOperationMessage);
            Assert.Equal("Working tree clean", viewModel.StatusSummary);
        }

        private sealed class FakeGitService : IGitService
        {
            private readonly Queue<GitStatus> statuses;

            public FakeGitService(params GitStatus[] statuses)
            {
                this.statuses = new Queue<GitStatus>(statuses);
            }

            public IReadOnlyList<string> StagedPaths => stagedPaths;

            private readonly List<string> stagedPaths = new List<string>();

            public Task<GitStatus> GetStatusAsync(CancellationToken ct = default)
            {
                return Task.FromResult(statuses.Count > 0 ? statuses.Dequeue() : new GitStatus());
            }

            public Task<OperationResult> StageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default)
            {
                stagedPaths.AddRange(filePaths);
                return Task.FromResult(OperationResult.Ok("File staged."));
            }

            public Task<OperationResult> UnstageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("File unstaged."));

            public Task<OperationResult> CommitAsync(string message, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Commit created."));

            public Task<IReadOnlyList<CommitInfo>> GetCommitLogAsync(int maxCount, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<CommitInfo>>(new List<CommitInfo>());

            public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<BranchInfo>>(new List<BranchInfo>());

            public Task<OperationResult> CheckoutBranchAsync(string branchName, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Branch checked out."));

            public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<RemoteInfo>>(new List<RemoteInfo>());
        }
    }
}
