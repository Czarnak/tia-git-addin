using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.SimaticMl
{
    /// <summary>
    /// The outcome of <see cref="SimaticMlParser.ParseText"/>: a diagnostics-in-result contract, never a
    /// thrown exception, so callers (including future comparison strategies) can inspect <see cref="IsSuccess"/>
    /// and <see cref="Diagnostics"/> without a try/catch around routine parse failures.
    /// </summary>
    public sealed class SimaticMlParseResult
    {
        public SimaticMlParseResult(SimaticMlFile? model, bool isPartial, IEnumerable<PlcComparisonDiagnostic> diagnostics)
        {
            Model = model;
            IsPartial = isPartial;
            Diagnostics = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray();
        }

        public SimaticMlFile? Model { get; }
        public bool IsPartial { get; }
        public IReadOnlyList<PlcComparisonDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Model != null && !Diagnostics.Any(d => d.Severity == PlcDiagnosticSeverity.Error);
    }
}
