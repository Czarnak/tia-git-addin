using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// One line of a <see cref="TextComparisonViewModel"/>. <see cref="StatusLabel"/> gives Added/Removed/
    /// Omitted lines a visible marker so the change is never conveyed by background color alone.
    /// </summary>
    public sealed class TextDiffLineViewModel
    {
        public TextDiffLineViewModel(TextDiffLine line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            Kind = line.Kind;
            LeftLineNumber = line.LeftLineNumber;
            RightLineNumber = line.RightLineNumber;
            Text = line.Text;
            StatusLabel = ToStatusLabel(line.Kind);
        }

        public TextDiffLineKind Kind { get; }
        public int? LeftLineNumber { get; }
        public int? RightLineNumber { get; }
        public string Text { get; }
        public string StatusLabel { get; }

        private static string ToStatusLabel(TextDiffLineKind kind) => kind switch
        {
            TextDiffLineKind.Added => "+",
            TextDiffLineKind.Removed => "−",
            TextDiffLineKind.Omitted => "…",
            _ => string.Empty,
        };
    }

    /// <summary>Focused view model for <see cref="TextPresentation"/>: the bounded line-by-line diff.</summary>
    public sealed class TextComparisonViewModel : ComparisonPresentationViewModel
    {
        public TextComparisonViewModel(TextPresentation presentation, ComparisonViewModelMetadata metadata)
            : base(ComparisonPresentationKind.Text, metadata)
        {
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));

            Lines = presentation.Lines.Select(l => new TextDiffLineViewModel(l)).ToArray();
            IsTruncated = presentation.IsTruncated;
            UsedLinearFallback = presentation.UsedLinearFallback;
        }

        public IReadOnlyList<TextDiffLineViewModel> Lines { get; }
        public bool IsTruncated { get; }
        public bool UsedLinearFallback { get; }
    }
}
