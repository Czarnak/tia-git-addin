using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TiaGitAddIn.Services.Revision
{
    /// <summary>
    /// Raw-byte counterpart to <see cref="TiaGitAddIn.Services.IGitProcessRunner"/>. Implementations must
    /// read process stdout as bytes (e.g. via <c>Process.StandardOutput.BaseStream</c>), never through a
    /// <see cref="System.IO.StreamReader"/>, so binary git blob content is never transcoded or corrupted.
    /// </summary>
    public sealed class GitBinaryProcessResult
    {
        public GitBinaryProcessResult(int exitCode, IEnumerable<byte> standardOutput, string standardError, bool timedOut)
        {
            ExitCode = exitCode;
            StandardOutput = (standardOutput ?? throw new ArgumentNullException(nameof(standardOutput))).ToArray();
            StandardError = standardError ?? string.Empty;
            TimedOut = timedOut;
        }

        public int ExitCode { get; }
        public IReadOnlyList<byte> StandardOutput { get; }
        public string StandardError { get; }
        public bool TimedOut { get; }
        public bool IsSuccess => ExitCode == 0 && !TimedOut;
    }

    public interface IGitBinaryProcessRunner
    {
        Task<GitBinaryProcessResult> RunBinaryAsync(
            string gitExecutablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            int maximumStandardOutputBytes,
            CancellationToken cancellationToken);
    }
}
