using System;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    /// <summary>Maps an <see cref="UnsupportedPresentation"/> to <see cref="UnsupportedComparisonViewModel"/>.</summary>
    public sealed class UnsupportedPresentationViewModelFactory : IComparisonPresentationViewModelFactory
    {
        public bool CanMap(ComparisonPresentation presentation) => presentation is UnsupportedPresentation;

        public ComparisonPresentationViewModel Map(PlcComparisonResult result, ComparisonViewModelMetadata metadata)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            return new UnsupportedComparisonViewModel(metadata);
        }
    }
}
