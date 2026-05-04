namespace TiaGitAddIn.Models
{
    public sealed class BranchInfo
    {
        public string Name { get; set; } = string.Empty;

        public bool IsCurrent { get; set; }

        public string? TrackingBranch { get; set; }

        public int AheadBy { get; set; }

        public int BehindBy { get; set; }
    }
}
