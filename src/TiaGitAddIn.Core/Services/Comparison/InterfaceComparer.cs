using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// Recursively compares two already-normalized <see cref="InterfaceSnapshot"/>s (produced by
    /// <see cref="InterfaceSnapshotBuilder"/>) into an immutable <see cref="InterfacePresentation"/>.
    /// Every sibling level (sections, then members, then nested subelements) is matched independently by
    /// normalized ordinal key: right-side items in declaration order first, then left-only items in left
    /// order. A move (a member/section reappearing under a different parent) can never be reported as a
    /// single "modified" node, because matching never crosses a parent boundary -- it always surfaces as an
    /// independent Removed under the old parent and Added under the new one.
    /// </summary>
    public sealed class InterfaceComparer
    {
        public InterfacePresentation Compare(InterfaceSnapshot left, InterfaceSnapshot right)
        {
            if (left == null) throw new ArgumentNullException(nameof(left));
            if (right == null) throw new ArgumentNullException(nameof(right));

            List<InterfaceSectionComparison> sections = MatchSiblings(
                left.Sections, right.Sections, section => section.Name, BuildSectionComparison);

            return new InterfacePresentation(sections);
        }

        private static InterfaceSectionComparison BuildSectionComparison(InterfaceSectionSnapshot? left, InterfaceSectionSnapshot? right)
        {
            IReadOnlyList<InterfaceMemberSnapshot> leftMembers = left?.Members ?? Array.Empty<InterfaceMemberSnapshot>();
            IReadOnlyList<InterfaceMemberSnapshot> rightMembers = right?.Members ?? Array.Empty<InterfaceMemberSnapshot>();

            List<InterfaceMemberComparison> members = MatchSiblings(
                leftMembers, rightMembers, member => member.Path, BuildMemberComparison);

            InterfaceChangeKind kind = DetermineChangeKind(
                left?.IsPresent ?? false, right?.IsPresent ?? false, () => members.Any(HasChange));

            return new InterfaceSectionComparison(kind, left, right, members);
        }

        private static InterfaceMemberComparison BuildMemberComparison(InterfaceMemberSnapshot? left, InterfaceMemberSnapshot? right)
        {
            IReadOnlyList<InterfaceMemberSnapshot> leftChildren = left?.Children ?? Array.Empty<InterfaceMemberSnapshot>();
            IReadOnlyList<InterfaceMemberSnapshot> rightChildren = right?.Children ?? Array.Empty<InterfaceMemberSnapshot>();

            List<InterfaceMemberComparison> children = MatchSiblings(
                leftChildren, rightChildren, child => child.Path, BuildMemberComparison);

            IReadOnlyList<InterfaceFieldChange> fieldChanges = left != null && right != null
                ? ComputeFieldChanges(left, right)
                : Array.Empty<InterfaceFieldChange>();

            InterfaceChangeKind kind = DetermineChangeKind(
                left != null, right != null, () => fieldChanges.Count > 0 || children.Any(HasChange));

            return new InterfaceMemberComparison(kind, left, right, fieldChanges, children);
        }

        private static bool HasChange(InterfaceMemberComparison comparison) => comparison.ChangeKind != InterfaceChangeKind.Unchanged;

        private static InterfaceChangeKind DetermineChangeKind(bool leftPresent, bool rightPresent, Func<bool> hasDescendantChange)
        {
            if (leftPresent && !rightPresent) return InterfaceChangeKind.Removed;
            if (!leftPresent && rightPresent) return InterfaceChangeKind.Added;
            if (!leftPresent) return InterfaceChangeKind.Unchanged;

            return hasDescendantChange() ? InterfaceChangeKind.Modified : InterfaceChangeKind.Unchanged;
        }

        /// <summary>
        /// Matches two sibling lists by normalized ordinal key: every right-side item, in declaration order,
        /// paired with its left match (or created as a right-only "Added" node); then every remaining
        /// left-only item, in left declaration order, as a "Removed" node. Assumes unique keys per side
        /// (guaranteed for well-formed PLC interfaces); a duplicate key keeps only its last occurrence.
        /// </summary>
        private static List<TComparison> MatchSiblings<TSnapshot, TComparison>(
            IReadOnlyList<TSnapshot> leftItems,
            IReadOnlyList<TSnapshot> rightItems,
            Func<TSnapshot, string> keySelector,
            Func<TSnapshot?, TSnapshot?, TComparison> createComparison)
            where TSnapshot : class
        {
            var leftByKey = new Dictionary<string, TSnapshot>(StringComparer.Ordinal);
            foreach (TSnapshot item in leftItems)
            {
                leftByKey[keySelector(item)] = item;
            }

            var results = new List<TComparison>(leftItems.Count + rightItems.Count);
            var consumedLeftKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (TSnapshot rightItem in rightItems)
            {
                string key = keySelector(rightItem);
                if (leftByKey.TryGetValue(key, out TSnapshot? leftItem))
                {
                    consumedLeftKeys.Add(key);
                    results.Add(createComparison(leftItem, rightItem));
                }
                else
                {
                    results.Add(createComparison(null, rightItem));
                }
            }

            foreach (TSnapshot leftItem in leftItems)
            {
                string key = keySelector(leftItem);
                if (!consumedLeftKeys.Contains(key))
                {
                    consumedLeftKeys.Add(key);
                    results.Add(createComparison(leftItem, null));
                }
            }

            return results;
        }

        private static IReadOnlyList<InterfaceFieldChange> ComputeFieldChanges(InterfaceMemberSnapshot left, InterfaceMemberSnapshot right)
        {
            var changes = new List<InterfaceFieldChange>();

            AddIfDifferent(changes, InterfaceFieldKind.Name, left.Name, right.Name);
            AddIfDifferent(changes, InterfaceFieldKind.Datatype, left.Datatype, right.Datatype);
            AddIfDifferentEnum(changes, InterfaceFieldKind.Retain, left.Retain, right.Retain);
            AddIfDifferent(changes, InterfaceFieldKind.DefaultValue, left.DefaultValue, right.DefaultValue);
            AddIfDifferent(changes, InterfaceFieldKind.StartValue, left.StartValue, right.StartValue);
            AddDictionaryChanges(changes, InterfaceFieldKind.Comment, left.Comments, right.Comments);
            AddIfDifferent(changes, InterfaceFieldKind.Accessibility, left.Accessibility, right.Accessibility);
            AddIfDifferentEnum(changes, InterfaceFieldKind.Informative, left.Informative, right.Informative);
            AddIfDifferent(changes, InterfaceFieldKind.Version, left.Version, right.Version);
            AddDictionaryChanges(changes, InterfaceFieldKind.SemanticAttribute, left.SemanticAttributes, right.SemanticAttributes);

            return changes;
        }

        private static void AddIfDifferent(List<InterfaceFieldChange> changes, InterfaceFieldKind field, string? leftValue, string? rightValue)
        {
            if (!string.Equals(leftValue, rightValue, StringComparison.Ordinal))
            {
                changes.Add(new InterfaceFieldChange(field, key: null, leftValue, rightValue));
            }
        }

        private static void AddIfDifferentEnum(List<InterfaceFieldChange> changes, InterfaceFieldKind field, SemanticBoolean left, SemanticBoolean right)
        {
            if (left != right)
            {
                changes.Add(new InterfaceFieldChange(field, key: null, left.ToString(), right.ToString()));
            }
        }

        private static void AddDictionaryChanges(
            List<InterfaceFieldChange> changes,
            InterfaceFieldKind field,
            IReadOnlyDictionary<string, string> left,
            IReadOnlyDictionary<string, string> right)
        {
            var keys = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string key in left.Keys) keys.Add(key);
            foreach (string key in right.Keys) keys.Add(key);

            foreach (string key in keys)
            {
                string? leftValue = left.TryGetValue(key, out string? l) ? l : null;
                string? rightValue = right.TryGetValue(key, out string? r) ? r : null;
                if (!string.Equals(leftValue, rightValue, StringComparison.Ordinal))
                {
                    changes.Add(new InterfaceFieldChange(field, key, leftValue, rightValue));
                }
            }
        }
    }
}
