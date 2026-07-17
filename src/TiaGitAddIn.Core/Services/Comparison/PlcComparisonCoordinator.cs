using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Revision;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// The sole strategy-routing seam: given two already-loaded <see cref="PlcRevision"/> objects, it
    /// classifies the pair, selects exactly one compatible <see cref="IPlcComparisonStrategy"/>, and
    /// guarantees that every non-cancelled call returns an invariant-valid <see cref="PlcComparisonResult"/>.
    /// </summary>
    public interface IPlcComparisonCoordinator
    {
        Task<PlcComparisonResult> CompareAsync(
            PlcRevision left,
            PlcRevision right,
            CancellationToken cancellationToken);

        /// <summary>
        /// Builds a hard-error result for a revision that failed to load, before any
        /// <see cref="PlcComparisonContext"/> could be built. Never exposes raw text.
        /// </summary>
        PlcComparisonResult CreateRevisionLoadError(
            PlcArtifactKind bestKnownKind,
            PlcComparisonMode requestedMode,
            Exception exception,
            PlcRevisionSide side);
    }

    /// <summary>
    /// Thrown by an <see cref="IPlcComparisonStrategy"/> when it cannot complete a semantic comparison but
    /// the situation is not a hard failure. The coordinator downgrades this to a text-only fallback result
    /// whenever raw text is available for the pair, and otherwise lets it fall through to the generic hard-
    /// error boundary. Carries one already-safe (sanitized) diagnostic and a non-blank user-facing
    /// limitation; unlike a raw <see cref="Exception.Message"/>, neither value is sanitized again by the
    /// coordinator, so callers must only pass already-redacted content here.
    /// </summary>
    public sealed class RecoverableComparisonException : Exception
    {
        public RecoverableComparisonException(string limitation, PlcComparisonDiagnostic diagnostic)
            : base(diagnostic?.Message ?? limitation)
        {
            if (string.IsNullOrWhiteSpace(limitation))
            {
                throw new ArgumentException("A non-blank limitation is required.", nameof(limitation));
            }

            Limitation = limitation;
            Diagnostic = diagnostic ?? throw new ArgumentNullException(nameof(diagnostic));
        }

        public string Limitation { get; }
        public PlcComparisonDiagnostic Diagnostic { get; }
    }

    /// <summary>
    /// Routing precedence is exact and the first matching rule wins: cancellation escapes unmodified
    /// before anything else runs; a pair classified <see cref="PlcArtifactKind.Binary"/> always routes to
    /// an explicit unsupported result; among the strategies registered for the pair's resolved kind, zero
    /// registrations with decoded raw text produce a factory text fallback (<c>CMP-ROUTE-FALLBACK</c>),
    /// zero registrations without raw text produce an explicit unsupported result
    /// (<c>CMP-ROUTE-UNSUPPORTED</c>), exactly one registration is invoked, and more than one is a
    /// configuration error surfaced as a hard result (<c>CMP-ROUTE-DUPLICATE</c>). A strategy result whose
    /// artifact kind/requested mode disagrees with the resolved pair becomes a hard error
    /// (<c>CMP-RESULT-INVARIANT</c>) rather than being trusted as-is. This type never inspects a revision's
    /// original path/suffix directly -- classification (and therefore routing) comes only from
    /// <see cref="IPlcArtifactClassifier"/>.
    /// </summary>
    public sealed class PlcComparisonCoordinator : IPlcComparisonCoordinator
    {
        private readonly IPlcArtifactClassifier _classifier;
        private readonly PlcComparisonResultFactory _resultFactory;
        private readonly ComparisonDiagnosticSanitizer _sanitizer;
        private readonly IReadOnlyDictionary<PlcArtifactKind, IReadOnlyList<IPlcComparisonStrategy>> _registrations;

        public PlcComparisonCoordinator(
            IPlcArtifactClassifier classifier,
            IEnumerable<IPlcComparisonStrategy> strategies,
            PlcComparisonResultFactory resultFactory,
            ComparisonDiagnosticSanitizer sanitizer)
        {
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _resultFactory = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
            _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
            _registrations = BuildRegistrations(strategies ?? throw new ArgumentNullException(nameof(strategies)));
        }

        public async Task<PlcComparisonResult> CompareAsync(
            PlcRevision left,
            PlcRevision right,
            CancellationToken cancellationToken)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));
            cancellationToken.ThrowIfCancellationRequested();

            PlcArtifactPairDescriptor pair = _classifier.Resolve(left, right);
            var context = new PlcComparisonContext(new PlcComparisonRequest(left, right, pair), BuildRawText(left, right));

            if (pair.ArtifactKind == PlcArtifactKind.Binary)
            {
                string limitation = WithFallback(pair.Limitation, "Binary content cannot be compared; only the file-level change is shown.");
                return _resultFactory.CreateUnsupported(context, limitation, pair.Diagnostics);
            }

            IReadOnlyList<IPlcComparisonStrategy> candidates = _registrations.TryGetValue(
                pair.ArtifactKind, out IReadOnlyList<IPlcComparisonStrategy>? found)
                ? found
                : Array.Empty<IPlcComparisonStrategy>();

            if (candidates.Count == 0) return RouteWithNoStrategy(context, pair);
            if (candidates.Count > 1) return CreateDuplicateRouteError(pair);

            return await InvokeStrategyAsync(candidates[0], context, pair, cancellationToken).ConfigureAwait(false);
        }

        public PlcComparisonResult CreateRevisionLoadError(
            PlcArtifactKind bestKnownKind,
            PlcComparisonMode requestedMode,
            Exception exception,
            PlcRevisionSide side)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            bool isSizeLimit = exception is RevisionSizeLimitException;
            string code = isSizeLimit ? "CMP-REVISION-LIMIT" : "CMP-REVISION-LOAD";
            string limitation = isSizeLimit
                ? "The revision exceeds the configured size limit and could not be loaded."
                : "The revision could not be loaded.";

            PlcComparisonDiagnostic diagnostic = _sanitizer.ForUser(
                code, PlcDiagnosticSeverity.Error, exception.Message, new PlcSourceLocation(side));
            return _resultFactory.CreateHardError(bestKnownKind, requestedMode, limitation, new[] { diagnostic });
        }

        private PlcComparisonResult RouteWithNoStrategy(PlcComparisonContext context, PlcArtifactPairDescriptor pair)
        {
            if (context.RawText != null)
            {
                PlcComparisonDiagnostic diagnostic = _sanitizer.ForUser("CMP-ROUTE-FALLBACK", PlcDiagnosticSeverity.Warning,
                    $"No {pair.ArtifactKind} comparison strategy is registered; showing a text comparison instead.");
                string limitation = WithFallback(pair.Limitation, $"{pair.ArtifactKind} semantic comparison is unavailable.");
                return _resultFactory.CreateTextFallback(context, limitation, Append(pair.Diagnostics, diagnostic));
            }

            PlcComparisonDiagnostic unsupportedDiagnostic = _sanitizer.ForUser("CMP-ROUTE-UNSUPPORTED", PlcDiagnosticSeverity.Warning,
                $"No {pair.ArtifactKind} comparison strategy is registered and no text could be decoded for a fallback.");
            string unsupportedLimitation = WithFallback(pair.Limitation, $"{pair.ArtifactKind} comparison is unsupported.");
            return _resultFactory.CreateUnsupported(context, unsupportedLimitation, Append(pair.Diagnostics, unsupportedDiagnostic));
        }

        private PlcComparisonResult CreateDuplicateRouteError(PlcArtifactPairDescriptor pair)
        {
            PlcComparisonDiagnostic diagnostic = _sanitizer.ForUser("CMP-ROUTE-DUPLICATE", PlcDiagnosticSeverity.Error,
                $"More than one comparison strategy is registered for {pair.ArtifactKind}; routing is ambiguous.");
            return _resultFactory.CreateHardError(pair.ArtifactKind, pair.RequestedMode,
                "Multiple comparison strategies are registered for this artifact; the comparison cannot be routed.",
                new[] { diagnostic });
        }

        private async Task<PlcComparisonResult> InvokeStrategyAsync(IPlcComparisonStrategy strategy,
            PlcComparisonContext context, PlcArtifactPairDescriptor pair, CancellationToken cancellationToken)
        {
            PlcComparisonResult result;
            try
            {
                result = await strategy.CompareAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RecoverableComparisonException ex) when (context.RawText != null)
            {
                return _resultFactory.CreateTextFallback(context, ex.Limitation, Append(pair.Diagnostics, ex.Diagnostic));
            }
            catch (FormatException ex) when (context.RawText != null)
            {
                PlcComparisonDiagnostic diagnostic = _sanitizer.ForUser("CMP-STRATEGY-RECOVERED", PlcDiagnosticSeverity.Warning, ex.Message);
                string limitation = WithFallback(pair.Limitation, $"{pair.ArtifactKind} could not be parsed; showing a text comparison instead.");
                return _resultFactory.CreateTextFallback(context, limitation, Append(pair.Diagnostics, diagnostic));
            }
            catch (Exception ex)
            {
                PlcComparisonDiagnostic diagnostic = _sanitizer.ForUser("CMP-STRATEGY-ERROR", PlcDiagnosticSeverity.Error, ex.Message);
                return _resultFactory.CreateHardError(pair.ArtifactKind, pair.RequestedMode,
                    "The comparison strategy failed unexpectedly.", new[] { diagnostic });
            }

            return ValidateResultInvariant(result, pair);
        }

        private PlcComparisonResult ValidateResultInvariant(PlcComparisonResult result, PlcArtifactPairDescriptor pair)
        {
            if (result.ArtifactKind == pair.ArtifactKind && result.RequestedMode == pair.RequestedMode)
            {
                return result;
            }

            PlcComparisonDiagnostic diagnostic = _sanitizer.ForUser("CMP-RESULT-INVARIANT", PlcDiagnosticSeverity.Error,
                $"Strategy returned {result.ArtifactKind}/{result.RequestedMode} for a {pair.ArtifactKind}/{pair.RequestedMode} comparison.");
            return _resultFactory.CreateHardError(pair.ArtifactKind, pair.RequestedMode,
                "The comparison strategy returned a result for a different artifact than requested.",
                new[] { diagnostic });
        }

        private static ComparisonRawText? BuildRawText(PlcRevision left, PlcRevision right)
        {
            bool leftDecoded = left.IsMissing || left.Text != null;
            bool rightDecoded = right.IsMissing || right.Text != null;
            return leftDecoded && rightDecoded
                ? new ComparisonRawText(left.Text, right.Text, left.IsMissing, right.IsMissing)
                : null;
        }

        private static string WithFallback(string limitation, string defaultLimitation)
            => string.IsNullOrEmpty(limitation) ? defaultLimitation : limitation;

        private static IReadOnlyList<PlcComparisonDiagnostic> Append(
            IReadOnlyList<PlcComparisonDiagnostic> existing, PlcComparisonDiagnostic extra)
        {
            var combined = new List<PlcComparisonDiagnostic>(existing.Count + 1);
            combined.AddRange(existing);
            combined.Add(extra);
            return combined;
        }

        private static IReadOnlyDictionary<PlcArtifactKind, IReadOnlyList<IPlcComparisonStrategy>> BuildRegistrations(
            IEnumerable<IPlcComparisonStrategy> strategies)
        {
            var registrations = new Dictionary<PlcArtifactKind, List<IPlcComparisonStrategy>>();
            foreach (IPlcComparisonStrategy strategy in strategies)
            {
                if (strategy == null)
                {
                    throw new ArgumentException("Strategy collection must not contain null entries.", nameof(strategies));
                }

                foreach (PlcArtifactKind kind in strategy.SupportedKinds)
                {
                    if (!registrations.TryGetValue(kind, out List<IPlcComparisonStrategy>? list))
                    {
                        list = new List<IPlcComparisonStrategy>();
                        registrations[kind] = list;
                    }

                    list.Add(strategy);
                }
            }

            return registrations.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<IPlcComparisonStrategy>)kv.Value.ToArray());
        }
    }
}
