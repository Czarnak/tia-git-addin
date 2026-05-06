namespace TiaGitAddIn.Models.Lad
{
    public sealed class LadWireSegment
    {
        public int FromColumn { get; set; }
        public int FromRow { get; set; }
        public int ToColumn { get; set; }
        public int ToRow { get; set; }
        public bool IsOrBranch { get; set; }
    }
}