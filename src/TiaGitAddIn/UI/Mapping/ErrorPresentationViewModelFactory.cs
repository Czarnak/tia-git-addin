using System;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    /// <summary>Maps an <see cref="ErrorPresentation"/> to <see cref="ErrorComparisonViewModel"/>.</summary>
    public sealed class ErrorPresentationViewModelFactory : IComparisonPresentationViewModelFactory
    {
        public bool CanMap(ComparisonPresentation presentation) => presentation is ErrorPresentation;

        public ComparisonPresentationViewModel Map(PlcComparisonResult result, ComparisonViewModelMetadata metadata)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            return new ErrorComparisonViewModel(metadata);
        }
    }
}
