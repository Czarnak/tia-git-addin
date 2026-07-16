using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TiaGitAddIn.Tests.Architecture
{
    public class ComparisonBoundaryTests
    {
        [Fact]
        public void ProductionComparisonPathContainsNoInternalOrLiveObjectCompareApi()
        {
            string root = RepositoryRoot.Find();
            string[] forbidden =
            {
                "Siemens.Automation.CommonServices.Compare",
                "CompareEditorStarter",
                "PlcSoftware.CompareTo(",
                "CompareToOnline(",
                "typeof(CompareEditorStarter)",
                "GetType(\"Siemens.Automation.CommonServices.Compare"
            };

            string[] files = Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}TiaGitAddIn.Tests{Path.DirectorySeparatorChar}"))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}TiaGitAddIn.IntegrationTests{Path.DirectorySeparatorChar}"))
                .ToArray();

            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                foreach (string token in forbidden)
                {
                    Assert.DoesNotContain(token, text, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Fact]
        public void EvidenceDocumentsSelectProjectOwnedComparisonWithoutConflict()
        {
            string root = RepositoryRoot.Find();
            string investigation = File.ReadAllText(Path.Combine(root, "docs", "tia-v21-compare-api-investigation.md"));
            string design = File.ReadAllText(Path.Combine(root, "docs", "plans", "2026-07-15-plc-diff-and-vci-workflow-design.md"));
            string prd = File.ReadAllText(Path.Combine(root, "docs", "PRD.md"));
            string readme = File.ReadAllText(Path.Combine(root, "README.md"));

            Assert.Contains("PlcSoftware.CompareTo", investigation, StringComparison.Ordinal);
            Assert.Contains("CompareToOnline", investigation, StringComparison.Ordinal);
            Assert.Contains("project-owned", investigation, StringComparison.OrdinalIgnoreCase);
            Assert.All(new[] { design, prd, readme }, text =>
                Assert.Contains("project-owned", text, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("Git blobs are accepted by PlcSoftware.CompareTo", string.Join("\n", investigation, design, prd, readme), StringComparison.OrdinalIgnoreCase);
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

        [Fact]
        public void CoreAndAddInKeepTheirFrameworkAndReferenceBoundary()
        {
            string root = RepositoryRoot.Find();
            XDocument core = XDocument.Load(Path.Combine(root, "src", "TiaGitAddIn.Core", "TiaGitAddIn.Core.csproj"));
            XDocument addIn = XDocument.Load(Path.Combine(root, "src", "TiaGitAddIn", "TiaGitAddIn.csproj"));

            Assert.Equal("netstandard2.0", core.Descendants("TargetFramework").Single().Value);
            Assert.DoesNotContain(core.Descendants("Reference"), x =>
                ((string?)x.Attribute("Include"))?.StartsWith("Siemens.", StringComparison.OrdinalIgnoreCase) == true);
            Assert.DoesNotContain(core.Descendants("UseWPF"), x => x.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("net48", addIn.Descendants("TargetFramework").Single().Value);
            Assert.Contains(addIn.Descendants("Reference"), x =>
                ((string?)x.Attribute("Include"))?.StartsWith("Siemens.", StringComparison.OrdinalIgnoreCase) == true);
        }

        [Fact]
        public void PublisherDeclaresOnlyDocumentedComparisonAndGitPermissions()
        {
            string xml = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "src", "TiaGitAddIn", "AddInPublisherConfiguration.xml"));
            Assert.Contains("TIA.ReadWrite", xml, StringComparison.Ordinal);
            Assert.Contains("ProcessStartPermission", xml, StringComparison.Ordinal);
            Assert.DoesNotContain("ComparePermission", xml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SACT", xml, StringComparison.OrdinalIgnoreCase);
        }
    }
}
