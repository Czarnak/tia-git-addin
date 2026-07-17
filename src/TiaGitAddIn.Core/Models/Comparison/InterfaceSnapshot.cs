using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TiaGitAddIn.Models.Comparison
{
    /// <summary>
    /// A tri-state boolean: <see cref="Unspecified"/> is distinct from both <see cref="False"/> and
    /// <see cref="True"/> so a blank/absent PLC attribute never collapses onto a real value during
    /// comparison (see <c>Retain</c>/<c>Informative</c> on <see cref="InterfaceMemberSnapshot"/>).
    /// </summary>
    public enum SemanticBoolean { Unspecified, False, True }

    /// <summary>The result of matching one interface node (section or member) between two revisions.</summary>
    public enum InterfaceChangeKind { Unchanged, Added, Removed, Modified }

    /// <summary>Every independently-trackable field on an <see cref="InterfaceMemberSnapshot"/>.</summary>
    public enum InterfaceFieldKind
    {
        Name, Datatype, Retain, DefaultValue, StartValue, Comment,
        Accessibility, Informative, Version, SemanticAttribute
    }

    /// <summary>
    /// Canonical ordering of PLC block interface sections, shared by <see cref="Services.Comparison.InterfaceSnapshotBuilder"/>
    /// and the legacy <see cref="Sact.SactInterfaceSections"/> forwarder so the ordering is defined in exactly one place.
    /// </summary>
    public static class InterfaceSectionOrder
    {
        public static readonly IReadOnlyList<string> Canonical = new[]
        {
            "Input", "Output", "InOut", "Static", "Temp", "Constant", "Return"
        };
    }

    /// <summary>
    /// An immutable, Siemens-independent snapshot of one PLC block's complete interface (all sections and
    /// members) on one side of a comparison. Built exclusively by <see cref="Services.Comparison.InterfaceSnapshotBuilder"/>
    /// from a parsed <see cref="Services.SimaticMl.BlockDefinition"/>; never mutated after construction.
    /// </summary>
    public sealed class InterfaceSnapshot
    {
        public InterfaceSnapshot(IEnumerable<InterfaceSectionSnapshot> sections)
        { Sections = (sections ?? throw new ArgumentNullException(nameof(sections))).ToArray(); }
        public IReadOnlyList<InterfaceSectionSnapshot> Sections { get; }
    }

    /// <summary>
    /// One interface section (Input/Output/InOut/Static/Temp/Constant/Return, or a non-canonical name found
    /// in the source) on one side of a comparison. <see cref="IsPresent"/> distinguishes a section that is
    /// genuinely declared (even with zero members) from a canonical slot that simply is not present in this
    /// side's source at all.
    /// </summary>
    public sealed class InterfaceSectionSnapshot
    {
        public InterfaceSectionSnapshot(string name, bool isPresent, IEnumerable<InterfaceMemberSnapshot> members)
        { Name = Require(name); IsPresent = isPresent; Members = (members ?? throw new ArgumentNullException(nameof(members))).ToArray(); }
        public string Name { get; }
        public bool IsPresent { get; }
        public IReadOnlyList<InterfaceMemberSnapshot> Members { get; }
        private static string Require(string value) => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Section name is required.", nameof(value)) : value;
    }

    /// <summary>
    /// One interface member (or nested subelement) on one side of a comparison, fully normalized per the
    /// Task 8 normalization matrix. <see cref="Path"/> is the NFC-normalized, '/'-joined ancestor chain used
    /// as the ordinal matching key by <see cref="Services.Comparison.InterfaceComparer"/>.
    /// </summary>
    public sealed class InterfaceMemberSnapshot
    {
        public InterfaceMemberSnapshot(string section, string path, string name, string datatype,
            SemanticBoolean retain, string? defaultValue, string? startValue,
            IReadOnlyDictionary<string, string> comments, string? accessibility,
            SemanticBoolean informative, string? version,
            IReadOnlyDictionary<string, string> semanticAttributes,
            IEnumerable<InterfaceMemberSnapshot> children)
        {
            Section = section; Path = path; Name = name; Datatype = datatype; Retain = retain;
            DefaultValue = defaultValue; StartValue = startValue;
            Comments = new ReadOnlyDictionary<string, string>((comments ?? throw new ArgumentNullException(nameof(comments)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
            Accessibility = accessibility; Informative = informative; Version = version;
            SemanticAttributes = new ReadOnlyDictionary<string, string>((semanticAttributes ?? throw new ArgumentNullException(nameof(semanticAttributes)))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
            Children = (children ?? throw new ArgumentNullException(nameof(children))).ToArray();
        }
        public string Section { get; }
        public string Path { get; }
        public string Name { get; }
        public string Datatype { get; }
        public SemanticBoolean Retain { get; }
        public string? DefaultValue { get; }
        public string? StartValue { get; }
        public IReadOnlyDictionary<string, string> Comments { get; }
        public string? Accessibility { get; }
        public SemanticBoolean Informative { get; }
        public string? Version { get; }
        public IReadOnlyDictionary<string, string> SemanticAttributes { get; }
        public IReadOnlyList<InterfaceMemberSnapshot> Children { get; }
    }
}
