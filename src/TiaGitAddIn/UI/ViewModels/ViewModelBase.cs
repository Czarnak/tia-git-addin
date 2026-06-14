using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public abstract class ViewModelBase(IUiDispatcher? uiDispatcher) : INotifyPropertyChanged
    {
        private bool isBusy;
        private string busyMessage = string.Empty;
        private CancellationTokenSource? cts;

        protected ViewModelBase()
            : this(null)
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected IUiDispatcher UiDispatcher { get; } = uiDispatcher ?? ImmediateUiDispatcher.Instance;

        public bool IsBusy
        {
            get => isBusy;
            protected set
            {
                if (SetProperty(ref isBusy, value))
                {
                    OnBusyChanged();
                }
            }
        }

        public string BusyMessage
        {
            get => busyMessage;
            protected set => SetProperty(ref busyMessage, value);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            InvokeOnUI(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        protected void InvokeOnUI(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (UiDispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                UiDispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// Runs <paramref name="operation"/> inside the shared busy/cancellation shell: it cancels any
        /// in-flight operation, toggles <see cref="IsBusy"/>/<see cref="BusyMessage"/>, routes
        /// cancellation/errors through <see cref="ReportStatus"/>, and disposes the
        /// <see cref="CancellationTokenSource"/> when finished.
        /// </summary>
        protected async Task RunBusyAsync(string message, Func<CancellationToken, Task> operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            cts?.Cancel();
            CancellationTokenSource localCts = new();
            cts = localCts;

            IsBusy = true;
            BusyMessage = message;
            try
            {
                await operation(localCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                InvokeOnUI(() => ReportStatus("Cancelled."));
            }
            catch (Exception ex)
            {
                InvokeOnUI(() => ReportStatus($"Error: {ex.Message}"));
            }
            finally
            {
                if (ReferenceEquals(cts, localCts))
                {
                    cts = null;
                }

                localCts.Dispose();
                IsBusy = false;
                BusyMessage = string.Empty;
            }
        }

        /// <summary>Cancels the in-flight <see cref="RunBusyAsync"/> operation, if any.</summary>
        protected void RequestCancel() => cts?.Cancel();

        /// <summary>
        /// Invoked whenever <see cref="IsBusy"/> changes so derived view models can refresh
        /// command availability. Implementations should marshal to the UI thread themselves.
        /// </summary>
        protected virtual void OnBusyChanged()
        {
        }

        /// <summary>Reports a cancellation or error outcome surfaced by <see cref="RunBusyAsync"/>.</summary>
        protected virtual void ReportStatus(string message)
        {
        }

        protected bool SetProperty<T>(
            ref T field,
            T newValue,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            field = newValue;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(
            T currentValue,
            T newValue,
            Action<T> assign,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                return false;
            }

            InvokeOnUI(() =>
            {
                assign(newValue);
                OnPropertyChanged(propertyName);
            });
            return true;
        }
    }
}
