using System.Collections.ObjectModel;
using System.Linq;
using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadNetworkViewModel(LadNetworkLayout layout) : ViewModelBase
    {
        private const double CellWidth = 120;
        private const double CellHeight = 60;

        public int NetworkNumber => layout.NetworkNumber;
        public CompareState DiffState => layout.DiffState;

        public double CanvasWidth { get; } = layout.ColumnCount * CellWidth;
        public double CanvasHeight { get; } = layout.RowCount * CellHeight;

        public ObservableCollection<LadElementViewModel> Elements { get; } = new ObservableCollection<LadElementViewModel>(
                layout.Elements.Select(e => new LadElementViewModel(e, CellWidth, CellHeight))
            );
        public ObservableCollection<LadWireViewModel> Wires { get; } = new ObservableCollection<LadWireViewModel>(
                layout.Wires.Select(w => new LadWireViewModel(w, CellWidth, CellHeight))
            );
    }
}