using System;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// Base type for every focused, WPF-bindable comparison view model. Each concrete subclass wraps
    /// exactly one foundation <see cref="ComparisonPresentation"/> plus the metadata shared by all
    /// presentation kinds (header, limitation, diagnostics, raw text).
    /// </summary>
    public abstract class ComparisonPresentationViewModel
    {
        protected ComparisonPresentationViewModel(ComparisonPresentationKind kind, ComparisonViewModelMetadata metadata)
        {
            Kind = kind;
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }

        public ComparisonPresentationKind Kind { get; }
        public ComparisonViewModelMetadata Metadata { get; }
        public string Header => Metadata.Header;
        public string Limitation => Metadata.Limitation;
        public bool HasLimitation => Metadata.HasLimitation;
        public bool HasRawText => Metadata.HasRawText;
    }
}
