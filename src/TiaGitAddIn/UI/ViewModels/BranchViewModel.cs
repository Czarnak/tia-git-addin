using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class BranchViewModel : ViewModelBase
    {
        private readonly IGitService gitService;
        private ObservableCollection<BranchInfo> branches = new ObservableCollection<BranchInfo>();
        private BranchInfo? selectedBranch;
        private string newBranchName = string.Empty;
        private string lastOperationMessage = string.Empty;
        private bool isBusy;

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
                    InvokeOnUI(() => ((AsyncCommand)SwitchBranchCommand).RaiseCanExecuteChanged());
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
                    InvokeOnUI(() => ((AsyncCommand)CreateBranchCommand).RaiseCanExecuteChanged());
                }
            }
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(lastOperationMessage, value ?? string.Empty, updated => lastOperationMessage = updated);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(isBusy, value, updated => isBusy = updated))
                {
                    RaiseCommandStates();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CreateBranchCommand { get; }
        public ICommand SwitchBranchCommand { get; }
        public ICommand FetchCommand { get; }
        public ICommand PullCommand { get; }
        public ICommand PushCommand { get; }

        public async Task RefreshAsync()
        {
            IsBusy = true;
            try
            {
                var branchList = await gitService.GetBranchesAsync().ConfigureAwait(false);
                InvokeOnUI(() =>
                {
                    Branches = new ObservableCollection<BranchInfo>(branchList);
                    SelectedBranch = Branches.FirstOrDefault(b => b.IsCurrent) ?? Branches.FirstOrDefault();
                });
            }
            finally { IsBusy = false; }
        }

        private async Task CreateBranchAsync()
        {
            IsBusy = true;
            try
            {
                var result = await gitService.CreateBranchAsync(NewBranchName).ConfigureAwait(false);
                InvokeOnUI(() =>
                {
                    LastOperationMessage = BuildOperationMessage(result);
                    if (result.Success)
                    {
                        NewBranchName = string.Empty;
                    }
                });
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            finally { IsBusy = false; }
        }

        private async Task SwitchBranchAsync()
        {
            if (SelectedBranch == null) return;
            IsBusy = true;
            try
            {
                var result = await gitService.SwitchBranchAsync(SelectedBranch.Name).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = BuildOperationMessage(result));
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            finally { IsBusy = false; }
        }

        private async Task FetchAsync()
        {
            IsBusy = true;
            try
            {
                var result = await gitService.FetchAsync().ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = BuildOperationMessage(result));
            }
            finally { IsBusy = false; }
        }

        private async Task PullAsync()
        {
            IsBusy = true;
            try
            {
                var result = await gitService.PullAsync().ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = BuildOperationMessage(result));
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            finally { IsBusy = false; }
        }

        private async Task PushAsync()
        {
            IsBusy = true;
            try
            {
                var result = await gitService.PushAsync().ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = BuildOperationMessage(result));
            }
            finally { IsBusy = false; }
        }

        private static string BuildOperationMessage(OperationResult result)
        {
            return string.IsNullOrWhiteSpace(result.Detail) ? result.Message : $"{result.Message} {result.Detail}";
        }

        private void RaiseCommandStates()
        {
            InvokeOnUI(() =>
            {
                ((AsyncCommand)RefreshCommand).RaiseCanExecuteChanged();
                ((AsyncCommand)CreateBranchCommand).RaiseCanExecuteChanged();
                ((AsyncCommand)SwitchBranchCommand).RaiseCanExecuteChanged();
                ((AsyncCommand)FetchCommand).RaiseCanExecuteChanged();
                ((AsyncCommand)PullCommand).RaiseCanExecuteChanged();
                ((AsyncCommand)PushCommand).RaiseCanExecuteChanged();
            });
        }

    }
}
