namespace TiaGitAddIn.Services.SimaticMl
{
    public interface ISimaticMlSchemaLocator
    {
        SimaticMlSchemaLocation Locate(string? explicitSchemaDirectory = null);
    }
}
