using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Revision;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.Mapping;
using TiaGitAddIn.UI.ViewModels;
using TiaGitAddIn.UI.ViewModels.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public class DiffViewModelTests
    {
        [Fact]
        public async Task SelectEntryAsync_AfterLoadCommitDiff_RequestsParentAndCommitRevisionsForSelectedFile()
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
            var revisionProvider = new RecordingRevisionProvider();
            var viewModel = new DiffViewModel(
                gitService, revisionProvider, new StubComparisonCoordinator(), new StubMapper(),
                uiDispatcher: ImmediateUiDispatcher.Instance);
            const string commitHash = "1234567890abcdef1234567890abcdef12345678";

            await viewModel.LoadCommitDiffAsync(commitHash);
            await viewModel.SelectEntryAsync(viewModel.Entries.Single(), CancellationToken.None);

            Assert.Equal(PlcRevisionSourceKind.ParentOfCommit, revisionProvider.LeftSource!.Kind);
            Assert.Equal(commitHash, revisionProvider.LeftSource.CommitHash);
            Assert.Equal(PlcRevisionSourceKind.Commit, revisionProvider.RightSource!.Kind);
            Assert.Equal(commitHash, revisionProvider.RightSource.CommitHash);
            Assert.Equal("Project/Program blocks/Main.xml", revisionProvider.LastPath);
        }

        [Fact]
        public async Task SelectEntryAsync_AppliesMappedPresentationToCurrentPresentation()
        {
            var gitService = new FakeGitService
            {
                CommitDiff = new DiffResult
                {
                    Entries = new List<DiffEntry>
                    {
                        new DiffEntry { FilePath = "a.xml", ChangeType = "M", Hunks = new List<DiffHunk>() }
                    }
                }
            };
            var mapper = new StubMapper();
            var viewModel = new DiffViewModel(
                gitService, new RecordingRevisionProvider(), new StubComparisonCoordinator(), mapper,
                uiDispatcher: ImmediateUiDispatcher.Instance);

            await viewModel.LoadCommitDiffAsync("abc1234567890abcdef1234567890abcdef1234");
            Assert.Null(viewModel.CurrentPresentation);

            await viewModel.SelectEntryAsync(viewModel.Entries.Single(), CancellationToken.None);

            Assert.NotNull(viewModel.CurrentPresentation);
            Assert.Same(mapper.LastProduced, viewModel.CurrentPresentation);
        }

        [Fact]
        public void SelectedEntry_SetterOnlyStoresValueAndTriggersNoComparison()
        {
            var gitService = new FakeGitService();
            var viewModel = new DiffViewModel(
                gitService, new RecordingRevisionProvider(), new StubComparisonCoordinator(), new StubMapper(),
                uiDispatcher: ImmediateUiDispatcher.Instance);
            var entry = new DiffEntryViewModel(new DiffEntry { FilePath = "a.xml", ChangeType = "M", Hunks = new List<DiffHunk>() });

            viewModel.SelectedEntry = entry;

            Assert.Same(entry, viewModel.SelectedEntry);
            Assert.Null(viewModel.CurrentPresentation);
        }

        /// <summary>
        /// Scans the comparison-facing production surface this task rewired for stray <c>async void</c>
        /// methods. Every match must either be the one recognized WPF event-handler boundary
        /// (<c>DiffView.OnSelectionChanged(object, SelectionChangedEventArgs)</c>) or absent entirely from
        /// the files that must never contain one; every Task-returning comparison/loading method on those
        /// same files must declare both <c>Task</c> and <c>CancellationToken</c>.
        /// </summary>
        [Fact]
        public void AsyncVoidIsConfinedToWpfEventBoundaries()
        {
            string root = RepositoryRoot();

            string[] mustHaveZeroAsyncVoid =
            {
                Path.Combine(root, "src", "TiaGitAddIn", "UI", "ViewModels", "DiffViewModel.cs"),
                Path.Combine(root, "src", "TiaGitAddIn", "UI", "ViewModels", "MainViewModel.cs"),
                Path.Combine(root, "src", "TiaGitAddIn", "UI", "ViewModels", "Comparison", "ComparisonSelectionCoordinator.cs"),
            };

            foreach (string file in mustHaveZeroAsyncVoid)
            {
                string text = File.ReadAllText(file);
                Assert.DoesNotContain("async void", text, StringComparison.Ordinal);
            }

            string[] scannedDirectories =
            {
                Path.Combine(root, "src", "TiaGitAddIn.Core", "Services", "Revision"),
                Path.Combine(root, "src", "TiaGitAddIn.Core", "Services", "Comparison"),
                Path.Combine(root, "src", "TiaGitAddIn", "UI", "Mapping"),
            };

            var scannedFiles = new List<string>(mustHaveZeroAsyncVoid);
            foreach (string directory in scannedDirectories)
            {
                scannedFiles.AddRange(Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories));
            }

            string diffViewCodeBehind = Path.Combine(root, "src", "TiaGitAddIn", "UI", "Views", "DiffView.xaml.cs");
            scannedFiles.Add(diffViewCodeBehind);

            var asyncVoidPattern = new Regex(@"async\s+void\s+(\w+)\s*\(([^)]*)\)", RegexOptions.Compiled);
            var eventHandlerShape = new Regex(@"object\s+\w+\s*,\s*\w*EventArgs\s+\w+", RegexOptions.Compiled);

            int recognizedBoundaryCount = 0;

            foreach (string file in scannedFiles.Distinct())
            {
                string text = File.ReadAllText(file);
                foreach (Match match in asyncVoidPattern.Matches(text))
                {
                    string methodName = match.Groups[1].Value;
                    string parameters = match.Groups[2].Value;
                    bool isEventHandlerShape = eventHandlerShape.IsMatch(parameters);

                    bool isRecognizedDiffViewBoundary =
                        string.Equals(Path.GetFileName(file), "DiffView.xaml.cs", StringComparison.OrdinalIgnoreCase) &&
                        methodName == "OnSelectionChanged" &&
                        isEventHandlerShape;

                    if (isRecognizedDiffViewBoundary)
                    {
                        recognizedBoundaryCount++;
                        continue;
                    }

                    Assert.True(isEventHandlerShape,
                        $"'{methodName}' in '{file}' is 'async void' with parameters '{parameters}', " +
                        "which is not a recognized WPF event-handler boundary.");
                }
            }

            Assert.Equal(1, recognizedBoundaryCount);

            AssertDeclarationHasTaskAndCancellationToken(
                mustHaveZeroAsyncVoid[0], "SelectEntryAsync");
            AssertDeclarationHasTaskAndCancellationToken(
                mustHaveZeroAsyncVoid[2], "SelectAsync");
            AssertDeclarationHasTaskAndCancellationToken(
                Path.Combine(root, "src", "TiaGitAddIn.Core", "Services", "Revision", "IPlcRevisionProvider.cs"), "LoadAsync");
            AssertDeclarationHasTaskAndCancellationToken(
                Path.Combine(root, "src", "TiaGitAddIn.Core", "Services", "Comparison", "PlcComparisonCoordinator.cs"), "CompareAsync");
            AssertDeclarationHasTaskAndCancellationToken(
                Path.Combine(root, "src", "TiaGitAddIn.Core", "Services", "Comparison", "LadComparisonStrategy.cs"), "CompareAsync");
        }

        private static void AssertDeclarationHasTaskAndCancellationToken(string file, string methodName)
        {
            string text = File.ReadAllText(file);
            var declarationPattern = new Regex($@"[^\n;{{]*\b{Regex.Escape(methodName)}\s*\([^)]*\)", RegexOptions.Compiled);
            Match match = declarationPattern.Match(text);

            Assert.True(match.Success, $"Could not find a declaration of '{methodName}' in '{file}'.");
            Assert.Contains("Task", match.Value, StringComparison.Ordinal);
            Assert.Contains("CancellationToken", match.Value, StringComparison.Ordinal);
        }

        private static string RepositoryRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "TiaGitAddIn.sln")))
            {
                current = current.Parent;
            }

            return current?.FullName
                ?? throw new DirectoryNotFoundException("Repository root containing TiaGitAddIn.sln was not found.");
        }

        private sealed class RecordingRevisionProvider : IPlcRevisionProvider
        {
            public PlcRevisionSource? LeftSource { get; private set; }
            public PlcRevisionSource? RightSource { get; private set; }
            public string? LastPath { get; private set; }

            public Task<PlcRevisionLease> LoadAsync(
                PlcRevisionSide side, PlcRevisionSource source, string repositoryRelativePath, CancellationToken cancellationToken)
            {
                if (side == PlcRevisionSide.Left) LeftSource = source; else RightSource = source;
                LastPath = repositoryRelativePath;

                byte[] bytes = System.Text.Encoding.UTF8.GetBytes("content");
                var revision = PlcRevision.Present(
                    side, source, repositoryRelativePath, bytes, PlcTextEncoding.Utf8WithoutBom, "content", false, string.Empty);
                return Task.FromResult(PlcRevisionLease.Create(revision, TempRoot()));
            }

            public PlcRevisionLease Missing(
                PlcRevisionSide side, PlcRevisionSource source, string repositoryRelativePath, PlcRevisionMissingReason reason)
            {
                var revision = PlcRevision.Missing(side, source, repositoryRelativePath, reason);
                return PlcRevisionLease.Create(revision, TempRoot());
            }

            private static string TempRoot() =>
                Path.Combine(Path.GetTempPath(), "TiaGitAddInTests", Guid.NewGuid().ToString("N"));
        }

        private sealed class StubComparisonCoordinator : IPlcComparisonCoordinator
        {
            public Task<PlcComparisonResult> CompareAsync(PlcRevision left, PlcRevision right, CancellationToken cancellationToken)
            {
                var presentation = new UnsupportedPresentation();
                return Task.FromResult(new PlcComparisonResult(
                    PlcArtifactKind.Text, PlcComparisonMode.Unsupported, PlcComparisonMode.Unsupported,
                    PlcSupportLevel.Unsupported, "stub", Array.Empty<PlcComparisonDiagnostic>(), presentation, null));
            }

            public PlcComparisonResult CreateRevisionLoadError(
                PlcArtifactKind bestKnownKind, PlcComparisonMode requestedMode, Exception exception, PlcRevisionSide side)
            {
                return new PlcComparisonResult(
                    bestKnownKind, requestedMode, PlcComparisonMode.Unsupported, PlcSupportLevel.Unsupported,
                    "load error", Array.Empty<PlcComparisonDiagnostic>(), new ErrorPresentation(), null);
            }
        }

        private sealed class StubMapper : IComparisonPresentationMapper
        {
            public ComparisonPresentationViewModel? LastProduced { get; private set; }

            public ComparisonPresentationViewModel Map(PlcComparisonResult result)
            {
                ComparisonViewModelMetadata metadata = ComparisonViewModelMetadata.From(result);
                LastProduced = new UnsupportedComparisonViewModel(metadata);
                return LastProduced;
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
