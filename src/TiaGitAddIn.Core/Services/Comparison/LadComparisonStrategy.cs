using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services.SimaticMl;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// The native LAD comparison strategy. Parses each present side as SimaticML and always builds a
    /// precise <see cref="InterfacePresentation"/> from whichever sides parsed successfully. When every
    /// present side's networks use the recognized <c>FlgNet</c> format, it additionally renders the full
    /// visual ladder network comparison via the unchanged legacy <see cref="SimaticMlComparer"/> and returns
    /// a <see cref="PlcSupportLevel.Full"/>/<see cref="PlcComparisonMode.Visual"/> result wrapping a
    /// <see cref="LadPresentation"/>. When the network structure is not recognized, it returns a
    /// <see cref="PlcSupportLevel.Partial"/>/<see cref="PlcComparisonMode.Structured"/> result exposing only
    /// the trusted <see cref="InterfacePresentation"/>. Malformed XML on either present side is not a hard
    /// failure here: it throws <see cref="RecoverableComparisonException"/> so the coordinator can downgrade
    /// to its own text fallback whenever raw text is available. This type performs no asynchronous work of
    /// its own (parsing/comparison are synchronous, bounded operations), so cancellation is only ever
    /// observed via explicit checks and is never caught -- it always reaches the caller unmodified.
    /// </summary>
    public sealed class LadComparisonStrategy : IPlcComparisonStrategy
    {
        private static readonly PlcArtifactKind[] SupportedArtifactKinds = { PlcArtifactKind.Lad };

        private readonly PlcComparisonResultFactory resultFactory;
        private readonly ComparisonDiagnosticSanitizer sanitizer;

        public LadComparisonStrategy(PlcComparisonResultFactory resultFactory, ComparisonDiagnosticSanitizer sanitizer)
        {
            this.resultFactory = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
            this.sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        }

        public IReadOnlyCollection<PlcArtifactKind> SupportedKinds => SupportedArtifactKinds;

        public Task<PlcComparisonResult> CompareAsync(PlcComparisonContext context, CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            cancellationToken.ThrowIfCancellationRequested();

            SimaticMlFile? leftModel = ParseSideOrThrow(context.Request.Left, cancellationToken);
            SimaticMlFile? rightModel = ParseSideOrThrow(context.Request.Right, cancellationToken);

            BlockDefinition? leftBlock = leftModel?.Blocks.FirstOrDefault();
            BlockDefinition? rightBlock = rightModel?.Blocks.FirstOrDefault();

            InterfaceSnapshot leftSnapshot = InterfaceSnapshotBuilder.Build(leftBlock);
            InterfaceSnapshot rightSnapshot = InterfaceSnapshotBuilder.Build(rightBlock);
            InterfacePresentation interfacePresentation = new InterfaceComparer().Compare(leftSnapshot, rightSnapshot);

            IReadOnlyList<PlcComparisonDiagnostic> diagnostics = context.Request.Pair.Diagnostics;

            if (!HasRecognizedNetworkStructure(leftBlock) || !HasRecognizedNetworkStructure(rightBlock))
            {
                return Task.FromResult(resultFactory.CreateSemantic(
                    context,
                    PlcComparisonMode.Structured,
                    PlcSupportLevel.Partial,
                    "LAD network structure is only partially supported; showing trusted block and interface structure.",
                    diagnostics,
                    interfacePresentation));
            }

            SactCompareResult legacyResult = SimaticMlComparer.Compare(leftModel, rightModel);

            return Task.FromResult(resultFactory.CreateSemantic(
                context,
                PlcComparisonMode.Visual,
                PlcSupportLevel.Full,
                string.Empty,
                diagnostics,
                new LadPresentation(legacyResult, interfacePresentation)));
        }

        /// <summary>Returns <c>null</c> for a missing side; throws <see cref="RecoverableComparisonException"/> for malformed XML.</summary>
        private SimaticMlFile? ParseSideOrThrow(PlcRevision revision, CancellationToken cancellationToken)
        {
            if (revision.IsMissing)
            {
                return null;
            }

            string text = revision.Text ?? throw new InvalidOperationException(
                "A LAD revision routed to this strategy must have decoded text.");

            SimaticMlParseResult parseResult = SimaticMlParser.ParseText(
                text, SimaticMlParserLimits.Default, revision.Side, cancellationToken);

            if (parseResult.IsSuccess && parseResult.Model != null)
            {
                return parseResult.Model;
            }

            PlcComparisonDiagnostic diagnostic = parseResult.Diagnostics.Count > 0
                ? parseResult.Diagnostics[0]
                : sanitizer.ForUser("CMP-LAD-PARSE", PlcDiagnosticSeverity.Error, "The LAD document could not be parsed.");

            throw new RecoverableComparisonException(
                "The LAD document could not be parsed as SimaticML; showing a text comparison instead.",
                diagnostic);
        }

        /// <summary>
        /// A block with no compile units (or a missing side, represented as <c>null</c> here) has nothing to
        /// render, so it is never itself a reason to downgrade to Partial. Otherwise every compile unit's
        /// network must use the recognized <c>FlgNet</c> format for the full visual comparison to proceed.
        /// </summary>
        private static bool HasRecognizedNetworkStructure(BlockDefinition? block)
        {
            if (block == null || block.CompileUnits.Count == 0)
            {
                return true;
            }

            return block.CompileUnits.All(unit =>
                unit.Network != null && string.Equals(unit.Network.Format, "FlgNet", StringComparison.Ordinal));
        }
    }
}
