using System.Linq;
using System.Text.RegularExpressions;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// Turns an unsafe, caller-supplied diagnostic string into a user-visible <see cref="PlcComparisonDiagnostic"/>.
    /// Strips URL userinfo, token/password/secret/apikey-shaped values, Windows/Unix temporary paths, and
    /// .NET stack-trace lines before the message is ever shown to a user, and caps the result at
    /// <see cref="MaximumMessageLength"/> characters. If redaction leaves nothing usable, a stable,
    /// non-blank fallback message referencing the diagnostic code is used instead -- required because
    /// <see cref="PlcComparisonDiagnostic"/> rejects a blank message.
    /// </summary>
    public sealed class ComparisonDiagnosticSanitizer
    {
        private const int MaximumMessageLength = 1024;

        private static readonly Regex StackTraceLine = new Regex(
            @"^\s{0,32}at\s+\S.{0,500}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex UrlWithUserInfo = new Regex(
            @"\b[a-z][a-z0-9+.\-]{1,15}://[^\s@/]{1,256}@[^\s]{1,1024}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex CredentialAssignment = new Regex(
            @"\b(token|password|secret|apikey)\s*[=:]\s*[^\s&,;]{1,256}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex WindowsTempPath = new Regex(
            @"[a-z]:\\(?:[^\\\s]{1,100}\\){0,10}temp\\[^\s]{0,500}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex UnixTempPath = new Regex(
            @"/(?:tmp|var/tmp)/[^\s]{0,500}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex ExtraWhitespace = new Regex(@"\s{2,}", RegexOptions.Compiled);

        /// <summary>
        /// Builds a redacted, length-capped, user-visible diagnostic. <paramref name="location"/> is
        /// caller-owned and already safe (never a path) -- it is forwarded unchanged.
        /// </summary>
        public PlcComparisonDiagnostic ForUser(string code, PlcDiagnosticSeverity severity, string unsafeMessage,
            PlcSourceLocation? location = null)
        {
            string safeMessage = Sanitize(unsafeMessage, code);
            return new PlcComparisonDiagnostic(code, severity, safeMessage, location);
        }

        private static string Sanitize(string? unsafeMessage, string code)
        {
            string withoutStackTrace = RemoveStackTraceLines(unsafeMessage ?? string.Empty);
            string redacted = RedactSensitiveContent(withoutStackTrace);
            string collapsed = ExtraWhitespace.Replace(redacted, " ").Trim();
            string capped = collapsed.Length > MaximumMessageLength
                ? collapsed.Substring(0, MaximumMessageLength)
                : collapsed;

            return string.IsNullOrWhiteSpace(capped)
                ? $"Comparison failed; see the Add-In log with reference {code}."
                : capped;
        }

        private static string RemoveStackTraceLines(string text)
        {
            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            var keptLines = normalized.Split('\n').Where(line => !StackTraceLine.IsMatch(line));
            return string.Join(" ", keptLines);
        }

        private static string RedactSensitiveContent(string text)
        {
            string withoutUrls = UrlWithUserInfo.Replace(text, "[redacted-url]");
            string withoutCredentials = CredentialAssignment.Replace(withoutUrls, "$1=[redacted]");
            string withoutWindowsPaths = WindowsTempPath.Replace(withoutCredentials, "[redacted-path]");
            return UnixTempPath.Replace(withoutWindowsPaths, "[redacted-path]");
        }
    }
}
