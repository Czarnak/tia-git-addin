using System;
using System.Collections.Generic;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.UI.ViewModels.Comparison
{
    /// <summary>
    /// Read-only presentation wrapper for one <see cref="PlcComparisonDiagnostic"/>. Formats
    /// <see cref="PlcSourceLocation"/> (when present) as "Left|Right", optionally followed by
    /// "line N" and/or "column N", so the view never has to interpret the raw location fields.
    /// </summary>
    public sealed class ComparisonDiagnosticViewModel
    {
        public ComparisonDiagnosticViewModel(PlcComparisonDiagnostic diagnostic)
        {
            if (diagnostic == null) throw new ArgumentNullException(nameof(diagnostic));

            Code = diagnostic.Code;
            Severity = diagnostic.Severity;
            Message = diagnostic.Message;
            LocationLabel = FormatLocation(diagnostic.Location);
        }

        public string Code { get; }
        public PlcDiagnosticSeverity Severity { get; }
        public string Message { get; }
        public string LocationLabel { get; }
        public bool HasLocation => LocationLabel.Length > 0;

        private static string FormatLocation(PlcSourceLocation? location)
        {
            if (location == null)
            {
                return string.Empty;
            }

            var parts = new List<string> { location.Side.ToString() };

            if (location.Line.HasValue)
            {
                parts.Add($"line {location.Line.Value}");
            }

            if (location.Column.HasValue)
            {
                parts.Add($"column {location.Column.Value}");
            }

            return string.Join(", ", parts);
        }
    }
}
