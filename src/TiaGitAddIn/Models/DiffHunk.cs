using System.Collections.Generic;

namespace TiaGitAddIn.Models
{
    public sealed class DiffHunk
    {
        public string Header { get; set; } = string.Empty;

        public int OldStart { get; set; }

        public int OldCount { get; set; }

        public int NewStart { get; set; }

        public int NewCount { get; set; }

        public IReadOnlyList<DiffLine> Lines { get; set; } = new List<DiffLine>();
    }
}
