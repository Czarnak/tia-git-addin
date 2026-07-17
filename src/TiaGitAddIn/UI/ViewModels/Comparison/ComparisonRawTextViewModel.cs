using System;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// Read-only presentation wrapper for <see cref="ComparisonRawText"/>: normalizes the nullable
    /// left/right text into non-null strings so the view can bind directly without null checks.
    /// </summary>
    public sealed class ComparisonRawTextViewModel
    {
        public ComparisonRawTextViewModel(ComparisonRawText rawText)
        {
            if (rawText == null) throw new ArgumentNullException(nameof(rawText));

            LeftText = rawText.LeftText ?? string.Empty;
            RightText = rawText.RightText ?? string.Empty;
            IsLeftMissing = rawText.IsLeftMissing;
            IsRightMissing = rawText.IsRightMissing;
        }

        public string LeftText { get; }
        public string RightText { get; }
        public bool IsLeftMissing { get; }
        public bool IsRightMissing { get; }
    }
}
