using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TiaGitAddIn.Services.SimaticMl
{
    public enum SimaticMlSchemaDiagnosticSeverity
    {
        Warning,
        Error
    }

    public sealed class SimaticMlSchemaDiagnostic
    {
        public SimaticMlSchemaDiagnostic(
            SimaticMlSchemaDiagnosticSeverity severity,
            string message,
            int lineNumber = 0,
            int linePosition = 0)
        {
            Severity = severity;
            Message = message;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        public SimaticMlSchemaDiagnosticSeverity Severity { get; }

        public string Message { get; }

        public int LineNumber { get; }

        public int LinePosition { get; }
    }

    public sealed class SimaticMlSchemaLocation
    {
        private SimaticMlSchemaLocation(
            bool isAvailable,
            string? schemaDirectory,
            string source,
            string? portalVersion,
            IEnumerable<string> missingExpectedSchemas,
            string? reason)
        {
            IsAvailable = isAvailable;
            SchemaDirectory = schemaDirectory;
            Source = source;
            PortalVersion = portalVersion;
            MissingExpectedSchemas = new ReadOnlyCollection<string>(missingExpectedSchemas.ToList());
            Reason = reason;
        }

        public bool IsAvailable { get; }

        public string? SchemaDirectory { get; }

        public string Source { get; }

        public string? PortalVersion { get; }

        public IReadOnlyList<string> MissingExpectedSchemas { get; }

        public string? Reason { get; }

        public static SimaticMlSchemaLocation Available(
            string schemaDirectory,
            string source,
            string? portalVersion,
            IEnumerable<string> missingExpectedSchemas) =>
            new(
                isAvailable: true,
                schemaDirectory: schemaDirectory,
                source: source,
                portalVersion: portalVersion,
                missingExpectedSchemas: missingExpectedSchemas,
                reason: null);

        public static SimaticMlSchemaLocation Unavailable(string source, string reason) =>
            new(
                isAvailable: false,
                schemaDirectory: null,
                source: source,
                portalVersion: null,
                missingExpectedSchemas: Enumerable.Empty<string>(),
                reason: reason);
    }

    public sealed class SimaticMlSchemaValidationResult
    {
        private SimaticMlSchemaValidationResult(
            bool isValid,
            bool schemaAvailable,
            string? schemaDirectory,
            IEnumerable<SimaticMlSchemaDiagnostic> diagnostics)
        {
            IsValid = isValid;
            SchemaAvailable = schemaAvailable;
            SchemaDirectory = schemaDirectory;
            Diagnostics = new ReadOnlyCollection<SimaticMlSchemaDiagnostic>(diagnostics.ToList());
        }

        public bool IsValid { get; }

        public bool SchemaAvailable { get; }

        public string? SchemaDirectory { get; }

        public IReadOnlyList<SimaticMlSchemaDiagnostic> Diagnostics { get; }

        public static SimaticMlSchemaValidationResult Create(
            bool isValid,
            bool schemaAvailable,
            string? schemaDirectory,
            IEnumerable<SimaticMlSchemaDiagnostic> diagnostics) =>
            new(isValid, schemaAvailable, schemaDirectory, diagnostics);
    }
}
