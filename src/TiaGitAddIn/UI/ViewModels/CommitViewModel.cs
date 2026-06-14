using System;
using System.Threading.Tasks;
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

        public CommitViewModel(
            IGitService gitService,
            Func<Task> refreshStatusAsync,
            IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            this.refreshStatusAsync = refreshStatusAsync ?? throw new ArgumentNullException(nameof(refreshStatusAsync));
            CommitCommand = new AsyncCommand(CommitAsync, () => CanCommit);
            CancelCommand = new RelayCommand(_ => RequestCancel(), _ => IsBusy);
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
                    CommitCommand.RaiseCanExecuteChanged();
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

        public bool CanCommit => !IsBusy && !string.IsNullOrWhiteSpace(CommitMessage);

        public AsyncCommand CommitCommand { get; }
        public RelayCommand CancelCommand { get; }

        public Task CommitAsync()
        {
            string message = CommitMessage.Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                ValidationMessage = "Commit message is required.";
                OnPropertyChanged(nameof(CanCommit));
                CommitCommand.RaiseCanExecuteChanged();
                return Task.CompletedTask;
            }

            return RunBusyAsync("Committing…", async ct =>
            {
                OperationResult result = await gitService.CommitAsync(message, ct).ConfigureAwait(false);
                InvokeOnUI(() => LastOperationMessage = result.DisplayMessage);
                if (result.Success)
                {
                    InvokeOnUI(() => CommitMessage = string.Empty);
                    await refreshStatusAsync().ConfigureAwait(false);
                }
            });
        }

        protected override void ReportStatus(string message) => LastOperationMessage = message;

        protected override void OnBusyChanged()
        {
            InvokeOnUI(() =>
            {
                OnPropertyChanged(nameof(CanCommit));
                CommitCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            });
        }
    }
}
