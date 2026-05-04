using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI.ViewModels;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class CommitViewModelTests
    {
        [Fact]
        public async Task CommitAsyncRejectsWhitespaceMessage()
        {
            FakeGitService gitService = new FakeGitService();
            CommitViewModel viewModel = new CommitViewModel(gitService, () => Task.CompletedTask)
            {
                CommitMessage = "   "
            };

            await viewModel.CommitAsync();

            Assert.False(viewModel.CanCommit);
            Assert.Equal(0, gitService.CommitCalls);
            Assert.Equal("Commit message is required.", viewModel.ValidationMessage);
        }

        [Fact]
        public async Task CommitAsyncCommitsMessageClearsEditorAndRefreshes()
        {
            FakeGitService gitService = new FakeGitService();
            int refreshCount = 0;
            CommitViewModel viewModel = new CommitViewModel(
                gitService,
                () =>
                {
                    refreshCount++;
                    return Task.CompletedTask;
                })
            {
                CommitMessage = "Export updated PLC blocks"
            };

            await viewModel.CommitAsync();

            Assert.Equal("Export updated PLC blocks", gitService.LastCommitMessage);
            Assert.Equal(string.Empty, viewModel.CommitMessage);
            Assert.Equal("Commit created.", viewModel.LastOperationMessage);
            Assert.Equal(1, refreshCount);
        }

        private sealed class FakeGitService : IGitService
        {
            public int CommitCalls { get; private set; }

            public string LastCommitMessage { get; private set; } = string.Empty;

            public Task<OperationResult> CommitAsync(string message, CancellationToken ct = default)
            {
                CommitCalls++;
                LastCommitMessage = message;
                return Task.FromResult(OperationResult.Ok("Commit created."));
            }

            public Task<GitStatus> GetStatusAsync(CancellationToken ct = default) =>
                Task.FromResult(new GitStatus());

            public Task<OperationResult> StageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("File staged."));

            public Task<OperationResult> UnstageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("File unstaged."));

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
