using System.Text;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Tests.Comparison
{
    internal static class ComparisonTestData
    {
        public static PlcRevision TextRevision(PlcRevisionSide side, string text, string path = "Program.xml")
            => PlcRevision.Present(side, PlcRevisionSource.WorkingTree, path, Encoding.UTF8.GetBytes(text),
                PlcTextEncoding.Utf8WithoutBom, text, false, string.Empty);

        public static PlcRevision MissingRevision(PlcRevisionSide side, string path = "Program.xml")
            => PlcRevision.Missing(side, PlcRevisionSource.WorkingTree, path,
                side == PlcRevisionSide.Left ? PlcRevisionMissingReason.Added : PlcRevisionMissingReason.Deleted);

        public static PlcArtifactPairDescriptor Pair(PlcArtifactKind kind, PlcComparisonMode requestedMode,
            PlcPairChangeKind changeKind = PlcPairChangeKind.Modified)
        {
            var descriptor = new PlcArtifactDescriptor(kind, requestedMode, new[] { "test" });
            return new PlcArtifactPairDescriptor(changeKind == PlcPairChangeKind.Added ? null : descriptor,
                changeKind == PlcPairChangeKind.Removed ? null : descriptor, kind, requestedMode,
                changeKind, string.Empty);
        }

        public static PlcComparisonContext Context(PlcArtifactKind kind, PlcComparisonMode requestedMode,
            string leftText = "left", string rightText = "right", string path = "Program.xml")
            => new PlcComparisonContext(new PlcComparisonRequest(TextRevision(PlcRevisionSide.Left, leftText, path),
                TextRevision(PlcRevisionSide.Right, rightText, path), Pair(kind, requestedMode)),
                new ComparisonRawText(leftText, rightText, false, false));
    }
}
