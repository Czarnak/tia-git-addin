using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
        private string busyMessage = string.Empty;
        private bool isBusy;
        private CancellationTokenSource? cts;

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
            CancelCommand = new RelayCommand(_ => cts?.Cancel(), _ => IsBusy);
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
        public ICommand CancelCommand { get; }

        public async Task RefreshAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Loading branches…";
            try
            {
                var branchList = await gitService.GetBranchesAsync(ct).ConfigureAwait(false);
                InvokeOnUI(() =>
                {
                    Branches = new ObservableCollection<BranchInfo>(branchList);
                    SelectedBranch = Branches.FirstOrDefault(b => b.IsCurrent) ?? Branches.FirstOrDefault();
                });
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private async Task CreateBranchAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Creating branch…";
            try
            {
                var result = await gitService.CreateBranchAsync(NewBranchName, ct).ConfigureAwait(false);
                InvokeOnUI(() =>
                {
                    LastOperationMessage = BuildOperationMessage(result);
                    if (result.Success) NewBranchName = string.Empty;
                });
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private async Task SwitchBranchAsync()
        {
            if (SelectedBranch == null) return;
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = $"Switching to {SelectedBranch.Name}…";
            try
            {
                var result = await gitService.SwitchBranchAsync(SelectedBranch.Name, ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = BuildOperationMessage(result));
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private async Task FetchAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Fetching…";
            try
            {
                var result = await gitService.FetchAsync(ct: ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = BuildOperationMessage(result));
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private async Task PullAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Pulling…";
            try
            {
                var result = await gitService.PullAsync(ct: ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = BuildOperationMessage(result));
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private async Task PushAsync()
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Pushing…";
            try
            {
                var result = await gitService.PushAsync(ct: ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = BuildOperationMessage(result));
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => LastOperationMessage = "Cancelled.");
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => LastOperationMessage = $"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
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
                ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
            });
        }
    }
}
