using System;
using System.Windows.Threading;

namespace TiaGitAddIn.UI
{
    public sealed class WpfUiDispatcher(Dispatcher dispatcher) : IUiDispatcher
    {
        private readonly Dispatcher dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

        public static WpfUiDispatcher FromCurrentThread() =>
            new(Dispatcher.CurrentDispatcher);

        public bool CheckAccess() => dispatcher.CheckAccess();

        public void Invoke(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            dispatcher.Invoke(action);
        }
    }
}
