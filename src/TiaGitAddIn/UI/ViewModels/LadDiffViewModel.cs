using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Services;

namespace TiaGitAddIn.UI.ViewModels
{
    public class LadDiffViewModel : ViewModelBase
    {
        private readonly IGitFileExtractor _gitFileExtractor;
        private readonly ISactService _sactService;
        private readonly IAddInLogger _logger;
        private readonly IUiDispatcher? _uiDispatcher;

        private bool _isLadDiffLoaded;
        private string _ladDiffError = string.Empty;
        private bool _isBusy;
        private string _busyMessage = string.Empty;

        public LadDiffViewModel(IGitFileExtractor gitFileExtractor, ISactService sactService, IAddInLogger logger, IUiDispatcher? uiDispatcher)
        {
            _gitFileExtractor = gitFileExtractor;
            _sactService = sactService;
            _logger = logger;
            _uiDispatcher = uiDispatcher;
            Networks = new ObservableCollection<LadNetworkPairViewModel>();
        }

        public ObservableCollection<LadNetworkPairViewModel> Networks { get; }

        public bool IsLadDiffLoaded
        {
            get => _isLadDiffLoaded;
            set => SetProperty(ref _isLadDiffLoaded, value);
        }

        public string LadDiffError
        {
            get => _ladDiffError;
            set => SetProperty(ref _ladDiffError, value);
        }

        public bool IsSactAvailable => _sactService.IsAvailable;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string BusyMessage
        {
            get => _busyMessage;
            set => SetProperty(ref _busyMessage, value);
        }

        public async Task LoadLadDiffAsync(string? commitHash, string filePath, CancellationToken ct)
        {
            _logger.Info($"LoadLadDiffAsync started for {filePath} at commit {commitHash ?? "WORKING_TREE"}");

            if (!IsSactAvailable)
            {
                _logger.Info("LoadLadDiffAsync: SACT is not available.");
                LadDiffError = "SACT not installed \u2014 install SIMATIC Automation Compare Tool for visual LAD diff.";
                IsLadDiffLoaded = false;
                return;
            }

            IsBusy = true;
            BusyMessage = "Loading LAD diff...";
            LadDiffError = string.Empty;

            if (_uiDispatcher != null)
            {
                _uiDispatcher.Invoke(() => Networks.Clear());
            }
            else
            {
                Networks.Clear();
            }

            string rightTempPath = string.Empty;
            string? leftTempPath = null;

            try
            {
                rightTempPath = await _gitFileExtractor.ExtractFileAsync(commitHash, filePath, ct).ConfigureAwait(false);
                leftTempPath = await _gitFileExtractor.ExtractParentFileAsync(commitHash, filePath, ct).ConfigureAwait(false);

                _logger.Info("LoadLadDiffAsync: Invoking SACT CompareAsync...");
                SactCompareResult? sactResult = await _sactService.CompareAsync(leftTempPath, rightTempPath, ct).ConfigureAwait(true);

                if (sactResult == null)
                {
                    _logger.Info("LoadLadDiffAsync: CompareAsync returned null. Setting error message.");
                    LadDiffError = "Failed to parse visual logic graph or timeout occurred. Please fall back to text view.";
                    IsLadDiffLoaded = false;
                    return;
                }

                _logger.Info("LoadLadDiffAsync: CompareAsync succeeded. Generating layouts.");
                var netPairs = LadLayoutEngine.LayoutAll(sactResult);
                
                PopulateNetworks(netPairs);
                IsLadDiffLoaded = true;
                _logger.Info($"LoadLadDiffAsync: Completed successfully. Generated {netPairs.Count} network pairs.");
            }
            catch (Exception ex)
            {
                _logger.Error($"LoadLadDiffAsync: Exception caught: {ex.Message}", ex);
                LadDiffError = $"Error computing visual diff: {ex.Message}";
                IsLadDiffLoaded = false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void PopulateNetworks(List<Models.Lad.LadNetworkPairLayout> layouts)
        {
            Action action = () =>
            {
                Networks.Clear();
                foreach (var layout in layouts)
                {
                    Networks.Add(new LadNetworkPairViewModel(layout));
                }
            };

            if (_uiDispatcher != null)
            {
                _uiDispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        public void Clear()
        {
            IsLadDiffLoaded = false;
            LadDiffError = string.Empty;

            if (_uiDispatcher != null)
            {
                _uiDispatcher.Invoke(() => Networks.Clear());
            }
            else
            {
                Networks.Clear();
            }
        }

        private void CleanupTempFile(string? path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                // Only cleanup if it's actually a temp file (working tree diff uses actual file for rightTempPath)
                if (path.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }
    }
}