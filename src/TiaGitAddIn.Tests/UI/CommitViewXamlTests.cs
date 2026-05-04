using System.IO;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class CommitViewXamlTests
    {
        [Fact]
        public void CommitMessageTextBoxDoesNotEnableWpfSpellCheck()
        {
            string xaml = File.ReadAllText(GetCommitViewPath());

            Assert.DoesNotContain("SpellCheck.IsEnabled", xaml);
        }

        private static string GetCommitViewPath()
        {
            string root = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(
                    root,
                    "src",
                    "TiaGitAddIn",
                    "UI",
                    "Views",
                    "CommitView.xaml");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                DirectoryInfo? parent = Directory.GetParent(root);
                if (parent == null)
                {
                    break;
                }

                root = parent.FullName;
            }

            throw new FileNotFoundException("CommitView.xaml not found.");
        }
    }
}
