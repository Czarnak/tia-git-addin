using System;
using System.IO;
using System.Linq;

namespace TiaGitAddIn.Configuration
{
    public static class PathValidator
    {
        private const int MaxPathLength = 260;

        public static ValidationResult Validate(string? path)
        {
            if (path == null)
            {
                return ValidationResult.Invalid("Path is required.");
            }

            string safePath = path;

            if (string.IsNullOrWhiteSpace(safePath))
            {
                return ValidationResult.Invalid("Path is required.");
            }

            if (safePath.Length > MaxPathLength)
            {
                return ValidationResult.Invalid("Path is too long.");
            }

            if (safePath.Any(char.IsControl))
            {
                return ValidationResult.Invalid("Path contains control characters.");
            }

            string[] segments = safePath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == ".."))
            {
                return ValidationResult.Invalid("Path traversal is not allowed.");
            }

            char[] invalidChars = Path.GetInvalidPathChars();
            if (safePath.Any(invalidChars.Contains))
            {
                return ValidationResult.Invalid("Path contains invalid characters.");
            }

            return ValidationResult.Valid();
        }

        public static ValidationResult ValidateGitExecutablePath(string? path)
        {
            ValidationResult pathResult = Validate(path);
            if (!pathResult.IsValid)
            {
                return pathResult;
            }

            string fileName = Path.GetFileName(path);
            if (string.Equals(path, "git", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(fileName, "git.exe", StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Valid();
            }

            return ValidationResult.Invalid("Git executable must be git or git.exe.");
        }
    }
}
