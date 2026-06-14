using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.ViewModels;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class StatusViewModelTests
    {
        [Fact]
        public async Task RefreshAsyncPopulatesEntriesFromGitService()
        {
            var status = new GitStatus
            {
                CurrentBranch = "main",
                Entries = new List<FileStatusEntry>
                {
                    new FileStatusEntry { FilePath = "file1.txt", IndexStatus = FileStatus.Modified },
                    new FileStatusEntry { FilePath = "file2.txt", IndexStatus = FileStatus.Untracked, WorkTreeStatus = FileStatus.Untracked }
                }
            };
            var gitService = new FakeGitService(status);
            var viewModel = new StatusViewModel(gitService);

            await viewModel.RefreshAsync();

            Assert.Equal("main", viewModel.CurrentBranch);
            Assert.Single(viewModel.StagedEntries);
            Assert.Single(viewModel.UntrackedEntries);
            Assert.Equal("2 changed files", viewModel.StatusSummary);
        }

        [Fact]
        public async Task RefreshAsyncUsesConfiguredUiDispatcherForBoundUpdates()
        {
            var status = new GitStatus
            {
                CurrentBranch = "main",
                Entries = new List<FileStatusEntry>
                {
                    new FileStatusEntry { FilePath = "file1.txt", IndexStatus = FileStatus.Modified }
                }
            };
            var gitService = new FakeGitService(status);
            var dispatcher = new RecordingUiDispatcher();
            var viewModel = new StatusViewModel(gitService, dispatcher);

            await viewModel.RefreshAsync();

            Assert.True(dispatcher.InvokeCount > 0);
            Assert.Equal("main", viewModel.CurrentBranch);
            Assert.Single(viewModel.StagedEntries);
        }

        [Fact]
        public async Task StageSelectedAsyncCallsGitServiceAndRefreshes()
        {
            var status1 = new GitStatus
            {
                Entries = new List<FileStatusEntry>
                {
                    new FileStatusEntry { FilePath = "file1.txt", WorkTreeStatus = FileStatus.Modified }
                }
            };
            var status2 = new GitStatus { Entries = new List<FileStatusEntry>() };
            var gitService = new FakeGitService(status1, status2);
            var viewModel = new StatusViewModel(gitService);

            await viewModel.RefreshAsync();
            var entryToStage = viewModel.UnstagedEntries[0];
            await viewModel.StageSelectedAsync(entryToStage);

            Assert.Contains("file1.txt", gitService.StagedPaths);
            Assert.Empty(viewModel.UnstagedEntries);
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

            public Task<OperationResult> StageAllAsync(CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("All changes staged."));

            public Task<OperationResult> CommitAsync(string message, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Commit created."));

            public Task<OperationResult> FetchAsync(string? remote = null, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Fetch completed."));

            public Task<OperationResult> PullAsync(string? remote = null, string? branch = null, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Pull completed."));

            public Task<OperationResult> PushAsync(string? remote = null, string? branch = null, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Push completed."));

            public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<BranchInfo>>(new List<BranchInfo>());

            public Task<OperationResult> CreateBranchAsync(string name, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Branch created."));

            public Task<OperationResult> SwitchBranchAsync(string name, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Branch switched."));

            public Task<OperationResult> CheckoutBranchAsync(string branchName, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Branch checked out."));

            public Task<IReadOnlyList<CommitInfo>> GetCommitLogAsync(int maxCount, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<CommitInfo>>(new List<CommitInfo>());

            public Task<DiffResult> GetWorkingTreeDiffAsync(CancellationToken ct = default) =>
                Task.FromResult(new DiffResult());

            public Task<DiffResult> GetCommitDiffAsync(string commitHash, CancellationToken ct = default) =>
                Task.FromResult(new DiffResult());

            public Task<IReadOnlyList<string>> GetCommitFilesAsync(string commitHash, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<string>>(new List<string>());

            public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<RemoteInfo>>(new List<RemoteInfo>());
        }

        private sealed class RecordingUiDispatcher : IUiDispatcher
        {
            public int InvokeCount { get; private set; }

            public bool CheckAccess() => false;

            public void Invoke(Action action)
            {
                InvokeCount++;
                action();
            }
        }
    }
}
