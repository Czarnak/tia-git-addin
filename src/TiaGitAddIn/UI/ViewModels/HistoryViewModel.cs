using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class HistoryViewModel : ViewModelBase
    {
        private const int DefaultMaxCount = 100;

        private readonly IGitService gitService;
        private ObservableCollection<CommitInfo> commits = new();
        private CommitInfo? selectedCommit;
        private ObservableCollection<string> changedFiles = new();
        private string lastOperationMessage = string.Empty;
        private CancellationTokenSource? changedFilesCts;

        public HistoryViewModel(IGitService gitService, IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            RefreshCommand = new AsyncCommand(() => RefreshAsync(), () => !IsBusy);
            CancelCommand = new RelayCommand(_ => RequestCancel(), _ => IsBusy);
        }

        public ObservableCollection<CommitInfo> Commits
        {
            get => commits;
            private set => SetProperty(commits, value, updated => commits = updated);
        }

        public CommitInfo? SelectedCommit
        {
            get => selectedCommit;
            set
            {
                if (SetProperty(selectedCommit, value, updated => selectedCommit = updated))
                {
                    LoadChangedFilesAsync(value);
                }
            }
        }

        public ObservableCollection<string> ChangedFiles
        {
            get => changedFiles;
            private set => SetProperty(changedFiles, value, updated => changedFiles = updated);
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(lastOperationMessage, value ?? string.Empty, updated => lastOperationMessage = updated);
        }

        public AsyncCommand RefreshCommand { get; }
        public RelayCommand CancelCommand { get; }

        public Task RefreshAsync() =>
            RunBusyAsync("Loading history…", async ct =>
            {
                var log = await gitService.GetCommitLogAsync(DefaultMaxCount, ct).ConfigureAwait(false);
                InvokeOnUI(() =>
                {
                    Commits = new ObservableCollection<CommitInfo>(log);
                    SelectedCommit = null;
                    ChangedFiles = new ObservableCollection<string>();
                    LastOperationMessage = $"Loaded {log.Count} commits.";
                });
            });

        private async void LoadChangedFilesAsync(CommitInfo? commit)
        {
            changedFilesCts?.Cancel();

            if (commit == null)
            {
                InvokeOnUI(() => ChangedFiles = new ObservableCollection<string>());
                return;
            }

            changedFilesCts = new CancellationTokenSource();
            var ct = changedFilesCts.Token;

            try
            {
                var files = await gitService.GetCommitFilesAsync(commit.Hash, ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();

                InvokeOnUI(() =>
                {
                    if (SelectedCommit?.Hash == commit.Hash)
                        ChangedFiles = new ObservableCollection<string>(files);
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error loading changed files: {ex.Message}");
            }
        }

        protected override void ReportStatus(string message) => LastOperationMessage = message;

        protected override void OnBusyChanged()
        {
            InvokeOnUI(() =>
            {
                RefreshCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
