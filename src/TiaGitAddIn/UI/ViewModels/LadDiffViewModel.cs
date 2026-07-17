using System;
using System.Collections.ObjectModel;
using System.Linq;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.ViewModels
{
    /// <summary>
    /// Result-only LAD block view model, built by <see cref="Mapping.LadPresentationViewModelFactory"/>
    /// directly from an already-produced <c>SactCompareResult</c> clone and
    /// <see cref="InterfaceComparisonViewModel"/>. It has no git/SACT dependency: the network layout and
    /// interface comparison are computed once, in the constructor, from data the coordinator/mapper pipeline
    /// already loaded and compared.
    /// </summary>
    public class LadDiffViewModel : ViewModelBase
    {
        private readonly IAddInLogger logger;

        private bool isLadDiffLoaded;
        private string ladDiffError = string.Empty;

        public LadDiffViewModel(SactCompareResult result, InterfaceComparisonViewModel interfaceComparison,
            IAddInLogger logger, IUiDispatcher? uiDispatcher)
            : base(uiDispatcher)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Networks = new ObservableCollection<LadNetworkPairViewModel>(
                LadLayoutEngine.LayoutAll(result).Select(layout => new LadNetworkPairViewModel(layout)));
            InterfaceComparison = interfaceComparison ?? throw new ArgumentNullException(nameof(interfaceComparison));
            IsLadDiffLoaded = true;
        }

        public ObservableCollection<LadNetworkPairViewModel> Networks { get; }

        /// <summary>The deep interface comparison for this LAD block.</summary>
        public InterfaceComparisonViewModel InterfaceComparison { get; }

        public bool IsLadDiffLoaded
        {
            get => isLadDiffLoaded;
            set => SetProperty(ref isLadDiffLoaded, value);
        }

        public string LadDiffError
        {
            get => ladDiffError;
            set => SetProperty(ref ladDiffError, value);
        }
    }
}
