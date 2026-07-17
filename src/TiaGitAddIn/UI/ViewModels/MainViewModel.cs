using System;
using System.ComponentModel;
using System.Threading.Tasks;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Revision;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.Mapping;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        public MainViewModel(
            string repositoryPath,
            IGitService gitService,
            IPlcRevisionProvider revisionProvider,
            IPlcComparisonCoordinator comparisonCoordinator,
            IComparisonPresentationMapper mapper,
            IConfigurationService configurationService,
            IAddInLogger? logger = null,
            IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
                throw new ArgumentException("Repository path is required.", nameof(repositoryPath));
            if (gitService == null)
                throw new ArgumentNullException(nameof(gitService));
            if (revisionProvider == null)
                throw new ArgumentNullException(nameof(revisionProvider));
            if (comparisonCoordinator == null)
                throw new ArgumentNullException(nameof(comparisonCoordinator));
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            if (configurationService == null)
                throw new ArgumentNullException(nameof(configurationService));

            RepositoryPath = repositoryPath;
            Status = new StatusViewModel(gitService, UiDispatcher);
            Commit = new CommitViewModel(gitService, Status.RefreshAsync, UiDispatcher);
            Branch = new BranchViewModel(gitService, UiDispatcher);
            History = new HistoryViewModel(gitService, UiDispatcher);
            Diff = new DiffViewModel(gitService, revisionProvider, comparisonCoordinator, mapper, logger, UiDispatcher);
            Settings = new SettingsViewModel(configurationService, repositoryPath, UiDispatcher);

            CancelCommand = new RelayCommand(_ => CancelAll(), _ => IsBusy);

            Status.PropertyChanged += OnChildPropertyChanged;
            Commit.PropertyChanged += OnChildPropertyChanged;
            Branch.PropertyChanged += OnChildPropertyChanged;
            History.PropertyChanged += OnChildPropertyChanged;
            Diff.PropertyChanged += OnChildPropertyChanged;
        }

        public string RepositoryPath { get; }

        public StatusViewModel Status { get; }
        public CommitViewModel Commit { get; }
        public BranchViewModel Branch { get; }
        public HistoryViewModel History { get; }
        public DiffViewModel Diff { get; }
        public SettingsViewModel Settings { get; }

        public RelayCommand CancelCommand { get; }

        public Task RefreshAsync() => Status.RefreshAsync();

        private void OnChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HistoryViewModel.SelectedCommit) && sender is HistoryViewModel)
            {
                CommitInfo? commit = History.SelectedCommit;
                if (commit != null)
                {
                    // Fire-and-forget is safe here: LoadCommitDiffAsync is built on RunBusyAsync, which
                    // catches every exception (including OperationCanceledException) internally and
                    // reports it via ReportStatus, so the returned Task never faults or needs observing.
                    _ = Diff.LoadCommitDiffAsync(commit.Hash);
                }

                return;
            }

            if (e.PropertyName == nameof(IsBusy) || e.PropertyName == nameof(BusyMessage))
            {
                RecomputeAggregate();
            }
        }

        private void RecomputeAggregate()
        {
            bool busy = Status.IsBusy || Commit.IsBusy || Branch.IsBusy || History.IsBusy || Diff.IsBusy;

            string msg = Status.BusyMessage;
            if (string.IsNullOrEmpty(msg)) msg = Commit.BusyMessage;
            if (string.IsNullOrEmpty(msg)) msg = Branch.BusyMessage;
            if (string.IsNullOrEmpty(msg)) msg = History.BusyMessage;
            if (string.IsNullOrEmpty(msg)) msg = Diff.BusyMessage;

            InvokeOnUI(() =>
            {
                IsBusy = busy;
                BusyMessage = msg;
            });
        }

        protected override void OnBusyChanged()
        {
            InvokeOnUI(() => CancelCommand.RaiseCanExecuteChanged());
        }

        private void CancelAll()
        {
            if (Status.IsBusy) Status.CancelCommand.Execute(null);
            if (Commit.IsBusy) Commit.CancelCommand.Execute(null);
            if (Branch.IsBusy) Branch.CancelCommand.Execute(null);
            if (History.IsBusy) History.CancelCommand.Execute(null);
            if (Diff.IsBusy) Diff.CancelCommand.Execute(null);
        }
    }
}
