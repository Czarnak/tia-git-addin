using System.Collections.Generic;

namespace TiaGitAddIn.Models.Sact
{
    public sealed class SactCompareResult
    {
        public string Left { get; set; } = string.Empty;
        public string Right { get; set; } = string.Empty;
        public CompareState State { get; set; }
        public SactInterfaceResult? Interface { get; set; }
        public SactContentResult? Content { get; set; }
        public Dictionary<string, object>? Attributes { get; set; }
    }
}