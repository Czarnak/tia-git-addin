using System.Collections.Generic;

namespace TiaGitAddIn.Models.Sact
{
    public sealed class SactNetworkResult
    {
        public CompareState State { get; set; }
        public string? Title { get; set; }
        public string? Comment { get; set; }
        public SactNumberPair Number { get; set; } = new SactNumberPair();
        public Dictionary<string, SactComponentData> Body { get; set; } = new Dictionary<string, SactComponentData>();
    }
}