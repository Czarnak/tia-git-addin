using TiaGitAddIn.Models;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class FileStatusItemViewModel(FileStatusEntry entry)
    {

        public string FilePath { get; } = entry.FilePath;

        public string Area { get; } = GetArea(entry);

        public FileStatus Status { get; } = entry.IsStaged ? entry.IndexStatus : entry.WorkTreeStatus;

        public string StatusText { get; } = GetStatusText(entry);

        public bool CanStage { get; } = entry.IsUnstaged || entry.IndexStatus == FileStatus.Untracked;

        public bool CanUnstage { get; } = entry.IsStaged;

        private static string GetArea(FileStatusEntry entry)
        {
            if (entry.IsConflicted)
            {
                return "Conflicts";
            }

            return entry.IsStaged ? "Staged" : "Working tree";
        }

        private static string GetStatusText(FileStatusEntry entry)
        {
            if (entry.IsConflicted)
            {
                return "Conflicted";
            }

            FileStatus status = entry.IsStaged ? entry.IndexStatus : entry.WorkTreeStatus;
            return status.ToString();
        }
    }
}
