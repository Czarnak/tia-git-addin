using System;

namespace TiaGitAddIn.Services.Revision
{
    /// <summary>
    /// A revision could not be loaded (validation failure, process failure, or a read that did not match
    /// the size git itself reported). Callers at the comparison-load boundary must treat this as a hard
    /// error rather than silently falling back.
    /// </summary>
    public class RevisionLoadException : Exception
    {
        public RevisionLoadException(string message) : base(message)
        {
        }

        public RevisionLoadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// A revision's reported or observed size exceeds the configured maximum. Always thrown before any
    /// blob content is read, or (for the raw process stream) as soon as the running total exceeds the limit.
    /// </summary>
    public sealed class RevisionSizeLimitException : RevisionLoadException
    {
        public RevisionSizeLimitException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// A <see cref="PlcRevisionLease"/> could not clean up its scoped temporary directory after exhausting
    /// its retry budget. The message carries a redacted lease identifier only, never the full temporary path.
    /// </summary>
    public sealed class RevisionCleanupException : Exception
    {
        public RevisionCleanupException(string message) : base(message)
        {
        }

        public RevisionCleanupException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
