using System;
using System.Collections.Generic;

namespace TiaGitAddIn.Models.Comparison
{
    public abstract class ComparisonPresentation
    {
        protected ComparisonPresentation(ComparisonPresentationKind kind) { Kind = kind; }
        public ComparisonPresentationKind Kind { get; }
    }

    public abstract class LogicNetworkPresentation : ComparisonPresentation
    {
        protected LogicNetworkPresentation() : base(ComparisonPresentationKind.LogicNetwork) { }
    }

    /// <summary>
    /// Bounds for <see cref="TextDiffLine"/>/<see cref="TextPresentation"/> production: how many lines per
    /// side, how many characters per line, and how large the O(n*m) alignment matrix may grow before a
    /// comparer must fall back to a cheaper linear strategy.
    /// </summary>
    public sealed class TextComparisonLimits
    {
        public TextComparisonLimits(int maximumLinesPerSide, int maximumLineLength, long maximumMatrixCells)
        {
            if (maximumLinesPerSide <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLinesPerSide));
            if (maximumLineLength <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLineLength));
            if (maximumMatrixCells <= 0) throw new ArgumentOutOfRangeException(nameof(maximumMatrixCells));
            MaximumLinesPerSide = maximumLinesPerSide; MaximumLineLength = maximumLineLength; MaximumMatrixCells = maximumMatrixCells;
        }
        public int MaximumLinesPerSide { get; }
        public int MaximumLineLength { get; }
        public long MaximumMatrixCells { get; }
        public static TextComparisonLimits Default { get; } = new TextComparisonLimits(20_000, 32_768, 4_000_000);
    }

    public sealed class TextDiffLine
    {
        public TextDiffLine(TextDiffLineKind kind, int? leftLineNumber, int? rightLineNumber, string text)
        { Kind = kind; LeftLineNumber = leftLineNumber; RightLineNumber = rightLineNumber; Text = text ?? throw new ArgumentNullException(nameof(text)); }
        public TextDiffLineKind Kind { get; }
        public int? LeftLineNumber { get; }
        public int? RightLineNumber { get; }
        public string Text { get; }
    }

    public sealed class TextPresentation : ComparisonPresentation
    {
        public TextPresentation(IEnumerable<TextDiffLine> lines, bool isTruncated = false, bool usedLinearFallback = false)
            : base(ComparisonPresentationKind.Text)
        { Lines = ImmutableCopy.Of(lines, nameof(lines)); IsTruncated = isTruncated; UsedLinearFallback = usedLinearFallback; }
        public IReadOnlyList<TextDiffLine> Lines { get; }
        public bool IsTruncated { get; }
        public bool UsedLinearFallback { get; }
    }

    public sealed class UnsupportedPresentation : ComparisonPresentation
    {
        public UnsupportedPresentation() : base(ComparisonPresentationKind.Unsupported) { }
    }

    public sealed class ErrorPresentation : ComparisonPresentation
    {
        public ErrorPresentation() : base(ComparisonPresentationKind.Error) { }
    }
}
