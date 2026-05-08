using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadNetworkPairViewModel : ViewModelBase
    {
        public LadNetworkPairViewModel(LadNetworkPairLayout pair)
        {
            NetworkNumber = pair.NetworkNumber;
            DiffState = pair.DiffState;
            Title = pair.Title;
            Comment = pair.Comment;

            if (pair.Left != null)
            {
                Left = new LadNetworkViewModel(pair.Left);
            }

            if (pair.Right != null)
            {
                Right = new LadNetworkViewModel(pair.Right);
            }
        }

        public int NetworkNumber { get; }
        public CompareState DiffState { get; }
        public string? Title { get; }
        public string? Comment { get; }
        public LadNetworkViewModel? Left { get; }
        public LadNetworkViewModel? Right { get; }

        public bool HasLeft => Left != null;
        public bool HasRight => Right != null;
    }
}
