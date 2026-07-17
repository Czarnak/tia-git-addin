using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Models.Comparison
{
    /// <summary>
    /// One field-level difference found on a matched <see cref="InterfaceMemberComparison"/>. <see cref="Key"/>
    /// is null for scalar fields (Datatype, StartValue, ...) and carries the language code or semantic
    /// attribute name for the two dictionary-valued fields (<see cref="InterfaceFieldKind.Comment"/> and
    /// <see cref="InterfaceFieldKind.SemanticAttribute"/>), one <see cref="InterfaceFieldChange"/> per key
    /// that differs.
    /// </summary>
    public sealed class InterfaceFieldChange
    {
        public InterfaceFieldChange(InterfaceFieldKind field, string? key, string? leftValue, string? rightValue)
        {
            Field = field; Key = key; LeftValue = leftValue; RightValue = rightValue;
        }
        public InterfaceFieldKind Field { get; }
        public string? Key { get; }
        public string? LeftValue { get; }
        public string? RightValue { get; }
    }

    /// <summary>
    /// The recursive comparison result for one interface member (and its nested subelements), produced by
    /// <see cref="Services.Comparison.InterfaceComparer"/>. Exactly one of <see cref="Left"/>/<see cref="Right"/>
    /// is null for <see cref="InterfaceChangeKind.Added"/>/<see cref="InterfaceChangeKind.Removed"/>; both are
    /// non-null for <see cref="InterfaceChangeKind.Unchanged"/>/<see cref="InterfaceChangeKind.Modified"/>.
    /// </summary>
    public sealed class InterfaceMemberComparison
    {
        public InterfaceMemberComparison(
            InterfaceChangeKind changeKind,
            InterfaceMemberSnapshot? left,
            InterfaceMemberSnapshot? right,
            IEnumerable<InterfaceFieldChange> fieldChanges,
            IEnumerable<InterfaceMemberComparison> children)
        {
            if (left == null && right == null)
            {
                throw new ArgumentException("A member comparison requires at least one snapshot side.");
            }

            ChangeKind = changeKind;
            Left = left;
            Right = right;
            FieldChanges = (fieldChanges ?? throw new ArgumentNullException(nameof(fieldChanges))).ToArray();
            Children = (children ?? throw new ArgumentNullException(nameof(children))).ToArray();
        }

        public InterfaceChangeKind ChangeKind { get; }
        public InterfaceMemberSnapshot? Left { get; }
        public InterfaceMemberSnapshot? Right { get; }
        public IReadOnlyList<InterfaceFieldChange> FieldChanges { get; }
        public IReadOnlyList<InterfaceMemberComparison> Children { get; }
    }

    /// <summary>
    /// The recursive comparison result for one interface section, produced by
    /// <see cref="Services.Comparison.InterfaceComparer"/>. <see cref="Members"/> is scoped to this section
    /// only -- descendants are never flattened into a shared root list.
    /// </summary>
    public sealed class InterfaceSectionComparison
    {
        public InterfaceSectionComparison(
            InterfaceChangeKind changeKind,
            InterfaceSectionSnapshot? left,
            InterfaceSectionSnapshot? right,
            IEnumerable<InterfaceMemberComparison> members)
        {
            if (left == null && right == null)
            {
                throw new ArgumentException("A section comparison requires at least one snapshot side.");
            }

            ChangeKind = changeKind;
            Left = left;
            Right = right;
            Members = (members ?? throw new ArgumentNullException(nameof(members))).ToArray();
        }

        public InterfaceChangeKind ChangeKind { get; }
        public InterfaceSectionSnapshot? Left { get; }
        public InterfaceSectionSnapshot? Right { get; }
        public IReadOnlyList<InterfaceMemberComparison> Members { get; }

        /// <summary>Display name, taken from whichever side is present (both agree on it once matched).</summary>
        public string Name => (Right ?? Left)!.Name;
    }

    /// <summary>
    /// The complete, immutable result of comparing two <see cref="InterfaceSnapshot"/>s, produced by
    /// <see cref="Services.Comparison.InterfaceComparer"/>. Consumed by WPF (Task 9) and by
    /// <see cref="LadPresentation"/>/<c>FbdPresentation</c> as the shared interface-comparison payload.
    /// </summary>
    public sealed class InterfacePresentation : ComparisonPresentation
    {
        public InterfacePresentation(IEnumerable<InterfaceSectionComparison> sections)
            : base(ComparisonPresentationKind.Interface)
        {
            Sections = (sections ?? throw new ArgumentNullException(nameof(sections))).ToArray();
        }

        public IReadOnlyList<InterfaceSectionComparison> Sections { get; }

        /// <summary>
        /// True when any section (recursively: any member, any nested subelement, any field) differs between
        /// the two sides. Cheap because <see cref="InterfaceChangeKind"/> already encodes the recursive
        /// Modified-propagation computed once at comparison time.
        /// </summary>
        public bool HasChanges => Sections.Any(section => section.ChangeKind != InterfaceChangeKind.Unchanged);
    }
}
