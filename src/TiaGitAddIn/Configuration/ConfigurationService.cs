using System;
using System.IO;
using System.Web.Script.Serialization;
using TiaGitAddIn.Models;

namespace TiaGitAddIn.Configuration
{
    public sealed class ConfigurationService : IConfigurationService
    {
        public const string FileName = ".tia-git-addin.json";

        public GitConfiguration Load(string repositoryRoot)
        {
            EnsureValidRepositoryRoot(repositoryRoot);

            string path = GetConfigurationPath(repositoryRoot);
            if (!File.Exists(path))
            {
                return new GitConfiguration { RepositoryPath = repositoryRoot };
            }

            GitConfiguration? configuration;
            try
            {
                string json = File.ReadAllText(path);
                configuration = new JavaScriptSerializer().Deserialize<GitConfiguration>(json);
            }
            catch (IOException)
            {
                return new GitConfiguration { RepositoryPath = repositoryRoot };
            }
            catch (ArgumentException)
            {
                return new GitConfiguration { RepositoryPath = repositoryRoot };
            }
            catch (InvalidOperationException)
            {
                return new GitConfiguration { RepositoryPath = repositoryRoot };
            }

            return Normalize(configuration, repositoryRoot);
        }

        public void Save(string repositoryRoot, GitConfiguration configuration)
        {
            EnsureValidRepositoryRoot(repositoryRoot);

            string path = GetConfigurationPath(repositoryRoot);
            string json = new JavaScriptSerializer().Serialize(configuration);
            File.WriteAllText(path, json);
        }

        private static string GetConfigurationPath(string repositoryRoot) =>
            Path.Combine(repositoryRoot, FileName);

        private static GitConfiguration Normalize(
            GitConfiguration? configuration,
            string repositoryRoot)
        {
            if (configuration == null)
            {
                return new GitConfiguration { RepositoryPath = repositoryRoot };
            }

            return new GitConfiguration
            {
                GitExecutablePath = string.IsNullOrWhiteSpace(configuration.GitExecutablePath)
                    ? "git"
                    : configuration.GitExecutablePath,
                RepositoryPath = string.IsNullOrWhiteSpace(configuration.RepositoryPath)
                    ? repositoryRoot
                    : configuration.RepositoryPath,
                DefaultRemote = string.IsNullOrWhiteSpace(configuration.DefaultRemote)
                    ? "origin"
                    : configuration.DefaultRemote,
                CommitAuthorName = configuration.CommitAuthorName ?? string.Empty,
                CommitAuthorEmail = configuration.CommitAuthorEmail ?? string.Empty
            };
        }

        private static void EnsureValidRepositoryRoot(string repositoryRoot)
        {
            ValidationResult result = PathValidator.Validate(repositoryRoot);
            if (!result.IsValid)
            {
                throw new InvalidDataException(result.ErrorMessage);
            }
        }
    }
}
