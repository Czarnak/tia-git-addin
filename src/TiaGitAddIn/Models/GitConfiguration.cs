namespace TiaGitAddIn.Models
{
    public sealed class GitConfiguration
    {
        public string GitExecutablePath { get; set; } = "git";

        public string RepositoryPath { get; set; } = string.Empty;

        public string DefaultRemote { get; set; } = "origin";

        public string CommitAuthorName { get; set; } = string.Empty;

        public string CommitAuthorEmail { get; set; } = string.Empty;
    }
}
