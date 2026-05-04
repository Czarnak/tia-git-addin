using System.IO;
using TiaGitAddIn.Configuration;

namespace TiaGitAddIn.Services
{
    public sealed class RepositoryDiscovery : IRepositoryDiscovery
    {
        public string? FindRepositoryRoot(string startPath)
        {
            ValidationResult validation = PathValidator.Validate(startPath);
            if (!validation.IsValid)
            {
                return null;
            }

            DirectoryInfo? current = new DirectoryInfo(startPath);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                    File.Exists(Path.Combine(current.FullName, ".git")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}
