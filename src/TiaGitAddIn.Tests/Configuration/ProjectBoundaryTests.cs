using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace TiaGitAddIn.Tests.Configuration
{
    public sealed class ProjectBoundaryTests
    {
        private static readonly string[] RequiredAcceptanceMarkers =
        {
            "GitAcceptanceV1",
            "GitAcceptanceV2",
        };

        private static readonly (string Name, Regex Pattern)[] SensitiveDataPatterns =
        {
            ("password", new Regex("password", RegexOptions.IgnoreCase)),
            ("token", new Regex("token", RegexOptions.IgnoreCase)),
            ("authorization", new Regex("authorization", RegexOptions.IgnoreCase)),
            ("customer", new Regex("customer", RegexOptions.IgnoreCase)),
            ("IPv4 address", new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.IgnoreCase)),
            ("http:// URL", new Regex(@"http://", RegexOptions.IgnoreCase)),
            ("https:// URL", new Regex(@"https://", RegexOptions.IgnoreCase)),
            ("Windows drive path", new Regex(@"[A-Z]:\\", RegexOptions.IgnoreCase)),
        };

        [Fact]
        public void IntegrationProjectTargetsNet8AndReferencesOnlyCore()
        {
            XDocument project = XDocument.Load(PathAt(
                "src", "TiaGitAddIn.IntegrationTests", "TiaGitAddIn.IntegrationTests.csproj"));

            Assert.Equal("net8.0", Value(project, "TargetFramework"));
            Assert.Equal("12.0", Value(project, "LangVersion"));
            Assert.Equal("true", Value(project, "IsTestProject"));
            Assert.Null(project.Descendants("UseWPF").SingleOrDefault());

            string[] references = project.Descendants("ProjectReference")
                .Select(element => ((string?)element.Attribute("Include") ?? string.Empty)
                    .Replace('/', '\\'))
                .ToArray();
            Assert.Equal(
                new[] { "..\\TiaGitAddIn.Core\\TiaGitAddIn.Core.csproj" },
                references);

            string xml = project.ToString(SaveOptions.DisableFormatting);
            Assert.DoesNotContain("TiaGitAddIn.csproj", xml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Siemens", xml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PresentationFramework", xml, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BothTestProjectsPinCoverletMsbuild604Privately()
        {
            AssertCoverlet(PathAt("src", "TiaGitAddIn.Tests", "TiaGitAddIn.Tests.csproj"));
            AssertCoverlet(PathAt(
                "src", "TiaGitAddIn.IntegrationTests", "TiaGitAddIn.IntegrationTests.csproj"));
        }

        [Fact]
        public void VciGitFixturesMatchManifestAndContainNoSensitiveData()
        {
            string fixturesDirectory = PathAt(
                "src", "TiaGitAddIn.IntegrationTests", "TestData", "VciGitWorkflow");
            string manifestPath = Path.Combine(fixturesDirectory, "manifest.json");

            JObject manifest = JObject.Parse(File.ReadAllText(manifestPath));
            JArray fixtures = Assert.IsType<JArray>(manifest["fixtures"]);
            Assert.NotEmpty(fixtures);

            var foundMarkers = new HashSet<string>(StringComparer.Ordinal);

            foreach (JToken fixture in fixtures)
            {
                string fileName = (string?)fixture["file"]
                    ?? throw new InvalidOperationException("Fixture entry missing 'file'.");
                string expectedSha256 = (string?)fixture["sha256"]
                    ?? throw new InvalidOperationException("Fixture entry missing 'sha256'.");

                string filePath = Path.Combine(fixturesDirectory, fileName);
                Assert.True(
                    File.Exists(filePath),
                    $"Fixture '{fileName}' referenced by manifest.json was not found.");

                byte[] bytes = File.ReadAllBytes(filePath);
                Assert.Equal(expectedSha256, ComputeUppercaseSha256(bytes));

                string content = Encoding.UTF8.GetString(bytes);
                AssertContainsNoSensitiveData(fileName, content);

                foreach (string marker in RequiredAcceptanceMarkers)
                {
                    if (content.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    {
                        foundMarkers.Add(marker);
                    }
                }
            }

            foreach (string marker in RequiredAcceptanceMarkers)
            {
                Assert.Contains(marker, foundMarkers);
            }
        }

        private static string ComputeUppercaseSha256(byte[] content)
        {
            using SHA256 sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(content)).Replace("-", string.Empty);
        }

        private static void AssertContainsNoSensitiveData(string fixtureFile, string content)
        {
            foreach ((string name, Regex pattern) in SensitiveDataPatterns)
            {
                Assert.False(
                    pattern.IsMatch(content),
                    $"Fixture '{fixtureFile}' appears to contain sensitive data matching '{name}'.");
            }
        }

        private static void AssertCoverlet(string path)
        {
            XElement package = XDocument.Load(path)
                .Descendants("PackageReference")
                .Single(element => string.Equals(
                    (string?)element.Attribute("Include"),
                    "coverlet.msbuild",
                    StringComparison.OrdinalIgnoreCase));
            Assert.Equal("6.0.4", (string?)package.Attribute("Version"));
            Assert.Equal("all", (string?)package.Attribute("PrivateAssets"));
        }

        private static string? Value(XDocument project, string name) =>
            project.Descendants(name).Select(element => element.Value).SingleOrDefault();

        private static string PathAt(params string[] segments) =>
            segments.Aggregate(RepositoryRoot(), Path.Combine);

        private static string RepositoryRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "TiaGitAddIn.sln")))
            {
                current = current.Parent;
            }

            return current?.FullName
                ?? throw new DirectoryNotFoundException("Repository root containing TiaGitAddIn.sln was not found.");
        }
    }
}