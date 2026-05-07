using System.Collections.ObjectModel;
using System.Linq;
using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadNetworkViewModel : ViewModelBase
    {
        private const double MinimumCellWidth = 210;
        private const double MinimumCellHeight = 180;
        private const double CellPaddingX = 90;
        private const double CellPaddingY = 60;

        public LadNetworkViewModel(LadNetworkLayout layout)
        {
            double cellWidth = CalculateCellWidth(layout);
            double cellHeight = CalculateCellHeight(layout);

            NetworkNumber = layout.NetworkNumber;
            DiffState = layout.DiffState;
            CanvasWidth = layout.ColumnCount * cellWidth;
            CanvasHeight = layout.RowCount * cellHeight;
            Elements = new ObservableCollection<LadElementViewModel>(
                layout.Elements.Select(e => new LadElementViewModel(e, cellWidth, cellHeight)));
            Wires = new ObservableCollection<LadWireViewModel>(
                layout.Wires.Select(w => new LadWireViewModel(w, cellWidth, cellHeight)));
        }

        public int NetworkNumber { get; }
        public CompareState DiffState { get; }

        public double CanvasWidth { get; }
        public double CanvasHeight { get; }

        public ObservableCollection<LadElementViewModel> Elements { get; }
        public ObservableCollection<LadWireViewModel> Wires { get; }

        private static double CalculateCellWidth(LadNetworkLayout layout)
        {
            double widestElement = layout.Elements
                .Select(e => e.Width)
                .DefaultIfEmpty(90)
                .Max();

            return System.Math.Max(MinimumCellWidth, widestElement + CellPaddingX);
        }

        private static double CalculateCellHeight(LadNetworkLayout layout)
        {
            double tallestElement = layout.Elements
                .Select(e => e.Height)
                .DefaultIfEmpty(60)
                .Max();

            return System.Math.Max(MinimumCellHeight, tallestElement + CellPaddingY);
        }
    }
}
