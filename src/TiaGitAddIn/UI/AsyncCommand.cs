using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TiaGitAddIn.UI
{
    public sealed class AsyncCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null) : ICommand
    {
        private readonly Func<object?, Task> executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        private bool isExecuting;

        public AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
            : this(_ => execute(), _ => canExecute?.Invoke() ?? true)
        {
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) =>
            !isExecuting && (canExecute == null || canExecute(parameter));

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
                await executeAsync(parameter).ConfigureAwait(true);
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
