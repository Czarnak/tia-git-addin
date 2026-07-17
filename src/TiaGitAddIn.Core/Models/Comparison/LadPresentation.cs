using System;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services.Comparison;

namespace TiaGitAddIn.Models.Comparison
{
    /// <summary>
    /// The complete comparison result for one LAD block: the legacy, visual-graph-oriented
    /// <see cref="SactCompareResult"/> (network/wire/component states, defensively cloned so no caller can
    /// mutate shared state) plus the new, precisely-normalized <see cref="InterfacePresentation"/>. Built by
    /// the LAD comparison strategy (Task 10); FBD may embed the same <see cref="InterfacePresentation"/> in a
    /// future <c>FbdPresentation</c>.
    /// </summary>
    public sealed class LadPresentation : LogicNetworkPresentation
    {
        private readonly SactCompareResult legacySnapshot;

        public LadPresentation(SactCompareResult legacyResult, InterfacePresentation interfacePresentation)
        {
            legacySnapshot = SactCompareResultCloner.Clone(legacyResult ?? throw new ArgumentNullException(nameof(legacyResult)));
            Interface = interfacePresentation ?? throw new ArgumentNullException(nameof(interfacePresentation));
        }

        public InterfacePresentation Interface { get; }

        /// <summary>Returns a fresh, independently-mutable copy every time -- never the same instance twice.</summary>
        public SactCompareResult CreateLegacyResult() => SactCompareResultCloner.Clone(legacySnapshot);
    }
}
