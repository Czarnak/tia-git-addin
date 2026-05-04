using System;

namespace TiaGitAddIn.UI
{
    public sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public static ImmediateUiDispatcher Instance { get; } = new ImmediateUiDispatcher();

        private ImmediateUiDispatcher()
        {
        }

        public bool CheckAccess() => true;

        public void Invoke(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action();
        }
    }
}
