using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TiaGitAddIn.UI
{
    public sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> executeAsync;
        private readonly Func<bool>? canExecute;
        private bool isExecuting;

        public AsyncCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            this.executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) =>
            !isExecuting && (canExecute == null || canExecute());

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            isExecuting = true;
            RaiseCanExecuteChanged();
            try
            {
                await executeAsync().ConfigureAwait(true);
            }
            finally
            {
                isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
