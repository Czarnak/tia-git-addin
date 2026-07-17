using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// Focused view model for <see cref="ErrorPresentation"/>: a marker with no payload beyond the
    /// shared metadata (header/limitation/diagnostics carry the failure explanation).
    /// </summary>
    public sealed class ErrorComparisonViewModel : ComparisonPresentationViewModel
    {
        public ErrorComparisonViewModel(ComparisonViewModelMetadata metadata)
            : base(ComparisonPresentationKind.Error, metadata)
        {
        }
    }
}
