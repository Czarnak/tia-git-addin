using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class StatusViewModel : ViewModelBase
    {
        private readonly IGitService gitService;
        private ObservableCollection<FileStatusItemViewModel> stagedEntries = new();
        private ObservableCollection<FileStatusItemViewModel> unstagedEntries = new();
        private ObservableCollection<FileStatusItemViewModel> untrackedEntries = new();
        private ObservableCollection<FileStatusItemViewModel> allEntries = new();
        private string currentBranch = string.Empty;
        private string trackingSummary = string.Empty;
        private string statusSummary = "Status not loaded";
        private string lastOperationMessage = string.Empty;

        public StatusViewModel(IGitService gitService, IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            RefreshCommand = new AsyncCommand(() => RefreshAsync(), () => !IsBusy);
            StageSelectedCommand = new AsyncCommand(p => StageSelectedAsync(p), _ => !IsBusy);
            UnstageSelectedCommand = new AsyncCommand(p => UnstageSelectedAsync(p), _ => !IsBusy);
            StageAllCommand = new AsyncCommand(() => StageAllAsync(), () => !IsBusy);
            CancelCommand = new RelayCommand(_ => RequestCancel(), _ => IsBusy);
        }

        public ObservableCollection<FileStatusItemViewModel> StagedEntries
        {
            get => stagedEntries;
            private set => SetProperty(stagedEntries, value, updated => stagedEntries = updated);
        }

        public ObservableCollection<FileStatusItemViewModel> UnstagedEntries
        {
            get => unstagedEntries;
            private set => SetProperty(unstagedEntries, value, updated => unstagedEntries = updated);
        }

        public ObservableCollection<FileStatusItemViewModel> UntrackedEntries
        {
            get => untrackedEntries;
            private set => SetProperty(untrackedEntries, value, updated => untrackedEntries = updated);
        }

        public ObservableCollection<FileStatusItemViewModel> Entries
        {
            get => allEntries;
            private set => SetProperty(allEntries, value, updated => allEntries = updated);
        }

        public string CurrentBranch
        {
            get => currentBranch;
            private set => SetProperty(currentBranch, value ?? string.Empty, updated => currentBranch = updated);
        }

        public string TrackingSummary
        {
            get => trackingSummary;
            private set => SetProperty(trackingSummary, value ?? string.Empty, updated => trackingSummary = updated);
        }

        public string StatusSummary
        {
            get => statusSummary;
            private set => SetProperty(statusSummary, value ?? string.Empty, updated => statusSummary = updated);
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(lastOperationMessage, value ?? string.Empty, updated => lastOperationMessage = updated);
        }

        public AsyncCommand RefreshCommand { get; }
        public AsyncCommand StageSelectedCommand { get; }
        public AsyncCommand UnstageSelectedCommand { get; }
        public AsyncCommand StageAllCommand { get; }
        public RelayCommand CancelCommand { get; }

        public Task RefreshAsync() =>
            RunBusyAsync("Refreshing status…", LoadStatusCoreAsync);

        // Status load without the busy/cancel shell, so a post-operation refresh can reuse the
        // caller's RunBusyAsync scope and token instead of nesting another RunBusyAsync.
        private async Task LoadStatusCoreAsync(CancellationToken ct)
        {
            GitStatus status = await gitService.GetStatusAsync(ct).ConfigureAwait(false);
            InvokeOnUI(() =>
            {
                CurrentBranch = string.IsNullOrWhiteSpace(status.CurrentBranch) ? "(unknown)" : status.CurrentBranch;
                TrackingSummary = BuildTrackingSummary(status);
                StagedEntries = new ObservableCollection<FileStatusItemViewModel>(status.StagedEntries.Select(e => new FileStatusItemViewModel(e)));
                UnstagedEntries = new ObservableCollection<FileStatusItemViewModel>(status.UnstagedEntries.Select(e => new FileStatusItemViewModel(e)));
                UntrackedEntries = new ObservableCollection<FileStatusItemViewModel>(status.UntrackedEntries.Select(e => new FileStatusItemViewModel(e)));
                Entries = new ObservableCollection<FileStatusItemViewModel>(status.Entries.Select(e => new FileStatusItemViewModel(e)));
                StatusSummary = status.IsClean ? "Working tree clean" : $"{status.Entries.Count} changed files";
            });
        }

        public Task StageSelectedAsync(object? parameter) =>
            ExecuteOnSelectionAsync(parameter, "Staging files…", gitService.StageAsync);

        public Task UnstageSelectedAsync(object? parameter) =>
            ExecuteOnSelectionAsync(parameter, "Unstaging files…", gitService.UnstageAsync);

        public Task StageAllAsync() =>
            RunBusyAsync("Staging all files…", async ct =>
            {
                OperationResult result = await gitService.StageAllAsync(ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = result.DisplayMessage);
                if (result.Success)
                {
                    await LoadStatusCoreAsync(ct).ConfigureAwait(false);
                }
            });

        private Task ExecuteOnSelectionAsync(
            object? parameter,
            string message,
            Func<IReadOnlyList<string>, CancellationToken, Task<OperationResult>> operation)
        {
            List<string>? paths = ResolveSelectedPaths(parameter);
            if (paths == null)
            {
                return Task.CompletedTask;
            }

            return RunBusyAsync(message, async ct =>
            {
                OperationResult result = await operation(paths, ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = result.DisplayMessage);
                if (result.Success)
                {
                    await LoadStatusCoreAsync(ct).ConfigureAwait(false);
                }
            });
        }

        private static List<string>? ResolveSelectedPaths(object? parameter)
        {
            IEnumerable<FileStatusItemViewModel>? selectedItems = parameter switch
            {
                FileStatusItemViewModel single => new[] { single },
                IEnumerable<FileStatusItemViewModel> multiple => multiple,
                System.Collections.IList list => list.Cast<FileStatusItemViewModel>(),
                _ => null
            };

            if (selectedItems == null)
            {
                return null;
            }

            List<string> paths = selectedItems.Select(e => e.FilePath).ToList();
            return paths.Count == 0 ? null : paths;
        }

        private static string BuildTrackingSummary(GitStatus status)
        {
            List<string> parts = new();
            if (!string.IsNullOrWhiteSpace(status.TrackingBranch)) parts.Add(status.TrackingBranch!);
            if (status.AheadBy > 0) parts.Add("ahead " + status.AheadBy);
            if (status.BehindBy > 0) parts.Add("behind " + status.BehindBy);
            return string.Join(", ", parts);
        }

        protected override void ReportStatus(string message) => LastOperationMessage = message;

        protected override void OnBusyChanged()
        {
            InvokeOnUI(() =>
            {
                RefreshCommand.RaiseCanExecuteChanged();
                StageSelectedCommand.RaiseCanExecuteChanged();
                UnstageSelectedCommand.RaiseCanExecuteChanged();
                StageAllCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
