namespace TiaGitAddIn.Services
{
    public sealed class SactProcessResult
    {
        public int ExitCode { get; set; }

        public string StandardOutput { get; set; } = string.Empty;

        public string StandardError { get; set; } = string.Empty;

        public bool TimedOut { get; set; }

        public bool IsSuccess => ExitCode == 0 && !TimedOut;
    }
}