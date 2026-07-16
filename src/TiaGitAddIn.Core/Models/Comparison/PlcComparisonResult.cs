using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Models.Comparison
{
    /// <summary>
    /// The single, complete comparison outcome. The constructor centrally enforces two invariants:
    /// the limitation text must be empty for <see cref="PlcSupportLevel.Full"/> and non-blank for
    /// every other support level, and the presentation kind must be one the actual mode allows.
    /// </summary>
    public sealed class PlcComparisonResult
    {
        private static readonly IReadOnlyDictionary<PlcComparisonMode, ComparisonPresentationKind[]> CompatiblePresentationKinds =
            new Dictionary<PlcComparisonMode, ComparisonPresentationKind[]>
            {
                [PlcComparisonMode.Visual] = new[] { ComparisonPresentationKind.LogicNetwork, ComparisonPresentationKind.Interface },
                [PlcComparisonMode.Structured] = new[] { ComparisonPresentationKind.Interface, ComparisonPresentationKind.Scl },
                [PlcComparisonMode.Text] = new[] { ComparisonPresentationKind.Text },
                [PlcComparisonMode.Unsupported] = new[] { ComparisonPresentationKind.Unsupported, ComparisonPresentationKind.Error },
            };

        public PlcComparisonResult(
            PlcArtifactKind artifactKind,
            PlcComparisonMode requestedMode,
            PlcComparisonMode actualMode,
            PlcSupportLevel supportLevel,
            string limitation,
            IEnumerable<PlcComparisonDiagnostic> diagnostics,
            ComparisonPresentation presentation,
            ComparisonRawText? rawText)
        {
            if (limitation == null) throw new ArgumentNullException(nameof(limitation));
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));

            RequireValidLimitation(supportLevel, limitation);
            RequireCompatiblePresentation(actualMode, presentation);

            ArtifactKind = artifactKind;
            RequestedMode = requestedMode;
            ActualMode = actualMode;
            SupportLevel = supportLevel;
            Limitation = limitation;
            Diagnostics = diagnostics.ToArray();
            Presentation = presentation;
            RawText = rawText;
        }

        public PlcArtifactKind ArtifactKind { get; }
        public PlcComparisonMode RequestedMode { get; }
        public PlcComparisonMode ActualMode { get; }
        public PlcSupportLevel SupportLevel { get; }
        public string Limitation { get; }
        public IReadOnlyList<PlcComparisonDiagnostic> Diagnostics { get; }
        public ComparisonPresentation Presentation { get; }
        public ComparisonRawText? RawText { get; }

        private static void RequireValidLimitation(PlcSupportLevel supportLevel, string limitation)
        {
            bool isValid = supportLevel == PlcSupportLevel.Full
                ? limitation.Length == 0
                : !string.IsNullOrWhiteSpace(limitation);

            if (!isValid)
            {
                throw new ArgumentException(
                    $"Limitation must be empty for {PlcSupportLevel.Full} and non-blank for every other support level.",
                    nameof(limitation));
            }
        }

        private static void RequireCompatiblePresentation(PlcComparisonMode actualMode, ComparisonPresentation presentation)
        {
            bool isCompatible = CompatiblePresentationKinds.TryGetValue(actualMode, out ComparisonPresentationKind[]? allowedKinds)
                && allowedKinds.Contains(presentation.Kind);

            if (!isCompatible)
            {
                throw new ArgumentException(
                    $"Presentation kind {presentation.Kind} is not valid for comparison mode {actualMode}.",
                    nameof(presentation));
            }
        }
    }
}
