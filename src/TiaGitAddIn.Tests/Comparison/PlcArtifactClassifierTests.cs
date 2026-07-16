using System;
using System.Collections.Generic;
using System.Text;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Comparison
{
    public sealed class PlcArtifactClassifierTests
    {
        private const string FbdXml = "<SW.Blocks.FB><AttributeList><ProgrammingLanguage>FBD</ProgrammingLanguage></AttributeList></SW.Blocks.FB>";
        private const string ValidScl = "FUNCTION_BLOCK Motor\nBEGIN\nEND_FUNCTION_BLOCK";

        [Theory]
        [InlineData("Neutral/Program.xml", "<SW.Blocks.FB><AttributeList><ProgrammingLanguage>LAD</ProgrammingLanguage></AttributeList></SW.Blocks.FB>", PlcArtifactKind.Lad, PlcComparisonMode.Visual)]
        [InlineData("Neutral/Program.xml", "<SW.Blocks.FB><AttributeList><ProgrammingLanguage>FBD</ProgrammingLanguage></AttributeList></SW.Blocks.FB>", PlcArtifactKind.Fbd, PlcComparisonMode.Visual)]
        [InlineData("Neutral/Program.scl", "FUNCTION_BLOCK Motor\nBEGIN\nEND_FUNCTION_BLOCK", PlcArtifactKind.Scl, PlcComparisonMode.Structured)]
        [InlineData("SCL/Program.xml", "<root><value>plain xml</value></root>", PlcArtifactKind.GenericXml, PlcComparisonMode.Text)]
        [InlineData("Neutral/Program.stl", "A I 0.0", PlcArtifactKind.Stl, PlcComparisonMode.Text)]
        [InlineData("Neutral/Program.sfc", "SFC Test", PlcArtifactKind.Sfc, PlcComparisonMode.Text)]
        public void ClassifyUsesTheEvidenceMatrix(string path, string text, PlcArtifactKind kind, PlcComparisonMode mode)
        {
            PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, text, path);
            PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);
            Assert.Equal(kind, result.ArtifactKind);
            Assert.Equal(mode, result.PreferredMode);
            Assert.NotEmpty(result.Evidence);
        }

        [Fact]
        public void ValidSclSuffixWithoutLexicalEvidenceFallsBackToTextWithDiagnostic()
        {
            PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, "ordinary notes", "Program.scl");
            PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);
            Assert.Equal(PlcArtifactKind.Text, result.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Text, result.PreferredMode);
            Assert.Contains(result.Evidence, value => value.IndexOf("invalid-scl-evidence", StringComparison.Ordinal) >= 0);
        }

        [Fact]
        public void ConflictingFbdAndSclSidesResolveToTextFallback()
        {
            PlcRevision left = ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml, "Program.xml");
            PlcRevision right = ComparisonTestData.TextRevision(PlcRevisionSide.Right, ValidScl, "Program.scl");
            PlcArtifactPairDescriptor pair = new PlcArtifactClassifier().Resolve(left, right);
            Assert.Equal(PlcArtifactKind.Text, pair.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Text, pair.RequestedMode);
            Assert.Contains(pair.Diagnostics, d => d.Code == "CMP-CLASS-CONFLICT");
            Assert.False(string.IsNullOrWhiteSpace(pair.Limitation));
        }

        [Theory]
        [InlineData(true, PlcPairChangeKind.Added)]
        [InlineData(false, PlcPairChangeKind.Removed)]
        public void MissingSideClassifiesFromAvailableSide(bool leftMissing, PlcPairChangeKind expected)
        {
            PlcRevision left = leftMissing ? ComparisonTestData.MissingRevision(PlcRevisionSide.Left)
                : ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml);
            PlcRevision right = leftMissing ? ComparisonTestData.TextRevision(PlcRevisionSide.Right, FbdXml)
                : PlcRevision.Missing(PlcRevisionSide.Right, PlcRevisionSource.WorkingTree, "Program.xml", PlcRevisionMissingReason.Deleted);
            PlcArtifactPairDescriptor pair = new PlcArtifactClassifier().Resolve(left, right);
            Assert.Equal(expected, pair.ChangeKind);
            Assert.Equal(PlcArtifactKind.Fbd, pair.ArtifactKind);
        }

        // --- Supplementary coverage beyond the brief's literal test theories: precedence-order edges,
        //     the binary branch, source-kind independence, and the argument-validation guards. ---

        [Fact]
        public void ClassifyReturnsBinaryUnsupportedForUndecodedContent()
        {
            PlcRevision revision = PlcRevision.Present(PlcRevisionSide.Left, PlcRevisionSource.WorkingTree, "Program.bin",
                new byte[] { 0x00, 0x01, 0x02 }, PlcTextEncoding.None, null, true,
                "NUL bytes were found without a supported Unicode BOM.");

            PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);

            Assert.Equal(PlcArtifactKind.Binary, result.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Unsupported, result.PreferredMode);
            Assert.NotEmpty(result.Evidence);
        }

        [Theory]
        [MemberData(nameof(RevisionSources))]
        public void ClassificationIsIndependentOfRevisionSource(PlcRevisionSource source)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(FbdXml);
            PlcRevision revision = PlcRevision.Present(PlcRevisionSide.Left, source, "Program.xml", bytes,
                PlcTextEncoding.Utf8WithoutBom, FbdXml, false, string.Empty);

            PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);

            Assert.Equal(PlcArtifactKind.Fbd, result.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Visual, result.PreferredMode);
        }

        public static IEnumerable<object[]> RevisionSources()
        {
            yield return new object[] { PlcRevisionSource.WorkingTree };
            yield return new object[] { PlcRevisionSource.Head };
            yield return new object[] { PlcRevisionSource.Commit("deadbeef") };
            yield return new object[] { PlcRevisionSource.ParentOfCommit("deadbeef") };
        }

        [Fact]
        public void SimaticMlContentWinsOverSclSuffixByPrecedence()
        {
            // The suffix says .scl, but well-formed SimaticML/FBD content matches the earlier
            // precedence rule (3), which must win over the suffix-driven SCL rule (4).
            PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml, "Program.scl");

            PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);

            Assert.Equal(PlcArtifactKind.Fbd, result.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Visual, result.PreferredMode);
        }

        [Fact]
        public void SimaticMlBlockWithUnsupportedLanguageFallsThroughToGenericXml()
        {
            const string stlProgrammingLanguageXml =
                "<SW.Blocks.FB><AttributeList><ProgrammingLanguage>STL</ProgrammingLanguage></AttributeList></SW.Blocks.FB>";
            PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, stlProgrammingLanguageXml, "Program.xml");

            PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);

            Assert.Equal(PlcArtifactKind.GenericXml, result.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Text, result.PreferredMode);
        }

        [Fact]
        public void PlainTextWithoutSpecialSuffixFallsBackToText()
        {
            PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, "hello world", "Notes.txt");

            PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);

            Assert.Equal(PlcArtifactKind.Text, result.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Text, result.PreferredMode);
            Assert.NotEmpty(result.Evidence);
        }

        [Fact]
        public void ResolveReturnsBinaryUnsupportedWhenEitherSideIsBinary()
        {
            PlcRevision left = PlcRevision.Present(PlcRevisionSide.Left, PlcRevisionSource.WorkingTree, "Program.bin",
                new byte[] { 0x00, 0x01 }, PlcTextEncoding.None, null, true,
                "NUL bytes were found without a supported Unicode BOM.");
            PlcRevision right = ComparisonTestData.TextRevision(PlcRevisionSide.Right, FbdXml, "Program.xml");

            PlcArtifactPairDescriptor pair = new PlcArtifactClassifier().Resolve(left, right);

            Assert.Equal(PlcArtifactKind.Binary, pair.ArtifactKind);
            Assert.Equal(PlcComparisonMode.Unsupported, pair.RequestedMode);
            Assert.Equal(PlcPairChangeKind.Modified, pair.ChangeKind);
            Assert.NotNull(pair.Left);
            Assert.NotNull(pair.Right);
        }

        [Fact]
        public void ResolveThrowsWhenBothSidesAreMissing()
        {
            PlcRevision left = ComparisonTestData.MissingRevision(PlcRevisionSide.Left);
            PlcRevision right = ComparisonTestData.MissingRevision(PlcRevisionSide.Right);

            Assert.Throws<ArgumentException>(() => new PlcArtifactClassifier().Resolve(left, right));
        }

        [Fact]
        public void ClassifyThrowsForMissingRevision()
        {
            PlcRevision revision = ComparisonTestData.MissingRevision(PlcRevisionSide.Left);

            Assert.Throws<ArgumentException>(() => new PlcArtifactClassifier().Classify(revision));
        }

        [Fact]
        public void ClassifyThrowsForNullRevision()
            => Assert.Throws<ArgumentNullException>(() => new PlcArtifactClassifier().Classify(null!));

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResolveThrowsForNullSide(bool leftIsNull)
        {
            PlcRevision present = ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml);
            var classifier = new PlcArtifactClassifier();

            Assert.Throws<ArgumentNullException>(() => leftIsNull
                ? classifier.Resolve(null!, present)
                : classifier.Resolve(present, null!));
        }
    }
}
