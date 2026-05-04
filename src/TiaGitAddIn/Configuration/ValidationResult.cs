namespace TiaGitAddIn.Configuration
{
    public sealed class ValidationResult
    {
        public bool IsValid { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public static ValidationResult Valid() =>
            new ValidationResult { IsValid = true };

        public static ValidationResult Invalid(string errorMessage) =>
            new ValidationResult { IsValid = false, ErrorMessage = errorMessage };
    }
}
