using System;
using System.IO;
using System.Linq;
using TiaGitAddIn.Services.SimaticMl;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public sealed class SimaticMlSchemaValidatorTests : IDisposable
    {
        private readonly string tempRoot;

        public SimaticMlSchemaValidatorTests()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "tia-git-addin-schema-validator-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [Fact]
        public void Validate_WithUnavailableSchemas_ReturnsNonBlockingResult()
        {
            string xmlPath = WriteFile("valid.xml", "<Document />");
            var validator = new SimaticMlSchemaValidator();

            SimaticMlSchemaValidationResult result = validator.Validate(
                xmlPath,
                SimaticMlSchemaLocation.Unavailable("missing", "not found"));

            Assert.True(result.IsValid);
            Assert.False(result.SchemaAvailable);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == SimaticMlSchemaDiagnosticSeverity.Warning);
        }

        [Fact]
        public void Validate_WithMalformedXml_ReturnsError()
        {
            string schemaDirectory = CreateSchemaDirectory();
            string xmlPath = WriteFile("malformed.xml", "<Document>");
            var validator = new SimaticMlSchemaValidator();

            SimaticMlSchemaValidationResult result = validator.Validate(
                xmlPath,
                SimaticMlSchemaLocation.Available(schemaDirectory, "explicit", null, Array.Empty<string>()));

            Assert.False(result.IsValid);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == SimaticMlSchemaDiagnosticSeverity.Error);
        }

        [Fact]
        public void Validate_WithMatchingSchema_ReturnsValid()
        {
            string schemaDirectory = CreateSchemaDirectory();
            string xmlPath = WriteFile("valid.xml", "<Document><Name>Block</Name></Document>");
            var validator = new SimaticMlSchemaValidator();

            SimaticMlSchemaValidationResult result = validator.Validate(
                xmlPath,
                SimaticMlSchemaLocation.Available(schemaDirectory, "explicit", null, Array.Empty<string>()));

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.Message)));
            Assert.True(result.SchemaAvailable);
        }

        [Fact]
        public void Validate_WithLocalSchemaInclude_ReturnsValid()
        {
            string schemaDirectory = Path.Combine(tempRoot, "IncludedSchemas");
            Directory.CreateDirectory(schemaDirectory);
            File.WriteAllText(
                Path.Combine(schemaDirectory, "CommonTypes.xsd"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">" +
                "<xs:simpleType name=\"BlockNameType\"><xs:restriction base=\"xs:string\" /></xs:simpleType>" +
                "</xs:schema>");
            File.WriteAllText(
                Path.Combine(schemaDirectory, "Document.xsd"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">" +
                "<xs:include schemaLocation=\"CommonTypes.xsd\" />" +
                "<xs:element name=\"Document\">" +
                "<xs:complexType><xs:sequence>" +
                "<xs:element name=\"Name\" type=\"BlockNameType\" />" +
                "</xs:sequence></xs:complexType>" +
                "</xs:element>" +
                "</xs:schema>");
            string xmlPath = WriteFile("included-valid.xml", "<Document><Name>Block</Name></Document>");
            var validator = new SimaticMlSchemaValidator();

            SimaticMlSchemaValidationResult result = validator.Validate(
                xmlPath,
                SimaticMlSchemaLocation.Available(schemaDirectory, "explicit", null, Array.Empty<string>()));

            Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Diagnostics.Select(d => d.Message)));
        }

        [Fact]
        public void Validate_WithSchemaViolation_ReturnsError()
        {
            string schemaDirectory = CreateSchemaDirectory();
            string xmlPath = WriteFile("invalid.xml", "<Document />");
            var validator = new SimaticMlSchemaValidator();

            SimaticMlSchemaValidationResult result = validator.Validate(
                xmlPath,
                SimaticMlSchemaLocation.Available(schemaDirectory, "explicit", null, Array.Empty<string>()));

            Assert.False(result.IsValid);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == SimaticMlSchemaDiagnosticSeverity.Error);
        }

        public void Dispose()
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }

        private string CreateSchemaDirectory()
        {
            string schemaDirectory = Path.Combine(tempRoot, "Schemas");
            Directory.CreateDirectory(schemaDirectory);
            File.WriteAllText(
                Path.Combine(schemaDirectory, "Document.xsd"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">" +
                "<xs:element name=\"Document\">" +
                "<xs:complexType><xs:sequence>" +
                "<xs:element name=\"Name\" type=\"xs:string\" />" +
                "</xs:sequence></xs:complexType>" +
                "</xs:element>" +
                "</xs:schema>");
            return schemaDirectory;
        }

        private string WriteFile(string fileName, string content)
        {
            string path = Path.Combine(tempRoot, fileName);
            File.WriteAllText(path, content);
            return path;
        }
    }
}
