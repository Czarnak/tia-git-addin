using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class DiffViewModel : ViewModelBase
    {
        private readonly IGitService gitService;
        private IGitFileExtractor gitFileExtractor;
        private ISactService sactService;
        private IAddInLogger? logger;
        private ObservableCollection<DiffEntryViewModel> entries = new();
        private DiffEntryViewModel? selectedEntry;
        private ObservableCollection<DiffLineViewModel> displayedLines = new();
        private string lastOperationMessage = string.Empty;
        private string busyMessage = string.Empty;
        private bool isBusy;
        private bool showVisualDiff;
        private CancellationTokenSource? cts;

        public DiffViewModel(
            IGitService gitService,
            IGitFileExtractor gitFileExtractor,
            ISactService sactService,
            IAddInLogger? logger = null,
            IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.gitFileExtractor = gitFileExtractor ?? throw new ArgumentNullException(nameof(gitFileExtractor));
            this.sactService = sactService ?? throw new ArgumentNullException(nameof(sactService));
            this.logger = logger;
            LadDiff = new LadDiffViewModel(gitFileExtractor, sactService, logger ?? new NullLogger(), uiDispatcher);
            LoadWorkingTreeCommand = new AsyncCommand(() => LoadWorkingTreeDiffAsync(), () => !IsBusy);
            CancelCommand = new RelayCommand(_ => cts?.Cancel(), _ => IsBusy);
            ToggleVisualCommand = new RelayCommand(_ => ShowVisualDiff = !ShowVisualDiff, _ => SelectedEntry?.IsTiaArtifact ?? false);
        }

        private sealed class NullLogger : IAddInLogger
        {
            public void Error(string message, Exception exception) { }
            public void Info(string message) { }
        }

        public LadDiffViewModel LadDiff { get; }

        public ObservableCollection<DiffEntryViewModel> Entries
        {
            get => entries;
            private set => SetProperty(entries, value, updated => entries = updated);
        }

        public DiffEntryViewModel? SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (SetProperty(selectedEntry, value, updated => selectedEntry = updated))
                {
                    BuildDisplayedLines(value);
                    UpdateLadDiff(value);
                    ((RelayCommand)ToggleVisualCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool ShowVisualDiff
        {
            get => showVisualDiff;
            set => SetProperty(showVisualDiff, value, updated => showVisualDiff = updated);
        }

        public ICommand ToggleVisualCommand { get; }

        public ObservableCollection<DiffLineViewModel> DisplayedLines
        {
            get => displayedLines;
            private set => SetProperty(displayedLines, value, updated => displayedLines = updated);
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(lastOperationMessage, value ?? string.Empty, updated => lastOperationMessage = updated);
        }

        public string BusyMessage
        {
            get => busyMessage;
            private set => SetProperty(busyMessage, value ?? string.Empty, updated => busyMessage = updated);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(isBusy, value, updated => isBusy = updated))
                {
                    InvokeOnUI(() =>
                    {
                        ((AsyncCommand)LoadWorkingTreeCommand).RaiseCanExecuteChanged();
                        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
                    });
                }
            }
        }

        public ICommand LoadWorkingTreeCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task LoadCommitDiffAsync(string commitHash)
        {
            if (string.IsNullOrWhiteSpace(commitHash)) return;
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Loading diff…";
            try
            {
                var result = await gitService.GetCommitDiffAsync(commitHash, ct).ConfigureAwait(false);
                ApplyDiffResult(result, $"Commit {commitHash.Substring(0, Math.Min(7, commitHash.Length))}");
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error loading diff: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        public async Task LoadWorkingTreeDiffAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Loading working tree diff…";
            try
            {
                var result = await gitService.GetWorkingTreeDiffAsync(ct).ConfigureAwait(false);
                ApplyDiffResult(result, "Working tree");
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error loading diff: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private void ApplyDiffResult(DiffResult result, string title)
        {
            InvokeOnUI(() =>
            {
                Entries = new ObservableCollection<DiffEntryViewModel>(
                    result.Entries.Select(e => new DiffEntryViewModel(e)));
                SelectedEntry = Entries.FirstOrDefault();
                LastOperationMessage = $"{title} · {result.Entries.Count} file(s) changed.";
            });
        }

        private void BuildDisplayedLines(DiffEntryViewModel? entry)
        {
            if (entry == null)
            {
                InvokeOnUI(() => DisplayedLines = new ObservableCollection<DiffLineViewModel>());
                return;
            }

            List<DiffLineViewModel> lines = new();
            foreach (var hunk in entry.Entry.Hunks)
            {
                lines.Add(DiffLineViewModel.FromHunkHeader(hunk.Header));
                foreach (var line in hunk.Lines)
                {
                    lines.Add(DiffLineViewModel.FromLine(line));
                }
            }
            InvokeOnUI(() => DisplayedLines = new ObservableCollection<DiffLineViewModel>(lines));
        }

        private async void UpdateLadDiff(DiffEntryViewModel? entry)
        {
            if (entry == null || !entry.IsTiaArtifact)
            {
                LadDiff.Clear();
                ShowVisualDiff = false;
                return;
            }

            // Always try to load if it's a TIA artifact
            // We use the last loaded commit hash or null for working tree
            string? currentCommit = lastOperationMessage.StartsWith("Commit") 
                ? lastOperationMessage.Split(' ')[1] 
                : null;

            await LadDiff.LoadLadDiffAsync(currentCommit, entry.FilePath, CancellationToken.None).ConfigureAwait(false);
            
            // Auto-switch to visual if successfully loaded and it's a LAD/FBD block
            if (LadDiff.IsLadDiffLoaded)
            {
                ShowVisualDiff = true;
            }
        }
    }

    public sealed class DiffEntryViewModel
    {
        private static readonly HashSet<string> TiaXmlExtensions = new(StringComparer.OrdinalIgnoreCase)
            { ".xml", ".db", ".fc", ".fb", ".ob", ".sdb", ".udt" };

        private static readonly string[] TiaPathKeywords =
            ["Program blocks", "Function blocks", "Data blocks", "Safety blocks", "NetworkSource", "LAD", "FBD", "SFC"];

        public DiffEntryViewModel(DiffEntry entry)
        {
            Entry = entry;
            FilePath = entry.FilePath;
            FileName = Path.GetFileName(entry.FilePath);
            ChangeType = entry.ChangeType ?? string.Empty;
            IsTiaArtifact = DetectTiaArtifact(entry.FilePath);
            TiaSummary = IsTiaArtifact ? BuildTiaSummary(entry) : string.Empty;
        }

        public DiffEntry Entry { get; }
        public string FilePath { get; }
        public string FileName { get; }
        public string ChangeType { get; }
        public bool IsTiaArtifact { get; }
        public string TiaSummary { get; }

        private static bool DetectTiaArtifact(string path)
        {
            string ext = Path.GetExtension(path);
            if (string.Equals(ext, ".xml", StringComparison.OrdinalIgnoreCase)) return true;
            if (!TiaXmlExtensions.Contains(ext)) return false;
            foreach (string keyword in TiaPathKeywords)
            {
                if (path.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private static string BuildTiaSummary(DiffEntry entry)
        {
            int added = entry.Hunks.Sum(h => h.Lines.Count(l => l.Type == DiffLineType.Added));
            int deleted = entry.Hunks.Sum(h => h.Lines.Count(l => l.Type == DiffLineType.Deleted));
            string language = DetectLanguage(entry.FilePath);
            string langLabel = string.IsNullOrEmpty(language) ? "TIA artifact" : language;
            return $"{langLabel} · +{added} / −{deleted} lines (heuristic)";
        }

        private static string DetectLanguage(string path)
        {
            if (path.IndexOf("LAD", StringComparison.OrdinalIgnoreCase) >= 0) return "LAD";
            if (path.IndexOf("FBD", StringComparison.OrdinalIgnoreCase) >= 0) return "FBD";
            if (path.IndexOf("SFC", StringComparison.OrdinalIgnoreCase) >= 0) return "SFC";
            if (path.IndexOf("SCL", StringComparison.OrdinalIgnoreCase) >= 0) return "SCL";
            if (path.IndexOf("STL", StringComparison.OrdinalIgnoreCase) >= 0) return "STL";
            return string.Empty;
        }
    }

    public sealed class DiffLineViewModel
    {
        private DiffLineViewModel() { }

        public static DiffLineViewModel FromLine(DiffLine line) =>
            new()
            {
                LineType = line.Type,
                Content = line.Content ?? string.Empty,
                OldLineNumberText = line.OldLineNumber?.ToString() ?? string.Empty,
                NewLineNumberText = line.NewLineNumber?.ToString() ?? string.Empty
            };

        public static DiffLineViewModel FromHunkHeader(string header) =>
            new()
            {
                LineType = DiffLineType.Header,
                Content = header ?? string.Empty,
                OldLineNumberText = string.Empty,
                NewLineNumberText = string.Empty
            };

        public DiffLineType LineType { get; private set; }
        public string Content { get; private set; } = string.Empty;
        public string OldLineNumberText { get; private set; } = string.Empty;
        public string NewLineNumberText { get; private set; } = string.Empty;
    }
}
