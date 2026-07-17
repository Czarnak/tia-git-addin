using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    /// <summary>
    /// Produces one concrete <see cref="ComparisonPresentationViewModel"/> for one concrete
    /// <see cref="ComparisonPresentation"/> type. Implementations match on the presentation's
    /// concrete type (<see cref="CanMap"/>), never on <see cref="PlcComparisonResult.ArtifactKind"/>
    /// or <see cref="PlcComparisonResult.ActualMode"/> -- those routing decisions already happened
    /// upstream when the presentation was produced.
    /// </summary>
    public interface IComparisonPresentationViewModelFactory
    {
        bool CanMap(ComparisonPresentation presentation);

        ComparisonPresentationViewModel Map(PlcComparisonResult result, ComparisonViewModelMetadata metadata);
    }
}
