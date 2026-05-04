using System;
using System.Threading.Tasks;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        public MainViewModel(
            string repositoryPath,
            IGitService gitService,
            IUiDispatcher? uiDispatcher = null)
            : base(uiDispatcher)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                throw new ArgumentException("Repository path is required.", nameof(repositoryPath));
            }

            RepositoryPath = repositoryPath;
            Status = new StatusViewModel(gitService, UiDispatcher);
            Commit = new CommitViewModel(gitService, Status.RefreshAsync, UiDispatcher);
        }

        public string RepositoryPath { get; }

        public StatusViewModel Status { get; }

        public CommitViewModel Commit { get; }

        public Task RefreshAsync() => Status.RefreshAsync();
    }
}
