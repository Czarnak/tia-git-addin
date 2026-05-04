using System.IO;
using Xunit;

namespace TiaGitAddIn.Tests.Configuration
{
    public sealed class ReleaseWorkflowTests
    {
        [Fact]
        public void ReleaseWorkflowPublishesAddInWhenVersionTagIsPushed()
        {
            string workflow = File.ReadAllText(GetReleaseWorkflowPath());
            const string packagePath = "src/TiaGitAddIn/bin/Release/net48/TiaGitAddIn.addin";

            Assert.Contains("tags:", workflow);
            Assert.Contains("'v*'", workflow);
            Assert.Contains("runs-on: [self-hosted, Windows]", workflow);
            Assert.Contains("dotnet build TiaGitAddIn.sln", workflow);
            Assert.Equal(3, CountOccurrences(workflow, packagePath));
            Assert.Contains("actions/upload-artifact", workflow);
            Assert.Contains("if-no-files-found: error", workflow);
            Assert.Contains("softprops/action-gh-release", workflow);
            Assert.Contains("body_path: release-notes.md", workflow);
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }

            return count;
        }

        private static string GetReleaseWorkflowPath()
        {
            string root = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(
                    root,
                    ".github",
                    "workflows",
                    "release.yml");
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

            throw new FileNotFoundException("release.yml not found.");
        }
    }
}
