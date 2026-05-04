using System;
using System.Threading.Tasks;
using TiaGitAddIn.Services;

namespace TiaGitAddIn.UI.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        public MainViewModel(string repositoryPath, IGitService gitService)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
            {
                throw new ArgumentException("Repository path is required.", nameof(repositoryPath));
            }

            RepositoryPath = repositoryPath;
            Status = new StatusViewModel(gitService);
            Commit = new CommitViewModel(gitService, Status.RefreshAsync);
        }

        public string RepositoryPath { get; }

        public StatusViewModel Status { get; }

        public CommitViewModel Commit { get; }

        public Task RefreshAsync() => Status.RefreshAsync();
    }
}
