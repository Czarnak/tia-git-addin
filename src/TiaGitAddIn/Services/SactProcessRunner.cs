using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AddInProcess = Siemens.Engineering.AddIn.Utilities.Process;
using AddInProcessStartInfo = Siemens.Engineering.AddIn.Utilities.ProcessStartInfo;

namespace TiaGitAddIn.Services
{
    public sealed class SactProcessRunner(TimeSpan timeout) : ISactProcessRunner
    {
        public SactProcessRunner()
            : this(TimeSpan.FromSeconds(30))
        {
        }

        public Task<SactProcessResult> RunAsync(
            string fileName,
            string arguments,
            CancellationToken ct,
            IDictionary<string, string>? environmentVariables = null)
        {
            return Task.Run(
                () => RunProcess(fileName, arguments, ct, environmentVariables),
                ct);
        }

        private SactProcessResult RunProcess(
            string fileName,
            string arguments,
            CancellationToken cancellationToken,
            IDictionary<string, string>? environmentVariables)
        {
            using (AddInProcess process = new())
            {
                process.StartInfo = new AddInProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                if (environmentVariables != null)
                {
                    foreach (var kvp in environmentVariables)
                    {
#if NETFRAMEWORK
                        process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
#else
                        process.StartInfo.Environment[kvp.Key] = kvp.Value;
#endif
                    }
                }

                using (CancellationTokenRegistration cancellation = cancellationToken.Register(() => TryKill(process)))
                {
                    process.Start();

                    // We need to read the streams asynchronously to avoid deadlocks
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();

                    bool exited = process.WaitForExit((int)timeout.TotalMilliseconds);
                    if (!exited)
                    {
                        TryKill(process);
                        return new SactProcessResult
                        {
                            ExitCode = -1,
                            TimedOut = true,
                            StandardError = "SACT operation timed out."
                        };
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    return new SactProcessResult
                    {
                        ExitCode = process.ExitCode,
                        StandardOutput = outputTask.Result,
                        StandardError = errorTask.Result
                    };
                }
            }
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
            catch (Exception)
            {
            }
        }
    }
}