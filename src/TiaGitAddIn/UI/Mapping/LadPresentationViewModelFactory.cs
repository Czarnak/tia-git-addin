using System;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.UI.ViewModels;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.Mapping
{
    /// <summary>
    /// Maps a <see cref="LadPresentation"/> to <see cref="LadComparisonViewModel"/>: clones the legacy
    /// <c>SactCompareResult</c> exactly once via <see cref="LadPresentation.CreateLegacyResult"/>, maps
    /// the embedded <see cref="InterfacePresentation"/> through <see cref="InterfaceComparisonViewModel"/>,
    /// and constructs the result-only <see cref="LadDiffViewModel"/> from both.
    /// </summary>
    public sealed class LadPresentationViewModelFactory : IComparisonPresentationViewModelFactory
    {
        private readonly IAddInLogger logger;
        private readonly IUiDispatcher? uiDispatcher;

        public LadPresentationViewModelFactory(IAddInLogger logger, IUiDispatcher? uiDispatcher = null)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.uiDispatcher = uiDispatcher;
        }

        public bool CanMap(ComparisonPresentation presentation) => presentation is LadPresentation;

        public ComparisonPresentationViewModel Map(PlcComparisonResult result, ComparisonViewModelMetadata metadata)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var presentation = (LadPresentation)result.Presentation;
            SactCompareResult legacyResult = presentation.CreateLegacyResult();
            var interfaceViewModel = new InterfaceComparisonViewModel(presentation.Interface, metadata);
            var ladDiffViewModel = new LadDiffViewModel(legacyResult, interfaceViewModel, logger, uiDispatcher);

            return new LadComparisonViewModel(ladDiffViewModel, metadata);
        }
    }
}
