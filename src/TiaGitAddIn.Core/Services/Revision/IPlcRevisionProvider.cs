using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Revision
{
    /// <summary>
    /// Loads an immutable, size-gated, strictly-decoded <see cref="PlcRevision"/> for one side of a
    /// comparison, wrapped in a <see cref="PlcRevisionLease"/> that owns that revision's scoped temporary
    /// working file (if any) for the lease's lifetime.
    /// </summary>
    public interface IPlcRevisionProvider
    {
        Task<PlcRevisionLease> LoadAsync(PlcRevisionSide side, PlcRevisionSource source,
            string repositoryRelativePath, CancellationToken cancellationToken);

        PlcRevisionLease Missing(PlcRevisionSide side, PlcRevisionSource source,
            string repositoryRelativePath, PlcRevisionMissingReason reason);
    }
}
