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
