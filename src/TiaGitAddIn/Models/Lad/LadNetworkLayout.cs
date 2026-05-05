using System.Collections.Generic;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Models.Lad
{
    public sealed class LadNetworkLayout
    {
        public int NetworkNumber { get; set; }
        public CompareState DiffState { get; set; }
        public List<LadElementLayout> Elements { get; set; } = new List<LadElementLayout>();
        public List<LadWireSegment> Wires { get; set; } = new List<LadWireSegment>();
        public int ColumnCount { get; set; }
        public int RowCount { get; set; }
    }
}