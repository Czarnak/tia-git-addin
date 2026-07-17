using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// Focused view model for <see cref="UnsupportedPresentation"/>: a marker with no payload beyond
    /// the shared metadata (header/limitation explain what is unsupported and why).
    /// </summary>
    public sealed class UnsupportedComparisonViewModel : ComparisonPresentationViewModel
    {
        public UnsupportedComparisonViewModel(ComparisonViewModelMetadata metadata)
            : base(ComparisonPresentationKind.Unsupported, metadata)
        {
        }
    }
}
