namespace TiaGitAddIn.Models.Sact
{
    public sealed class SactInterfaceMemberComparison
    {
        public string Section { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string LeftDatatype { get; set; } = string.Empty;
        public string RightDatatype { get; set; } = string.Empty;
        public string LeftStartValue { get; set; } = string.Empty;
        public string RightStartValue { get; set; } = string.Empty;
        public CompareState State { get; set; }
    }
}
