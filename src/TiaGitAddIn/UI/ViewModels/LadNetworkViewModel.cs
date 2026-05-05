using System.Collections.ObjectModel;
using System.Linq;
using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadNetworkViewModel : ViewModelBase
    {
        private const double CellWidth = 120;
        private const double CellHeight = 60;
        private readonly LadNetworkLayout _layout;

        public LadNetworkViewModel(LadNetworkLayout layout)
        {
            _layout = layout;
            
            CanvasWidth = layout.ColumnCount * CellWidth;
            CanvasHeight = layout.RowCount * CellHeight;
            
            Elements = new ObservableCollection<LadElementViewModel>(
                layout.Elements.Select(e => new LadElementViewModel(e, CellWidth, CellHeight))
            );
            Wires = new ObservableCollection<LadWireViewModel>(
                layout.Wires.Select(w => new LadWireViewModel(w, CellWidth, CellHeight))
            );
        }

        public int NetworkNumber => _layout.NetworkNumber;
        public CompareState DiffState => _layout.DiffState;
        
        public double CanvasWidth { get; }
        public double CanvasHeight { get; }

        public ObservableCollection<LadElementViewModel> Elements { get; }
        public ObservableCollection<LadWireViewModel> Wires { get; }
    }
}