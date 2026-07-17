using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// One file-list selection to compare: a repository-relative path, the commit hash to compare against
    /// (<c>null</c> means the working tree vs. <c>HEAD</c>), and the pair's change kind (drives which side,
    /// if any, is loaded as a missing revision instead of an actual one).
    /// </summary>
    public sealed class ComparisonSelection
    {
        public ComparisonSelection(string repositoryRelativePath, string? commitHash, PlcPairChangeKind changeKind)
        {
            RepositoryRelativePath = repositoryRelativePath;
            CommitHash = commitHash;
            ChangeKind = changeKind;
        }

        public string RepositoryRelativePath { get; }
        public string? CommitHash { get; }
        public PlcPairChangeKind ChangeKind { get; }
    }
}
