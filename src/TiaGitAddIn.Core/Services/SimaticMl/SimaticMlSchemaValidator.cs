using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Schema;

namespace TiaGitAddIn.Services.SimaticMl
{
    public sealed class SimaticMlSchemaValidator
    {
        private readonly ISimaticMlSchemaLocator schemaLocator;

        public SimaticMlSchemaValidator()
            : this(new SimaticMlSchemaLocator())
        {
        }

        public SimaticMlSchemaValidator(ISimaticMlSchemaLocator schemaLocator)
        {
            this.schemaLocator = schemaLocator ?? throw new ArgumentNullException(nameof(schemaLocator));
        }

        public SimaticMlSchemaValidationResult Validate(string xmlPath, string? explicitSchemaDirectory = null) =>
            Validate(xmlPath, schemaLocator.Locate(explicitSchemaDirectory));

        public SimaticMlSchemaValidationResult Validate(string xmlPath, SimaticMlSchemaLocation schemaLocation)
        {
            var diagnostics = new List<SimaticMlSchemaDiagnostic>();

            if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath))
            {
                diagnostics.Add(new SimaticMlSchemaDiagnostic(
                    SimaticMlSchemaDiagnosticSeverity.Error,
                    "SimaticML file does not exist."));

                return SimaticMlSchemaValidationResult.Create(false, false, null, diagnostics);
            }

            if (!schemaLocation.IsAvailable || string.IsNullOrWhiteSpace(schemaLocation.SchemaDirectory))
            {
                diagnostics.Add(new SimaticMlSchemaDiagnostic(
                    SimaticMlSchemaDiagnosticSeverity.Warning,
                    schemaLocation.Reason ?? "TIA Portal schema directory is unavailable; schema validation was skipped."));

                return SimaticMlSchemaValidationResult.Create(true, false, null, diagnostics);
            }

            string schemaDirectory = schemaLocation.SchemaDirectory!;
            XmlSchemaSet schemaSet = LoadSchemas(schemaDirectory, diagnostics);
            if (diagnostics.Any(diagnostic => diagnostic.Severity == SimaticMlSchemaDiagnosticSeverity.Error))
            {
                return SimaticMlSchemaValidationResult.Create(false, true, schemaDirectory, diagnostics);
            }

            ValidateXml(xmlPath, schemaSet, diagnostics);

            bool isValid = !diagnostics.Any(diagnostic => diagnostic.Severity == SimaticMlSchemaDiagnosticSeverity.Error);
            return SimaticMlSchemaValidationResult.Create(isValid, true, schemaDirectory, diagnostics);
        }

        private static XmlSchemaSet LoadSchemas(string schemaDirectory, List<SimaticMlSchemaDiagnostic> diagnostics)
        {
            var schemaSet = new XmlSchemaSet
            {
                XmlResolver = new LocalSchemaXmlResolver(schemaDirectory)
            };

            string[] schemaFiles = Directory.GetFiles(schemaDirectory, "*.xsd", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string schemaFile in schemaFiles)
            {
                try
                {
                    schemaSet.Add(null, schemaFile);
                }
                catch (XmlSchemaException ex)
                {
                    diagnostics.Add(new SimaticMlSchemaDiagnostic(
                        SimaticMlSchemaDiagnosticSeverity.Error,
                        $"Failed to load schema '{Path.GetFileName(schemaFile)}': {ex.Message}",
                        ex.LineNumber,
                        ex.LinePosition));
                }
                catch (XmlException ex)
                {
                    diagnostics.Add(new SimaticMlSchemaDiagnostic(
                        SimaticMlSchemaDiagnosticSeverity.Error,
                        $"Failed to read schema '{Path.GetFileName(schemaFile)}': {ex.Message}",
                        ex.LineNumber,
                        ex.LinePosition));
                }
            }

            if (schemaFiles.Length == 0)
            {
                diagnostics.Add(new SimaticMlSchemaDiagnostic(
                    SimaticMlSchemaDiagnosticSeverity.Error,
                    "Schema directory does not contain .xsd files."));
            }

            return schemaSet;
        }

        private static void ValidateXml(
            string xmlPath,
            XmlSchemaSet schemaSet,
            List<SimaticMlSchemaDiagnostic> diagnostics)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet,
                XmlResolver = null
            };

            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.ValidationEventHandler += (_, args) =>
            {
                diagnostics.Add(new SimaticMlSchemaDiagnostic(
                    args.Severity == XmlSeverityType.Error
                        ? SimaticMlSchemaDiagnosticSeverity.Error
                        : SimaticMlSchemaDiagnosticSeverity.Warning,
                    args.Message,
                    args.Exception?.LineNumber ?? 0,
                    args.Exception?.LinePosition ?? 0));
            };

            try
            {
                using XmlReader reader = XmlReader.Create(xmlPath, settings);
                while (reader.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                diagnostics.Add(new SimaticMlSchemaDiagnostic(
                    SimaticMlSchemaDiagnosticSeverity.Error,
                    ex.Message,
                    ex.LineNumber,
                    ex.LinePosition));
            }
            catch (XmlSchemaException ex)
            {
                diagnostics.Add(new SimaticMlSchemaDiagnostic(
                    SimaticMlSchemaDiagnosticSeverity.Error,
                    ex.Message,
                    ex.LineNumber,
                    ex.LinePosition));
            }
        }

        private sealed class LocalSchemaXmlResolver : XmlResolver
        {
            private readonly string schemaRoot;

            public LocalSchemaXmlResolver(string schemaDirectory)
            {
                schemaRoot = EnsureTrailingSeparator(Path.GetFullPath(schemaDirectory));
            }

            public override ICredentials? Credentials
            {
                set { }
            }

            public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                if (!absoluteUri.IsFile)
                {
                    throw new XmlException("Only local schema files are allowed.");
                }

                string fullPath = Path.GetFullPath(absoluteUri.LocalPath);
                if (!IsInsideSchemaRoot(fullPath))
                {
                    throw new XmlException("Schema references outside the selected schema directory are not allowed.");
                }

                return File.OpenRead(fullPath);
            }

            public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
            {
                Uri resolvedUri = base.ResolveUri(baseUri, relativeUri);
                if (!resolvedUri.IsFile)
                {
                    throw new XmlException("Only local schema files are allowed.");
                }

                string fullPath = Path.GetFullPath(resolvedUri.LocalPath);
                if (!IsInsideSchemaRoot(fullPath))
                {
                    throw new XmlException("Schema references outside the selected schema directory are not allowed.");
                }

                return new Uri(fullPath);
            }

            private bool IsInsideSchemaRoot(string fullPath) =>
                EnsureTrailingSeparator(fullPath).StartsWith(schemaRoot, StringComparison.OrdinalIgnoreCase);

            private static string EnsureTrailingSeparator(string path) =>
                path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                    ? path
                    : path + Path.DirectorySeparatorChar;
        }
    }
}
