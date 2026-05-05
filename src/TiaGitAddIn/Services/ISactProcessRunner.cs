using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TiaGitAddIn.Services
{
    public interface ISactProcessRunner
    {
        Task<SactProcessResult> RunAsync(
            string fileName,
            string arguments,
            CancellationToken ct,
            IDictionary<string, string>? environmentVariables = null);
    }
}