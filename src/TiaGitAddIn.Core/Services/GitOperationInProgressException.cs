using System;

namespace TiaGitAddIn.Services
{
    public sealed class GitOperationInProgressException : InvalidOperationException
    {
        public GitOperationInProgressException()
            : base("Another Git operation is already running.")
        {
        }
    }
}
