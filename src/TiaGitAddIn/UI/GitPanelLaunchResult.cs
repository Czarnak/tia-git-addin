using System;
using TiaGitAddIn.UI.ViewModels;

namespace TiaGitAddIn.UI
{
    public sealed class GitPanelLaunchResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Func<MainViewModel>? CreateViewModel { get; set; }

        public static GitPanelLaunchResult Ok(Func<MainViewModel> createViewModel) =>
            new GitPanelLaunchResult
            {
                Success = true,
                CreateViewModel = createViewModel
            };

        public static GitPanelLaunchResult Fail(string message) =>
            new GitPanelLaunchResult
            {
                Success = false,
                Message = message
            };
    }
}
