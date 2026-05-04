using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ObservableCollection<FileStatusItemViewModel> stagedEntries = new ObservableCollection<FileStatusItemViewModel>();
        private ObservableCollection<FileStatusItemViewModel> unstagedEntries = new ObservableCollection<FileStatusItemViewModel>();
        private ObservableCollection<FileStatusItemViewModel> untrackedEntries = new ObservableCollection<FileStatusItemViewModel>();
        private string currentBranch = string.Empty;
        private string trackingSummary = string.Empty;
        private string statusSummary = "Status not loaded";
        private string lastOperationMessage = string.Empty;
        private bool isBusy;

        public StatusViewModel(IGitService gitService)
        {
            this.gitService = gitService ?? throw new ArgumentNullException(nameof(gitService));
            RefreshCommand = new AsyncCommand(() => RefreshAsync(), () => !IsBusy);
            StageSelectedCommand = new AsyncCommand(p => StageSelectedAsync(p), _ => !IsBusy);
            UnstageSelectedCommand = new AsyncCommand(p => UnstageSelectedAsync(p), _ => !IsBusy);
            StageAllCommand = new AsyncCommand(() => StageAllAsync(), () => !IsBusy);
        }

        public ObservableCollection<FileStatusItemViewModel> StagedEntries
        {
            get => stagedEntries;
            private set => SetProperty(ref stagedEntries, value);
        }

        public ObservableCollection<FileStatusItemViewModel> UnstagedEntries
        {
            get => unstagedEntries;
            private set => SetProperty(ref unstagedEntries, value);
        }

        public ObservableCollection<FileStatusItemViewModel> UntrackedEntries
        {
            get => untrackedEntries;
            private set => SetProperty(ref untrackedEntries, value);
        }

        public string CurrentBranch
        {
            get => currentBranch;
            private set => SetProperty(ref currentBranch, value ?? string.Empty);
        }

        public string TrackingSummary
        {
            get => trackingSummary;
            private set => SetProperty(ref trackingSummary, value ?? string.Empty);
        }

        public string StatusSummary
        {
            get => statusSummary;
            private set => SetProperty(ref statusSummary, value ?? string.Empty);
        }

        public string LastOperationMessage
        {
            get => lastOperationMessage;
            private set => SetProperty(ref lastOperationMessage, value ?? string.Empty);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (SetProperty(ref isBusy, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand StageSelectedCommand { get; }
        public ICommand UnstageSelectedCommand { get; }
        public ICommand StageAllCommand { get; }

        public async Task RefreshAsync()
        {
            IsBusy = true;
            try
            {
                GitStatus status = await gitService.GetStatusAsync().ConfigureAwait(false);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentBranch = string.IsNullOrWhiteSpace(status.CurrentBranch) ? "(unknown)" : status.CurrentBranch;
                    TrackingSummary = BuildTrackingSummary(status);
                    
                    StagedEntries = new ObservableCollection<FileStatusItemViewModel>(status.StagedEntries.Select(e => new FileStatusItemViewModel(e)));
                    UnstagedEntries = new ObservableCollection<FileStatusItemViewModel>(status.UnstagedEntries.Select(e => new FileStatusItemViewModel(e)));
                    UntrackedEntries = new ObservableCollection<FileStatusItemViewModel>(status.UntrackedEntries.Select(e => new FileStatusItemViewModel(e)));
                    
                    StatusSummary = status.IsClean ? "Working tree clean" : $"{status.Entries.Count} changed files";
                });
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task StageSelectedAsync(object? parameter)
        {
            IEnumerable<FileStatusItemViewModel>? selectedItems = null;
            if (parameter is FileStatusItemViewModel single) selectedItems = new[] { single };
            else if (parameter is IEnumerable<FileStatusItemViewModel> multiple) selectedItems = multiple;
            else if (parameter is System.Collections.IList list) selectedItems = list.Cast<FileStatusItemViewModel>();

            if (selectedItems == null || !selectedItems.Any()) return;

            IsBusy = true;
            try
            {
                var paths = selectedItems.Select(e => e.FilePath).ToList();
                OperationResult result = await gitService.StageAsync(paths).ConfigureAwait(false);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => LastOperationMessage = BuildOperationMessage(result));
                
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            finally { IsBusy = false; }
        }

        public async Task UnstageSelectedAsync(object? parameter)
        {
            IEnumerable<FileStatusItemViewModel>? selectedItems = null;
            if (parameter is FileStatusItemViewModel single) selectedItems = new[] { single };
            else if (parameter is IEnumerable<FileStatusItemViewModel> multiple) selectedItems = multiple;
            else if (parameter is System.Collections.IList list) selectedItems = list.Cast<FileStatusItemViewModel>();

            if (selectedItems == null || !selectedItems.Any()) return;

            IsBusy = true;
            try
            {
                var paths = selectedItems.Select(e => e.FilePath).ToList();
                OperationResult result = await gitService.UnstageAsync(paths).ConfigureAwait(false);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => LastOperationMessage = BuildOperationMessage(result));
                
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            finally { IsBusy = false; }
        }

        public async Task StageAllAsync()
        {
            IsBusy = true;
            try
            {
                OperationResult result = await gitService.StageAllAsync().ConfigureAwait(false);
                
                System.Windows.Application.Current.Dispatcher.Invoke(() => LastOperationMessage = BuildOperationMessage(result));
                
                if (result.Success) await RefreshAsync().ConfigureAwait(false);
            }
            finally { IsBusy = false; }
        }

        private static string BuildTrackingSummary(GitStatus status)
        {
            List<string> parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(status.TrackingBranch)) parts.Add(status.TrackingBranch);
            if (status.AheadBy > 0) parts.Add("ahead " + status.AheadBy);
            if (status.BehindBy > 0) parts.Add("behind " + status.BehindBy);
            return string.Join(", ", parts);
        }

        private static string BuildOperationMessage(OperationResult result)
        {
            return string.IsNullOrWhiteSpace(result.Detail) ? result.Message : $"{result.Message} {result.Detail}";
        }

        private void RaiseCommandStates()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ((AsyncCommand)RefreshCommand).RaiseCanExecuteChanged();
                ((AsyncCommand)StageSelectedCommand).RaiseCanExecuteChanged();
                ((AsyncCommand)UnstageSelectedCommand).RaiseCanExecuteChanged();
                ((AsyncCommand)StageAllCommand).RaiseCanExecuteChanged();
            });
        }
    }
}
