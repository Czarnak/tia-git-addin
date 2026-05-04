namespace TiaGitAddIn.Models
{
    public enum FileStatus
    {
        Unmodified = 0,
        Modified,
        Added,
        Deleted,
        Renamed,
        Copied,
        Untracked,
        Ignored,
        Conflicted
    }
}
