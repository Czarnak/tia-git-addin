using System;

namespace TiaGitAddIn.UI
{
    public interface IUiDispatcher
    {
        bool CheckAccess();

        void Invoke(Action action);
    }
}
