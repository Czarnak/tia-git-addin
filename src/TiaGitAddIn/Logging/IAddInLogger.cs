using System;

namespace TiaGitAddIn.Logging
{
    public interface IAddInLogger
    {
        void Error(string message, Exception exception);

        void Info(string message);
    }
}
