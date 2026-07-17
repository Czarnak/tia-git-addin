using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Revision;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.Mapping;
using TiaGitAddIn.UI.ViewModels.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class ComparisonSelectionCoordinatorTests
    {
        private static readonly ComparisonPresentationViewModel ExistingViewModel = BuildPlaceholderViewModel();

        [Fact]
        public async Task NewerSelectionIsTheOnlyResultApplied()
        {
            var provider = new ControllableRevisionProvider();
            var applied = new List<string>();
            var sut = CreateSelectionCoordinator(provider, vm => applied.Add(vm.Metadata.RawText!.RightText!));

            Task first = sut.SelectAsync(new ComparisonSelection("A.xml", null, PlcPairChangeKind.Modified), CancellationToken.None);
            Task second = sut.SelectAsync(new ComparisonSelection("B.xml", null, PlcPairChangeKind.Modified), CancellationToken.None);

            provider.Complete("B.xml", "B");
            await second;

            provider.CompleteIgnoringCancellation("A.xml", "A");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

            Assert.Equal(new[] { "B" }, applied);
        }

        [Fact]
        public async Task StandaloneCancellationKeepsCurrentResultAndDisposesBothLeasesOnce()
        {
            var provider = new ControllableRevisionProvider();
            var applied = new List<ComparisonPresentationViewModel> { ExistingViewModel };
            var sut = CreateSelectionCoordinator(provider, vm => { applied.Clear(); applied.Add(vm); });

            using var cts = new CancellationTokenSource();
            Task pending = sut.SelectAsync(new ComparisonSelection("C.xml", null, PlcPairChangeKind.Modified), cts.Token);

            provider.ReleaseLeases("C.xml");
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            Assert.Same(ExistingViewModel, Assert.Single(applied));
            Assert.All(provider.LeasesFor("C.xml"), lease => Assert.Equal(1, lease.DisposeCountForTests));
            Assert.Empty(sut.AppliedErrorsForTests);
        }

        [Fact]
        public async Task RevisionLoadFailureAppliesAnErrorPresentationForTheCurrentGeneration()
        {
            var provider = new ThrowingRevisionProvider();
            ComparisonPresentationViewModel? applied = null;
            var sut = CreateSelectionCoordinator(provider, vm => applied = vm);

            await sut.SelectAsync(new ComparisonSelection("Broken.xml", null, PlcPairChangeKind.Modified), CancellationToken.None);

            Assert.NotNull(applied);
            Assert.Equal(ComparisonPresentationKind.Error, applied!.Kind);
            Assert.Single(sut.AppliedErrorsForTests);
        }

        [Fact]
        public async Task AddedSelectionUsesAMissingLeftLeaseAndRemovedUsesAMissingRightLease()
        {
            var provider = new ControllableRevisionProvider();
            ComparisonPresentationViewModel? applied = null;
            var sut = CreateSelectionCoordinator(provider, vm => applied = vm);

            provider.ReleaseLeases("Added.xml");
            await sut.SelectAsync(new ComparisonSelection("Added.xml", null, PlcPairChangeKind.Added), CancellationToken.None);
            Assert.NotNull(applied);

            provider.ReleaseLeases("Removed.xml");
            await sut.SelectAsync(new ComparisonSelection("Removed.xml", null, PlcPairChangeKind.Removed), CancellationToken.None);
            Assert.NotNull(applied);

            Assert.True(provider.LeasesFor("Added.xml").Single(l => l.Revision.Side == PlcRevisionSide.Left).Revision.IsMissing);
            Assert.False(provider.LeasesFor("Added.xml").Single(l => l.Revision.Side == PlcRevisionSide.Right).Revision.IsMissing);
            Assert.False(provider.LeasesFor("Removed.xml").Single(l => l.Revision.Side == PlcRevisionSide.Left).Revision.IsMissing);
            Assert.True(provider.LeasesFor("Removed.xml").Single(l => l.Revision.Side == PlcRevisionSide.Right).Revision.IsMissing);
        }

        private static ComparisonSelectionCoordinator CreateSelectionCoordinator(
            IPlcRevisionProvider provider, Action<ComparisonPresentationViewModel> apply)
        {
            var classifier = new PlcArtifactClassifier();
            var textComparer = new LineTextComparer(TextComparisonLimits.Default);
            var resultFactory = new PlcComparisonResultFactory(textComparer);
            var sanitizer = new ComparisonDiagnosticSanitizer();
            var strategies = new IPlcComparisonStrategy[] { new TextFallbackStrategy(resultFactory) };
            var comparisonCoordinator = new PlcComparisonCoordinator(classifier, strategies, resultFactory, sanitizer);
            var mapper = new ComparisonPresentationMapper(new IComparisonPresentationViewModelFactory[]
            {
                new TextPresentationViewModelFactory(),
                new UnsupportedPresentationViewModelFactory(),
                new ErrorPresentationViewModelFactory(),
            });

            return new ComparisonSelectionCoordinator(
                provider, comparisonCoordinator, mapper, ImmediateUiDispatcher.Instance, apply, new FileLogger());
        }

        private static ComparisonPresentationViewModel BuildPlaceholderViewModel()
        {
            var result = new PlcComparisonResult(
                PlcArtifactKind.Unknown, PlcComparisonMode.Unsupported, PlcComparisonMode.Unsupported,
                PlcSupportLevel.Unsupported, "placeholder", Array.Empty<PlcComparisonDiagnostic>(),
                new UnsupportedPresentation(), null);
            return new UnsupportedComparisonViewModel(ComparisonViewModelMetadata.From(result));
        }

        private sealed class ThrowingRevisionProvider : IPlcRevisionProvider
        {
            public Task<PlcRevisionLease> LoadAsync(
                PlcRevisionSide side, PlcRevisionSource source, string repositoryRelativePath, CancellationToken cancellationToken)
                => throw new RevisionLoadException("Simulated revision load failure.");

            public PlcRevisionLease Missing(
                PlcRevisionSide side, PlcRevisionSource source, string repositoryRelativePath, PlcRevisionMissingReason reason)
                => throw new InvalidOperationException("Not expected to be called in this test.");
        }

        /// <summary>
        /// Caller-gated fake for <see cref="IPlcRevisionProvider"/>: <see cref="LoadAsync"/> for a given
        /// path blocks on a per-path <see cref="TaskCompletionSource{TResult}"/> until <see cref="Complete"/>,
        /// <see cref="CompleteIgnoringCancellation"/>, or <see cref="ReleaseLeases"/> is called for that same
        /// path, letting tests deterministically control which selection resolves first regardless of
        /// cancellation state. Tracks every lease it produces per path so tests can assert on disposal.
        /// </summary>
        private sealed class ControllableRevisionProvider : IPlcRevisionProvider
        {
            private readonly object gate = new();
            private readonly Dictionary<string, PathState> states = new();
            private readonly string temporaryRoot = Path.Combine(
                Path.GetTempPath(), "TiaGitAddInTests", "ComparisonSelectionCoordinator", Guid.NewGuid().ToString("N"));

            public async Task<PlcRevisionLease> LoadAsync(
                PlcRevisionSide side, PlcRevisionSource source, string repositoryRelativePath, CancellationToken cancellationToken)
            {
                PathState state = GetState(repositoryRelativePath);
                string text = await state.Gate.Task.ConfigureAwait(false);

                byte[] bytes = Encoding.UTF8.GetBytes(text);
                var revision = PlcRevision.Present(
                    side, source, repositoryRelativePath, bytes, PlcTextEncoding.Utf8WithoutBom, text, false, string.Empty);
                PlcRevisionLease lease = PlcRevisionLease.Create(revision, temporaryRoot);

                lock (state.Leases) state.Leases.Add(lease);
                return lease;
            }

            public PlcRevisionLease Missing(
                PlcRevisionSide side, PlcRevisionSource source, string repositoryRelativePath, PlcRevisionMissingReason reason)
            {
                PathState state = GetState(repositoryRelativePath);
                var revision = PlcRevision.Missing(side, source, repositoryRelativePath, reason);
                PlcRevisionLease lease = PlcRevisionLease.Create(revision, temporaryRoot);

                lock (state.Leases) state.Leases.Add(lease);
                return lease;
            }

            public void Complete(string path, string text) => GetState(path).Gate.TrySetResult(text);

            public void CompleteIgnoringCancellation(string path, string text) => GetState(path).Gate.TrySetResult(text);

            public void ReleaseLeases(string path) => GetState(path).Gate.TrySetResult("released-content");

            public IReadOnlyList<PlcRevisionLease> LeasesFor(string path)
            {
                PathState state = GetState(path);
                lock (state.Leases) return state.Leases.ToArray();
            }

            private PathState GetState(string path)
            {
                lock (gate)
                {
                    if (!states.TryGetValue(path, out PathState? state))
                    {
                        state = new PathState();
                        states[path] = state;
                    }

                    return state;
                }
            }

            private sealed class PathState
            {
                public TaskCompletionSource<string> Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
                public List<PlcRevisionLease> Leases { get; } = new();
            }
        }
    }
}
