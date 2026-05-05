namespace TiaGitAddIn.Services
{
    public interface ISactPathResolver
    {
        string? ResolveSiemensInstallPath();
        string? ResolveNodePath();
    }
}
