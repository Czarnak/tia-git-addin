using TiaGitAddIn.Models;

namespace TiaGitAddIn.Configuration
{
    public interface IConfigurationService
    {
        GitConfiguration Load(string repositoryRoot);

        void Save(string repositoryRoot, GitConfiguration configuration);
    }
}
