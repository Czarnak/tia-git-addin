using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Models.Lad
{
    public sealed class LadNetworkPairLayout
    {
        public int NetworkNumber { get; set; }
        public CompareState DiffState { get; set; }
        public LadNetworkLayout? Left { get; set; }
        public LadNetworkLayout? Right { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
    }
}
