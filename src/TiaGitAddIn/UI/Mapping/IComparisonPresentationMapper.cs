using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    /// <summary>
    /// Turns one complete <see cref="PlcComparisonResult"/> into exactly one bindable
    /// <see cref="ComparisonPresentationViewModel"/>. The single production implementation is
    /// <see cref="ComparisonPresentationMapper"/>; this seam exists so callers (Task 10's production
    /// wiring and its tests) depend on the abstraction, not the aggregation mechanics.
    /// </summary>
    public interface IComparisonPresentationMapper
    {
        ComparisonPresentationViewModel Map(PlcComparisonResult result);
    }
}
