using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// Bounded SCL lexical evidence probe used by <see cref="PlcArtifactClassifier"/>: detects a matched
    /// top-level block opener/terminator pair (e.g. FUNCTION_BLOCK / END_FUNCTION_BLOCK) outside string
    /// literals and comments. This intentionally is not a full SCL lexer — it recognizes only enough
    /// syntax (single-quoted strings with '' escaping, // line comments, (* *) block comments) to keep
    /// string/comment content out of the keyword scan.
    /// </summary>
    public static class SclLexicalProbe
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

        private static readonly (string Opener, string Terminator)[] BlockPairs =
        {
            ("FUNCTION_BLOCK", "END_FUNCTION_BLOCK"),
            ("ORGANIZATION_BLOCK", "END_ORGANIZATION_BLOCK"),
            ("DATA_BLOCK", "END_DATA_BLOCK"),
            ("FUNCTION", "END_FUNCTION"),
            ("TYPE", "END_TYPE"),
        };

        public static bool HasTopLevelBlockEvidence(string boundedText, out string opener, out string terminator)
        {
            if (boundedText == null) throw new ArgumentNullException(nameof(boundedText));

            string stripped = StripStringsAndComments(boundedText);
            foreach ((string candidateOpener, string candidateTerminator) in BlockPairs)
            {
                if (ContainsWord(stripped, candidateOpener) && ContainsWord(stripped, candidateTerminator))
                {
                    opener = candidateOpener;
                    terminator = candidateTerminator;
                    return true;
                }
            }

            opener = string.Empty;
            terminator = string.Empty;
            return false;
        }

        private static bool ContainsWord(string text, string word)
        {
            try
            {
                return Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        /// <summary>
        /// Removes SCL single-quoted string literals (with '' as an escaped quote) and // and (* *)
        /// comments in one linear character scan, replacing each stripped region with a single space so
        /// surrounding tokens never merge. Deliberately not a full-grammar lexer: no identifiers, numbers,
        /// or operators are tokenized — only enough is recognized to keep strings/comments out of the
        /// keyword scan above.
        /// </summary>
        private static string StripStringsAndComments(string text)
        {
            var builder = new StringBuilder(text.Length);
            int i = 0;
            int length = text.Length;

            while (i < length)
            {
                char c = text[i];

                if (c == '\'')
                {
                    i++;
                    while (i < length)
                    {
                        if (text[i] == '\'')
                        {
                            if (i + 1 < length && text[i + 1] == '\'') { i += 2; continue; }
                            i++;
                            break;
                        }

                        i++;
                    }

                    builder.Append(' ');
                    continue;
                }

                if (c == '/' && i + 1 < length && text[i + 1] == '/')
                {
                    i += 2;
                    while (i < length && text[i] != '\n') i++;
                    builder.Append(' ');
                    continue;
                }

                if (c == '(' && i + 1 < length && text[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < length && !(text[i] == '*' && text[i + 1] == ')')) i++;
                    i = Math.Min(i + 2, length);
                    builder.Append(' ');
                    continue;
                }

                builder.Append(c);
                i++;
            }

            return builder.ToString();
        }
    }
}
