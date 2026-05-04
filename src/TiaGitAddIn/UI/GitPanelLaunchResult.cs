using TiaGitAddIn.UI.ViewModels;

namespace TiaGitAddIn.UI
{
    public sealed class GitPanelLaunchResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public MainViewModel? ViewModel { get; set; }

        public static GitPanelLaunchResult Ok(MainViewModel viewModel) =>
            new GitPanelLaunchResult
            {
                Success = true,
                ViewModel = viewModel
            };

        public static GitPanelLaunchResult Fail(string message) =>
            new GitPanelLaunchResult
            {
                Success = false,
                Message = message
            };
    }
}
