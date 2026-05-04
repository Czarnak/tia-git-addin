using System;
using System.IO;

namespace TiaGitAddIn.Logging
{
    public sealed class FileLogger : IAddInLogger
    {
        private readonly string logFilePath;

        public FileLogger()
            : this(GetDefaultLogFilePath())
        {
        }

        public FileLogger(string logFilePath)
        {
            this.logFilePath = logFilePath;
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
            try
            {
                string? directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string entry = string.Format(
                    "{0:O} [{1}] {2}{3}",
                    DateTimeOffset.Now,
                    level,
                    message,
                    Environment.NewLine);

                File.AppendAllText(logFilePath, entry);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
