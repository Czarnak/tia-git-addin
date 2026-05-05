using System.Collections.Generic;

namespace TiaGitAddIn.Models.Sact
{
    public sealed class SactInterfaceResult
    {
        public CompareState State { get; set; }
        public Dictionary<string, object>? Sections { get; set; }
    }
}