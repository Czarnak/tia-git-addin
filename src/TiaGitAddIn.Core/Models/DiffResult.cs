using System.Collections.Generic;

namespace TiaGitAddIn.Models
{
    public sealed class DiffResult
    {
        public IReadOnlyList<DiffEntry> Entries { get; set; } = new List<DiffEntry>();
    }
}
