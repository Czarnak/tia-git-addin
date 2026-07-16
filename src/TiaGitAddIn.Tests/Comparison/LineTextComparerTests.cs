using System;
using System.Linq;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Comparison
{
    public sealed class LineTextComparerTests
    {
        [Fact]
        public void CompareRetainsIndependentLineNumbersAndChangeKinds()
        {
            var comparer = new LineTextComparer(TextComparisonLimits.Default);
            TextPresentation result = comparer.Compare(new ComparisonRawText(
                "same\nremoved\nold", "same\nadded\nnew", false, false));

            Assert.Collection(result.Lines,
                line => Assert.Equal(TextDiffLineKind.Unchanged, line.Kind),
                line => Assert.Equal(TextDiffLineKind.Removed, line.Kind),
                line => Assert.Equal(TextDiffLineKind.Added, line.Kind),
                line => Assert.Equal(TextDiffLineKind.Removed, line.Kind),
                line => Assert.Equal(TextDiffLineKind.Added, line.Kind));
            Assert.Equal(2, result.Lines[1].LeftLineNumber);
            Assert.Null(result.Lines[1].RightLineNumber);
            Assert.Null(result.Lines[2].LeftLineNumber);
            Assert.Equal(2, result.Lines[2].RightLineNumber);
        }

        [Fact]
        public void CompareSwitchesToBoundedLinearDiffAboveMatrixLimit()
        {
            var limits = new TextComparisonLimits(100, 100, maximumMatrixCells: 4);
            var comparer = new LineTextComparer(limits);
            TextPresentation result = comparer.Compare(new ComparisonRawText("a\nb\nc", "a\nx\nc", false, false));
            Assert.True(result.UsedLinearFallback);
            Assert.Contains(result.Lines, line => line.Kind == TextDiffLineKind.Removed && line.Text == "b");
            Assert.Contains(result.Lines, line => line.Kind == TextDiffLineKind.Added && line.Text == "x");
        }

        [Fact]
        public void CompareTruncatesDisplayAtConfiguredLineAndLengthLimits()
        {
            var comparer = new LineTextComparer(new TextComparisonLimits(2, 3, 100));
            TextPresentation result = comparer.Compare(new ComparisonRawText("abcdef\nline2\nline3", "abcdef\nline2\nline3", false, false));
            Assert.True(result.IsTruncated);
            Assert.Contains(result.Lines, line => line.Kind == TextDiffLineKind.Omitted);
            Assert.All(result.Lines.Where(line => line.Kind != TextDiffLineKind.Omitted), line => Assert.True(line.Text.Length <= 3));
        }

        // --- Supplementary coverage beyond the brief's literal test theories: the exact matrix-cell
        //     boundary, CRLF/CR normalization, truncation independence, the deterministic tie-break for
        //     cases the brief's tests never exercise, raw-text immutability, and constructor guards. ---

        [Fact]
        public void MatrixCellLimitBoundaryUsesBoundedPathExactlyAtLimit()
        {
            // 2x2 = 4 cells, exactly at the limit -> bounded path.
            var atLimit = new LineTextComparer(new TextComparisonLimits(100, 100, maximumMatrixCells: 4));
            TextPresentation atLimitResult = atLimit.Compare(new ComparisonRawText("a\nb", "a\nx", false, false));
            Assert.False(atLimitResult.UsedLinearFallback);

            // 2x3 = 6 cells, one side larger, exceeds a limit of 4 -> linear fallback.
            var overLimit = new LineTextComparer(new TextComparisonLimits(100, 100, maximumMatrixCells: 4));
            TextPresentation overLimitResult = overLimit.Compare(new ComparisonRawText("a\nb", "a\nx\ny", false, false));
            Assert.True(overLimitResult.UsedLinearFallback);
        }

        [Theory]
        [InlineData("same\r\nremoved\r\nold", "same\r\nadded\r\nnew")]
        [InlineData("same\rremoved\rold", "same\radded\rnew")]
        public void CrLfAndCrLineEndingsNormalizeToLfForDisplay(string left, string right)
        {
            var comparer = new LineTextComparer(TextComparisonLimits.Default);
            TextPresentation result = comparer.Compare(new ComparisonRawText(left, right, false, false));

            Assert.Collection(result.Lines,
                line => Assert.Equal("same", line.Text),
                line => Assert.Equal("removed", line.Text),
                line => Assert.Equal("added", line.Text),
                line => Assert.Equal("old", line.Text),
                line => Assert.Equal("new", line.Text));
        }

        [Fact]
        public void TruncationAppendsExactlyOneOmittedRowRegardlessOfHowManyLinesAreCut()
        {
            string left = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"left{i}"));
            string right = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"right{i}"));
            var comparer = new LineTextComparer(new TextComparisonLimits(3, 100, 1_000_000));

            TextPresentation result = comparer.Compare(new ComparisonRawText(left, right, false, false));

            Assert.True(result.IsTruncated);
            Assert.Single(result.Lines, line => line.Kind == TextDiffLineKind.Omitted);
        }

        [Fact]
        public void CharacterLengthTruncationAloneSetsIsTruncated()
        {
            var comparer = new LineTextComparer(new TextComparisonLimits(100, 3, 100));
            TextPresentation result = comparer.Compare(new ComparisonRawText("abcdef", "abcdef", false, false));

            Assert.True(result.IsTruncated);
            Assert.Single(result.Lines, line => line.Kind == TextDiffLineKind.Omitted);
        }

        [Fact]
        public void LineCountTruncationAloneSetsIsTruncated()
        {
            var comparer = new LineTextComparer(new TextComparisonLimits(1, 100, 100));
            TextPresentation result = comparer.Compare(new ComparisonRawText("a\nb", "a\nb", false, false));

            Assert.True(result.IsTruncated);
            Assert.Single(result.Lines, line => line.Kind == TextDiffLineKind.Omitted);
        }

        [Fact]
        public void CompareNeverMutatesTheSourceRawText()
        {
            var rawText = new ComparisonRawText("left1\nleft2", "right1\nright2", false, false);
            var comparer = new LineTextComparer(new TextComparisonLimits(1, 3, 1));

            comparer.Compare(rawText);

            Assert.Equal("left1\nleft2", rawText.LeftText);
            Assert.Equal("right1\nright2", rawText.RightText);
        }

        [Fact]
        public void BoundedDiffTieBreaksDeterministicallyAsRemovalThenAddition()
        {
            // A genuine 3-way DP tie the brief's own tests never exercise. This documents (and pins,
            // for regression purposes) this implementation's chosen deterministic resolution: prefer
            // removal, then addition, then a paired substitution, when multiple operations tie for the
            // minimal edit cost.
            var comparer = new LineTextComparer(TextComparisonLimits.Default);
            TextPresentation result = comparer.Compare(new ComparisonRawText("p\nq", "q\np", false, false));

            Assert.Collection(result.Lines,
                line => { Assert.Equal(TextDiffLineKind.Added, line.Kind); Assert.Equal("q", line.Text); Assert.Equal(1, line.RightLineNumber); },
                line => { Assert.Equal(TextDiffLineKind.Unchanged, line.Kind); Assert.Equal("p", line.Text); Assert.Equal(1, line.LeftLineNumber); Assert.Equal(2, line.RightLineNumber); },
                line => { Assert.Equal(TextDiffLineKind.Removed, line.Kind); Assert.Equal("q", line.Text); Assert.Equal(2, line.LeftLineNumber); });
        }

        [Theory]
        [InlineData(0, 10, 10)]
        [InlineData(10, 0, 10)]
        [InlineData(10, 10, 0)]
        public void ConstructorRejectsNonPositiveLimits(int maximumLinesPerSide, int maximumLineLength, long maximumMatrixCells)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TextComparisonLimits(maximumLinesPerSide, maximumLineLength, maximumMatrixCells));
        }

        [Fact]
        public void DefaultLimitsMatchSpecifiedValues()
        {
            Assert.Equal(20_000, TextComparisonLimits.Default.MaximumLinesPerSide);
            Assert.Equal(32_768, TextComparisonLimits.Default.MaximumLineLength);
            Assert.Equal(4_000_000, TextComparisonLimits.Default.MaximumMatrixCells);
        }
    }
}
