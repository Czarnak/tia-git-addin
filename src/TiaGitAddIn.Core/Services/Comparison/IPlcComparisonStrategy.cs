using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.Comparison
{
    public interface IPlcComparisonStrategy
    {
        IReadOnlyCollection<PlcArtifactKind> SupportedKinds { get; }

        Task<PlcComparisonResult> CompareAsync(
            PlcComparisonContext context,
            CancellationToken cancellationToken);
    }
}
