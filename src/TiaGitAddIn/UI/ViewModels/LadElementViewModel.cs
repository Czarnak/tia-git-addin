using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadElementViewModel : ViewModelBase
    {
        private readonly LadElementLayout _layout;

        public LadElementViewModel(LadElementLayout layout, double cellWidth, double cellHeight)
        {
            _layout = layout;
            
            X = layout.Column * cellWidth;
            Y = layout.Row * cellHeight;
            Width = cellWidth - 10;
            Height = cellHeight - 10;
        }

        public double X { get; }
        public double Y { get; }
        public double Width { get; }
        public double Height { get; }

        public LadElementType ElementType => _layout.ElementType;
        public string DisplayName => _layout.DisplayName;
        public string Operand => _layout.Operand;
        public CompareState DiffState => _layout.DiffState;
    }
}