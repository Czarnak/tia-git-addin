namespace TiaGitAddIn.Models
{
    public sealed class DiffLine
    {
        public DiffLineType Type { get; set; }

        public int? OldLineNumber { get; set; }

        public int? NewLineNumber { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}
