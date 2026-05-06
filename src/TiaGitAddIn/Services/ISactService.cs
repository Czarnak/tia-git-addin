using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services
{
    public interface ISactService
    {
        bool IsAvailable { get; }
        Task<SactCompareResult?> CompareAsync(string? leftXmlPath, string? rightXmlPath, CancellationToken ct);   

    }
}