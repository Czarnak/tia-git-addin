using System;
using TiaGitAddIn.Services.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Comparison
{
    public sealed class SclLexicalProbeTests
    {
        [Theory]
        [InlineData("FUNCTION_BLOCK Motor\nBEGIN\nEND_FUNCTION_BLOCK", "FUNCTION_BLOCK", "END_FUNCTION_BLOCK")]
        [InlineData("FUNCTION Add : Int\nEND_FUNCTION", "FUNCTION", "END_FUNCTION")]
        [InlineData("ORGANIZATION_BLOCK Main\nEND_ORGANIZATION_BLOCK", "ORGANIZATION_BLOCK", "END_ORGANIZATION_BLOCK")]
        [InlineData("DATA_BLOCK Db1\nEND_DATA_BLOCK", "DATA_BLOCK", "END_DATA_BLOCK")]
        [InlineData("TYPE MyUdt\nEND_TYPE", "TYPE", "END_TYPE")]
        [InlineData("function_block motor\nend_function_block", "FUNCTION_BLOCK", "END_FUNCTION_BLOCK")]
        public void HasTopLevelBlockEvidenceFindsRecognizedOpenerTerminatorPairs(string text, string expectedOpener, string expectedTerminator)
        {
            bool found = SclLexicalProbe.HasTopLevelBlockEvidence(text, out string opener, out string terminator);

            Assert.True(found);
            Assert.Equal(expectedOpener, opener);
            Assert.Equal(expectedTerminator, terminator);
        }

        [Fact]
        public void HasTopLevelBlockEvidenceReturnsFalseForOrdinaryText()
        {
            bool found = SclLexicalProbe.HasTopLevelBlockEvidence("ordinary notes", out string opener, out string terminator);

            Assert.False(found);
            Assert.Equal(string.Empty, opener);
            Assert.Equal(string.Empty, terminator);
        }

        [Fact]
        public void HasTopLevelBlockEvidenceIgnoresMarkersInsideLineComments()
        {
            const string text = "// FUNCTION_BLOCK Motor\n// END_FUNCTION_BLOCK\nordinary notes";

            bool found = SclLexicalProbe.HasTopLevelBlockEvidence(text, out _, out _);

            Assert.False(found);
        }

        [Fact]
        public void HasTopLevelBlockEvidenceIgnoresMarkersInsideBlockComments()
        {
            const string text = "(* FUNCTION_BLOCK Motor END_FUNCTION_BLOCK *)\nordinary notes";

            bool found = SclLexicalProbe.HasTopLevelBlockEvidence(text, out _, out _);

            Assert.False(found);
        }

        [Fact]
        public void HasTopLevelBlockEvidenceIgnoresMarkersInsideStringLiterals()
        {
            const string text = "comment := 'FUNCTION_BLOCK Motor END_FUNCTION_BLOCK'; ordinary notes";

            bool found = SclLexicalProbe.HasTopLevelBlockEvidence(text, out _, out _);

            Assert.False(found);
        }

        [Fact]
        public void HasTopLevelBlockEvidenceHandlesEscapedQuoteInsideStringLiteral()
        {
            // The doubled '' is an escaped single quote inside the string, so the string does not
            // terminate early; real block evidence follows only outside the (still-open-looking) literal.
            const string text = "note := 'it''s a test'; FUNCTION_BLOCK Motor\nEND_FUNCTION_BLOCK";

            bool found = SclLexicalProbe.HasTopLevelBlockEvidence(text, out string opener, out string terminator);

            Assert.True(found);
            Assert.Equal("FUNCTION_BLOCK", opener);
            Assert.Equal("END_FUNCTION_BLOCK", terminator);
        }

        [Fact]
        public void HasTopLevelBlockEvidenceDoesNotMatchBareFunctionInsideFunctionBlockOpener()
        {
            // "FUNCTION_BLOCK" is present (never "FUNCTION" as its own word), and "END_FUNCTION" is
            // present as its own bare word (never completing "END_FUNCTION_BLOCK"). Correct word-boundary
            // matching must report no evidence: the FUNCTION_BLOCK pair is missing its terminator, and the
            // bare FUNCTION opener never actually occurs (word boundaries stop only at the underscore,
            // which is itself a word character) — only a naive substring search would incorrectly pair
            // these two unrelated fragments.
            const string text = "FUNCTION_BLOCK Motor\nEND_FUNCTION x";

            bool found = SclLexicalProbe.HasTopLevelBlockEvidence(text, out string opener, out string terminator);

            Assert.False(found);
            Assert.Equal(string.Empty, opener);
            Assert.Equal(string.Empty, terminator);
        }

        [Fact]
        public void HasTopLevelBlockEvidenceThrowsForNullText()
            => Assert.Throws<ArgumentNullException>(() => SclLexicalProbe.HasTopLevelBlockEvidence(null!, out _, out _));
    }
}
