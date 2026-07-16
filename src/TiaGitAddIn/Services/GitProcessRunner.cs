using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Configuration;
using TiaGitAddIn.Services.Revision;
using AddInProcess = Siemens.Engineering.AddIn.Utilities.Process;
using AddInProcessStartInfo = Siemens.Engineering.AddIn.Utilities.ProcessStartInfo;

[assembly: InternalsVisibleTo("TiaGitAddIn.Tests")]

namespace TiaGitAddIn.Services
{
    public sealed class GitProcessRunner(TimeSpan timeout) : IGitProcessRunner, IGitBinaryProcessRunner
    {
        private const int BinaryReadBufferSize = 81920;

        public GitProcessRunner()
            : this(TimeSpan.FromSeconds(30))
        {
        }

        public Task<GitProcessResult> RunAsync(
            string gitExecutablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            ValidateProcessTarget(gitExecutablePath, workingDirectory);

            return Task.Run(
                () => RunProcess(gitExecutablePath, workingDirectory, arguments, cancellationToken),
                cancellationToken);
        }

        public Task<GitBinaryProcessResult> RunBinaryAsync(
            string gitExecutablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            int maximumStandardOutputBytes,
            CancellationToken cancellationToken)
        {
            ValidateProcessTarget(gitExecutablePath, workingDirectory);
            if (maximumStandardOutputBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumStandardOutputBytes));
            }

            return Task.Run(
                () => RunBinaryProcessAsync(gitExecutablePath, workingDirectory, arguments, maximumStandardOutputBytes, cancellationToken),
                cancellationToken);
        }

        private static void ValidateProcessTarget(string gitExecutablePath, string workingDirectory)
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
        }

        private GitProcessResult RunProcess(
            string gitExecutablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken)
        {
            using (AddInProcess process = new())
            {
                process.StartInfo = BuildStartInfo(gitExecutablePath, workingDirectory, arguments);

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

        // Binary reads never go through StandardOutput's StreamReader (which would transcode/corrupt
        // non-text blob content). Instead process.StandardOutput.BaseStream is read directly and bounded
        // by ReadBoundedAsync. A Task.WhenAny race lets a size-limit fault interrupt WaitForExit promptly
        // instead of waiting out the full timeout; the final `await outputTask` still re-observes any
        // fault even if WaitForExit happened to win the race, so no path can swallow it.
        private async Task<GitBinaryProcessResult> RunBinaryProcessAsync(
            string gitExecutablePath,
            string workingDirectory,
            IReadOnlyList<string> arguments,
            int maximumStandardOutputBytes,
            CancellationToken cancellationToken)
        {
            using (AddInProcess process = new())
            {
                process.StartInfo = BuildStartInfo(gitExecutablePath, workingDirectory, arguments);

                using (CancellationTokenRegistration cancellation = cancellationToken.Register(() => TryKill(process)))
                {
                    process.Start();

                    Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    Task<byte[]> outputTask = ReadBoundedAsync(
                        process.StandardOutput.BaseStream, maximumStandardOutputBytes, cancellationToken);
                    Task<bool> waitTask = Task.Run(
                        () => process.WaitForExit((int)timeout.TotalMilliseconds), CancellationToken.None);

                    Task firstCompleted = await Task.WhenAny(outputTask, waitTask).ConfigureAwait(false);
                    if (firstCompleted == outputTask && outputTask.IsFaulted)
                    {
                        TryKill(process);
                    }

                    bool exited = await waitTask.ConfigureAwait(false);
                    if (!exited)
                    {
                        TryKill(process);
                        return new GitBinaryProcessResult(-1, Array.Empty<byte>(), "Git operation timed out.", true);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Always awaited last: re-observes a size-limit (or cancellation) fault even if
                    // waitTask happened to complete first, so it can never be silently missed.
                    byte[] output = await outputTask.ConfigureAwait(false);
                    string standardError = await errorTask.ConfigureAwait(false);
                    return new GitBinaryProcessResult(process.ExitCode, output, standardError, false);
                }
            }
        }

        /// <summary>
        /// Reads <paramref name="stream"/> to completion in <see cref="BinaryReadBufferSize"/> chunks,
        /// throwing <see cref="RevisionSizeLimitException"/> and clearing everything accumulated so far
        /// as soon as the running total exceeds <paramref name="maximumBytes"/>. Internal and exposed to
        /// the test assembly via <see cref="InternalsVisibleToAttribute"/> so this bounded-read behavior
        /// can be verified without spinning up a real OS process for every scenario.
        /// </summary>
        internal static async Task<byte[]> ReadBoundedAsync(Stream stream, int maximumBytes, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[BinaryReadBufferSize];
            using (MemoryStream accumulated = new MemoryStream())
            {
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    accumulated.Write(buffer, 0, bytesRead);
                    if (accumulated.Length > maximumBytes)
                    {
                        accumulated.SetLength(0);
                        Array.Clear(buffer, 0, buffer.Length);
                        throw new RevisionSizeLimitException(
                            $"Git standard output exceeded the {maximumBytes}-byte limit.");
                    }
                }

                Array.Clear(buffer, 0, buffer.Length);
                return accumulated.ToArray();
            }
        }

        private static AddInProcessStartInfo BuildStartInfo(
            string gitExecutablePath, string workingDirectory, IReadOnlyList<string> arguments)
            => new AddInProcessStartInfo
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

        internal static string BuildArgumentString(IReadOnlyList<string> arguments)
        {
            StringBuilder builder = new();
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

        private static void TryKill(AddInProcess process)
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
