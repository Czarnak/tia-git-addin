using TiaGitAddIn.Models;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class FileStatusItemViewModel
    {
        public FileStatusItemViewModel(FileStatusEntry entry)
        {
            FilePath = entry.FilePath;
            Area = GetArea(entry);
            StatusText = GetStatusText(entry);
            CanStage = entry.IsUnstaged || entry.IndexStatus == FileStatus.Untracked;
            CanUnstage = entry.IsStaged;
        }

        public string FilePath { get; }

        public string Area { get; }

        public string StatusText { get; }

        public bool CanStage { get; }

        public bool CanUnstage { get; }

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
