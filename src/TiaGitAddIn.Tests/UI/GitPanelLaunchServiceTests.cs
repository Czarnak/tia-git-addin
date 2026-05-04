using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.ViewModels;
using TiaGitAddIn.UI.Views;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class GitPanelLaunchServiceTests
    {
        [Fact]
        public void CreateViewModelFailsWhenWorkspaceCannotBeResolved()
        {
            GitPanelLaunchService service = new GitPanelLaunchService(
                new VciWorkspaceLocator(),
                new RepositoryDiscovery(),
                new ConfigurationService(),
                (_, __) => new FakeGitService(),
                new NullLogger());

            GitPanelLaunchResult result = service.CreateViewModel(new object());

            Assert.False(result.Success);
            Assert.Null(result.ViewModel);
            Assert.Equal("Unable to resolve a VCI workspace path from the selected TIA item.", result.Message);
        }

        [Fact]
        public void CreateViewModelBuildsPanelForDiscoveredRepository()
        {
            string repositoryRoot = CreateRepositoryRoot();
            WorkspaceContext context = new WorkspaceContext(repositoryRoot);
            string? gitPath = null;
            string? workingDirectory = null;
            GitPanelLaunchService service = new GitPanelLaunchService(
                new VciWorkspaceLocator(),
                new RepositoryDiscovery(),
                new ConfigurationService(),
                (configuredGitPath, configuredWorkingDirectory) =>
                {
                    gitPath = configuredGitPath;
                    workingDirectory = configuredWorkingDirectory;
                    return new FakeGitService();
                },
                new NullLogger());

            GitPanelLaunchResult result = service.CreateViewModel(context);

            Assert.True(result.Success);
            Assert.NotNull(result.ViewModel);
            Assert.Equal(repositoryRoot, result.ViewModel!.RepositoryPath);
            Assert.Equal("git", gitPath);
            Assert.Equal(repositoryRoot, workingDirectory);
        }

        [Fact]
        public void CreateViewModelReturnsFailureWhenDependencyThrows()
        {
            GitPanelLaunchService service = new GitPanelLaunchService(
                new ThrowingWorkspaceLocator(),
                new RepositoryDiscovery(),
                new ConfigurationService(),
                (_, __) => new FakeGitService(),
                new NullLogger());

            GitPanelLaunchResult result = service.CreateViewModel(new object());

            Assert.False(result.Success);
            Assert.Null(result.ViewModel);
            Assert.Contains("Unable to open Git panel.", result.Message);
            Assert.Contains("workspace lookup failed", result.Message);
        }

        private static string CreateRepositoryRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "tia-git-addin-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, ".git"));
            return root;
        }

        private sealed class WorkspaceContext
        {
            public WorkspaceContext(string workspacePath)
            {
                WorkspacePath = workspacePath;
            }

            public string WorkspacePath { get; }
        }

        private sealed class ThrowingWorkspaceLocator : IVciWorkspaceLocator
        {
            public string? TryGetWorkspacePath(object projectContext)
            {
                throw new InvalidOperationException("workspace lookup failed");
            }
        }

        private sealed class NullLogger : IAddInLogger
        {
            public void Error(string message, Exception exception)
            {
            }

            public void Info(string message)
            {
            }
        }

        private sealed class FakeGitService : IGitService
        {
            public Task<GitStatus> GetStatusAsync(CancellationToken ct = default) =>
                Task.FromResult(new GitStatus());

            public Task<OperationResult> StageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default) =>
                Task.FromResult(OperationResult.Ok("File staged."));

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
