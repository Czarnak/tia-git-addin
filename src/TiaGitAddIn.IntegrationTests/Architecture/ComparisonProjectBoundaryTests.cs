using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TiaGitAddIn.IntegrationTests.Architecture
{
    public class ComparisonProjectBoundaryTests
    {
        [Fact]
        public void IntegrationTestsProjectIsNet8CoreOnlyNoWpfNoSiemens()
        {
            string root = RepositoryRoot.Find();
            XDocument integrationTests = XDocument.Load(Path.Combine(root, "src", "TiaGitAddIn.IntegrationTests", "TiaGitAddIn.IntegrationTests.csproj"));

            // Verify target framework is net8.0
            Assert.Equal("net8.0", integrationTests.Descendants("TargetFramework").Single().Value);

            // Verify exactly one ProjectReference ending with TiaGitAddIn.Core.csproj
            var projectReferences = integrationTests.Descendants("ProjectReference")
                .Where(pr => ((string?)pr.Attribute("Include"))?.EndsWith("TiaGitAddIn.Core.csproj", StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
            Assert.Single(projectReferences);

            // Verify no UseWPF property
            Assert.DoesNotContain(integrationTests.Descendants("UseWPF"), x => x.Value.Equals("true", StringComparison.OrdinalIgnoreCase));

            // Verify no Siemens.* references
            Assert.DoesNotContain(integrationTests.Descendants("Reference"), x =>
                ((string?)x.Attribute("Include"))?.StartsWith("Siemens.", StringComparison.OrdinalIgnoreCase) == true);

            // Verify no TiaGitAddIn.csproj reference (only TiaGitAddIn.Core allowed)
            Assert.DoesNotContain(integrationTests.Descendants("ProjectReference"), pr =>
                ((string?)pr.Attribute("Include"))?.EndsWith("TiaGitAddIn.csproj", StringComparison.OrdinalIgnoreCase) == true &&
                !((string?)pr.Attribute("Include"))?.EndsWith("TiaGitAddIn.Core.csproj", StringComparison.OrdinalIgnoreCase) == true);
        }

        internal static class RepositoryRoot
        {
            public static string Find()
            {
                DirectoryInfo? cursor = new DirectoryInfo(AppContext.BaseDirectory);
                while (cursor != null && !File.Exists(Path.Combine(cursor.FullName, "TiaGitAddIn.sln")))
                {
                    cursor = cursor.Parent;
                }

                return cursor?.FullName ?? throw new DirectoryNotFoundException("TiaGitAddIn.sln was not found.");
            }
        }
    }
}
