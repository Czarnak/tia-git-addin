using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class LadInterfaceRowViewModel : ViewModelBase
    {
        private LadInterfaceRowViewModel(
            int rowNumber,
            string section,
            string displayName,
            string dataType,
            string defaultValue,
            string comment,
            bool isSectionHeader,
            CompareState state)
        {
            RowNumber = rowNumber;
            Section = section;
            DisplayName = displayName;
            DataType = dataType;
            DefaultValue = defaultValue;
            Comment = comment;
            IsSectionHeader = isSectionHeader;
            State = state;
        }

        public int RowNumber { get; }
        public string Section { get; }
        public string DisplayName { get; }
        public string DataType { get; }
        public string DefaultValue { get; }
        public string Comment { get; }
        public bool IsSectionHeader { get; }
        public bool IsMember => !IsSectionHeader;
        public CompareState State { get; }

        public static LadInterfaceRowViewModel CreateSection(int rowNumber, string section)
        {
            return new LadInterfaceRowViewModel(
                rowNumber,
                section,
                section,
                string.Empty,
                string.Empty,
                string.Empty,
                true,
                CompareState.Equal);
        }

        public static LadInterfaceRowViewModel CreateMember(int rowNumber, SactInterfaceMemberComparison member)
        {
            string dataType = string.IsNullOrWhiteSpace(member.RightDatatype)
                ? member.LeftDatatype
                : member.RightDatatype;
            string defaultValue = string.IsNullOrWhiteSpace(member.RightStartValue)
                ? member.LeftStartValue
                : member.RightStartValue;

            return new LadInterfaceRowViewModel(
                rowNumber,
                member.Section,
                member.Name,
                dataType,
                defaultValue,
                string.Empty,
                false,
                member.State);
        }
    }
}
