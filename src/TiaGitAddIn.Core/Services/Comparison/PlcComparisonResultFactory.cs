using System;
using System.Collections.Generic;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// The sole place that turns strategy/fallback outcomes into a <see cref="PlcComparisonResult"/>.
    /// Each method sets the actual mode, support level, and presentation combination that the result
    /// invariant allows for that outcome kind.
    /// </summary>
    public sealed class PlcComparisonResultFactory
    {
        private readonly ITextComparer _textComparer;

        public PlcComparisonResultFactory(ITextComparer textComparer)
        {
            _textComparer = textComparer ?? throw new ArgumentNullException(nameof(textComparer));
        }

        /// <summary>Creates a semantic (visual/structured) result. Retains <see cref="PlcComparisonContext.RawText"/> as-is.</summary>
        public PlcComparisonResult CreateSemantic(PlcComparisonContext context, PlcComparisonMode actualMode,
            PlcSupportLevel supportLevel, string limitation, IEnumerable<PlcComparisonDiagnostic> diagnostics,
            ComparisonPresentation presentation)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return new PlcComparisonResult(
                context.Request.Pair.ArtifactKind,
                context.Request.Pair.RequestedMode,
                actualMode,
                supportLevel,
                limitation,
                diagnostics,
                presentation,
                context.RawText);
        }

        /// <summary>Creates a text-only fallback result at <see cref="PlcSupportLevel.Fallback"/>. Requires raw text.</summary>
        public PlcComparisonResult CreateTextFallback(PlcComparisonContext context, string limitation,
            IEnumerable<PlcComparisonDiagnostic> diagnostics)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (context.RawText == null) throw new ArgumentException("Raw text is required for a text fallback result.", nameof(context));

            TextPresentation presentation = _textComparer.Compare(context.RawText);

            return new PlcComparisonResult(
                context.Request.Pair.ArtifactKind,
                context.Request.Pair.RequestedMode,
                PlcComparisonMode.Text,
                PlcSupportLevel.Fallback,
                limitation,
                diagnostics,
                presentation,
                context.RawText);
        }

        /// <summary>Creates an explicit unsupported result. No raw-text presentation is exposed.</summary>
        public PlcComparisonResult CreateUnsupported(PlcComparisonContext context, string limitation,
            IEnumerable<PlcComparisonDiagnostic> diagnostics)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return new PlcComparisonResult(
                context.Request.Pair.ArtifactKind,
                context.Request.Pair.RequestedMode,
                PlcComparisonMode.Unsupported,
                PlcSupportLevel.Unsupported,
                limitation,
                diagnostics,
                new UnsupportedPresentation(),
                null);
        }

        /// <summary>Creates a hard-failure result. Unsupported mode/support, error presentation, no raw text.</summary>
        public PlcComparisonResult CreateHardError(PlcArtifactKind artifactKind, PlcComparisonMode requestedMode,
            string limitation, IEnumerable<PlcComparisonDiagnostic> diagnostics)
        {
            return new PlcComparisonResult(
                artifactKind,
                requestedMode,
                PlcComparisonMode.Unsupported,
                PlcSupportLevel.Unsupported,
                limitation,
                diagnostics,
                new ErrorPresentation(),
                null);
        }
    }
}
