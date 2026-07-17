using System;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    /// <summary>Maps a <see cref="TextPresentation"/> (Text mode) to <see cref="TextComparisonViewModel"/>.</summary>
    public sealed class TextPresentationViewModelFactory : IComparisonPresentationViewModelFactory
    {
        public bool CanMap(ComparisonPresentation presentation) => presentation is TextPresentation;

        public ComparisonPresentationViewModel Map(PlcComparisonResult result, ComparisonViewModelMetadata metadata)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            return new TextComparisonViewModel((TextPresentation)result.Presentation, metadata);
        }
    }
}
