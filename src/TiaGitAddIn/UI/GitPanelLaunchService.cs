using System;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI.ViewModels;

namespace TiaGitAddIn.UI
{
    public sealed class GitPanelLaunchService
    {
        private readonly IVciWorkspaceLocator workspaceLocator;
        private readonly IRepositoryDiscovery repositoryDiscovery;
        private readonly IConfigurationService configurationService;
        private readonly Func<string, string, IGitService> createGitService;
        private readonly IAddInLogger logger;

        public GitPanelLaunchService()
            : this(
                new VciWorkspaceLocator(),
                new RepositoryDiscovery(),
                new ConfigurationService(),
                (gitPath, repositoryPath) => new GitService(
                    new GitProcessRunner(),
                    new OperationSerializer(),
                    gitPath,
                    repositoryPath),
                new FileLogger())
        {
        }

        public GitPanelLaunchService(
            IVciWorkspaceLocator workspaceLocator,
            IRepositoryDiscovery repositoryDiscovery,
            IConfigurationService configurationService,
            Func<string, string, IGitService> createGitService)
            : this(
                workspaceLocator,
                repositoryDiscovery,
                configurationService,
                createGitService,
                new FileLogger())
        {
        }

        public GitPanelLaunchService(
            IVciWorkspaceLocator workspaceLocator,
            IRepositoryDiscovery repositoryDiscovery,
            IConfigurationService configurationService,
            Func<string, string, IGitService> createGitService,
            IAddInLogger logger)
        {
            this.workspaceLocator = workspaceLocator ?? throw new ArgumentNullException(nameof(workspaceLocator));
            this.repositoryDiscovery = repositoryDiscovery ?? throw new ArgumentNullException(nameof(repositoryDiscovery));
            this.configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            this.createGitService = createGitService ?? throw new ArgumentNullException(nameof(createGitService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public GitPanelLaunchResult CreateViewModel(object projectContext)
        {
            try
            {
                string? workspacePath = workspaceLocator.TryGetWorkspacePath(projectContext);
                if (workspacePath == null || workspacePath.Trim().Length == 0)
                {
                    return GitPanelLaunchResult.Fail(
                        "Unable to resolve a VCI workspace path from the selected TIA item.");
                }

                string resolvedWorkspacePath = workspacePath;
                string? repositoryRoot = repositoryDiscovery.FindRepositoryRoot(resolvedWorkspacePath);
                if (repositoryRoot == null || repositoryRoot.Trim().Length == 0)
                {
                    return GitPanelLaunchResult.Fail(
                        "The selected VCI workspace is not inside a Git repository.");
                }

                string resolvedRepositoryRoot = repositoryRoot;
                GitConfiguration configuration = configurationService.Load(resolvedRepositoryRoot);
                string gitExecutablePath = string.IsNullOrWhiteSpace(configuration.GitExecutablePath)
                    ? "git"
                    : configuration.GitExecutablePath!;
                IGitService gitService = createGitService(
                    gitExecutablePath,
                    configuration.RepositoryPath);

                return GitPanelLaunchResult.Ok(() =>
                    new MainViewModel(configuration.RepositoryPath, gitService, configurationService, WpfUiDispatcher.FromCurrentThread()));
            }
            catch (Exception ex)
            {
                logger.Error("Unable to create Git panel view model.", ex);
                return GitPanelLaunchResult.Fail("Unable to open Git panel. " + ex.Message);
            }
        }
    }
}
