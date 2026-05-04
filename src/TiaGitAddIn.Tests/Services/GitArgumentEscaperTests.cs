using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public sealed class GitArgumentEscaperTests
    {
        [Theory]
        [InlineData("status", "status")]
        [InlineData(@"Blocks\Main.scl", @"Blocks\Main.scl")]
        [InlineData(@"C:\Program Files\Git\cmd\git.exe", @"""C:\Program Files\Git\cmd\git.exe""")]
        [InlineData(@"folder\", @"""folder\\""")]
        [InlineData("commit \"message\"", @"""commit \""message\""""")]
        public void EscapePreservesWindowsArgumentMeaning(string value, string expected)
        {
            Assert.Equal(expected, GitArgumentEscaper.Escape(value));
        }
    }
}
