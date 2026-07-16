using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Comparison
{
    public sealed class ComparisonDiagnosticSanitizerTests
    {
        [Fact]
        public void SanitizeRemovesSecretsPathsUserInfoAndStackTrace()
        {
            string unsafeMessage = "failed https://alice:secret@example.test/repo token=abc123 " +
                @"C:\Users\alice\AppData\Local\Temp\TiaGitAddIn\comparison\lease\Program.xml" +
                "\r\n   at Namespace.Type.Method()";

            PlcComparisonDiagnostic result = new ComparisonDiagnosticSanitizer().ForUser(
                "CMP-PARSE-001", PlcDiagnosticSeverity.Error, unsafeMessage,
                new PlcSourceLocation(PlcRevisionSide.Right, 12, 4));

            Assert.Equal("CMP-PARSE-001", result.Code);
            Assert.Equal(12, result.Location!.Line);
            Assert.DoesNotContain("alice", result.Message, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", result.Message, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("abc123", result.Message, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Temp", result.Message, System.StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(" at ", result.Message, System.StringComparison.OrdinalIgnoreCase);
        }

        // --- Supplementary coverage beyond the brief's literal test theory: the non-blank fallback,
        //     the length cap, plain pass-through of code/severity/location, and the Unix temp path case
        //     the brief's single adversarial string does not exercise. ---

        [Fact]
        public void FallbackMessageIsUsedWhenRedactionLeavesNothing()
        {
            string unsafeMessage = "   at Namespace.Type.Method()\r\n   at Other.Method()";

            PlcComparisonDiagnostic result = new ComparisonDiagnosticSanitizer().ForUser(
                "CMP-PARSE-002", PlcDiagnosticSeverity.Warning, unsafeMessage);

            Assert.Equal("Comparison failed; see the Add-In log with reference CMP-PARSE-002.", result.Message);
        }

        [Fact]
        public void MessageIsCappedAtOneThousandTwentyFourCharacters()
        {
            string longMessage = new string('x', 5000);

            PlcComparisonDiagnostic result = new ComparisonDiagnosticSanitizer().ForUser(
                "CMP-PARSE-003", PlcDiagnosticSeverity.Info, longMessage);

            Assert.True(result.Message.Length <= 1024);
        }

        [Fact]
        public void ForUserPreservesCodeSeverityAndOptionalLocation()
        {
            PlcComparisonDiagnostic result = new ComparisonDiagnosticSanitizer().ForUser(
                "CMP-PARSE-004", PlcDiagnosticSeverity.Info, "benign message");

            Assert.Equal("CMP-PARSE-004", result.Code);
            Assert.Equal(PlcDiagnosticSeverity.Info, result.Severity);
            Assert.Null(result.Location);
            Assert.Equal("benign message", result.Message);
        }

        [Fact]
        public void UnixTemporaryPathsAreRedacted()
        {
            string unsafeMessage = "failed reading /tmp/tia-git-addin/lease-42/Program.xml for user bob";

            PlcComparisonDiagnostic result = new ComparisonDiagnosticSanitizer().ForUser(
                "CMP-PARSE-005", PlcDiagnosticSeverity.Error, unsafeMessage);

            Assert.DoesNotContain("/tmp/", result.Message);
            Assert.DoesNotContain("lease-42", result.Message);
        }
    }
}
