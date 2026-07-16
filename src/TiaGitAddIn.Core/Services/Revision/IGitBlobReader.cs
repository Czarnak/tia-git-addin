using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Revision
{
    /// <summary>
    /// Reads a single committed git blob's size and raw bytes for a repository-relative path at a given
    /// <see cref="PlcRevisionSource"/>. Implementations must validate <paramref name="repositoryRelativePath"/>
    /// and the revision encoded in <paramref name="source"/> before invoking any process.
    /// </summary>
    public interface IGitBlobReader
    {
        Task<long> GetSizeAsync(PlcRevisionSource source, string repositoryRelativePath, CancellationToken cancellationToken);

        Task<IReadOnlyList<byte>> ReadAsync(PlcRevisionSource source, string repositoryRelativePath,
            int maximumBytes, CancellationToken cancellationToken);
    }
}
