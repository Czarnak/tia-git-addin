using System;
using System.Collections.Generic;
using Siemens.Engineering;
using Siemens.Engineering.AddIn;
using Siemens.Engineering.AddIn.Menu;

namespace TiaGitAddIn.Entry
{
    public sealed class GitProjectTreeProvider : ProjectTreeAddInProvider
    {
        private readonly TiaPortal tiaPortal;

        public GitProjectTreeProvider(TiaPortal tiaPortal)
        {
            this.tiaPortal = tiaPortal ?? throw new ArgumentNullException(nameof(tiaPortal));
        }

        protected override IEnumerable<ContextMenuAddIn> GetContextMenuAddIns()
        {
            yield return new GitProjectTreeMenu(tiaPortal);
        }
    }
}
