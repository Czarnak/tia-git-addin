using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public class SactServiceTests
    {
        private class FakeSactProcessRunner : ISactProcessRunner
        {
            public bool ReturnSuccess { get; set; } = true;
            public string OutputJson { get; set; } = "{}";
            public string? LastFileName { get; private set; }
            public string? LastArguments { get; private set; }
            public IDictionary<string, string>? LastEnvironment { get; private set; }

            public Task<SactProcessResult> RunAsync(
                string fileName,
                string arguments,
                CancellationToken ct,
                IDictionary<string, string>? environmentVariables = null)
            {
                LastFileName = fileName;
                LastArguments = arguments;
                LastEnvironment = environmentVariables;

                if (ReturnSuccess)
                {
                    return Task.FromResult(new SactProcessResult { ExitCode = 0, StandardOutput = OutputJson });
                }
                else
                {
                    return Task.FromResult(new SactProcessResult { ExitCode = 1, StandardError = "Failed" });
                }
            }
        }

        private class FakeSactPathResolver : ISactPathResolver
        {
            public string? SiemensPath { get; set; } = @"C:\Siemens";
            public string? NodePath { get; set; } = "node";

            public string? ResolveSiemensInstallPath() => SiemensPath;
            public string? ResolveNodePath() => NodePath;
        }

        [Fact]
        public async Task CompareAsync_SuccessfulProcess_ReturnsParsedResult()
        {
            var runner = new FakeSactProcessRunner
            {
                ReturnSuccess = true,
                OutputJson = @"{ ""Left"": ""Block1"", ""Right"": ""Block2"", ""State"": ""Equal"" }"
            };
            var resolver = new FakeSactPathResolver();

            var service = new SactService(resolver, runner);

            var result = await service.CompareAsync("left.xml", "right.xml", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Block1", result!.Left);
            Assert.Equal(CompareState.Equal, result.State);
            
            // Verify runner call
            Assert.Equal("node", runner.LastFileName);
            Assert.Contains("CompareBlocks.js", runner.LastArguments);
            Assert.Contains("left.xml", runner.LastArguments);
            Assert.Contains("right.xml", runner.LastArguments);
            
            Assert.NotNull(runner.LastEnvironment);
            Assert.True(runner.LastEnvironment!.ContainsKey("NODE_PATH"));
            Assert.Contains("node_modules", runner.LastEnvironment["NODE_PATH"]);
        }

        [Fact]
        public async Task CompareAsync_FailedProcess_ReturnsNull()
        {
            var runner = new FakeSactProcessRunner
            {
                ReturnSuccess = false
            };
            var resolver = new FakeSactPathResolver();

            var service = new SactService(resolver, runner);

            var result = await service.CompareAsync("left.xml", "right.xml", CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task CompareAsync_PathsNotResolved_ReturnsNull()
        {
            var runner = new FakeSactProcessRunner();
            var resolver = new FakeSactPathResolver { SiemensPath = null };

            var service = new SactService(resolver, runner);

            var result = await service.CompareAsync("left.xml", "right.xml", CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public void IsAvailable_ReturnsTrue_WhenBothPathsResolved()
        {
            var runner = new FakeSactProcessRunner();
            var resolver = new FakeSactPathResolver
            {
                SiemensPath = @"C:\Siemens",
                NodePath = "node"
            };
            
            var service = new SactService(resolver, runner);
            
            Assert.True(service.IsAvailable);
        }

        [Fact]
        public void IsAvailable_ReturnsFalse_WhenSiemensPathMissing()
        {
            var runner = new FakeSactProcessRunner();
            var resolver = new FakeSactPathResolver
            {
                SiemensPath = null,
                NodePath = "node"
            };
            
            var service = new SactService(resolver, runner);
            
            Assert.False(service.IsAvailable);
        }

        [Fact]
        public void IsAvailable_ReturnsFalse_WhenNodePathMissing()
        {
            var runner = new FakeSactProcessRunner();
            var resolver = new FakeSactPathResolver
            {
                SiemensPath = @"C:\Siemens",
                NodePath = null
            };
            
            var service = new SactService(resolver, runner);
            
            Assert.False(service.IsAvailable);
        }
    }
}