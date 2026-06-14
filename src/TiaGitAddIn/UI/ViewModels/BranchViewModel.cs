using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class BranchViewModel : ViewModelBase
    {
        private readonly IGitService gitService;
        private ObservableCollection<BranchInfo> branches = new();
        private BranchInfo? selectedBranch;
        private string newBranchName = string.Empty;
        private string lastOperationMessage = string.Empty;

        public BranchViewModel(IGitService gitService, IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));

            RefreshCommand = new AsyncCommand(() => RefreshAsync(), () => !IsBusy);
            CreateBranchCommand = new AsyncCommand(() => CreateBranchAsync(), () => !IsBusy && !string.IsNullOrWhiteSpace(NewBranchName));
            SwitchBranchCommand = new AsyncCommand(() => SwitchBranchAsync(), () => !IsBusy && SelectedBranch != null && !SelectedBranch.IsCurrent);
            FetchCommand = new AsyncCommand(() => FetchAsync(), () => !IsBusy);
            PullCommand = new AsyncCommand(() => PullAsync(), () => !IsBusy);
            PushCommand = new AsyncCommand(() => PushAsync(), () => !IsBusy);
            CancelCommand = new RelayCommand(_ => RequestCancel(), _ => IsBusy);
        }

        public ObservableCollection<BranchInfo> Branches
        {
            get => branches;
            private set => SetProperty(branches, value, updated => branches = updated);
        }

        public BranchInfo? SelectedBranch
        {
            get => selectedBranch;
            set
            {
                if (SetProperty(selectedBranch, value, updated => selectedBranch = updated))
                {
                    InvokeOnUI(() => SwitchBranchCommand.RaiseCanExecuteChanged());
                }
            }
        }

        public string NewBranchName
        {
            get => newBranchName;
            set
            {
                if (SetProperty(newBranchName, value ?? string.Empty, updated => newBranchName = updated))
                {
                    InvokeOnUI(() => CreateBranchCommand.RaiseCanExecuteChanged());
                }
            }
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(lastOperationMessage, value ?? string.Empty, updated => lastOperationMessage = updated);
        }

        public AsyncCommand RefreshCommand { get; }
        public AsyncCommand CreateBranchCommand { get; }
        public AsyncCommand SwitchBranchCommand { get; }
        public AsyncCommand FetchCommand { get; }
        public AsyncCommand PullCommand { get; }
        public AsyncCommand PushCommand { get; }
        public RelayCommand CancelCommand { get; }

        public Task RefreshAsync() =>
            RunBusyAsync("Loading branches…", LoadBranchesCoreAsync);

        // Branch load without the busy/cancel shell, so a post-operation refresh can reuse the
        // caller's RunBusyAsync scope and token instead of nesting another RunBusyAsync.
        private async Task LoadBranchesCoreAsync(CancellationToken ct)
        {
            var branchList = await gitService.GetBranchesAsync(ct).ConfigureAwait(false);
            InvokeOnUI(() =>
            {
                Branches = new ObservableCollection<BranchInfo>(branchList);
                SelectedBranch = Branches.FirstOrDefault(b => b.IsCurrent) ?? Branches.FirstOrDefault();
            });
        }

        private Task CreateBranchAsync() =>
            RunBusyAsync("Creating branch…", async ct =>
            {
                var result = await gitService.CreateBranchAsync(NewBranchName, ct).ConfigureAwait(false);
                InvokeOnUI(() =>
                {
                    LastOperationMessage = result.DisplayMessage;
                    if (result.Success) NewBranchName = string.Empty;
                });
                if (result.Success) await LoadBranchesCoreAsync(ct).ConfigureAwait(false);
            });

        private Task SwitchBranchAsync()
        {
            BranchInfo? target = SelectedBranch;
            if (target == null) return Task.CompletedTask;

            return RunBusyAsync($"Switching to {target.Name}…", async ct =>
            {
                var result = await gitService.SwitchBranchAsync(target.Name, ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = result.DisplayMessage);
                if (result.Success) await LoadBranchesCoreAsync(ct).ConfigureAwait(false);
            });
        }

        private Task FetchAsync() =>
            RunBusyAsync("Fetching…", async ct =>
            {
                var result = await gitService.FetchAsync(ct: ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = result.DisplayMessage);
            });

        private Task PullAsync() =>
            RunBusyAsync("Pulling…", async ct =>
            {
                var result = await gitService.PullAsync(ct: ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = result.DisplayMessage);
                if (result.Success) await LoadBranchesCoreAsync(ct).ConfigureAwait(false);
            });

        private Task PushAsync() =>
            RunBusyAsync("Pushing…", async ct =>
            {
                var result = await gitService.PushAsync(ct: ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = result.DisplayMessage);
            });

        protected override void ReportStatus(string message) => LastOperationMessage = message;

        protected override void OnBusyChanged()
        {
            InvokeOnUI(() =>
            {
                RefreshCommand.RaiseCanExecuteChanged();
                CreateBranchCommand.RaiseCanExecuteChanged();
                SwitchBranchCommand.RaiseCanExecuteChanged();
                FetchCommand.RaiseCanExecuteChanged();
                PullCommand.RaiseCanExecuteChanged();
                PushCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
