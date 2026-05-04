using System.IO;
using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public sealed class VciWorkspaceLocatorTests
    {
        [Fact]
        public void TryGetWorkspacePathAcceptsDirectoryPathProperty()
        {
            string root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
            try
            {
                VciWorkspaceLocator locator = new VciWorkspaceLocator();

                string? path = locator.TryGetWorkspacePath(new ProjectWithPath(root));

                Assert.Equal(root, path);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void TryGetWorkspacePathUsesParentDirectoryForProjectFilePath()
        {
            string root = Directory.CreateDirectory(
                Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
            string projectFile = Path.Combine(root, "Project.ap21");
            File.WriteAllText(projectFile, string.Empty);
            try
            {
                VciWorkspaceLocator locator = new VciWorkspaceLocator();

                string? path = locator.TryGetWorkspacePath(new ProjectWithPath(projectFile));

                Assert.Equal(root, path);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private sealed class ProjectWithPath
        {
            public ProjectWithPath(string path)
            {
                Path = path;
            }

            public string Path { get; }
        }
    }
}
