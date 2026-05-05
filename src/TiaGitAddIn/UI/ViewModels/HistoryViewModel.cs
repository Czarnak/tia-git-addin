using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
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
        private string busyMessage = string.Empty;
        private bool isBusy;
        private CancellationTokenSource? cts;
        private CancellationTokenSource? changedFilesCts;

        public HistoryViewModel(IGitService gitService, IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            RefreshCommand = new AsyncCommand(() => RefreshAsync(), () => !IsBusy);
            CancelCommand = new RelayCommand(_ => cts?.Cancel(), _ => IsBusy);
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
                        ((AsyncCommand)RefreshCommand).RaiseCanExecuteChanged();
                        ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
                    });
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task RefreshAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Loading history…";
            try
            {
                var log = await gitService.GetCommitLogAsync(DefaultMaxCount, ct).ConfigureAwait(false);
                InvokeOnUI(() =>
                {
                    Commits = new ObservableCollection<CommitInfo>(log);
                    SelectedCommit = null;
                    ChangedFiles = new ObservableCollection<string>();
                    LastOperationMessage = $"Loaded {log.Count} commits.";
                });
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error loading history: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

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
    }
}
