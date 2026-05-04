using TiaGitAddIn.Configuration;
using Xunit;

namespace TiaGitAddIn.Tests.Configuration
{
    public sealed class PathValidatorTests
    {
        [Theory]
        [InlineData(@"C:\Users\Dev\vci-workspace")]
        [InlineData(@"D:\Repos\Project")]
        public void ValidateAcceptsValidAbsolutePaths(string path)
        {
            ValidationResult result = PathValidator.Validate(path);

            Assert.True(result.IsValid, result.ErrorMessage);
        }

        [Theory]
        [InlineData(@"workspace")]
        [InlineData(@"project\blocks")]
        public void ValidateAcceptsValidRelativePaths(string path)
        {
            ValidationResult result = PathValidator.Validate(path);

            Assert.True(result.IsValid, result.ErrorMessage);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ValidateRejectsBlankPaths(string? path)
        {
            ValidationResult result = PathValidator.Validate(path);

            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData(@"..\outside")]
        [InlineData(@"C:\repo\..\..\windows\system32")]
        [InlineData(@"repo/../outside")]
        public void ValidateRejectsTraversalPaths(string path)
        {
            ValidationResult result = PathValidator.Validate(path);

            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("repo\0blocks")]
        [InlineData("repo\u0001blocks")]
        public void ValidateRejectsControlCharacters(string path)
        {
            ValidationResult result = PathValidator.Validate(path);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void ValidateRejectsOverlongPaths()
        {
            string path = @"C:\" + new string('a', 300);

            ValidationResult result = PathValidator.Validate(path);

            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData("git")]
        [InlineData("git.exe")]
        [InlineData(@"C:\Program Files\Git\cmd\git.exe")]
        public void ValidateGitExecutableAcceptsGitNames(string path)
        {
            ValidationResult result = PathValidator.ValidateGitExecutablePath(path);

            Assert.True(result.IsValid, result.ErrorMessage);
        }

        [Theory]
        [InlineData("git.bat")]
        [InlineData("powershell.exe")]
        [InlineData(@"..\git.exe")]
        public void ValidateGitExecutableRejectsUnsafeNames(string path)
        {
            ValidationResult result = PathValidator.ValidateGitExecutablePath(path);

            Assert.False(result.IsValid);
        }
    }
}
