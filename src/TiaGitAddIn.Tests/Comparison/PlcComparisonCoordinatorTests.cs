using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using TiaGitAddIn.Services.Revision;
using Xunit;

namespace TiaGitAddIn.Tests.Comparison
{
    public sealed class PlcComparisonCoordinatorTests
    {
        private const string FbdXml =
            "<SW.Blocks.FB><AttributeList><ProgrammingLanguage>FBD</ProgrammingLanguage></AttributeList></SW.Blocks.FB>";
        private const string SclText = "FUNCTION_BLOCK Motor\nBEGIN\nEND_FUNCTION_BLOCK";

        private static readonly PlcComparisonResultFactory ResultFactory =
            new PlcComparisonResultFactory(new LineTextComparer(TextComparisonLimits.Default));

        private static readonly IPlcComparisonStrategy TextStrategy = new TextFallbackStrategy(ResultFactory);

        private static readonly PlcRevision LeftFbd = ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml);
        private static readonly PlcRevision RightFbd = ComparisonTestData.TextRevision(PlcRevisionSide.Right, FbdXml);

        [Fact]
        public async Task SelectsExactlyOneCompatibleStrategy()
        {
            var fbd = new RecordingStrategy(PlcArtifactKind.Fbd, SemanticFbdResult());
            var scl = new RecordingStrategy(PlcArtifactKind.Scl, SemanticSclResult());
            IPlcComparisonCoordinator coordinator = CreateCoordinator(fbd, scl, new TextFallbackStrategy(ResultFactory));

            PlcComparisonResult result = await coordinator.CompareAsync(
                ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml),
                ComparisonTestData.TextRevision(PlcRevisionSide.Right, FbdXml),
                CancellationToken.None);

