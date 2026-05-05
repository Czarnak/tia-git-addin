using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TiaGitAddIn.UI;

namespace TiaGitAddIn.UI.ViewModels
{
    public abstract class ViewModelBase(IUiDispatcher? uiDispatcher) : INotifyPropertyChanged
    {
        protected ViewModelBase()
            : this(null)
        {
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected IUiDispatcher UiDispatcher { get; } = uiDispatcher ?? ImmediateUiDispatcher.Instance;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            InvokeOnUI(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }

        protected void InvokeOnUI(System.Action action)
        {
            if (action == null)
            {
                throw new System.ArgumentNullException(nameof(action));
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
            System.Action<T> assign,
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
