using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TiaGitAddIn.Services
{
    public interface IGitProcessRunner
    {
        Task<GitProcessResult> RunAsync(
            string gitExecutablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken);
    }
}
