using System.Collections.Generic;
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
        public string Comment { get; set; } = string.Empty;
        public string Equation { get; set; } = string.Empty;
        public CompareState DiffState { get; set; }
        public string UId { get; set; } = string.Empty;
        public List<string> InputPins { get; set; } = new List<string>();
        public List<string> OutputPins { get; set; } = new List<string>();
        public List<LadPinLayout> InputPinRows { get; set; } = new List<LadPinLayout>();
        public List<LadPinLayout> OutputPinRows { get; set; } = new List<LadPinLayout>();
        public double Width { get; set; } = 90;
        public double Height { get; set; } = 60;
    }
}
