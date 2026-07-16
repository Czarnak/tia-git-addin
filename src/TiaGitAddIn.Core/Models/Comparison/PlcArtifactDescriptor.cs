using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Models.Comparison
{
    public sealed class PlcArtifactDescriptor
    {
        public PlcArtifactDescriptor(PlcArtifactKind artifactKind, PlcComparisonMode preferredMode, IEnumerable<string> evidence)
        { ArtifactKind = artifactKind; PreferredMode = preferredMode; Evidence = ImmutableCopy.Of(evidence, nameof(evidence)); }
        public PlcArtifactKind ArtifactKind { get; }
        public PlcComparisonMode PreferredMode { get; }
        public IReadOnlyList<string> Evidence { get; }
    }

    public sealed class PlcArtifactPairDescriptor
    {
        public PlcArtifactPairDescriptor(PlcArtifactDescriptor? left, PlcArtifactDescriptor? right,
            PlcArtifactKind artifactKind, PlcComparisonMode requestedMode, PlcPairChangeKind changeKind,
            string limitation, IEnumerable<PlcComparisonDiagnostic>? diagnostics = null)
        {
            bool valid = changeKind == PlcPairChangeKind.Modified ? left != null && right != null
                : changeKind == PlcPairChangeKind.Added ? left == null && right != null
                : left != null && right == null;
            if (!valid) throw new ArgumentException("Pair sides do not match the declared change kind.", nameof(changeKind));
            Left = left; Right = right; ArtifactKind = artifactKind; RequestedMode = requestedMode;
            ChangeKind = changeKind; Limitation = limitation ?? string.Empty;
            Diagnostics = (diagnostics ?? Array.Empty<PlcComparisonDiagnostic>()).ToArray();
        }
        public PlcArtifactDescriptor? Left { get; }
        public PlcArtifactDescriptor? Right { get; }
        public PlcArtifactKind ArtifactKind { get; }
        public PlcComparisonMode RequestedMode { get; }
        public PlcPairChangeKind ChangeKind { get; }
        public string Limitation { get; }
        public IReadOnlyList<PlcComparisonDiagnostic> Diagnostics { get; }
    }
}
