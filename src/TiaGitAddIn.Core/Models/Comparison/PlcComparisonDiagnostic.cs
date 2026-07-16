using System;

namespace TiaGitAddIn.Models.Comparison
{
    public sealed class PlcSourceLocation
    {
        public PlcSourceLocation(PlcRevisionSide side, int? line = null, int? column = null, int? startOffset = null, int? length = null)
        { Side = side; Line = line; Column = column; StartOffset = startOffset; Length = length; }
        public PlcRevisionSide Side { get; }
        public int? Line { get; }
        public int? Column { get; }
        public int? StartOffset { get; }
        public int? Length { get; }
    }

    public sealed class PlcComparisonDiagnostic
    {
        public PlcComparisonDiagnostic(string code, PlcDiagnosticSeverity severity, string message, PlcSourceLocation? location = null)
        { Code = Require(code, nameof(code)); Severity = severity; Message = Require(message, nameof(message)); Location = location; }
        public string Code { get; }
        public PlcDiagnosticSeverity Severity { get; }
        public string Message { get; }
        public PlcSourceLocation? Location { get; }
        private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("A non-blank value is required.", name) : value;
    }
}
