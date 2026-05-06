using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Models.Lad
{
    public sealed class LadElementLayout
    {
        public int Column { get; set; }
        public int Row { get; set; }
        public LadElementType ElementType { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Operand { get; set; } = string.Empty;
        public CompareState DiffState { get; set; }
        public string UId { get; set; } = string.Empty;
    }
}