using TiaGitAddIn.Models.Lad;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadWireViewModel : ViewModelBase
    {
        private readonly LadWireSegment _segment;

        public LadWireViewModel(LadWireSegment segment, double cellWidth, double cellHeight)
        {
            _segment = segment;
            X1 = segment.FromColumn * cellWidth;
            Y1 = segment.FromRow * cellHeight;
            X2 = segment.ToColumn * cellWidth;
            Y2 = segment.ToRow * cellHeight;
            IsOrBranch = segment.IsOrBranch;
        }

        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }
        public bool IsOrBranch { get; }
    }
}