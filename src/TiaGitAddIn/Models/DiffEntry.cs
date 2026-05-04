using System.Collections.Generic;

namespace TiaGitAddIn.Models
{
    public sealed class DiffEntry
    {
        public string FilePath { get; set; } = string.Empty;

        public string ChangeType { get; set; } = string.Empty;

        public IReadOnlyList<DiffHunk> Hunks { get; set; } = new List<DiffHunk>();
    }
}
