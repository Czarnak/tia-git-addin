using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class StatusViewModel : ViewModelBase
    {
        private readonly IGitService gitService;
        private IReadOnlyList<FileStatusItemViewModel> entries =
            Array.Empty<FileStatusItemViewModel>();
        private FileStatusItemViewModel? selectedEntry;
        private string currentBranch = string.Empty;
        private string trackingSummary = string.Empty;
        private string statusSummary = "Status not loaded";
        private string lastOperationMessage = string.Empty;
        private bool isBusy;

        public StatusViewModel(IGitService gitService)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
            StageSelectedCommand = new AsyncCommand(StageSelectedAsync, () => CanStageSelected);
            UnstageSelectedCommand = new AsyncCommand(UnstageSelectedAsync, () => CanUnstageSelected);
        }

        public IReadOnlyList<FileStatusItemViewModel> Entries
        {
            get => entries;
            private set => SetProperty(entries, value, updated => entries = updated);
        }

        public FileStatusItemViewModel? SelectedEntry
        {
            get => selectedEntry;
            set
            {
                if (SetProperty(selectedEntry, value, updated => selectedEntry = updated))
                {
                    OnPropertyChanged(nameof(CanStageSelected));
                    OnPropertyChanged(nameof(CanUnstageSelected));
                    RaiseCommandStates();
                }
            }
        }

        public string CurrentBranch
        {
            get => currentBranch;
            private set => SetProperty(currentBranch, value, updated => currentBranch = updated);
        }

        public string TrackingSummary
        {
            get => trackingSummary;
            private set => SetProperty(trackingSummary, value, updated => trackingSummary = updated);
        }

        public string StatusSummary
        {
            get => statusSummary;
            private set => SetProperty(statusSummary, value, updated => statusSummary = updated);
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(lastOperationMessage, value, updated => lastOperationMessage = updated);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(isBusy, value, updated => isBusy = updated))
                {
                    OnPropertyChanged(nameof(CanStageSelected));
                    OnPropertyChanged(nameof(CanUnstageSelected));
                    RaiseCommandStates();
                }
            }
        }

        public bool CanStageSelected => !IsBusy && SelectedEntry?.CanStage == true;

        public bool CanUnstageSelected => !IsBusy && SelectedEntry?.CanUnstage == true;

        public ICommand RefreshCommand { get; }

        public ICommand StageSelectedCommand { get; }

        public ICommand UnstageSelectedCommand { get; }

        public async Task RefreshAsync()
        {
            IsBusy = true;
            try
            {
                GitStatus status = await gitService.GetStatusAsync().ConfigureAwait(true);
                CurrentBranch = string.IsNullOrWhiteSpace(status.CurrentBranch)
                    ? "(unknown)"
                    : status.CurrentBranch;
                TrackingSummary = BuildTrackingSummary(status);
                Entries = status.Entries.Select(entry => new FileStatusItemViewModel(entry)).ToList();
                SelectedEntry = Entries.FirstOrDefault();
                StatusSummary = status.IsClean ? "Working tree clean" : $"{status.Entries.Count} changed files";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task StageSelectedAsync()
        {
            FileStatusItemViewModel? entry = SelectedEntry;
            if (entry == null || !entry.CanStage)
            {
                return;
            }

            OperationResult result = await gitService.StageAsync(new[] { entry.FilePath }).ConfigureAwait(true);
            LastOperationMessage = BuildOperationMessage(result);
            if (result.Success)
            {
                await RefreshAsync().ConfigureAwait(true);
            }
        }

        public async Task UnstageSelectedAsync()
        {
            FileStatusItemViewModel? entry = SelectedEntry;
            if (entry == null || !entry.CanUnstage)
            {
                return;
            }

            OperationResult result = await gitService.UnstageAsync(new[] { entry.FilePath }).ConfigureAwait(true);
            LastOperationMessage = BuildOperationMessage(result);
            if (result.Success)
            {
                await RefreshAsync().ConfigureAwait(true);
            }
        }

        private static string BuildTrackingSummary(GitStatus status)
        {
            List<string> parts = new List<string>();
            string? trackingBranch = status.TrackingBranch;
            if (trackingBranch != null && trackingBranch.Trim().Length > 0)
            {
                parts.Add(trackingBranch);
            }

            if (status.AheadBy > 0)
            {
                parts.Add("ahead " + status.AheadBy);
            }

            if (status.BehindBy > 0)
            {
                parts.Add("behind " + status.BehindBy);
            }

            return string.Join(", ", parts);
        }

        private static string BuildOperationMessage(OperationResult result)
        {
            return string.IsNullOrWhiteSpace(result.Detail)
                ? result.Message
                : result.Message + " " + result.Detail;
        }

        private void RaiseCommandStates()
        {
            ((AsyncCommand)RefreshCommand).RaiseCanExecuteChanged();
            ((AsyncCommand)StageSelectedCommand).RaiseCanExecuteChanged();
            ((AsyncCommand)UnstageSelectedCommand).RaiseCanExecuteChanged();
        }
    }
}
