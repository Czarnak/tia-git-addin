using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// The <see cref="IPlcComparisonStrategy"/> for artifact kinds that only ever receive a text-only
    /// comparison: <see cref="PlcArtifactKind.Text"/>, <see cref="PlcArtifactKind.GenericXml"/>,
    /// <see cref="PlcArtifactKind.Stl"/>, and <see cref="PlcArtifactKind.Sfc"/>. It never attempts a
    /// semantic comparison itself; it only turns the classified pair into a
    /// <see cref="PlcSupportLevel.Fallback"/> result via <see cref="PlcComparisonResultFactory.CreateTextFallback"/>.
    /// This type performs no work that could throw and be mistakenly caught, so cancellation always
    /// propagates to the caller unwrapped.
    /// </summary>
    public sealed class TextFallbackStrategy : IPlcComparisonStrategy
    {
        private static readonly IReadOnlyCollection<PlcArtifactKind> SupportedArtifactKinds = new[]
        {
            PlcArtifactKind.Text,
            PlcArtifactKind.GenericXml,
            PlcArtifactKind.Stl,
            PlcArtifactKind.Sfc,
        };

        private readonly PlcComparisonResultFactory _resultFactory;

        public TextFallbackStrategy(PlcComparisonResultFactory resultFactory)
        {
            _resultFactory = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
        }

        public IReadOnlyCollection<PlcArtifactKind> SupportedKinds => SupportedArtifactKinds;

        public Task<PlcComparisonResult> CompareAsync(PlcComparisonContext context, CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            cancellationToken.ThrowIfCancellationRequested();

            string limitation = string.IsNullOrEmpty(context.Request.Pair.Limitation)
                ? $"{context.Request.Pair.ArtifactKind} semantic comparison is unavailable."
                : context.Request.Pair.Limitation;

            PlcComparisonResult result = _resultFactory.CreateTextFallback(context, limitation, context.Request.Pair.Diagnostics);
            return Task.FromResult(result);
        }
    }
}
