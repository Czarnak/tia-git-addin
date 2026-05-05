using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public class GitFileExtractorTests : IDisposable
    {
        private class FakeGitProcessRunner : IGitProcessRunner
        {
            public string? LastRequestedCommit { get; private set; }
            public string? LastRequestedFile { get; private set; }
            
            public Task<GitProcessResult> RunAsync(string gitExecutablePath, string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
            {
                Assert.Equal("show", arguments[0]);
                
                var target = arguments[1];
                var parts = target.Split(new[] { ':' }, 2);
                LastRequestedCommit = parts[0];
                LastRequestedFile = parts[1];

                if (LastRequestedCommit.EndsWith("^")) // Parent commit simulation
                {
                    if (LastRequestedCommit == "initialCommit^")
                    {
                        return Task.FromResult(new GitProcessResult { ExitCode = 128, StandardError = "fatal: ambiguous argument..." });
                    }
                    return Task.FromResult(new GitProcessResult { ExitCode = 0, StandardOutput = "ParentContent" });
                }

                return Task.FromResult(new GitProcessResult { ExitCode = 0, StandardOutput = "FileContent" });
            }
        }

        private readonly List<string> tempFilesToCleanUp = new List<string>();

        public void Dispose()
        {
            foreach (var file in tempFilesToCleanUp)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch { }
            }
        }

        [Fact]
        public async Task ExtractFileAsync_CreatesTempFileWithCorrectContent()
        {
            var runner = new FakeGitProcessRunner();
            var extractor = new GitFileExtractor(runner, "git", "C:\\repo");

            var tempPath = await extractor.ExtractFileAsync("commit123", "test.xml", CancellationToken.None);
            tempFilesToCleanUp.Add(tempPath);

            Assert.True(File.Exists(tempPath));
            Assert.Equal(".xml", Path.GetExtension(tempPath));
            Assert.Equal("FileContent", File.ReadAllText(tempPath));
            Assert.Equal("commit123", runner.LastRequestedCommit);
            Assert.Equal("test.xml", runner.LastRequestedFile);
        }

        [Fact]
        public async Task ExtractParentFileAsync_ReturnsTempFileForExistingParent()
        {
            var runner = new FakeGitProcessRunner();
            var extractor = new GitFileExtractor(runner, "git", "C:\\repo");

            var tempPath = await extractor.ExtractParentFileAsync("commit123", "test.xml", CancellationToken.None);
            
            Assert.NotNull(tempPath);
            tempFilesToCleanUp.Add(tempPath);
            Assert.True(File.Exists(tempPath));
            Assert.Equal(".xml", Path.GetExtension(tempPath));
            Assert.Equal("ParentContent", File.ReadAllText(tempPath));
            Assert.Equal("commit123^", runner.LastRequestedCommit);
        }

        [Fact]
        public async Task ExtractParentFileAsync_ReturnsNullWhenGitShowFails()
        {
            var runner = new FakeGitProcessRunner();
            var extractor = new GitFileExtractor(runner, "git", "C:\\repo");

            var tempPath = await extractor.ExtractParentFileAsync("initialCommit", "test.xml", CancellationToken.None);

            Assert.Null(tempPath);
            Assert.Equal("initialCommit^", runner.LastRequestedCommit);
        }
    }
}