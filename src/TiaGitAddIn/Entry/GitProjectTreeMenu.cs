using System;
using System.Windows;
using Siemens.Engineering;
using Siemens.Engineering.AddIn.Menu;

namespace TiaGitAddIn.Entry
{
    public sealed class GitProjectTreeMenu : ContextMenuAddIn
    {
        private const string DisplayName = "TIA Git";
        private readonly TiaPortal tiaPortal;

        public GitProjectTreeMenu(TiaPortal tiaPortal)
            : base(DisplayName)
        {
            this.tiaPortal = tiaPortal ?? throw new ArgumentNullException(nameof(tiaPortal));
        }

        protected override void BuildContextMenuItems(ContextMenuAddInRoot root)
        {
            root.Items.AddActionItem<IEngineeringObject>(
                "Open Git Panel...",
                OnOpenGitPanel,
                _ => MenuStatus.Enabled);
        }

        private void OnOpenGitPanel(MenuSelectionProvider<IEngineeringObject> provider)
        {
            _ = tiaPortal;
            _ = provider;
            MessageBox.Show(
                "Git panel UI is not implemented yet.",
                DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

    }
}
