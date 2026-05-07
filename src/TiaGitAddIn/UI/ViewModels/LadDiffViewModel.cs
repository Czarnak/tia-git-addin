using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
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
        private string _interfaceTitle = string.Empty;
        private static readonly string[] InterfaceSectionOrder =
        {
            "Input",
            "Output",
            "InOut",
            "Temp",
            "Constant",
            "Return"
        };

        public LadDiffViewModel(IGitFileExtractor gitFileExtractor, ISactService sactService, IAddInLogger logger, IUiDispatcher? uiDispatcher)
        {
            _gitFileExtractor = gitFileExtractor;
            _sactService = sactService;
            _logger = logger;
            _uiDispatcher = uiDispatcher;
            Networks = new ObservableCollection<LadNetworkPairViewModel>();
            InterfaceRows = new ObservableCollection<LadInterfaceRowViewModel>();
        }

        public ObservableCollection<LadNetworkPairViewModel> Networks { get; }
        public ObservableCollection<LadInterfaceRowViewModel> InterfaceRows { get; }

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

        public string InterfaceTitle
        {
            get => _interfaceTitle;
            set => SetProperty(ref _interfaceTitle, value);
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
                _uiDispatcher.Invoke(() =>
                {
                    Networks.Clear();
                    InterfaceRows.Clear();
                });
            }
            else
            {
                Networks.Clear();
                InterfaceRows.Clear();
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
                
                InterfaceTitle = GetInterfaceTitle(sactResult);
                PopulateInterfaceRows(sactResult.Interface?.Members);
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

        private void PopulateInterfaceRows(List<SactInterfaceMemberComparison>? rows)
        {
            Action action = () =>
            {
                InterfaceRows.Clear();
                if (rows == null)
                {
                    return;
                }

                foreach (var row in CreateInterfaceRows(rows))
                {
                    InterfaceRows.Add(row);
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
            InterfaceTitle = string.Empty;

            if (_uiDispatcher != null)
            {
                _uiDispatcher.Invoke(() =>
                {
                    Networks.Clear();
                    InterfaceRows.Clear();
                });
            }
            else
            {
                Networks.Clear();
                InterfaceRows.Clear();
            }
        }

        private void CleanupTempFile(string? path)
        {
            string tempPath = path ?? string.Empty;
            if (tempPath.Length > 0)
            {
                // Only cleanup if it's actually a temp file (working tree diff uses actual file for rightTempPath)
                if (File.Exists(tempPath) && tempPath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
        }

        private static List<LadInterfaceRowViewModel> CreateInterfaceRows(List<SactInterfaceMemberComparison> rows)
        {
            var orderedRows = new List<LadInterfaceRowViewModel>();
            var rowsBySection = rows
                .GroupBy(r => r.Section)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var sectionNames = InterfaceSectionOrder
                .Concat(rows.Select(r => r.Section).Where(s => !InterfaceSectionOrder.Contains(s, StringComparer.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            int rowNumber = 1;
            foreach (string section in sectionNames)
            {
                orderedRows.Add(LadInterfaceRowViewModel.CreateSection(rowNumber++, section));
                if (!rowsBySection.TryGetValue(section, out var sectionRows))
                {
                    continue;
                }

                foreach (SactInterfaceMemberComparison member in sectionRows)
                {
                    orderedRows.Add(LadInterfaceRowViewModel.CreateMember(rowNumber++, member));
                }
            }

            return orderedRows;
        }

        private static string GetInterfaceTitle(SactCompareResult result)
        {
            if (!string.IsNullOrWhiteSpace(result.Right))
            {
                return result.Right;
            }

            if (!string.IsNullOrWhiteSpace(result.Left))
            {
                return result.Left;
            }

            return "Block interface";
        }
    }
}
