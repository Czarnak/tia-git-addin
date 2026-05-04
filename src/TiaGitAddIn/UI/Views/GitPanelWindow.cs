using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using TiaGitAddIn.UI.ViewModels;

namespace TiaGitAddIn.UI.Views
{
    public sealed class GitPanelWindow : Window
    {
        private readonly MainViewModel viewModel;

        public GitPanelWindow(MainViewModel viewModel)
        {
            this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = viewModel;
            Title = "TIA Git";
            Width = 860;
            Height = 620;
            MinWidth = 720;
            MinHeight = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Content = BuildContent();
            Loaded += OnLoaded;
        }

        private static FrameworkElement BuildContent()
        {
            DockPanel shell = new DockPanel
            {
                Margin = new Thickness(12)
            };

            shell.Children.Add(BuildHeader());
            shell.Children.Add(BuildTabs());
            return shell;
        }

        private static FrameworkElement BuildHeader()
        {
            StackPanel header = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, 10)
            };
            DockPanel.SetDock(header, Dock.Top);

            TextBlock repository = new TextBlock
            {
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            repository.SetBinding(TextBlock.TextProperty, new Binding("RepositoryPath")
            {
                StringFormat = "Repository: {0}"
            });

            TextBlock branch = new TextBlock();
            branch.SetBinding(TextBlock.TextProperty, new Binding("Status.CurrentBranch")
            {
                StringFormat = "Branch: {0}"
            });

            TextBlock tracking = new TextBlock();
            tracking.SetBinding(TextBlock.TextProperty, new Binding("Status.TrackingSummary"));

            header.Children.Add(repository);
            header.Children.Add(branch);
            header.Children.Add(tracking);
            return header;
        }

        private static FrameworkElement BuildTabs()
        {
            return new TabControl
            {
                Items =
                {
                    new TabItem
                    {
                        Header = "Status",
                        Content = BuildStatusTab()
                    },
                    new TabItem
                    {
                        Header = "Commit",
                        Content = BuildCommitTab()
                    }
                }
            };
        }

        private static FrameworkElement BuildStatusTab()
        {
            DockPanel panel = new DockPanel
            {
                Margin = new Thickness(8)
            };

            StackPanel toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };
            DockPanel.SetDock(toolbar, Dock.Top);
            toolbar.Children.Add(CreateButton("Refresh", "Status.RefreshCommand"));
            toolbar.Children.Add(CreateButton("Stage", "Status.StageSelectedCommand"));
            toolbar.Children.Add(CreateButton("Unstage", "Status.UnstageSelectedCommand"));

            TextBlock summary = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 8)
            };
            summary.SetBinding(TextBlock.TextProperty, new Binding("Status.StatusSummary"));
            DockPanel.SetDock(summary, Dock.Top);

            TextBlock operation = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };
            operation.SetBinding(TextBlock.TextProperty, new Binding("Status.LastOperationMessage"));
            DockPanel.SetDock(operation, Dock.Bottom);

            ListView list = new ListView();
            list.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("Status.Entries"));
            list.SetBinding(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, new Binding("Status.SelectedEntry")
            {
                Mode = BindingMode.TwoWay
            });
            list.View = new GridView
            {
                Columns =
                {
                    new GridViewColumn { Header = "File", DisplayMemberBinding = new Binding("FilePath"), Width = 520 },
                    new GridViewColumn { Header = "Area", DisplayMemberBinding = new Binding("Area"), Width = 120 },
                    new GridViewColumn { Header = "Status", DisplayMemberBinding = new Binding("StatusText"), Width = 120 }
                }
            };

            panel.Children.Add(toolbar);
            panel.Children.Add(summary);
            panel.Children.Add(operation);
            panel.Children.Add(list);
            return panel;
        }

        private static FrameworkElement BuildCommitTab()
        {
            DockPanel panel = new DockPanel
            {
                Margin = new Thickness(8)
            };

            TextBox message = new TextBox
            {
                AcceptsReturn = true,
                MinHeight = 120,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            message.SetBinding(TextBox.TextProperty, new Binding("Commit.CommitMessage")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            DockPanel.SetDock(message, Dock.Top);

            Button commit = CreateButton("Commit", "Commit.CommitCommand");
            commit.HorizontalAlignment = HorizontalAlignment.Left;
            commit.Margin = new Thickness(0, 8, 0, 0);
            DockPanel.SetDock(commit, Dock.Top);

            TextBlock validation = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };
            validation.SetBinding(TextBlock.TextProperty, new Binding("Commit.ValidationMessage"));
            DockPanel.SetDock(validation, Dock.Top);

            TextBlock operation = new TextBlock
            {
                Margin = new Thickness(0, 8, 0, 0)
            };
            operation.SetBinding(TextBlock.TextProperty, new Binding("Commit.LastOperationMessage"));
            DockPanel.SetDock(operation, Dock.Top);

            panel.Children.Add(message);
            panel.Children.Add(commit);
            panel.Children.Add(validation);
            panel.Children.Add(operation);
            return panel;
        }

        private static Button CreateButton(string text, string commandPath)
        {
            Button button = new Button
            {
                Content = text,
                MinWidth = 88,
                Margin = new Thickness(0, 0, 8, 0),
                Padding = new Thickness(10, 4, 10, 4)
            };
            button.SetBinding(Button.CommandProperty, new Binding(commandPath));
            return button;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            await RefreshOnLoadAsync().ConfigureAwait(true);
        }

        private async Task RefreshOnLoadAsync()
        {
            try
            {
                await viewModel.RefreshAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "TIA Git",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
