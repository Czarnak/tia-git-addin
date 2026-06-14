using System;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models;
using TiaGitAddIn.Services;
using TiaGitAddIn.UI.ViewModels;

namespace TiaGitAddIn.UI
{
    public sealed class GitPanelLaunchService(
        IVciWorkspaceLocator workspaceLocator,
        IRepositoryDiscovery repositoryDiscovery,
        IConfigurationService configurationService,
        Func<string, string, IGitService> createGitService,
        IAddInLogger logger)
    {
        private readonly IVciWorkspaceLocator workspaceLocator = workspaceLocator ?? throw new ArgumentNullException(nameof(workspaceLocator));
        private readonly IRepositoryDiscovery repositoryDiscovery = repositoryDiscovery ?? throw new ArgumentNullException(nameof(repositoryDiscovery));
        private readonly IConfigurationService configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        private readonly Func<string, string, IGitService> createGitService = createGitService ?? throw new ArgumentNullException(nameof(createGitService));
        private readonly IAddInLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

                string? repositoryRoot = repositoryDiscovery.FindRepositoryRoot(workspacePath);
                if (repositoryRoot == null || repositoryRoot.Trim().Length == 0)
                {
                    return GitPanelLaunchResult.Fail(
                        "The selected VCI workspace is not inside a Git repository.");
                }

                GitConfiguration configuration = configurationService.Load(repositoryRoot);
                string gitExecutablePath = string.IsNullOrWhiteSpace(configuration.GitExecutablePath)
                    ? "git"
                    : configuration.GitExecutablePath!;

                IGitProcessRunner gitProcessRunner = new GitProcessRunner();
                IGitService gitService = createGitService(
                    gitExecutablePath,
                    configuration.RepositoryPath);

                IGitFileExtractor gitFileExtractor = new GitFileExtractor(gitProcessRunner, gitExecutablePath, configuration.RepositoryPath, logger);

                ISactService sactService = new SactService(logger);

                return GitPanelLaunchResult.Ok(() =>
                    new MainViewModel(
                        configuration.RepositoryPath,
                        gitService,
                        gitFileExtractor,
                        sactService,
                        configurationService,
                        logger,
                        WpfUiDispatcher.FromCurrentThread()));
            }
            catch (Exception ex)
            {
                logger.Error("Unable to create Git panel view model.", ex);
                return GitPanelLaunchResult.Fail("Unable to open Git panel. " + ex.Message);
            }
        }
    }
}
