using System;

namespace TiaGitAddIn.Models.Comparison
{
    public sealed class PlcComparisonRequest
    {
        public PlcComparisonRequest(PlcRevision left, PlcRevision right, PlcArtifactPairDescriptor pair)
        { Left = left ?? throw new ArgumentNullException(nameof(left)); Right = right ?? throw new ArgumentNullException(nameof(right)); Pair = pair ?? throw new ArgumentNullException(nameof(pair)); }
        public PlcRevision Left { get; }
        public PlcRevision Right { get; }
        public PlcArtifactPairDescriptor Pair { get; }
    }

    public sealed class ComparisonRawText
    {
        public ComparisonRawText(string? leftText, string? rightText, bool isLeftMissing, bool isRightMissing)
        { LeftText = leftText; RightText = rightText; IsLeftMissing = isLeftMissing; IsRightMissing = isRightMissing; }
        public string? LeftText { get; }
        public string? RightText { get; }
        public bool IsLeftMissing { get; }
        public bool IsRightMissing { get; }
    }

    public sealed class PlcComparisonContext
    {
        public PlcComparisonContext(PlcComparisonRequest request, ComparisonRawText? rawText)
        { Request = request ?? throw new ArgumentNullException(nameof(request)); RawText = rawText; }
        public PlcComparisonRequest Request { get; }
        public ComparisonRawText? RawText { get; }
    }
}
