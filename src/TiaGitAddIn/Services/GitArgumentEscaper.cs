using System.Text;

namespace TiaGitAddIn.Services
{
    public static class GitArgumentEscaper
    {
        public static string Escape(string argument)
        {
            if (argument.Length == 0)
            {
                return "\"\"";
            }

            bool mustQuote = argument.IndexOfAny(new[] { ' ', '\t', '"' }) >= 0 ||
                argument.EndsWith("\\", System.StringComparison.Ordinal);
            if (!mustQuote)
            {
                return argument;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append('"');

            int backslashCount = 0;
            foreach (char value in argument)
            {
                if (value == '\\')
                {
                    backslashCount++;
                    continue;
                }

                if (value == '"')
                {
                    builder.Append('\\', backslashCount * 2 + 1);
                    builder.Append('"');
                    backslashCount = 0;
                    continue;
                }

                builder.Append('\\', backslashCount);
                backslashCount = 0;
                builder.Append(value);
            }

            builder.Append('\\', backslashCount * 2);
            builder.Append('"');
            return builder.ToString();
        }
    }
}
