using System;
using System.Threading;
using System.Windows;
using Siemens.Engineering.AddIn.Menu;
using Siemens.Engineering.AddIn.VersionControl;
using TiaGitAddIn.Logging;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.ViewModels;
using TiaGitAddIn.UI.Views;

namespace TiaGitAddIn.Entry
{
    public sealed class GitVciWorkspaceMenu : ContextMenuAddIn
    {
        private const string DisplayName = "TIA Git";
        private readonly GitPanelLaunchService launchService;
        private readonly IAddInLogger logger;

        public GitVciWorkspaceMenu()
            : this(new GitPanelLaunchService(), new FileLogger())
        {
        }

        public GitVciWorkspaceMenu(
            GitPanelLaunchService launchService,
            IAddInLogger logger)
            : base(DisplayName)
        {
            this.launchService = launchService ?? throw new ArgumentNullException(nameof(launchService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override void BuildContextMenuItems(ContextMenuAddInRoot root)
        {
            root.Items.AddActionItem<WorkspaceFolder>(
                "Open Git Panel...",
                OnOpenGitPanel,
                _ => MenuStatus.Enabled);
            root.Items.AddActionItem<WorkspaceFile>(
                "Open Git Panel...",
                OnOpenGitPanel,
                _ => MenuStatus.Enabled);
        }

        private void OnOpenGitPanel(MenuSelectionProvider<WorkspaceFolder> provider)
        {
            OpenGitPanel(provider);
        }

        private void OnOpenGitPanel(MenuSelectionProvider<WorkspaceFile> provider)
        {
            OpenGitPanel(provider);
        }

        private void OpenGitPanel(object provider)
        {
            try
            {
                object? context = MenuSelectionContextResolver.Resolve(provider);
                if (context == null)
                {
                    ShowMessageAsync("No VCI workspace item was selected.", MessageBoxImage.Warning);
                    return;
                }

                GitPanelLaunchResult result = launchService.CreateViewModel(context);
                if (!result.Success || result.ViewModel == null)
                {
                    ShowMessageAsync(result.Message, MessageBoxImage.Warning);
                    return;
                }

                ShowPanel(result.ViewModel, logger);
            }
            catch (Exception ex)
            {
                logger.Error("Open VCI Git Panel callback failed.", ex);
                ShowMessageAsync(
                    "Unable to open Git panel. See %APPDATA%\\TiaGitAddIn\\logs\\tia-git-addin.log for details.",
                    MessageBoxImage.Error);
            }
        }

        private static void ShowMessageAsync(string message, MessageBoxImage image)
        {
            Thread thread = new Thread(() => ShowMessage(message, image));
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        private static void ShowMessage(string message, MessageBoxImage image)
        {
            MessageBox.Show(
                message,
                DisplayName,
                MessageBoxButton.OK,
                image);
        }

        private static void ShowPanel(MainViewModel viewModel, IAddInLogger logger)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    GitPanelWindow window = new GitPanelWindow(viewModel);
                    window.ShowDialog();
                }
                catch (Exception ex)
                {
                    logger.Error("Git panel UI thread failed.", ex);
                    ShowMessage(ex.Message, MessageBoxImage.Error);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
