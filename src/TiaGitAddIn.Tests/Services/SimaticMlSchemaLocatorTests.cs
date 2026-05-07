using System;
using System.IO;
using TiaGitAddIn.Services.SimaticMl;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public sealed class SimaticMlSchemaLocatorTests : IDisposable
    {
        private readonly string tempRoot;

        public SimaticMlSchemaLocatorTests()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "tia-git-addin-schema-locator-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [Fact]
        public void Locate_WithExplicitSchemaDirectory_ReturnsAvailableLocation()
        {
            string schemaDirectory = Path.Combine(tempRoot, "Schemas");
            Directory.CreateDirectory(schemaDirectory);
            File.WriteAllText(Path.Combine(schemaDirectory, "SW.Common_v2.xsd"), MinimalSchema());

            var locator = new SimaticMlSchemaLocator();

            SimaticMlSchemaLocation location = locator.Locate(schemaDirectory);

            Assert.True(location.IsAvailable);
            Assert.Equal(schemaDirectory, location.SchemaDirectory);
            Assert.Equal("explicit", location.Source);
        }

        [Fact]
        public void Locate_WithMissingExplicitSchemaDirectory_ReturnsUnavailableLocation()
        {
            var locator = new SimaticMlSchemaLocator();

            SimaticMlSchemaLocation location = locator.Locate(Path.Combine(tempRoot, "missing"));

            Assert.False(location.IsAvailable);
            Assert.Null(location.SchemaDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        private static string MinimalSchema() =>
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" />";
    }
}
