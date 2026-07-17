using System;
using System.Threading;
using System.Windows.Controls;
using TiaGitAddIn.UI.ViewModels;

namespace TiaGitAddIn.UI.Views
{
    public partial class DiffView : UserControl
    {
        public DiffView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The one genuine WPF event boundary in this view: an <c>async void</c> is unavoidable here
        /// because <see cref="SelectionChangedEventHandler"/> is a fire-and-forget event delegate. It only
        /// ever awaits <see cref="DiffViewModel.SelectEntryAsync"/> (a Task-returning, cancellation-aware
        /// method) and swallows exactly one exception kind -- cancellation from a superseded selection --
        /// which intentionally applies no state.
        /// </summary>
        private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is DiffViewModel viewModel)
            {
                try { await viewModel.SelectEntryAsync(viewModel.SelectedEntry, CancellationToken.None); }
                catch (OperationCanceledException) { }
            }
        }
    }
}
