using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.ViewModels;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public class DiffViewModelTests
    {
        [Theory]
        [InlineData("Project/Program blocks/Main.xml", true)]
        [InlineData("Project/Program blocks/SubFolder/Block.xml", true)]
        [InlineData("Project/Data blocks/GlobalDB.xml", true)]
        [InlineData("Project/LAD/Network1.xml", true)]
        [InlineData("Project/SomeFolder/NotAnArtifact.txt", false)]
        [InlineData("Project/SomeFolder/Generic.xml", true)] // Fixed: all .xml are now detected
        [InlineData("Project/PLC_1/Program blocks/MyBlock.xml", true)]
        public void DetectTiaArtifact_HeuristicCheck(string path, bool expected)
        {
            // Arrange
            var entry = new DiffEntry 
            { 
                FilePath = path,
                Hunks = new List<DiffHunk>()
            };

            // Act
            var vm = new DiffEntryViewModel(entry);

            // Assert
            Assert.Equal(expected, vm.IsTiaArtifact);
        }

        [Fact]
        public async Task LoadCommitDiffAsync_VisualDiffUsesFullSelectedCommitHash()
        {
            var gitService = new FakeGitService
            {
                CommitDiff = new DiffResult
                {
                    Entries = new List<DiffEntry>
                    {
                        new DiffEntry
                        {
                            FilePath = "Project/Program blocks/Main.xml",
                            ChangeType = "M",
                            Hunks = new List<DiffHunk>()
                        }
                    }
                }
            };
            var extractor = new RecordingGitFileExtractor();
            var sactService = new FakeSactService
            {
                ResultToReturn = new SactCompareResult
                {
                    State = CompareState.Changed,
                    Content = new SactContentResult
                    {
                        Networks = new Dictionary<string, SactNetworkResult>
                        {
                            { "0", new SactNetworkResult { State = CompareState.Changed, Number = new SactNumberPair { Left = 1, Right = 1 } } }
                        }
                    }
                }
            };
            var viewModel = new DiffViewModel(gitService, extractor, sactService, uiDispatcher: ImmediateUiDispatcher.Instance);
            const string commitHash = "1234567890abcdef1234567890abcdef12345678";

            await viewModel.LoadCommitDiffAsync(commitHash);

            Assert.Equal(commitHash, extractor.CommitHashSeenByExtractFile);
            Assert.Equal(commitHash, extractor.CommitHashSeenByExtractParentFile);
        }

        private sealed class RecordingGitFileExtractor : IGitFileExtractor
        {
            public string? CommitHashSeenByExtractFile { get; private set; }
            public string? CommitHashSeenByExtractParentFile { get; private set; }

            public Task<string> ExtractFileAsync(string? commitHash, string filePath, CancellationToken ct)
            {
                CommitHashSeenByExtractFile = commitHash;
                return Task.FromResult("right.xml");
            }

            public Task<string?> ExtractParentFileAsync(string? commitHash, string filePath, CancellationToken ct)
            {
                CommitHashSeenByExtractParentFile = commitHash;
                return Task.FromResult<string?>("left.xml");
            }
        }

        private sealed class FakeSactService : ISactService
        {
            public bool IsAvailable => true;
            public SactCompareResult? ResultToReturn { get; set; }

            public Task<SactCompareResult?> CompareAsync(string? leftXmlPath, string? rightXmlPath, CancellationToken ct)
            {
                return Task.FromResult(ResultToReturn);
            }
        }

        private sealed class FakeGitService : IGitService
        {
            public DiffResult CommitDiff { get; set; } = new DiffResult();

            public Task<GitStatus> GetStatusAsync(CancellationToken ct = default) => Task.FromResult(new GitStatus());
            public Task<OperationResult> StageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<OperationResult> UnstageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<OperationResult> StageAllAsync(CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<OperationResult> CommitAsync(string message, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<OperationResult> FetchAsync(string? remote = null, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<OperationResult> PullAsync(string? remote = null, string? branch = null, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<OperationResult> PushAsync(string? remote = null, string? branch = null, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<BranchInfo>>(new List<BranchInfo>());
            public Task<OperationResult> CreateBranchAsync(string name, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<OperationResult> SwitchBranchAsync(string name, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<OperationResult> CheckoutBranchAsync(string branchName, CancellationToken ct = default) => Task.FromResult(OperationResult.Ok());
            public Task<IReadOnlyList<CommitInfo>> GetCommitLogAsync(int maxCount, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<CommitInfo>>(new List<CommitInfo>());
            public Task<DiffResult> GetWorkingTreeDiffAsync(CancellationToken ct = default) => Task.FromResult(new DiffResult());
            public Task<DiffResult> GetCommitDiffAsync(string commitHash, CancellationToken ct = default) => Task.FromResult(CommitDiff);
            public Task<IReadOnlyList<string>> GetCommitFilesAsync(string commitHash, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(new List<string>());
            public Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<RemoteInfo>>(new List<RemoteInfo>());
        }
    }
}
