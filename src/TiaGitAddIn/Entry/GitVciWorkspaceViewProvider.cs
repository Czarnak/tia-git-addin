using System;
using System.Collections.Generic;
using Siemens.Engineering.AddIn.Menu;
using Siemens.Engineering.AddIn.VersionControl;
using TiaGitAddIn.Logging;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.Entry
{
    public sealed class GitVciWorkspaceViewProvider : VciWorkspaceViewAddInProvider
    {
        private readonly GitPanelLaunchService launchService;
        private readonly IAddInLogger logger;

        public GitVciWorkspaceViewProvider(
            GitPanelLaunchService launchService,
            IAddInLogger logger)
        {
            this.launchService = launchService ?? throw new ArgumentNullException(nameof(launchService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override IEnumerable<ContextMenuAddIn> GetContextMenuAddIns()
        {
            yield return new GitVciWorkspaceMenu(launchService, logger);
        }
    }
}
