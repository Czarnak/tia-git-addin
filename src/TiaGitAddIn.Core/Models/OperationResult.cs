namespace TiaGitAddIn.Models
{
    public sealed class OperationResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public string Detail { get; set; } = string.Empty;

        /// <summary>User-facing message combining <see cref="Message"/> with <see cref="Detail"/> when present.</summary>
        public string DisplayMessage =>
            string.IsNullOrWhiteSpace(Detail) ? Message : $"{Message} {Detail}";

        public static OperationResult Ok(string message = "") =>
            new()
            { Success = true, Message = message };

        public static OperationResult Fail(string message, string detail = "") =>
            new()
            { Success = false, Message = message, Detail = detail };
    }
}
