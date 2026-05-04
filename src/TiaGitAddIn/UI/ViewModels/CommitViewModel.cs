using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class CommitViewModel : ViewModelBase
    {
        private readonly IGitService gitService;
        private readonly Func<Task> refreshStatusAsync;
        private string commitMessage = string.Empty;
        private string validationMessage = string.Empty;
        private string lastOperationMessage = string.Empty;
        private string busyMessage = string.Empty;
        private bool isBusy;
        private CancellationTokenSource? cts;

        public CommitViewModel(
            IGitService gitService,
            Func<Task> refreshStatusAsync,
            IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.refreshStatusAsync = refreshStatusAsync ?? throw new ArgumentNullException(nameof(refreshStatusAsync));
            CommitCommand = new AsyncCommand(CommitAsync, () => CanCommit);
            CancelCommand = new RelayCommand(_ => cts?.Cancel(), _ => IsBusy);
        }

        public string CommitMessage
        {
            get => commitMessage;
            set
            {
                if (SetProperty(commitMessage, value ?? string.Empty, updated => commitMessage = updated))
                {
                    ValidationMessage = string.Empty;
                    OnPropertyChanged(nameof(CanCommit));
                    ((AsyncCommand)CommitCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string ValidationMessage
        {
            get => validationMessage;
            private set => SetProperty(validationMessage, value, updated => validationMessage = updated);
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(lastOperationMessage, value, updated => lastOperationMessage = updated);
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
                    OnPropertyChanged(nameof(CanCommit));
                    ((AsyncCommand)CommitCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool CanCommit => !IsBusy && !string.IsNullOrWhiteSpace(CommitMessage);

        public ICommand CommitCommand { get; }
        public ICommand CancelCommand { get; }

        public async Task CommitAsync()
        {
            string message = CommitMessage.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                ValidationMessage = "Commit message is required.";
                OnPropertyChanged(nameof(CanCommit));
                ((AsyncCommand)CommitCommand).RaiseCanExecuteChanged();
                return;
            }

            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            IsBusy = true;
            BusyMessage = "Committing…";
            try
            {
                OperationResult result = await gitService.CommitAsync(message, ct).ConfigureAwait(true);
                LastOperationMessage = BuildOperationMessage(result);
                if (result.Success)
                {
                    CommitMessage = string.Empty;
                    await refreshStatusAsync().ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException)
            {
                LastOperationMessage = "Cancelled.";
            }
            catch (Exception ex)
            {
                LastOperationMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        private static string BuildOperationMessage(OperationResult result)
        {
            return string.IsNullOrWhiteSpace(result.Detail)
                ? result.Message
                : result.Message + " " + result.Detail;
        }
    }
}
