using System;
using System.Collections.Generic;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Models.Sact
{
    /// <summary>
    /// Legacy, Siemens/SACT-shaped canonical ordering of TIA block interface sections. Superseded by
    /// <see cref="InterfaceSectionOrder"/>, which the new immutable interface comparison pipeline
    /// (<c>InterfaceSnapshotBuilder</c>, <c>InterfaceComparer</c>) and this type both use as the single
    /// source of truth. Kept (not deleted) until existing callers of <see cref="Order"/> migrate to the
    /// neutral name; <see cref="Order"/> forwards to the identical, up-to-date canonical list (now including
    /// "Static", which this legacy list previously omitted).
    /// </summary>
    [Obsolete("Use TiaGitAddIn.Models.Comparison.InterfaceSectionOrder.Canonical instead.")]
    public static class SactInterfaceSections
    {
        public static readonly IReadOnlyList<string> Order = InterfaceSectionOrder.Canonical;
    }
}
