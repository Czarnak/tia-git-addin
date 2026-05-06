namespace TiaGitAddIn.Services
{
    public interface IRepositoryDiscovery
    {
        string? FindRepositoryRoot(string startPath);
    }
}
