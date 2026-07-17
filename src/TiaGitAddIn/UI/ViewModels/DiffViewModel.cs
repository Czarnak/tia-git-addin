using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Revision;
using TiaGitAddIn.UI.Mapping;
using TiaGitAddIn.UI.ViewModels.Comparison;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class DiffViewModel : ViewModelBase
    {
        private readonly IGitService gitService;
        private readonly ComparisonSelectionCoordinator selectionCoordinator;
        private ObservableCollection<DiffEntryViewModel> entries = new();
        private DiffEntryViewModel? selectedEntry;
        private string lastOperationMessage = string.Empty;
        private string? currentDiffCommitHash;
        private ComparisonPresentationViewModel? currentPresentation;

        public DiffViewModel(
            IGitService gitService,
            IPlcRevisionProvider revisionProvider,
            IPlcComparisonCoordinator comparisonCoordinator,
            IComparisonPresentationMapper mapper,
            IAddInLogger? logger = null,
            IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            if (revisionProvider == null) throw new ArgumentNullException(nameof(revisionProvider));
            if (comparisonCoordinator == null) throw new ArgumentNullException(nameof(comparisonCoordinator));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            IAddInLogger effectiveLogger = logger ?? new NullLogger();
            selectionCoordinator = new ComparisonSelectionCoordinator(
                revisionProvider,
                comparisonCoordinator,
                mapper,
                UiDispatcher,
                viewModel => CurrentPresentation = viewModel,
                effectiveLogger);

            CancelCommand = new RelayCommand(_ => RequestCancel(), _ => IsBusy);
        }

        private sealed class NullLogger : IAddInLogger
        {
            public void Error(string message, Exception exception) { }
            public void Info(string message) { }
        }

        public ObservableCollection<DiffEntryViewModel> Entries
        {
            get => entries;
            private set => SetProperty(entries, value, updated => entries = updated);
        }

        public DiffEntryViewModel? SelectedEntry
        {
            get => selectedEntry;
            set => SetProperty(selectedEntry, value, updated => selectedEntry = updated);
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(lastOperationMessage, value ?? string.Empty, updated => lastOperationMessage = updated);
        }

        /// <summary>The mapped presentation for the current selection; <c>null</c> until one is applied.</summary>
        public ComparisonPresentationViewModel? CurrentPresentation
        {
            get => currentPresentation;
            private set => SetProperty(currentPresentation, value, updated => currentPresentation = updated);
        }

        public RelayCommand CancelCommand { get; }

        public Task LoadCommitDiffAsync(string commitHash)
        {
            if (string.IsNullOrWhiteSpace(commitHash)) return Task.CompletedTask;

            return RunBusyAsync("Loading diff…", async ct =>
            {
                var result = await gitService.GetCommitDiffAsync(commitHash, ct).ConfigureAwait(false);
                currentDiffCommitHash = commitHash;
                ApplyDiffResult(result, $"Commit {commitHash.Substring(0, Math.Min(7, commitHash.Length))}");
            });
        }

        public Task LoadWorkingTreeDiffAsync() =>
            RunBusyAsync("Loading working tree diff…", async ct =>
            {
                var result = await gitService.GetWorkingTreeDiffAsync(ct).ConfigureAwait(false);
                currentDiffCommitHash = null;
                ApplyDiffResult(result, "Working tree");
            });

        /// <summary>
        /// The genuine async boundary for a file-list selection change. Routes the entry through the
        /// cancellation-safe selection coordinator (latest-selection-wins) and updates no property other
        /// than <see cref="CurrentPresentation"/>, from inside the coordinator's own dispatcher callback.
        /// </summary>
        public Task SelectEntryAsync(DiffEntryViewModel? entry, CancellationToken cancellationToken)
        {
            if (entry == null)
            {
                return Task.CompletedTask;
            }

            var selection = new ComparisonSelection(entry.FilePath, currentDiffCommitHash, ToChangeKind(entry.ChangeType));
            return selectionCoordinator.SelectAsync(selection, cancellationToken);
        }

        private static PlcPairChangeKind ToChangeKind(string? changeType) => changeType?.Trim().ToUpperInvariant() switch
        {
            "A" => PlcPairChangeKind.Added,
            "D" => PlcPairChangeKind.Removed,
            _ => PlcPairChangeKind.Modified,
        };

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

        protected override void ReportStatus(string message) => LastOperationMessage = message;

        protected override void OnBusyChanged()
        {
            InvokeOnUI(() => CancelCommand.RaiseCanExecuteChanged());
        }
    }

    public sealed class DiffEntryViewModel
    {
        public DiffEntryViewModel(DiffEntry entry)
        {
            Entry = entry;
            FilePath = entry.FilePath;
            FileName = Path.GetFileName(entry.FilePath);
            ChangeType = entry.ChangeType ?? string.Empty;
        }

        public DiffEntry Entry { get; }
        public string FilePath { get; }
        public string FileName { get; }
        public string ChangeType { get; }
    }
}
