using System;
using System.Collections.Generic;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.Mapping;
using TiaGitAddIn.UI.ViewModels.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class ComparisonPresentationMapperTests
    {
        [Theory]
        [MemberData(nameof(FoundationResults))]
        public void MapCreatesOneCompatibleViewModelWithSharedMetadata(
            PlcComparisonResult result, Type expectedType, string expectedHeader)
        {
            IComparisonPresentationMapper mapper = CreateMapper();

            ComparisonPresentationViewModel viewModel = mapper.Map(result);

            Assert.IsType(expectedType, viewModel);
            Assert.Equal(expectedHeader, viewModel.Header);
            Assert.Equal(result.Limitation, viewModel.Limitation);
            Assert.Equal(result.RawText != null, viewModel.HasRawText);
        }

        [Fact]
        public void MapRejectsZeroOrMultipleFactories()
        {
            PlcComparisonResult result = TextFallbackResult();

            Assert.Throws<InvalidOperationException>(() =>
                new ComparisonPresentationMapper(Array.Empty<IComparisonPresentationViewModelFactory>()).Map(result));

            Assert.Throws<InvalidOperationException>(() =>
                new ComparisonPresentationMapper(new IComparisonPresentationViewModelFactory[]
                { new TextPresentationViewModelFactory(), new TextPresentationViewModelFactory() }).Map(result));
        }

        [Fact]
        public void MapThrowsForNullResult()
        {
            IComparisonPresentationMapper mapper = CreateMapper();

            Assert.Throws<ArgumentNullException>(() => mapper.Map(null!));
        }

        public static IEnumerable<object[]> FoundationResults()
        {
            PlcComparisonResult interfaceResult = InterfaceFullResult();
            yield return new object[]
            {
                interfaceResult,
                typeof(InterfaceComparisonViewModel),
                $"{interfaceResult.ActualMode} · {interfaceResult.SupportLevel}",
            };

            PlcComparisonResult ladResult = LadPartialResult();
            yield return new object[]
            {
                ladResult,
                typeof(LadComparisonViewModel),
                $"{ladResult.ActualMode} · {ladResult.SupportLevel}",
            };

            PlcComparisonResult textResult = TextFallbackResult();
            yield return new object[]
            {
                textResult,
                typeof(TextComparisonViewModel),
                $"{textResult.ActualMode} · {textResult.SupportLevel}",
            };

            PlcComparisonResult unsupportedResult = UnsupportedResult();
            yield return new object[]
            {
                unsupportedResult,
                typeof(UnsupportedComparisonViewModel),
                $"{unsupportedResult.ActualMode} · {unsupportedResult.SupportLevel}",
            };

            PlcComparisonResult errorResult = ErrorResult();
            yield return new object[]
            {
                errorResult,
                typeof(ErrorComparisonViewModel),
                $"{errorResult.ActualMode} · {errorResult.SupportLevel}",
            };
        }

        private static IComparisonPresentationMapper CreateMapper() =>
            new ComparisonPresentationMapper(new IComparisonPresentationViewModelFactory[]
            {
                new InterfacePresentationViewModelFactory(),
                new TextPresentationViewModelFactory(),
                new UnsupportedPresentationViewModelFactory(),
                new ErrorPresentationViewModelFactory(),
                new LadPresentationViewModelFactory(new FileLogger(), ImmediateUiDispatcher.Instance),
            });

        private static PlcComparisonResult InterfaceFullResult()
        {
            InterfaceSectionSnapshot section = Section("Input", Member("Input", "Alarm", "Bool"));
            var presentation = new InterfacePresentation(new[]
            {
                new InterfaceSectionComparison(InterfaceChangeKind.Unchanged, section, section, new[]
                {
                    new InterfaceMemberComparison(
                        InterfaceChangeKind.Unchanged,
                        section.Members[0],
                        section.Members[0],
                        Array.Empty<InterfaceFieldChange>(),
                        Array.Empty<InterfaceMemberComparison>()),
                }),
            });

            return new PlcComparisonResult(
                PlcArtifactKind.Fbd,
                PlcComparisonMode.Structured,
                PlcComparisonMode.Structured,
                PlcSupportLevel.Full,
                string.Empty,
                Array.Empty<PlcComparisonDiagnostic>(),
                presentation,
                null);
        }

        private static PlcComparisonResult LadPartialResult()
        {
            var sactResult = new SactCompareResult
            {
                State = CompareState.Changed,
                Content = new SactContentResult(),
            };
            InterfaceSectionSnapshot section = Section("Input", Member("Input", "Alarm", "Bool"));
            var interfacePresentation = new InterfacePresentation(new[]
            {
                new InterfaceSectionComparison(InterfaceChangeKind.Unchanged, section, section, Array.Empty<InterfaceMemberComparison>()),
            });
            var ladPresentation = new LadPresentation(sactResult, interfacePresentation);

            var diagnostics = new[]
            {
                new PlcComparisonDiagnostic("LAD001", PlcDiagnosticSeverity.Warning, "Ladder rendering used a best-effort layout."),
            };

            return new PlcComparisonResult(
                PlcArtifactKind.Lad,
                PlcComparisonMode.Visual,
                PlcComparisonMode.Visual,
                PlcSupportLevel.Partial,
                "Ladder rendering is best-effort; interface comparison is precise.",
                diagnostics,
                ladPresentation,
                null);
        }

        private static PlcComparisonResult TextFallbackResult()
        {
            var lines = new[]
            {
                new TextDiffLine(TextDiffLineKind.Unchanged, 1, 1, "NETWORK 1"),
                new TextDiffLine(TextDiffLineKind.Added, null, 2, "NEW LINE"),
            };
            var presentation = new TextPresentation(lines);
            var rawText = new ComparisonRawText("NETWORK 1", "NETWORK 1\r\nNEW LINE", false, false);

            return new PlcComparisonResult(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Text,
                PlcComparisonMode.Text,
                PlcSupportLevel.Fallback,
                "Fell back to line-based text comparison.",
                Array.Empty<PlcComparisonDiagnostic>(),
                presentation,
                rawText);
        }

        private static PlcComparisonResult UnsupportedResult() =>
            new PlcComparisonResult(
                PlcArtifactKind.Binary,
                PlcComparisonMode.Unsupported,
                PlcComparisonMode.Unsupported,
                PlcSupportLevel.Unsupported,
                "Binary artifacts cannot be compared.",
                Array.Empty<PlcComparisonDiagnostic>(),
                new UnsupportedPresentation(),
                null);

        private static PlcComparisonResult ErrorResult() =>
            new PlcComparisonResult(
                PlcArtifactKind.Unknown,
                PlcComparisonMode.Unsupported,
                PlcComparisonMode.Unsupported,
                PlcSupportLevel.Unsupported,
                "Comparison failed: the parser threw an unexpected exception.",
                Array.Empty<PlcComparisonDiagnostic>(),
                new ErrorPresentation(),
                null);

        private static InterfaceSectionSnapshot Section(string name, params InterfaceMemberSnapshot[] members) =>
            new InterfaceSectionSnapshot(name, true, members);

        private static InterfaceMemberSnapshot Member(string section, string name, string datatype) =>
            new InterfaceMemberSnapshot(
                section,
                name,
                name,
                datatype,
                SemanticBoolean.Unspecified,
                null,
                null,
                new Dictionary<string, string>(),
                null,
                SemanticBoolean.Unspecified,
                null,
                new Dictionary<string, string>(),
                Array.Empty<InterfaceMemberSnapshot>());
    }
}
