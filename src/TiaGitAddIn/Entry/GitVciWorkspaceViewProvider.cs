using System;
using System.Collections.Generic;
using Siemens.Engineering.AddIn.Menu;
using Siemens.Engineering.AddIn.VersionControl;
using TiaGitAddIn.Logging;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.Entry
{
    public sealed class GitVciWorkspaceViewProvider(
        GitPanelLaunchService launchService,
        IAddInLogger logger) : VciWorkspaceViewAddInProvider
    {
        private readonly GitPanelLaunchService launchService = launchService ?? throw new ArgumentNullException(nameof(launchService));
        private readonly IAddInLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public override IEnumerable<ContextMenuAddIn> GetContextMenuAddIns()
        {
            yield return new GitVciWorkspaceMenu(launchService, logger);
        }
    }
}
