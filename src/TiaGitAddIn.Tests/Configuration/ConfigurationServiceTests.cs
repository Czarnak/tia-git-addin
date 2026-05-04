using System.IO;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Models;
using Xunit;

namespace TiaGitAddIn.Tests.Configuration
{
    public sealed class ConfigurationServiceTests
    {
        [Fact]
        public void SaveThenLoadRoundTripsConfiguration()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            try
            {
                ConfigurationService service = new ConfigurationService();
                GitConfiguration config = new GitConfiguration
                {
                    GitExecutablePath = @"C:\Program Files\Git\cmd\git.exe",
                    RepositoryPath = root,
                    DefaultRemote = "upstream",
                    CommitAuthorName = "Engineer"
                };

                service.Save(root, config);
                GitConfiguration loaded = service.Load(root);

                Assert.Equal(config.GitExecutablePath, loaded.GitExecutablePath);
                Assert.Equal(config.RepositoryPath, loaded.RepositoryPath);
                Assert.Equal(config.DefaultRemote, loaded.DefaultRemote);
                Assert.Equal(config.CommitAuthorName, loaded.CommitAuthorName);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void LoadReturnsDefaultConfigurationWhenFileIsMalformed()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllText(Path.Combine(root, ConfigurationService.FileName), "{ invalid json");
                ConfigurationService service = new ConfigurationService();

                GitConfiguration loaded = service.Load(root);

                Assert.Equal(root, loaded.RepositoryPath);
                Assert.Equal("git", loaded.GitExecutablePath);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void LoadNormalizesMissingRepositoryPath()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllText(
                    Path.Combine(root, ConfigurationService.FileName),
                    "{\"GitExecutablePath\":\"git.exe\"}");
                ConfigurationService service = new ConfigurationService();

                GitConfiguration loaded = service.Load(root);

                Assert.Equal(root, loaded.RepositoryPath);
                Assert.Equal("git.exe", loaded.GitExecutablePath);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
