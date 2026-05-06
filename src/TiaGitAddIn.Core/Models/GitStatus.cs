using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Models
{
    public sealed class GitStatus
    {
        public string CurrentBranch { get; set; } = string.Empty;

        public string? TrackingBranch { get; set; }

        public int AheadBy { get; set; }

        public int BehindBy { get; set; }

        public IReadOnlyList<FileStatusEntry> Entries { get; set; } =
            new List<FileStatusEntry>();

        public bool IsClean => Entries.Count == 0;

        public bool HasConflicts => Entries.Any(entry => entry.IsConflicted);

        public IReadOnlyList<FileStatusEntry> StagedEntries =>
            Entries.Where(entry => entry.IsStaged).ToList();

        public IReadOnlyList<FileStatusEntry> UnstagedEntries =>
            Entries.Where(entry => entry.IsUnstaged).ToList();

        public IReadOnlyList<FileStatusEntry> UntrackedEntries =>
            Entries.Where(entry => entry.IndexStatus == FileStatus.Untracked).ToList();
    }
}
