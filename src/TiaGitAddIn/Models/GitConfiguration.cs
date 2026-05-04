namespace TiaGitAddIn.Models
{
    public sealed class GitConfiguration
    {
        public string? GitExecutablePath { get; set; } = "git";

        public string RepositoryPath { get; set; } = string.Empty;

        public string DefaultRemote { get; set; } = "origin";

        public int MaxLogEntries { get; set; } = 200;

        public int Version { get; set; } = 1;

        public string CommitAuthorName { get; set; } = string.Empty;

        public string CommitAuthorEmail { get; set; } = string.Empty;
    }
}