            Assert.Equal(1, fbd.CallCount);
            Assert.Equal(0, scl.CallCount);
            Assert.Equal(ComparisonPresentationKind.LogicNetwork, result.Presentation.Kind);
        }

        [Fact]
        public async Task DuplicateSemanticRegistrationReturnsHardError()
        {
            IPlcComparisonCoordinator coordinator = CreateCoordinator(
                new RecordingStrategy(PlcArtifactKind.Fbd, SemanticFbdResult()),
                new RecordingStrategy(PlcArtifactKind.Fbd, SemanticFbdResult()));
            PlcComparisonResult result = await coordinator.CompareAsync(LeftFbd, RightFbd, CancellationToken.None);
            Assert.Equal(ComparisonPresentationKind.Error, result.Presentation.Kind);
            Assert.Equal(PlcSupportLevel.Unsupported, result.SupportLevel);
            Assert.Null(result.RawText);
            Assert.Contains(result.Diagnostics, d => d.Code == "CMP-ROUTE-DUPLICATE");
        }

        [Fact]
        public async Task CancellationEscapesWithoutAnErrorResult()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                CreateCoordinator(new CancellingStrategy()).CompareAsync(LeftFbd, RightFbd, cts.Token));
        }

        [Theory]
        [InlineData(PlcArtifactKind.GenericXml, PlcComparisonMode.Text, PlcSupportLevel.Fallback, ComparisonPresentationKind.Text)]
        [InlineData(PlcArtifactKind.Stl, PlcComparisonMode.Text, PlcSupportLevel.Fallback, ComparisonPresentationKind.Text)]
        [InlineData(PlcArtifactKind.Sfc, PlcComparisonMode.Text, PlcSupportLevel.Fallback, ComparisonPresentationKind.Text)]
        [InlineData(PlcArtifactKind.Binary, PlcComparisonMode.Unsupported, PlcSupportLevel.Unsupported, ComparisonPresentationKind.Unsupported)]
        public async Task NonSemanticKindsReturnExplicitOutcome(PlcArtifactKind kind, PlcComparisonMode mode,
            PlcSupportLevel support, ComparisonPresentationKind presentation)
        {
            PlcComparisonResult result = await CreateCoordinator(TextStrategy)
                .CompareAsync(RevisionFor(kind, PlcRevisionSide.Left), RevisionFor(kind, PlcRevisionSide.Right), CancellationToken.None);
            Assert.Equal(kind, result.ArtifactKind);
            Assert.Equal(mode, result.ActualMode);
            Assert.Equal(support, result.SupportLevel);
            Assert.Equal(presentation, result.Presentation.Kind);
            Assert.False(string.IsNullOrWhiteSpace(result.Limitation));
        }

        // --- Additional coverage beyond the brief's literal test theories: pair-conflict routing,
        //     recoverable-exception/parser-format downgrade to text fallback, Partial semantic pass-
        //     through, a hard strategy failure that leaks no raw text, the post-strategy result
        //     invariant, revision-load-error mapping, and working-tree/commit source independence. ---

        [Fact]
        public async Task PairConflictRoutesToTextFallbackWithConflictDiagnostic()
        {
            PlcRevision left = ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml, "Program.xml");
            PlcRevision right = ComparisonTestData.TextRevision(PlcRevisionSide.Right, SclText, "Program.scl");
            IPlcComparisonCoordinator coordinator = CreateCoordinator(TextStrategy);

            PlcComparisonResult result = await coordinator.CompareAsync(left, right, CancellationToken.None);

            Assert.Equal(PlcArtifactKind.Text, result.ArtifactKind);
            Assert.Equal(PlcSupportLevel.Fallback, result.SupportLevel);
            Assert.Contains(result.Diagnostics, d => d.Code == "CMP-CLASS-CONFLICT");
            Assert.False(string.IsNullOrWhiteSpace(result.Limitation));
        }

        [Fact]
        public async Task RecoverableStrategyExceptionWithRawTextBecomesTextFallback()
        {
            PlcComparisonDiagnostic diagnostic = new ComparisonDiagnosticSanitizer().ForUser(
                "CMP-FBD-001", PlcDiagnosticSeverity.Warning, "Unresolved block reference.");
            var strategy = new ThrowingStrategy(PlcArtifactKind.Fbd,
                new RecoverableComparisonException("Unresolved block reference; showing a text comparison instead.", diagnostic));
            IPlcComparisonCoordinator coordinator = CreateCoordinator(strategy);

            PlcComparisonResult result = await coordinator.CompareAsync(LeftFbd, RightFbd, CancellationToken.None);

            Assert.Equal(ComparisonPresentationKind.Text, result.Presentation.Kind);
            Assert.Equal(PlcSupportLevel.Fallback, result.SupportLevel);
            Assert.NotNull(result.RawText);
            Assert.Contains(result.Diagnostics, d => d.Code == "CMP-FBD-001");
        }

        [Fact]
        public async Task MalformedSemanticInputFallsBackToTextWithRawSides()
        {
            var strategy = new ThrowingStrategy(PlcArtifactKind.Fbd, new FormatException("Malformed SimaticML block header."));
            IPlcComparisonCoordinator coordinator = CreateCoordinator(strategy);

            PlcComparisonResult result = await coordinator.CompareAsync(LeftFbd, RightFbd, CancellationToken.None);

            Assert.Equal(ComparisonPresentationKind.Text, result.Presentation.Kind);
            Assert.Equal(PlcSupportLevel.Fallback, result.SupportLevel);
            Assert.Equal(FbdXml, result.RawText!.LeftText);
            Assert.Equal(FbdXml, result.RawText!.RightText);
        }

        [Fact]
        public async Task RecoverableUnknownStructureReturnsSemanticPartialResult()
        {
            PlcComparisonResult partial = ResultFactory.CreateSemantic(
                ComparisonTestData.Context(PlcArtifactKind.Fbd, PlcComparisonMode.Visual, FbdXml, FbdXml),
                PlcComparisonMode.Visual, PlcSupportLevel.Partial,
                "Part of the logic network could not be resolved; showing a partial comparison.",
                Array.Empty<PlcComparisonDiagnostic>(), new StubLogicNetworkPresentation());
            IPlcComparisonCoordinator coordinator = CreateCoordinator(new RecordingStrategy(PlcArtifactKind.Fbd, partial));

            PlcComparisonResult result = await coordinator.CompareAsync(LeftFbd, RightFbd, CancellationToken.None);

            Assert.Equal(PlcSupportLevel.Partial, result.SupportLevel);
            Assert.Equal(ComparisonPresentationKind.LogicNetwork, result.Presentation.Kind);
            Assert.False(string.IsNullOrWhiteSpace(result.Limitation));
        }

        [Fact]
        public async Task StrategyExceptionBecomesHardErrorWithoutRawText()
        {
            var strategy = new ThrowingStrategy(PlcArtifactKind.Fbd, new InvalidOperationException(
                "failed at C:\\Users\\alice\\AppData\\Local\\Temp\\lease\\Program.xml token=abc123"));
            IPlcComparisonCoordinator coordinator = CreateCoordinator(strategy);

            PlcComparisonResult result = await coordinator.CompareAsync(LeftFbd, RightFbd, CancellationToken.None);

            Assert.Equal(ComparisonPresentationKind.Error, result.Presentation.Kind);
            Assert.Equal(PlcSupportLevel.Unsupported, result.SupportLevel);
            Assert.Null(result.RawText);
            Assert.DoesNotContain(result.Diagnostics, d => d.Message.IndexOf("abc123", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Fact]
        public async Task StrategyResultForWrongArtifactKindBecomesHardErrorWithInvariantCode()
        {
            PlcComparisonResult wrongKindResult = SemanticSclResult();
            IPlcComparisonCoordinator coordinator = CreateCoordinator(new RecordingStrategy(PlcArtifactKind.Fbd, wrongKindResult));

            PlcComparisonResult result = await coordinator.CompareAsync(LeftFbd, RightFbd, CancellationToken.None);

            Assert.Equal(ComparisonPresentationKind.Error, result.Presentation.Kind);
            Assert.Contains(result.Diagnostics, d => d.Code == "CMP-RESULT-INVARIANT");
        }

        [Fact]
        public async Task RoutingIsIndependentOfRevisionSource()
        {
            PlcRevision workingTreeLeft = PresentRevision(PlcRevisionSide.Left, PlcRevisionSource.WorkingTree);
            PlcRevision workingTreeRight = PresentRevision(PlcRevisionSide.Right, PlcRevisionSource.WorkingTree);
            PlcRevision commitLeft = PresentRevision(PlcRevisionSide.Left, PlcRevisionSource.Commit("deadbeef"));
            PlcRevision commitRight = PresentRevision(PlcRevisionSide.Right, PlcRevisionSource.Commit("deadbeef"));

            var workingTreeStrategy = new RecordingStrategy(PlcArtifactKind.Fbd, SemanticFbdResult());
            var commitStrategy = new RecordingStrategy(PlcArtifactKind.Fbd, SemanticFbdResult());

            PlcComparisonResult workingTreeResult = await CreateCoordinator(workingTreeStrategy)
                .CompareAsync(workingTreeLeft, workingTreeRight, CancellationToken.None);
            PlcComparisonResult commitResult = await CreateCoordinator(commitStrategy)
                .CompareAsync(commitLeft, commitRight, CancellationToken.None);

            Assert.Equal(1, workingTreeStrategy.CallCount);
            Assert.Equal(1, commitStrategy.CallCount);
            Assert.Equal(workingTreeResult.ArtifactKind, commitResult.ArtifactKind);
            Assert.Equal(workingTreeResult.ActualMode, commitResult.ActualMode);
            Assert.Equal(workingTreeResult.Presentation.Kind, commitResult.Presentation.Kind);
        }

        [Fact]
        public void CreateRevisionLoadErrorReturnsGenericLoadCodeForOtherExceptions()
        {
            IPlcComparisonCoordinator coordinator = CreateCoordinator();
            PlcComparisonResult result = coordinator.CreateRevisionLoadError(
                PlcArtifactKind.Fbd, PlcComparisonMode.Visual, new InvalidOperationException("boom"), PlcRevisionSide.Left);

            Assert.Equal(ComparisonPresentationKind.Error, result.Presentation.Kind);
            Assert.Equal(PlcSupportLevel.Unsupported, result.SupportLevel);
            Assert.Null(result.RawText);
            Assert.Contains(result.Diagnostics, d => d.Code == "CMP-REVISION-LOAD");
        }

        [Fact]
        public void CreateRevisionLoadErrorReturnsSizeLimitCodeForRevisionSizeLimitException()
        {
            IPlcComparisonCoordinator coordinator = CreateCoordinator();
            var exception = new RevisionSizeLimitException("Revision size (99 bytes) exceeds the 4-byte limit.");

            PlcComparisonResult result = coordinator.CreateRevisionLoadError(
                PlcArtifactKind.Unknown, PlcComparisonMode.Unsupported, exception, PlcRevisionSide.Right);

            Assert.Equal(ComparisonPresentationKind.Error, result.Presentation.Kind);
            Assert.Null(result.RawText);
            Assert.Contains(result.Diagnostics, d => d.Code == "CMP-REVISION-LIMIT");
        }

        [Fact]
        public void ConstructorRejectsNullArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new PlcComparisonCoordinator(
                null!, Array.Empty<IPlcComparisonStrategy>(), ResultFactory, new ComparisonDiagnosticSanitizer()));
            Assert.Throws<ArgumentNullException>(() => new PlcComparisonCoordinator(
                new PlcArtifactClassifier(), null!, ResultFactory, new ComparisonDiagnosticSanitizer()));
            Assert.Throws<ArgumentNullException>(() => new PlcComparisonCoordinator(
                new PlcArtifactClassifier(), Array.Empty<IPlcComparisonStrategy>(), null!, new ComparisonDiagnosticSanitizer()));
            Assert.Throws<ArgumentNullException>(() => new PlcComparisonCoordinator(
                new PlcArtifactClassifier(), Array.Empty<IPlcComparisonStrategy>(), ResultFactory, null!));
        }

        private static PlcComparisonCoordinator CreateCoordinator(params IPlcComparisonStrategy[] strategies)
            => new PlcComparisonCoordinator(new PlcArtifactClassifier(), strategies, ResultFactory, new ComparisonDiagnosticSanitizer());

        private static PlcComparisonResult SemanticFbdResult()
            => ResultFactory.CreateSemantic(
                ComparisonTestData.Context(PlcArtifactKind.Fbd, PlcComparisonMode.Visual, FbdXml, FbdXml),
                PlcComparisonMode.Visual, PlcSupportLevel.Full, string.Empty,
                Array.Empty<PlcComparisonDiagnostic>(), new StubLogicNetworkPresentation());

        private static PlcComparisonResult SemanticSclResult()
            => ResultFactory.CreateSemantic(
                ComparisonTestData.Context(PlcArtifactKind.Scl, PlcComparisonMode.Structured, SclText, SclText, "Program.scl"),
                PlcComparisonMode.Structured, PlcSupportLevel.Full, string.Empty,
                Array.Empty<PlcComparisonDiagnostic>(), new StubSclPresentation());

        private static PlcRevision RevisionFor(PlcArtifactKind kind, PlcRevisionSide side)
        {
            switch (kind)
            {
                case PlcArtifactKind.GenericXml:
                    return ComparisonTestData.TextRevision(side, "<root><value>plain xml</value></root>", "Program.xml");
                case PlcArtifactKind.Stl:
                    return ComparisonTestData.TextRevision(side, "A I 0.0", "Program.stl");
                case PlcArtifactKind.Sfc:
                    return ComparisonTestData.TextRevision(side, "SFC Test", "Program.sfc");
                case PlcArtifactKind.Binary:
                    return PlcRevision.Present(side, PlcRevisionSource.WorkingTree, "Program.bin",
                        new byte[] { 0x00, 0x01, 0x02 }, PlcTextEncoding.None, null, true,
                        "NUL bytes were found without a supported Unicode BOM.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static PlcRevision PresentRevision(PlcRevisionSide side, PlcRevisionSource source)
            => PlcRevision.Present(side, source, "Program.xml", Encoding.UTF8.GetBytes(FbdXml),
                PlcTextEncoding.Utf8WithoutBom, FbdXml, false, string.Empty);

        private sealed class RecordingStrategy : IPlcComparisonStrategy
        {
            private readonly PlcComparisonResult _result;

            public RecordingStrategy(PlcArtifactKind kind, PlcComparisonResult result)
            {
                SupportedKinds = new[] { kind };
                _result = result;
            }

            public IReadOnlyCollection<PlcArtifactKind> SupportedKinds { get; }
            public int CallCount { get; private set; }

            public Task<PlcComparisonResult> CompareAsync(PlcComparisonContext context, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CallCount++;
                return Task.FromResult(_result);
            }
        }

        private sealed class ThrowingStrategy : IPlcComparisonStrategy
        {
            private readonly Exception _exception;

            public ThrowingStrategy(PlcArtifactKind kind, Exception exception)
            {
                SupportedKinds = new[] { kind };
                _exception = exception;
            }

            public IReadOnlyCollection<PlcArtifactKind> SupportedKinds { get; }

            public Task<PlcComparisonResult> CompareAsync(PlcComparisonContext context, CancellationToken cancellationToken)
                => throw _exception;
        }

        private sealed class CancellingStrategy : IPlcComparisonStrategy
        {
            public IReadOnlyCollection<PlcArtifactKind> SupportedKinds { get; } = new[] { PlcArtifactKind.Fbd };

            public Task<PlcComparisonResult> CompareAsync(PlcComparisonContext context, CancellationToken cancellationToken)
                => throw new InvalidOperationException(
                    "The coordinator must never invoke a strategy once cancellation has already been requested.");
        }

        private sealed class StubLogicNetworkPresentation : LogicNetworkPresentation
        {
        }

        private sealed class StubSclPresentation : ComparisonPresentation
        {
            public StubSclPresentation() : base(ComparisonPresentationKind.Scl)
            {
            }
        }
    }
}
