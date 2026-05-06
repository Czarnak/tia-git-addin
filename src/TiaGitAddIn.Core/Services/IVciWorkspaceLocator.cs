namespace TiaGitAddIn.Services
{
    public interface IVciWorkspaceLocator
    {
        string? TryGetWorkspacePath(object projectContext);
    }
}
