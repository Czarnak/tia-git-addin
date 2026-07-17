using System;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Revision;
using TiaGitAddIn.UI.Mapping;
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

                // A single Siemens-backed GitProcessRunner instance implements both the text
                // (IGitProcessRunner) and binary (IGitBinaryProcessRunner) process seams used to load
                // comparison revisions. No test/System process runner is ever reachable from here.
                logger.Info("GitPanelLaunchService: adapter GitProcessRunner/SiemensAddIn selected for revision loading.");
                var gitProcessRunner = new GitProcessRunner();
                IGitService gitService = createGitService(
                    gitExecutablePath,
                    configuration.RepositoryPath);

                IGitBlobReader blobReader = new GitBlobReader(
                    gitProcessRunner, gitProcessRunner, gitExecutablePath, configuration.RepositoryPath);
                IPlcRevisionProvider revisionProvider = new PlcRevisionProvider(blobReader, PlcRevisionProviderOptions.Default);

                IPlcArtifactClassifier classifier = new PlcArtifactClassifier();
                var textComparer = new LineTextComparer(TextComparisonLimits.Default);
                var resultFactory = new PlcComparisonResultFactory(textComparer);
                var sanitizer = new ComparisonDiagnosticSanitizer();
                var strategies = new IPlcComparisonStrategy[]
                {
                    new LadComparisonStrategy(resultFactory, sanitizer),
                    new TextFallbackStrategy(resultFactory),
                };
                IPlcComparisonCoordinator comparisonCoordinator = new PlcComparisonCoordinator(
                    classifier, strategies, resultFactory, sanitizer);

                return GitPanelLaunchResult.Ok(() =>
                {
                    // The dispatcher must be captured on the thread that will actually own the WPF panel
                    // (a dedicated STA thread spun up by the caller), not the thread that called
                    // CreateViewModel -- so this, and anything that captures it, stays inside the factory.
                    IUiDispatcher dispatcher = WpfUiDispatcher.FromCurrentThread();

                    var mapper = new ComparisonPresentationMapper(new IComparisonPresentationViewModelFactory[]
                    {
                        new InterfacePresentationViewModelFactory(),
                        new TextPresentationViewModelFactory(),
                        new UnsupportedPresentationViewModelFactory(),
                        new ErrorPresentationViewModelFactory(),
                        new LadPresentationViewModelFactory(logger, dispatcher),
                    });

                    return new MainViewModel(
                        configuration.RepositoryPath,
                        gitService,
                        revisionProvider,
                        comparisonCoordinator,
                        mapper,
                        configurationService,
                        logger,
                        dispatcher);
                });
            }
            catch (Exception ex)
            {
                logger.Error("Unable to create Git panel view model.", ex);
                return GitPanelLaunchResult.Fail("Unable to open Git panel. " + ex.Message);
            }
        }
    }
}
