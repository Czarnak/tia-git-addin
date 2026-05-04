using System;
using System.Windows.Threading;

namespace TiaGitAddIn.UI
{
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        private readonly Dispatcher dispatcher;

        public WpfUiDispatcher(Dispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public static WpfUiDispatcher FromCurrentThread() =>
            new WpfUiDispatcher(Dispatcher.CurrentDispatcher);

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
