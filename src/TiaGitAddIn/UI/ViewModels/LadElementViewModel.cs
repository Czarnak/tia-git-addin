using System.Collections.ObjectModel;
using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadElementViewModel(LadElementLayout layout, double cellWidth, double cellHeight) : ViewModelBase
    {
        public double X { get; } = layout.Column * cellWidth;
        public double Y { get; } = layout.Row * cellHeight;
        public double Width { get; } = layout.Width;
        public double Height { get; } = layout.Height;

        public LadElementType ElementType => layout.ElementType;
        public string DisplayName => layout.DisplayName;
        public string Operand => layout.Operand;
        public CompareState DiffState => layout.DiffState;
        public ObservableCollection<string> InputPins { get; } = new ObservableCollection<string>(layout.InputPins);
        public ObservableCollection<string> OutputPins { get; } = new ObservableCollection<string>(layout.OutputPins);
    }
}
