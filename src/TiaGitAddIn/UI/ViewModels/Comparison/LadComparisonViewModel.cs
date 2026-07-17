using System;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// Focused view model for <see cref="LadPresentation"/>: wraps the result-only <see cref="LadDiffViewModel"/>
    /// (built by <see cref="Mapping.LadPresentationViewModelFactory"/> from a fresh <c>SactCompareResult</c>
    /// clone) so the LAD network canvas and interface comparison render together under one typed VM.
    /// </summary>
    public sealed class LadComparisonViewModel : ComparisonPresentationViewModel
    {
        public LadComparisonViewModel(LadDiffViewModel content, ComparisonViewModelMetadata metadata)
            : base(ComparisonPresentationKind.LogicNetwork, metadata)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public LadDiffViewModel Content { get; }
    }
}
