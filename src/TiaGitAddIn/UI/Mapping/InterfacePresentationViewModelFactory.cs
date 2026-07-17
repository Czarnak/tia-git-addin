using System;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    /// <summary>Maps a bare <see cref="InterfacePresentation"/> (Structured/Visual mode) to <see cref="InterfaceComparisonViewModel"/>.</summary>
    public sealed class InterfacePresentationViewModelFactory : IComparisonPresentationViewModelFactory
    {
        public bool CanMap(ComparisonPresentation presentation) => presentation is InterfacePresentation;

        public ComparisonPresentationViewModel Map(PlcComparisonResult result, ComparisonViewModelMetadata metadata)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            return new InterfaceComparisonViewModel((InterfacePresentation)result.Presentation, metadata);
        }
    }
}
