using System;

namespace TiaGitAddIn.Models
{
    public sealed class CommitInfo
    {
        public string Hash { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public DateTimeOffset AuthorDate { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string? ParentHash { get; set; }
    }
}
