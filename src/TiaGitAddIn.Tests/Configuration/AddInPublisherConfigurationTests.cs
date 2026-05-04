using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TiaGitAddIn.Tests.Configuration
{
    public sealed class AddInPublisherConfigurationTests
    {
        [Fact]
        public void RequiredSecurityPermissionsIncludeWpfWindowPermission()
        {
            XDocument document = XDocument.Load(GetConfigurationPath());
            XNamespace ns = "http://www.siemens.com/automation/Openness/AddIn/Publisher/V21";

            bool hasPermission = document
                .Descendants(ns + "SecurityPermissions")
                .Elements(ns + "System.Security.Permissions.SecurityPermission.UnmanagedCode")
                .Any();

            Assert.True(hasPermission);
        }

        private static string GetConfigurationPath()
        {
            string root = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(
                    root,
                    "src",
                    "TiaGitAddIn",
                    "AddInPublisherConfiguration.xml");
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

            throw new FileNotFoundException("AddInPublisherConfiguration.xml not found.");
        }
    }
}
