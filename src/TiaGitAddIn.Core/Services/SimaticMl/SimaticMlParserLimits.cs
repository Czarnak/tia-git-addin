using System;

namespace TiaGitAddIn.Services.SimaticMl
{
    /// <summary>
    /// Deterministic, caller-overridable bounds for <see cref="SimaticMlParser.ParseText"/>. Every limit must
    /// be a positive count; there is no "unbounded" option, so a caller cannot accidentally disable the
    /// safety net that protects against oversized or maliciously deep SimaticML documents.
    /// </summary>
    public sealed class SimaticMlParserLimits
    {
        public SimaticMlParserLimits(int maximumCharactersInDocument, int maximumElementCount, int maximumDepth)
        {
            if (maximumCharactersInDocument <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCharactersInDocument));
            if (maximumElementCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumElementCount));
            if (maximumDepth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));

            MaximumCharactersInDocument = maximumCharactersInDocument;
            MaximumElementCount = maximumElementCount;
            MaximumDepth = maximumDepth;
        }

        public int MaximumCharactersInDocument { get; }
        public int MaximumElementCount { get; }
        public int MaximumDepth { get; }

        public static SimaticMlParserLimits Default { get; } = new SimaticMlParserLimits(16_777_216, 250_000, 128);
    }
}
