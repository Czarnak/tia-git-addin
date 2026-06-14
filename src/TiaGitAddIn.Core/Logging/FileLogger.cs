using System;
using System.IO;

namespace TiaGitAddIn.Logging
{
    public sealed class FileLogger(string logFilePath) : IAddInLogger
    {
        private const long MaxLogBytes = 5 * 1024 * 1024;
        private static readonly object SyncRoot = new object();

        public FileLogger()
            : this(GetDefaultLogFilePath())
        {
        }

        public void Error(string message, Exception exception)
        {
            Write("ERROR", message + Environment.NewLine + exception);
        }

        public void Info(string message)
        {
            Write("INFO", message);
        }

        private static string GetDefaultLogFilePath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "TiaGitAddIn", "logs", "tia-git-addin.log");
        }

        private void Write(string level, string message)
        {
            string entry = string.Format(
                "{0:O} [{1}] {2}{3}",
                DateTimeOffset.Now,
                level,
                message,
                Environment.NewLine);

            try
            {
                // Serialize writes so concurrent loggers don't drop lines via swallowed IOExceptions.
                lock (SyncRoot)
                {
                    string? directory = Path.GetDirectoryName(logFilePath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // A rotation failure must not prevent the entry from being written.
                    try
                    {
                        RotateIfNeeded();
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }

                    File.AppendAllText(logFilePath, entry);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void RotateIfNeeded()
        {
            var info = new FileInfo(logFilePath);
            if (!info.Exists || info.Length < MaxLogBytes)
            {
                return;
            }

            string archivePath = logFilePath + ".1";
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            File.Move(logFilePath, archivePath);
        }
    }
}
