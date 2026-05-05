using System;
using Siemens.Engineering;
using Siemens.Engineering.AddIn.VersionControl;
using TiaGitAddIn.Logging;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.Entry
{
    public sealed class GitVciEditorProvider(TiaPortal tiaPortal) : VciEditorAddInProvider
    {
        private readonly TiaPortal tiaPortal = tiaPortal ?? throw new ArgumentNullException(nameof(tiaPortal));

        public override VciWorkspaceViewAddInProvider GetVciWorkspaceViewAddInProvider()
        {
            _ = tiaPortal;
            FileLogger logger = new();
            return new GitVciWorkspaceViewProvider(
                new GitPanelLaunchService(),
                logger);
        }
    }
}
