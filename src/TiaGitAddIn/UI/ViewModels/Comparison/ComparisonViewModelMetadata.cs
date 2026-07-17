using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// The metadata shared by every <see cref="ComparisonPresentationViewModel"/>, computed exactly
    /// once per <see cref="PlcComparisonResult"/> by <see cref="TiaGitAddIn.UI.Mapping.ComparisonPresentationMapper"/>
    /// and handed to whichever foundation factory maps the concrete presentation.
    /// </summary>
    public sealed class ComparisonViewModelMetadata
    {
        private ComparisonViewModelMetadata(PlcComparisonResult result)
        {
            ModeLabel = result.ActualMode.ToString();
            SupportLabel = result.SupportLevel.ToString();
            Header = $"{ModeLabel} · {SupportLabel}";
            Limitation = result.Limitation;
            Diagnostics = result.Diagnostics.Select(d => new ComparisonDiagnosticViewModel(d)).ToArray();
            RawText = result.RawText == null ? null : new ComparisonRawTextViewModel(result.RawText);
        }

        public string ModeLabel { get; }
        public string SupportLabel { get; }
        public string Header { get; }
        public string Limitation { get; }
        public IReadOnlyList<ComparisonDiagnosticViewModel> Diagnostics { get; }
        public ComparisonRawTextViewModel? RawText { get; }
        public bool HasLimitation => !string.IsNullOrWhiteSpace(Limitation);
        public bool HasRawText => RawText != null;

        public static ComparisonViewModelMetadata From(PlcComparisonResult result) =>
            new ComparisonViewModelMetadata(result ?? throw new ArgumentNullException(nameof(result)));
    }
}
