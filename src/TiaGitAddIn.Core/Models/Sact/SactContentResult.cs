using System.Collections.Generic;

namespace TiaGitAddIn.Models.Sact
{
    public sealed class SactContentResult
    {
        public CompareState State { get; set; }
        public Dictionary<string, SactNetworkResult> Networks { get; set; } = new Dictionary<string, SactNetworkResult>();
    }
}