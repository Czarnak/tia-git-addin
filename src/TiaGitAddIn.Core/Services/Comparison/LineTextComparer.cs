using System;
using System.Collections.Generic;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// Bounded line-based text comparer. Below <see cref="TextComparisonLimits.MaximumMatrixCells"/> it
    /// builds a full Wagner-Fischer style alignment (match/substitute/delete/insert, cost 0/1/1/1) and
    /// backtracks it into a diff; above that limit it falls back to an O(n+m) positional comparison.
    /// Display output is bounded on line count and per-line character length; the underlying
    /// <see cref="ComparisonRawText"/> passed in is never mutated or truncated.
    /// </summary>
    public sealed class LineTextComparer : ITextComparer
    {
        private readonly TextComparisonLimits _limits;

        public LineTextComparer(TextComparisonLimits limits)
        {
            _limits = limits ?? throw new ArgumentNullException(nameof(limits));
        }

        public TextPresentation Compare(ComparisonRawText rawText)
        {
            if (rawText == null) throw new ArgumentNullException(nameof(rawText));

            string[] leftLines = NormalizeAndSplitLines(rawText.LeftText);
            string[] rightLines = NormalizeAndSplitLines(rawText.RightText);

            (string[] boundedLeft, bool leftLinesCut) = BoundLineCount(leftLines);
            (string[] boundedRight, bool rightLinesCut) = BoundLineCount(rightLines);

            bool usedLinearFallback = (long)boundedLeft.Length * boundedRight.Length > _limits.MaximumMatrixCells;
            List<TextDiffLine> rawDiff = usedLinearFallback
                ? BuildLinearDiff(boundedLeft, boundedRight)
                : BuildBoundedDiff(boundedLeft, boundedRight);

            (List<TextDiffLine> lengthCappedDiff, bool anyLineLengthTruncated) = BoundLineLength(rawDiff);

            bool isTruncated = leftLinesCut || rightLinesCut || anyLineLengthTruncated;
            if (isTruncated)
            {
                lengthCappedDiff.Add(BuildOmittedRow(
                    leftLines.Length - boundedLeft.Length, rightLines.Length - boundedRight.Length, anyLineLengthTruncated));
            }

            return new TextPresentation(lengthCappedDiff, isTruncated, usedLinearFallback);
        }

        private static string[] NormalizeAndSplitLines(string? text)
        {
            if (text == null || text.Length == 0) return Array.Empty<string>();
            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            return normalized.Split('\n');
        }

        private (string[] lines, bool wasTruncated) BoundLineCount(string[] lines)
        {
            if (lines.Length <= _limits.MaximumLinesPerSide) return (lines, false);
            var bounded = new string[_limits.MaximumLinesPerSide];
            Array.Copy(lines, bounded, _limits.MaximumLinesPerSide);
            return (bounded, true);
        }

        private (List<TextDiffLine> lines, bool anyTruncated) BoundLineLength(List<TextDiffLine> lines)
        {
            bool anyTruncated = false;
            var result = new List<TextDiffLine>(lines.Count);
            foreach (TextDiffLine line in lines)
            {
                if (line.Text.Length > _limits.MaximumLineLength)
                {
                    anyTruncated = true;
                    result.Add(new TextDiffLine(line.Kind, line.LeftLineNumber, line.RightLineNumber,
                        line.Text.Substring(0, _limits.MaximumLineLength)));
                }
                else
                {
                    result.Add(line);
                }
            }

            return (result, anyTruncated);
        }

        private static TextDiffLine BuildOmittedRow(int leftOmittedCount, int rightOmittedCount, bool anyLineLengthTruncated)
        {
            var parts = new List<string>();
            if (leftOmittedCount > 0) parts.Add($"{leftOmittedCount} left line(s)");
            if (rightOmittedCount > 0) parts.Add($"{rightOmittedCount} right line(s)");

            string lineText = parts.Count > 0 ? $"{string.Join(" and ", parts)} omitted" : "Some content omitted";
            string lengthText = anyLineLengthTruncated ? " (some lines truncated to the display limit)" : string.Empty;
            return new TextDiffLine(TextDiffLineKind.Omitted, null, null, $"{lineText} due to display limits{lengthText}.");
        }

        /// <summary>Compares by position only, in O(n+m); an unequal position emits removal then addition.</summary>
        private static List<TextDiffLine> BuildLinearDiff(string[] left, string[] right)
        {
            var result = new List<TextDiffLine>();
            int max = Math.Max(left.Length, right.Length);

            for (int k = 0; k < max; k++)
            {
                bool hasLeft = k < left.Length;
                bool hasRight = k < right.Length;

                if (hasLeft && hasRight)
                {
                    AppendAlignedPair(result, left[k], k + 1, right[k], k + 1);
                }
                else if (hasLeft)
                {
                    result.Add(new TextDiffLine(TextDiffLineKind.Removed, k + 1, null, left[k]));
                }
                else
                {
                    result.Add(new TextDiffLine(TextDiffLineKind.Added, null, k + 1, right[k]));
                }
            }

            return result;
        }

        private static void AppendAlignedPair(List<TextDiffLine> result, string leftText, int leftLineNumber, string rightText, int rightLineNumber)
        {
            if (string.Equals(leftText, rightText, StringComparison.Ordinal))
            {
                result.Add(new TextDiffLine(TextDiffLineKind.Unchanged, leftLineNumber, rightLineNumber, leftText));
            }
            else
            {
                result.Add(new TextDiffLine(TextDiffLineKind.Removed, leftLineNumber, null, leftText));
                result.Add(new TextDiffLine(TextDiffLineKind.Added, null, rightLineNumber, rightText));
            }
        }

        private static List<TextDiffLine> BuildBoundedDiff(string[] left, string[] right)
        {
            int[,] dp = BuildAlignmentMatrix(left, right);
            List<DiffOp> ops = Backtrack(left, right, dp);
            ops.Reverse();
            return ExpandOps(ops, left, right);
        }

        private static int[,] BuildAlignmentMatrix(string[] left, string[] right)
        {
            int m = left.Length;
            int n = right.Length;
            var dp = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++) dp[i, 0] = i;
            for (int j = 0; j <= n; j++) dp[0, j] = j;

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    int diagCost = dp[i - 1, j - 1] + (string.Equals(left[i - 1], right[j - 1], StringComparison.Ordinal) ? 0 : 1);
                    int deleteCost = dp[i - 1, j] + 1;
                    int insertCost = dp[i, j - 1] + 1;
                    dp[i, j] = Math.Min(diagCost, Math.Min(deleteCost, insertCost));
                }
            }

            return dp;
        }

        /// <summary>
        /// Walks the alignment matrix from (m, n) back to (0, 0). An exact line match always takes the
        /// diagonal for free; that is always optimal for this cost model, so no tie-break is needed there.
        /// Otherwise, when multiple operations tie for the minimal cost, deletion (removal) is preferred
        /// over insertion (addition), and both are preferred over a diagonal substitution -- the
        /// deterministic tie-break this class documents for callers, since matched lines are unambiguous
        /// but a three-way cost tie is not resolved by the brief's own example tests.
        /// </summary>
        private static List<DiffOp> Backtrack(string[] left, string[] right, int[,] dp)
        {
            var ops = new List<DiffOp>();
            int i = left.Length;
            int j = right.Length;

            while (i > 0 || j > 0)
            {
                if (i > 0 && j > 0 && string.Equals(left[i - 1], right[j - 1], StringComparison.Ordinal))
                {
                    ops.Add(new DiffOp(DiffOpKind.Match, i - 1, j - 1));
                    i--; j--;
                    continue;
                }

                int current = dp[i, j];
                if (i > 0 && dp[i - 1, j] + 1 == current)
                {
                    ops.Add(new DiffOp(DiffOpKind.Delete, i - 1, -1));
                    i--;
                }
                else if (j > 0 && dp[i, j - 1] + 1 == current)
                {
                    ops.Add(new DiffOp(DiffOpKind.Insert, -1, j - 1));
                    j--;
                }
                else
                {
                    ops.Add(new DiffOp(DiffOpKind.Substitute, i - 1, j - 1));
                    i--; j--;
                }
            }

            return ops;
        }

        private static List<TextDiffLine> ExpandOps(List<DiffOp> ops, string[] left, string[] right)
        {
            var result = new List<TextDiffLine>(ops.Count);
            foreach (DiffOp op in ops)
            {
                switch (op.Kind)
                {
                    case DiffOpKind.Match:
                        result.Add(new TextDiffLine(TextDiffLineKind.Unchanged, op.LeftIndex + 1, op.RightIndex + 1, left[op.LeftIndex]));
                        break;
                    case DiffOpKind.Delete:
                        result.Add(new TextDiffLine(TextDiffLineKind.Removed, op.LeftIndex + 1, null, left[op.LeftIndex]));
                        break;
                    case DiffOpKind.Insert:
                        result.Add(new TextDiffLine(TextDiffLineKind.Added, null, op.RightIndex + 1, right[op.RightIndex]));
                        break;
                    case DiffOpKind.Substitute:
                        result.Add(new TextDiffLine(TextDiffLineKind.Removed, op.LeftIndex + 1, null, left[op.LeftIndex]));
                        result.Add(new TextDiffLine(TextDiffLineKind.Added, null, op.RightIndex + 1, right[op.RightIndex]));
                        break;
                }
            }

            return result;
        }

        private enum DiffOpKind { Match, Delete, Insert, Substitute }

        private readonly struct DiffOp
        {
            public DiffOp(DiffOpKind kind, int leftIndex, int rightIndex)
            {
                Kind = kind; LeftIndex = leftIndex; RightIndex = rightIndex;
            }

            public DiffOpKind Kind { get; }
            public int LeftIndex { get; }
            public int RightIndex { get; }
        }
    }
}
