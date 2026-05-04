namespace TiaGitAddIn.Models
{
    public sealed class FileStatusEntry
    {
        public string FilePath { get; set; } = string.Empty;

        public string? OriginalPath { get; set; }

        public FileStatus IndexStatus { get; set; } = FileStatus.Unmodified;

        public FileStatus WorkTreeStatus { get; set; } = FileStatus.Unmodified;

        public bool IsStaged =>
            IndexStatus != FileStatus.Unmodified &&
            IndexStatus != FileStatus.Untracked &&
            IndexStatus != FileStatus.Ignored;

        public bool IsUnstaged =>
            WorkTreeStatus != FileStatus.Unmodified &&
            WorkTreeStatus != FileStatus.Ignored;

        public bool IsConflicted =>
            IndexStatus == FileStatus.Conflicted ||
            WorkTreeStatus == FileStatus.Conflicted;
    }
}
