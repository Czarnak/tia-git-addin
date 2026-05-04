using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.ViewModels;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class GitPanelLaunchServiceTests
    {
        [Fact]
        public void CreateViewModelBuildsPanelForDiscoveredRepository()
        {
            var gitService = new FakeGitService();
            var configService = new FakeConfigurationService();
            var locator = new FakeWorkspaceLocator();
            var discovery = new FakeRepositoryDiscovery();
            var logger = new FakeLogger();
            
            var service = new GitPanelLaunchService(
                locator,
                discovery,
                configService,
                (p1, p2) => gitService,
                logger);

            GitPanelLaunchResult result = service.CreateViewModel(new object());

            Assert.True(result.Success);
            Assert.NotNull(result.CreateViewModel);
            MainViewModel viewModel = result.CreateViewModel();
            Assert.Equal("C:\\repo", viewModel.RepositoryPath);
        }

        private sealed class FakeWorkspaceLocator : IVciWorkspaceLocator
        {
            public string? TryGetWorkspacePath(object context) => "C:\\repo\\vci";
            public bool IsVciWorkspaceValid(string path) => true;
        }

        private sealed class FakeRepositoryDiscovery : IRepositoryDiscovery
        {
            public string? FindRepositoryRoot(string startPath) => "C:\\repo";
            public bool IsGitRepository(string path) => true;
            public Task<OperationResult> InitializeRepositoryAsync(string path, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Init"));
        }

        private sealed class FakeConfigurationService : IConfigurationService
        {
            public GitConfiguration Load(string workspacePath) => new GitConfiguration { RepositoryPath = workspacePath };
            public void Save(string workspacePath, GitConfiguration config) { }
            public string GetConfigFilePath(string workspacePath) => string.Empty;
        }

        private sealed class FakeLogger : IAddInLogger
        {
            public void Info(string message) { }
            public void Warn(string message) { }
            public void Error(string message, Exception? ex = null) { }
            public void Debug(string message) { }
        }

        private sealed class FakeGitService : IGitService
        {
            public Task<GitStatus> GetStatusAsync(CancellationToken ct = default) =>
                Task.FromResult(new GitStatus());

            public Task<OperationResult> StageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("File staged."));

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

            public Task<OperationResult> InitAsync(string path, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("Repository initialized."));

            public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<RemoteInfo>>(new List<RemoteInfo>());
        }
    }
}
