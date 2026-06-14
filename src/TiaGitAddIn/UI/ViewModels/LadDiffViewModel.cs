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
        private readonly IGitFileExtractor gitFileExtractor;
        private readonly ISactService sactService;
        private readonly IAddInLogger logger;

        private bool isLadDiffLoaded;
        private string ladDiffError = string.Empty;
        private string interfaceTitle = string.Empty;

        public LadDiffViewModel(IGitFileExtractor gitFileExtractor, ISactService sactService, IAddInLogger logger, IUiDispatcher? uiDispatcher)
            : base(uiDispatcher)
        {
            this.gitFileExtractor = gitFileExtractor;
            this.sactService = sactService;
            this.logger = logger;
            Networks = new ObservableCollection<LadNetworkPairViewModel>();
            InterfaceRows = new ObservableCollection<LadInterfaceRowViewModel>();
        }

        public ObservableCollection<LadNetworkPairViewModel> Networks { get; }
        public ObservableCollection<LadInterfaceRowViewModel> InterfaceRows { get; }

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

        public bool IsSactAvailable => sactService.IsAvailable;

        public string InterfaceTitle
        {
            get => interfaceTitle;
            set => SetProperty(ref interfaceTitle, value);
        }

        public async Task LoadLadDiffAsync(string? commitHash, string filePath, CancellationToken ct)
        {
            logger.Info($"LoadLadDiffAsync started for {filePath} at commit {commitHash ?? "WORKING_TREE"}");

            if (!IsSactAvailable)
            {
                logger.Info("LoadLadDiffAsync: SACT is not available.");
                LadDiffError = "SACT not installed — install SIMATIC Automation Compare Tool for visual LAD diff.";
                IsLadDiffLoaded = false;
                return;
            }

            IsBusy = true;
            BusyMessage = "Loading LAD diff...";
            LadDiffError = string.Empty;

            InvokeOnUI(() =>
            {
                Networks.Clear();
                InterfaceRows.Clear();
            });

            string? rightTempPath = null;
            string? leftTempPath = null;

            try
            {
                rightTempPath = await gitFileExtractor.ExtractFileAsync(commitHash, filePath, ct).ConfigureAwait(false);
                leftTempPath = await gitFileExtractor.ExtractParentFileAsync(commitHash, filePath, ct).ConfigureAwait(false);

                logger.Info("LoadLadDiffAsync: Invoking SACT CompareAsync...");
                SactCompareResult? sactResult = await sactService.CompareAsync(leftTempPath, rightTempPath, ct).ConfigureAwait(true);

                if (sactResult == null)
                {
                    logger.Info("LoadLadDiffAsync: CompareAsync returned null. Setting error message.");
                    LadDiffError = "Failed to parse visual logic graph. Please fall back to text view.";
                    IsLadDiffLoaded = false;
                    return;
                }

                logger.Info("LoadLadDiffAsync: CompareAsync succeeded. Generating layouts.");
                var netPairs = LadLayoutEngine.LayoutAll(sactResult);

                InterfaceTitle = GetInterfaceTitle(sactResult);
                PopulateInterfaceRows(sactResult.Interface?.Members);
                PopulateNetworks(netPairs);
                IsLadDiffLoaded = true;
                logger.Info($"LoadLadDiffAsync: Completed successfully. Generated {netPairs.Count} network pairs.");
            }
            catch (Exception ex)
            {
                logger.Error($"LoadLadDiffAsync: Exception caught: {ex.Message}", ex);
                LadDiffError = $"Error computing visual diff: {ex.Message}";
                IsLadDiffLoaded = false;
            }
            finally
            {
                CleanupTempFile(leftTempPath);
                CleanupTempFile(rightTempPath);
                IsBusy = false;
            }
        }

        private void PopulateNetworks(List<Models.Lad.LadNetworkPairLayout> layouts)
        {
            InvokeOnUI(() =>
            {
                Networks.Clear();
                foreach (var layout in layouts)
                {
                    Networks.Add(new LadNetworkPairViewModel(layout));
                }
            });
        }

        private void PopulateInterfaceRows(List<SactInterfaceMemberComparison>? rows)
        {
            InvokeOnUI(() =>
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
            });
        }

        public void Clear()
        {
            IsLadDiffLoaded = false;
            LadDiffError = string.Empty;
            InterfaceTitle = string.Empty;

            InvokeOnUI(() =>
            {
                Networks.Clear();
                InterfaceRows.Clear();
            });
        }

        private static void CleanupTempFile(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // Only delete genuine temp files; working-tree diffs return the real repo file path.
            if (File.Exists(path) && path.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
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

        private static List<LadInterfaceRowViewModel> CreateInterfaceRows(List<SactInterfaceMemberComparison> rows)
        {
            var orderedRows = new List<LadInterfaceRowViewModel>();
            var rowsBySection = rows
                .GroupBy(r => r.Section)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var sectionNames = SactInterfaceSections.Order
                .Concat(rows.Select(r => r.Section).Where(s => !SactInterfaceSections.Order.Contains(s, StringComparer.OrdinalIgnoreCase)))
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
