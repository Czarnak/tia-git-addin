using TiaGitAddIn.Models.Lad;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadWireViewModel(LadWireSegment segment, double cellWidth, double cellHeight) : ViewModelBase
    {
        public double X1 { get; } = segment.FromColumn * cellWidth;
        public double Y1 { get; } = segment.FromRow * cellHeight;
        public double X2 { get; } = segment.ToColumn * cellWidth;
        public double Y2 { get; } = segment.ToRow * cellHeight;
        public bool IsOrBranch { get; } = segment.IsOrBranch;
    }
}