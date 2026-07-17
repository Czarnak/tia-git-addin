using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// One field-level difference on a matched <see cref="InterfaceMemberRowViewModel"/>, ready for
    /// display: <see cref="Label"/> folds the optional dictionary key (language code / semantic
    /// attribute name) into the field name so the view never has to branch on <see cref="Key"/>.
    /// </summary>
    public sealed class InterfaceFieldChangeViewModel
    {
        public InterfaceFieldChangeViewModel(InterfaceFieldChange change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));

            Field = change.Field;
            Key = change.Key;
            LeftValue = change.LeftValue ?? string.Empty;
            RightValue = change.RightValue ?? string.Empty;
            Label = Key == null ? Field.ToString() : $"{Field} [{Key}]";
        }

        public InterfaceFieldKind Field { get; }
        public string? Key { get; }
        public string LeftValue { get; }
        public string RightValue { get; }
        public string Label { get; }
    }

    /// <summary>
    /// One row of the interface comparison tree (a member or nested subelement). Left and right
    /// datatype/default value are exposed as two separate properties -- never merged with
    /// <c>right ?? left</c> -- so a Modified row can show both sides instead of silently hiding
    /// whichever side happens to be blank.
    /// </summary>
    public sealed class InterfaceMemberRowViewModel
    {
        public InterfaceMemberRowViewModel(InterfaceMemberComparison comparison)
        {
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));

            ChangeKind = comparison.ChangeKind;
            Left = comparison.Left;
            Right = comparison.Right;
            Name = (comparison.Right ?? comparison.Left)!.Name;
            LeftDatatype = comparison.Left?.Datatype ?? string.Empty;
            RightDatatype = comparison.Right?.Datatype ?? string.Empty;
            LeftDefaultValue = comparison.Left?.DefaultValue ?? string.Empty;
            RightDefaultValue = comparison.Right?.DefaultValue ?? string.Empty;
            StatusLabel = ToStatusLabel(comparison.ChangeKind);
            FieldChanges = comparison.FieldChanges.Select(f => new InterfaceFieldChangeViewModel(f)).ToArray();
            Children = comparison.Children.Select(c => new InterfaceMemberRowViewModel(c)).ToArray();
        }

        public InterfaceChangeKind ChangeKind { get; }
        public InterfaceMemberSnapshot? Left { get; }
        public InterfaceMemberSnapshot? Right { get; }
        public string Name { get; }
        public string LeftDatatype { get; }
        public string RightDatatype { get; }
        public string LeftDefaultValue { get; }
        public string RightDefaultValue { get; }
        public string StatusLabel { get; }
        public IReadOnlyList<InterfaceFieldChangeViewModel> FieldChanges { get; }
        public IReadOnlyList<InterfaceMemberRowViewModel> Children { get; }
        public bool HasFieldChanges => FieldChanges.Count > 0;

        internal static string ToStatusLabel(InterfaceChangeKind kind) => kind switch
        {
            InterfaceChangeKind.Added => "Added",
            InterfaceChangeKind.Removed => "Removed",
            InterfaceChangeKind.Modified => "Modified",
            _ => "Unchanged",
        };
    }

    /// <summary>One interface section (Input/Output/InOut/...) with its member rows.</summary>
    public sealed class InterfaceSectionRowViewModel
    {
        public InterfaceSectionRowViewModel(InterfaceSectionComparison comparison)
        {
            if (comparison == null) throw new ArgumentNullException(nameof(comparison));

            ChangeKind = comparison.ChangeKind;
            Name = comparison.Name;
            StatusLabel = InterfaceMemberRowViewModel.ToStatusLabel(comparison.ChangeKind);
            Members = comparison.Members.Select(m => new InterfaceMemberRowViewModel(m)).ToArray();
        }

        public InterfaceChangeKind ChangeKind { get; }
        public string Name { get; }
        public string StatusLabel { get; }
        public IReadOnlyList<InterfaceMemberRowViewModel> Members { get; }
    }

    /// <summary>
    /// Focused view model for <see cref="InterfacePresentation"/>: recursively wraps every section and
    /// member so the view can bind directly to a tree of rows without touching Core comparison types.
    /// </summary>
    public sealed class InterfaceComparisonViewModel : ComparisonPresentationViewModel
    {
        public InterfaceComparisonViewModel(InterfacePresentation presentation, ComparisonViewModelMetadata metadata)
            : base(ComparisonPresentationKind.Interface, metadata)
        {
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));

            Sections = presentation.Sections.Select(s => new InterfaceSectionRowViewModel(s)).ToArray();
            HasChanges = presentation.HasChanges;
        }

        public IReadOnlyList<InterfaceSectionRowViewModel> Sections { get; }
        public bool HasChanges { get; }
    }
}
