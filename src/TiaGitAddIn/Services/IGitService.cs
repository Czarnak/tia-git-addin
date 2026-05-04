using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models;

namespace TiaGitAddIn.Services
{
    public interface IGitService
    {
        Task<GitStatus> GetStatusAsync(CancellationToken ct = default);

        Task<OperationResult> StageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default);

        Task<OperationResult> UnstageAsync(IReadOnlyList<string> filePaths, CancellationToken ct = default);

        Task<OperationResult> CommitAsync(string message, CancellationToken ct = default);

        Task<IReadOnlyList<CommitInfo>> GetCommitLogAsync(int maxCount, CancellationToken ct = default);

        Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(CancellationToken ct = default);

        Task<OperationResult> CheckoutBranchAsync(string branchName, CancellationToken ct = default);

        Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(CancellationToken ct = default);
    }
}
