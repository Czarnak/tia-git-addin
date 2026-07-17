using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Revision;
using TiaGitAddIn.UI.Mapping;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// Coordinates one comparison "selection" (a file entry becoming the current diff target) end to end:
    /// resolves the correct pair of <see cref="PlcRevisionSource"/>s, loads both sides through
    /// <see cref="IPlcRevisionProvider"/>, routes them through <see cref="IPlcComparisonCoordinator"/>, maps
    /// the result through <see cref="IComparisonPresentationMapper"/>, and applies the resulting view model
    /// on the captured <see cref="IUiDispatcher"/> -- but only for the newest selection still in flight.
    /// Every older, superseded selection has its own linked <see cref="CancellationTokenSource"/> canceled
    /// the instant a newer one starts: it either throws <see cref="OperationCanceledException"/> on one of
    /// its own awaited steps, or -- if it raced past those and reached the very end anyway -- is caught by
    /// the final generation/token recheck immediately before <c>apply</c> would otherwise run. Both revision
    /// leases for a selection are always disposed exactly once, in <c>finally</c>, regardless of whether
    /// that selection succeeded, failed, was replaced, or was cancelled on its own.
    /// </summary>
    public sealed class ComparisonSelectionCoordinator : IDisposable
    {
        private readonly IPlcRevisionProvider revisionProvider;
        private readonly IPlcComparisonCoordinator comparisonCoordinator;
        private readonly IComparisonPresentationMapper mapper;
        private readonly IUiDispatcher dispatcher;
        private readonly Action<ComparisonPresentationViewModel> apply;
        private readonly IAddInLogger logger;
        private readonly object gate = new();
        private readonly List<string> appliedErrorsForTests = new();

        private long generation;
        private CancellationTokenSource? activeCts;
        private bool disposed;

        public ComparisonSelectionCoordinator(
            IPlcRevisionProvider revisionProvider,
            IPlcComparisonCoordinator comparisonCoordinator,
            IComparisonPresentationMapper mapper,
            IUiDispatcher dispatcher,
            Action<ComparisonPresentationViewModel> apply,
            IAddInLogger logger)
        {
            this.revisionProvider = revisionProvider ?? throw new ArgumentNullException(nameof(revisionProvider));
            this.comparisonCoordinator = comparisonCoordinator ?? throw new ArgumentNullException(nameof(comparisonCoordinator));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            this.apply = apply ?? throw new ArgumentNullException(nameof(apply));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Diagnostic codes for every hard-error outcome actually applied, in application order. Test-only visibility.</summary>
        public IReadOnlyList<string> AppliedErrorsForTests
        {
            get { lock (gate) { return appliedErrorsForTests.ToArray(); } }
        }

        public Task SelectAsync(ComparisonSelection selection, CancellationToken cancellationToken)
        {
            if (selection == null) throw new ArgumentNullException(nameof(selection));

            long myGeneration = Interlocked.Increment(ref generation);
            CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            CancellationTokenSource? previous;
            lock (gate)
            {
                previous = activeCts;
                activeCts = linkedCts;
            }

            if (previous != null)
            {
                previous.Cancel();
                previous.Dispose();
            }

            return RunSelectionAsync(selection, myGeneration, linkedCts);
        }

        private async Task RunSelectionAsync(ComparisonSelection selection, long myGeneration, CancellationTokenSource linkedCts)
        {
            CancellationToken token = linkedCts.Token;
            PlcRevisionLease? leftLease = null;
            PlcRevisionLease? rightLease = null;

            try
            {
                (PlcRevisionSource leftSource, PlcRevisionSource rightSource) = ResolveSources(selection.CommitHash);

                PlcComparisonResult? loadErrorResult = null;
                try
                {
                    leftLease = selection.ChangeKind == PlcPairChangeKind.Added
                        ? revisionProvider.Missing(PlcRevisionSide.Left, leftSource, selection.RepositoryRelativePath, PlcRevisionMissingReason.Added)
                        : await revisionProvider.LoadAsync(PlcRevisionSide.Left, leftSource, selection.RepositoryRelativePath, token).ConfigureAwait(false);

                    rightLease = selection.ChangeKind == PlcPairChangeKind.Removed
                        ? revisionProvider.Missing(PlcRevisionSide.Right, rightSource, selection.RepositoryRelativePath, PlcRevisionMissingReason.Deleted)
                        : await revisionProvider.LoadAsync(PlcRevisionSide.Right, rightSource, selection.RepositoryRelativePath, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    PlcRevisionSide failedSide = leftLease == null ? PlcRevisionSide.Left : PlcRevisionSide.Right;
                    loadErrorResult = comparisonCoordinator.CreateRevisionLoadError(
                        PlcArtifactKind.Unknown, PlcComparisonMode.Unsupported, ex, failedSide);
                }

                PlcComparisonResult result = loadErrorResult
                    ?? await comparisonCoordinator.CompareAsync(leftLease!.Revision, rightLease!.Revision, token).ConfigureAwait(false);

                ComparisonPresentationViewModel viewModel = mapper.Map(result);
                bool isErrorOutcome = loadErrorResult != null;

                dispatcher.Invoke(() =>
                {
                    token.ThrowIfCancellationRequested();
                    if (Interlocked.Read(ref generation) != myGeneration)
                    {
                        return;
                    }

                    if (isErrorOutcome)
                    {
                        lock (gate)
                        {
                            appliedErrorsForTests.Add(string.Join(",", result.Diagnostics.Select(d => d.Code)));
                        }
                    }

                    apply(viewModel);
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error($"Comparison selection for '{selection.RepositoryRelativePath}' failed unexpectedly.", ex);
                throw;
            }
            finally
            {
                DisposeLease(rightLease);
                DisposeLease(leftLease);

                // Only clear activeCts if it still points at this selection's own CTS: a newer selection
                // may already have replaced it (and taken over responsibility for canceling/disposing this
                // one). Without this, a selection that completes on its own (never superseded) would leave
                // its already-disposed CTS referenced by activeCts, and the next unrelated SelectAsync call
                // would throw ObjectDisposedException trying to cancel/dispose it.
                lock (gate)
                {
                    if (ReferenceEquals(activeCts, linkedCts))
                    {
                        activeCts = null;
                    }
                }

                linkedCts.Dispose();
            }
        }

        private void DisposeLease(PlcRevisionLease? lease)
        {
            if (lease == null) return;

            try
            {
                lease.Dispose();
            }
            catch (RevisionCleanupException ex)
            {
                logger.Error("Failed to dispose a comparison revision lease.", ex);
            }
        }

        private static (PlcRevisionSource Left, PlcRevisionSource Right) ResolveSources(string? commitHash)
        {
            if (string.IsNullOrEmpty(commitHash))
            {
                return (PlcRevisionSource.Head, PlcRevisionSource.WorkingTree);
            }

            return (PlcRevisionSource.ParentOfCommit(commitHash!), PlcRevisionSource.Commit(commitHash!));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            CancellationTokenSource? current;
            lock (gate)
            {
                current = activeCts;
                activeCts = null;
            }

            current?.Cancel();
            current?.Dispose();
        }
    }
}
