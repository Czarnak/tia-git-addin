using System;
using System.Collections.Generic;
using System.Text;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Comparison
{
    public sealed class PlcComparisonContractTests
    {
        [Fact]
        public void PresentRevisionDefensivelyCopiesBytes()
        {
            byte[] input = Encoding.UTF8.GetBytes("left");
            PlcRevision revision = PlcRevision.Present(
                PlcRevisionSide.Left,
                PlcRevisionSource.WorkingTree,
                "Program.xml",
                input,
                PlcTextEncoding.Utf8WithoutBom,
                "left",
                false,
                string.Empty);

            input[0] = (byte)'X';

            Assert.Equal((byte)'l', revision.Bytes[0]);
            Assert.IsAssignableFrom<IReadOnlyList<byte>>(revision.Bytes);
        }

        [Theory]
        [InlineData(PlcSupportLevel.Full, "unexpected limitation")]
        [InlineData(PlcSupportLevel.Partial, "")]
        [InlineData(PlcSupportLevel.Fallback, " ")]
        [InlineData(PlcSupportLevel.Unsupported, "")]
        public void ResultRejectsInvalidLimitationInvariant(PlcSupportLevel support, string limitation)
        {
            Assert.Throws<ArgumentException>(() => new PlcComparisonResult(
                PlcArtifactKind.Text,
                PlcComparisonMode.Text,
                PlcComparisonMode.Text,
                support,
                limitation,
                Array.Empty<PlcComparisonDiagnostic>(),
                new TextPresentation(Array.Empty<TextDiffLine>()),
                new ComparisonRawText("left", "right", false, false)));
        }

        [Fact]
        public void ResultRejectsPresentationModeMismatch()
        {
            Assert.Throws<ArgumentException>(() => new PlcComparisonResult(
                PlcArtifactKind.Fbd,
                PlcComparisonMode.Visual,
                PlcComparisonMode.Visual,
                PlcSupportLevel.Full,
                string.Empty,
                Array.Empty<PlcComparisonDiagnostic>(),
                new TextPresentation(Array.Empty<TextDiffLine>()),
                new ComparisonRawText("left", "right", false, false)));
        }

        [Theory]
        [MemberData(nameof(SupportLevelPresentationCases))]
        public void FactoryProducesInvariantCompliantResultForEverySupportLevel(
            string scenario,
            Func<PlcComparisonResult> createResult,
            PlcSupportLevel expectedSupportLevel,
            ComparisonPresentationKind expectedPresentationKind)
        {
            PlcComparisonResult result = createResult();

            Assert.Equal(expectedSupportLevel, result.SupportLevel);
            Assert.Equal(expectedPresentationKind, result.Presentation.Kind);
            Assert.Equal(expectedSupportLevel == PlcSupportLevel.Full, result.Limitation.Length == 0);
            Assert.False(string.IsNullOrEmpty(scenario));
        }

        public static IEnumerable<object[]> SupportLevelPresentationCases()
        {
            var factory = new PlcComparisonResultFactory(new StubTextComparer());

            yield return new object[]
            {
                "Full",
                (Func<PlcComparisonResult>)(() => factory.CreateSemantic(
                    ComparisonTestData.Context(PlcArtifactKind.Fbd, PlcComparisonMode.Visual),
                    PlcComparisonMode.Visual,
                    PlcSupportLevel.Full,
                    string.Empty,
                    Array.Empty<PlcComparisonDiagnostic>(),
                    new StubLogicNetworkPresentation())),
                PlcSupportLevel.Full,
                ComparisonPresentationKind.LogicNetwork,
            };

            yield return new object[]
            {
                "Partial",
                (Func<PlcComparisonResult>)(() => factory.CreateSemantic(
                    ComparisonTestData.Context(PlcArtifactKind.Scl, PlcComparisonMode.Structured),
                    PlcComparisonMode.Structured,
                    PlcSupportLevel.Partial,
                    "Unresolved cross reference; interface comparison only.",
                    Array.Empty<PlcComparisonDiagnostic>(),
                    new StubInterfacePresentation())),
                PlcSupportLevel.Partial,
                ComparisonPresentationKind.Interface,
            };

            yield return new object[]
            {
                "Fallback",
                (Func<PlcComparisonResult>)(() => factory.CreateTextFallback(
                    ComparisonTestData.Context(PlcArtifactKind.GenericXml, PlcComparisonMode.Text),
                    "Generic XML semantic comparison is unavailable.",
                    Array.Empty<PlcComparisonDiagnostic>())),
                PlcSupportLevel.Fallback,
                ComparisonPresentationKind.Text,
            };

            yield return new object[]
            {
                "Unsupported",
                (Func<PlcComparisonResult>)(() => factory.CreateUnsupported(
                    ComparisonTestData.Context(PlcArtifactKind.Binary, PlcComparisonMode.Unsupported),
                    "Binary content is unsupported.",
                    Array.Empty<PlcComparisonDiagnostic>())),
                PlcSupportLevel.Unsupported,
                ComparisonPresentationKind.Unsupported,
            };

            yield return new object[]
            {
                "Error",
                (Func<PlcComparisonResult>)(() => factory.CreateHardError(
                    PlcArtifactKind.Fbd,
                    PlcComparisonMode.Visual,
                    "The requested revision could not be loaded.",
                    Array.Empty<PlcComparisonDiagnostic>())),
                PlcSupportLevel.Unsupported,
                ComparisonPresentationKind.Error,
            };
        }

        private sealed class StubTextComparer : ITextComparer
        {
            public TextPresentation Compare(ComparisonRawText rawText) => new TextPresentation(Array.Empty<TextDiffLine>());
        }

        private sealed class StubLogicNetworkPresentation : LogicNetworkPresentation
        {
        }

        private sealed class StubInterfacePresentation : ComparisonPresentation
        {
            public StubInterfacePresentation() : base(ComparisonPresentationKind.Interface)
            {
            }
        }
    }
}
