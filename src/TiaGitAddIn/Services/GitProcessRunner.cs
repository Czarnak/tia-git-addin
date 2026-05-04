using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Configuration;

namespace TiaGitAddIn.Services
{
    public sealed class GitProcessRunner : IGitProcessRunner
    {
        private readonly TimeSpan timeout;

        public GitProcessRunner()
            : this(TimeSpan.FromSeconds(30))
        {
        }

        public GitProcessRunner(TimeSpan timeout)
        {
            this.timeout = timeout;
        }

        public Task<GitProcessResult> RunAsync(
            string gitExecutablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            ValidationResult gitPathResult = PathValidator.ValidateGitExecutablePath(gitExecutablePath);
            if (!gitPathResult.IsValid)
            {
                throw new ArgumentException(gitPathResult.ErrorMessage, nameof(gitExecutablePath));
            }

            ValidationResult workingDirectoryResult = PathValidator.Validate(workingDirectory);
            if (!workingDirectoryResult.IsValid)
            {
                throw new ArgumentException(workingDirectoryResult.ErrorMessage, nameof(workingDirectory));
            }

            return Task.Run(
                () => RunProcess(gitExecutablePath, workingDirectory, arguments, cancellationToken),
                cancellationToken);
        }

        private GitProcessResult RunProcess(
            string gitExecutablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = gitExecutablePath,
                    Arguments = BuildArgumentString(arguments),
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (CancellationTokenRegistration cancellation = cancellationToken.Register(() => TryKill(process)))
                {
                    process.Start();
                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();

                    bool exited = process.WaitForExit((int)timeout.TotalMilliseconds);
                    if (!exited)
                    {
                        TryKill(process);
                        return new GitProcessResult
                        {
                            ExitCode = -1,
                            TimedOut = true,
                            StandardError = "Git operation timed out."
                        };
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    return new GitProcessResult
                    {
                        ExitCode = process.ExitCode,
                        StandardOutput = outputTask.Result,
                        StandardError = errorTask.Result
                    };
                }
            }
        }

        private static string BuildArgumentString(IReadOnlyList<string> arguments)
        {
            StringBuilder builder = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(GitArgumentEscaper.Escape(argument));
            }

            return builder.ToString();
        }

        private static void TryKill(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
