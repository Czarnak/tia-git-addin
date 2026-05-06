using System.Threading;
using System.Threading.Tasks;

namespace TiaGitAddIn.Services
{
    public interface IGitFileExtractor
    {
        Task<string> ExtractFileAsync(string? commitHash, string filePath, CancellationToken ct);
        Task<string?> ExtractParentFileAsync(string? commitHash, string filePath, CancellationToken ct);
    }
}