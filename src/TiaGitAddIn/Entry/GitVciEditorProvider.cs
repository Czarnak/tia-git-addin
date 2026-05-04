using System;
using Siemens.Engineering;
using Siemens.Engineering.AddIn.VersionControl;
using TiaGitAddIn.Logging;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.Entry
{
    public sealed class GitVciEditorProvider : VciEditorAddInProvider
    {
        private readonly TiaPortal tiaPortal;

        public GitVciEditorProvider(TiaPortal tiaPortal)
        {
            this.tiaPortal = tiaPortal ?? throw new ArgumentNullException(nameof(tiaPortal));
        }

        public override VciWorkspaceViewAddInProvider GetVciWorkspaceViewAddInProvider()
        {
            _ = tiaPortal;
            FileLogger logger = new FileLogger();
            return new GitVciWorkspaceViewProvider(
                new GitPanelLaunchService(),
                logger);
        }
    }
}
