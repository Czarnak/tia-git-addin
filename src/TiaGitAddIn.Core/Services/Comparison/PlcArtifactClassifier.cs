using System;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// Classifies a single <see cref="PlcRevision"/>'s artifact kind/preferred comparison mode, and
    /// resolves two revisions to one <see cref="PlcArtifactPairDescriptor"/>.
    /// </summary>
    public interface IPlcArtifactClassifier
    {
        PlcArtifactDescriptor Classify(PlcRevision revision);

        PlcArtifactPairDescriptor Resolve(PlcRevision left, PlcRevision right);
    }

    /// <summary>
    /// Suffix/path/content evidence classifier. Content evidence comes from the bounded, DOM-free probes
    /// in <see cref="SimaticMlEvidenceReader"/> and <see cref="SclLexicalProbe"/>. <see cref="Classify"/>
    /// never inspects <see cref="PlcRevision.Source"/>, so working-tree versus commit revisions of
    /// identical content always classify identically.
    /// </summary>
    public sealed class PlcArtifactClassifier : IPlcArtifactClassifier
    {
        private const int MaxInspectionLength = 1_048_576;

        /// <summary>
        /// Precedence is exact and the first matching rule wins even when a later rule would also match:
        /// 1. a missing revision is never classified directly (see <see cref="Resolve"/>, which classifies
        ///    only the present side);
        /// 2. undecoded/binary present bytes -> Binary/Unsupported;
        /// 3. a well-formed SimaticML root/block with a recognized ProgrammingLanguage value -> Lad or
        ///    Fbd/Visual;
        /// 4. an .scl suffix with lexical top-level block opener/terminator evidence outside strings and
        ///    comments -> Scl/Structured;
        /// 5. an .stl/.sfc suffix, or a bounded leading content marker -> Stl or Sfc/Text;
        /// 6. well-formed non-SimaticML XML -> GenericXml/Text;
        /// 7. everything else decoded -> Text/Text.
        /// </summary>
        public PlcArtifactDescriptor Classify(PlcRevision revision)
        {
            if (revision == null) throw new ArgumentNullException(nameof(revision));
            if (revision.IsMissing)
            {
                throw new ArgumentException(
                    "A missing revision cannot be classified; classify the present side instead.",
                    nameof(revision));
            }

            if (revision.IsBinary || revision.Text == null)
            {
                return ClassifyBinary(revision);
            }

            string boundedText = Bound(revision.Text);
            string suffix = NormalizeSuffix(revision.OriginalSuffix);
            SimaticMlEvidence xmlEvidence = SimaticMlEvidenceReader.Probe(boundedText);

            if (xmlEvidence.BlockElementName != null
                && TryMapProgrammingLanguage(xmlEvidence.ProgrammingLanguageValue, out PlcArtifactKind visualKind))
            {
                return new PlcArtifactDescriptor(visualKind, PlcComparisonMode.Visual, new[]
                {
                    $"root:{xmlEvidence.BlockElementName}",
                    $"programming-language:{xmlEvidence.ProgrammingLanguageValue}",
                });
            }

            if (suffix == ".scl")
            {
                return ClassifyScl(boundedText, suffix);
            }

            if (suffix == ".stl" || HasLeadingMarker(boundedText, "NETWORK"))
            {
                return new PlcArtifactDescriptor(PlcArtifactKind.Stl, PlcComparisonMode.Text,
                    new[] { suffix == ".stl" ? $"suffix:{suffix}" : "content-marker:stl-network-header" });
            }

            if (suffix == ".sfc" || HasLeadingMarker(boundedText, "SFC"))
            {
                return new PlcArtifactDescriptor(PlcArtifactKind.Sfc, PlcComparisonMode.Text,
                    new[] { suffix == ".sfc" ? $"suffix:{suffix}" : "content-marker:sfc-header" });
            }

            if (xmlEvidence.IsWellFormed)
            {
                return new PlcArtifactDescriptor(PlcArtifactKind.GenericXml, PlcComparisonMode.Text,
                    new[] { $"xml-root:{xmlEvidence.RootElementName ?? "unknown"}" });
            }

            return new PlcArtifactDescriptor(PlcArtifactKind.Text, PlcComparisonMode.Text,
                new[] { "decoded-text:no-specific-evidence" });
        }

        /// <summary>
        /// Resolves two revisions to one pair descriptor: agreeing present-side kinds/modes are retained;
        /// a single missing side uses the present side's classification with an explicit Added/Removed
        /// change kind; either side classifying as binary yields Binary/Unsupported; and conflicting
        /// present-side kinds fall back to Text/Text with a non-blank limitation and a CMP-CLASS-CONFLICT
        /// diagnostic. The revision's source kind (working tree vs. commit) never affects the outcome
        /// because <see cref="Classify"/> never inspects <see cref="PlcRevision.Source"/>.
        /// </summary>
        public PlcArtifactPairDescriptor Resolve(PlcRevision left, PlcRevision right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));
            if (left.IsMissing && right.IsMissing)
            {
                throw new ArgumentException("At least one side must be present to resolve a pair.");
            }

            if (left.IsMissing)
            {
                PlcArtifactDescriptor rightOnly = Classify(right);
                return new PlcArtifactPairDescriptor(null, rightOnly, rightOnly.ArtifactKind, rightOnly.PreferredMode,
                    PlcPairChangeKind.Added, string.Empty);
            }

            if (right.IsMissing)
            {
                PlcArtifactDescriptor leftOnly = Classify(left);
                return new PlcArtifactPairDescriptor(leftOnly, null, leftOnly.ArtifactKind, leftOnly.PreferredMode,
                    PlcPairChangeKind.Removed, string.Empty);
            }

            PlcArtifactDescriptor leftResult = Classify(left);
            PlcArtifactDescriptor rightResult = Classify(right);

            if (leftResult.ArtifactKind == PlcArtifactKind.Binary || rightResult.ArtifactKind == PlcArtifactKind.Binary)
            {
                return new PlcArtifactPairDescriptor(leftResult, rightResult, PlcArtifactKind.Binary,
                    PlcComparisonMode.Unsupported, PlcPairChangeKind.Modified, string.Empty);
            }

            if (leftResult.ArtifactKind == rightResult.ArtifactKind && leftResult.PreferredMode == rightResult.PreferredMode)
            {
                return new PlcArtifactPairDescriptor(leftResult, rightResult, leftResult.ArtifactKind,
                    leftResult.PreferredMode, PlcPairChangeKind.Modified, string.Empty);
            }

            var diagnostic = new PlcComparisonDiagnostic(
                "CMP-CLASS-CONFLICT",
                PlcDiagnosticSeverity.Warning,
                $"Left classified as {leftResult.ArtifactKind}; right classified as {rightResult.ArtifactKind}.");

            return new PlcArtifactPairDescriptor(leftResult, rightResult, PlcArtifactKind.Text, PlcComparisonMode.Text,
                PlcPairChangeKind.Modified, "Artifact kinds differ; semantic comparison is unavailable.",
                new[] { diagnostic });
        }

        private static PlcArtifactDescriptor ClassifyBinary(PlcRevision revision)
        {
            string reason = string.IsNullOrEmpty(revision.EncodingLimitation) ? "undecoded-content" : revision.EncodingLimitation;
            return new PlcArtifactDescriptor(PlcArtifactKind.Binary, PlcComparisonMode.Unsupported, new[] { $"binary:{reason}" });
        }

        private static PlcArtifactDescriptor ClassifyScl(string boundedText, string suffix)
        {
            if (SclLexicalProbe.HasTopLevelBlockEvidence(boundedText, out string opener, out string terminator))
            {
                return new PlcArtifactDescriptor(PlcArtifactKind.Scl, PlcComparisonMode.Structured, new[]
                {
                    $"suffix:{suffix}",
                    $"scl-block:{opener}..{terminator}",
                });
            }

            return new PlcArtifactDescriptor(PlcArtifactKind.Text, PlcComparisonMode.Text, new[]
            {
                $"suffix:{suffix}",
                "invalid-scl-evidence:no-top-level-block-markers-outside-strings-or-comments",
            });
        }

        private static string Bound(string text)
            => text.Length > MaxInspectionLength ? text.Substring(0, MaxInspectionLength) : text;

        private static string NormalizeSuffix(string suffix)
            => string.IsNullOrEmpty(suffix) ? string.Empty : suffix.Trim().ToLowerInvariant();

        private static bool TryMapProgrammingLanguage(string? value, out PlcArtifactKind kind)
        {
            string? trimmed = value?.Trim();
            if (string.Equals(trimmed, "LAD", StringComparison.OrdinalIgnoreCase)) { kind = PlcArtifactKind.Lad; return true; }
            if (string.Equals(trimmed, "FBD", StringComparison.OrdinalIgnoreCase)) { kind = PlcArtifactKind.Fbd; return true; }
            kind = PlcArtifactKind.Unknown;
            return false;
        }

        /// <summary>Only the first non-blank line of the bounded text is inspected — never any directory word.</summary>
        private static bool HasLeadingMarker(string boundedText, string marker)
        {
            foreach (string rawLine in boundedText.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0) continue;
                return line.StartsWith(marker, StringComparison.Ordinal);
            }

            return false;
        }
    }
}
