using System.Collections.Generic;

namespace TiaGitAddIn.Models.Sact
{
    /// <summary>
    /// Canonical ordering of TIA block interface sections, shared by the SimaticML comparer
    /// and the LAD diff view model so the ordering is defined in exactly one place.
    /// </summary>
    public static class SactInterfaceSections
    {
        public static readonly IReadOnlyList<string> Order = new[]
        {
            "Input",
            "Output",
            "InOut",
            "Temp",
            "Constant",
            "Return"
        };
    }
}
