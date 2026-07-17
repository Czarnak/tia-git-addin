using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TiaGitAddIn.Logging;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.UI;
using TiaGitAddIn.UI.ViewModels.Comparison;
using TiaGitAddIn.UI.Views;
using TiaGitAddIn.UI.Views.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    /// <summary>
    /// Renders every foundation <see cref="ComparisonPresentationViewModel"/> through the real,
    /// compiled <c>ComparisonTemplates.xaml</c> and <see cref="ComparisonPresentationHost"/> on a
    /// dedicated STA thread (<see cref="WpfTestHost"/>), verifying each produces its dedicated view
    /// type, the limitation panel reacts to support level, and no binding errors are raised.
    /// </summary>
    public sealed class ComparisonViewSmokeTests
    {
        public static IEnumerable<object[]> FoundationViewModels()
        {
            yield return new object[] { BuildInterfaceViewModel(), typeof(InterfaceDiffView) };
            yield return new object[] { BuildLadViewModel(), typeof(LadDiffView) };
            yield return new object[] { BuildTextViewModel(), typeof(TextDiffView) };
            yield return new object[] { BuildUnsupportedViewModel(), typeof(TextBlock) };
            yield return new object[] { BuildErrorViewModel(), typeof(StackPanel) };
        }

        [Theory]
        [MemberData(nameof(FoundationViewModels))]
        public void TemplateProducesDedicatedViewForFoundationViewModel(
            ComparisonPresentationViewModel viewModel, Type expectedViewType)
        {
            WpfTestHost.Run(_ =>
            {
                (FrameworkElement element, IReadOnlyList<string> errors) = RenderTemplateContent(viewModel);

                Assert.IsType(expectedViewType, element);
                Assert.Empty(errors);
            });
        }

        [Theory]
        [InlineData(PlcSupportLevel.Full, false)]
        [InlineData(PlcSupportLevel.Partial, true)]
        [InlineData(PlcSupportLevel.Fallback, true)]
        [InlineData(PlcSupportLevel.Unsupported, true)]
        public void HostShowsLimitationPanelOnlyWhenSupportLevelIsNotFull(PlcSupportLevel supportLevel, bool expectedVisible)
        {
            WpfTestHost.Run(_ =>
            {
                ComparisonPresentationViewModel viewModel = BuildInterfaceViewModel(supportLevel);
                var host = new ComparisonPresentationHost { DataContext = viewModel };

                host.Measure(new Size(900, 700));
                host.Arrange(new Rect(0, 0, 900, 700));
                host.UpdateLayout();

                var panel = (FrameworkElement)host.FindName("LimitationPanel");
                Visibility expected = expectedVisible ? Visibility.Visible : Visibility.Collapsed;
                Assert.Equal(expected, panel.Visibility);
            });
        }

        [Fact]
        public void HostContentControlRendersDedicatedViewViaMergedTemplates()
        {
            WpfTestHost.Run(_ =>
            {
                ComparisonPresentationViewModel viewModel = BuildInterfaceViewModel();
                var host = new ComparisonPresentationHost { DataContext = viewModel };

                host.Measure(new Size(900, 700));
                host.Arrange(new Rect(0, 0, 900, 700));
                host.UpdateLayout();

                InterfaceDiffView? nested = FindVisualDescendant<InterfaceDiffView>(host);
                Assert.NotNull(nested);
            });
        }

        private static T? FindVisualDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T typed)
                {
                    return typed;
                }

                T? descendant = FindVisualDescendant<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }

        [Fact]
        public void HostShowsRawTextTabOnlyWhenRawTextIsPresent()
        {
            WpfTestHost.Run(_ =>
            {
                ComparisonPresentationViewModel withRawText = BuildTextViewModel();
                ComparisonPresentationViewModel withoutRawText = BuildInterfaceViewModel();

                var hostWithRawText = new ComparisonPresentationHost { DataContext = withRawText };
                var hostWithoutRawText = new ComparisonPresentationHost { DataContext = withoutRawText };

                foreach (var host in new[] { hostWithRawText, hostWithoutRawText })
                {
                    host.Measure(new Size(900, 700));
                    host.Arrange(new Rect(0, 0, 900, 700));
                    host.UpdateLayout();
                }

                var rawTextTabWith = (FrameworkElement)hostWithRawText.FindName("RawTextTab");
                var rawTextTabWithout = (FrameworkElement)hostWithoutRawText.FindName("RawTextTab");

                Assert.Equal(Visibility.Visible, rawTextTabWith.Visibility);
                Assert.Equal(Visibility.Collapsed, rawTextTabWithout.Visibility);
            });
        }

        private static (FrameworkElement Element, IReadOnlyList<string> Errors) RenderTemplateContent(
            ComparisonPresentationViewModel viewModel)
        {
            var dictionary = new ResourceDictionary
            {
                Source = new Uri("/TiaGitAddIn;component/UI/Views/Comparison/ComparisonTemplates.xaml", UriKind.Relative),
            };
            var template = (DataTemplate)dictionary[new DataTemplateKey(viewModel.GetType())];

            var listener = new CollectingTraceListener();
            PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
            try
            {
                var content = (FrameworkElement)template.LoadContent();

                // A plain Grid (not a themed Control) relays DataContext through pure inheritance,
                // so any explicit DataContext binding declared on the template's own root (e.g. the
                // LAD template's DataContext="{Binding Content}") still resolves against it exactly
                // as it would under the real ComparisonPresentationHost's ContentControl.
                var host = new Grid { DataContext = viewModel };
                host.Children.Add(content);
                host.Measure(new Size(900, 700));
                host.Arrange(new Rect(0, 0, 900, 700));
                host.UpdateLayout();

                return (content, listener.Messages.ToArray());
            }
            finally
            {
                PresentationTraceSources.DataBindingSource.Listeners.Remove(listener);
            }
        }

        private static InterfaceComparisonViewModel BuildInterfaceViewModel(PlcSupportLevel supportLevel = PlcSupportLevel.Full)
        {
            InterfaceMemberSnapshot leftMember = Member("Input", "Alarm", "Bool");
            InterfaceMemberSnapshot rightMember = Member("Input", "Alarm", "Word");
            InterfaceSectionSnapshot leftSection = Section("Input", leftMember);
            InterfaceSectionSnapshot rightSection = Section("Input", rightMember);
            var comparison = new InterfaceSectionComparison(InterfaceChangeKind.Modified, leftSection, rightSection, new[]
            {
                new InterfaceMemberComparison(
                    InterfaceChangeKind.Modified,
                    leftMember,
                    rightMember,
                    new[] { new InterfaceFieldChange(InterfaceFieldKind.Datatype, null, "Bool", "Word") },
                    Array.Empty<InterfaceMemberComparison>()),
            });
            var presentation = new InterfacePresentation(new[] { comparison });

            string limitation = supportLevel == PlcSupportLevel.Full
                ? string.Empty
                : "Only a subset of the interface could be compared.";

            var result = new PlcComparisonResult(
                PlcArtifactKind.Fbd,
                PlcComparisonMode.Structured,
                PlcComparisonMode.Structured,
                supportLevel,
                limitation,
                new[] { new PlcComparisonDiagnostic("IF001", PlcDiagnosticSeverity.Info, "Interface comparison completed.") },
                presentation,
                null);

            return new InterfaceComparisonViewModel(presentation, ComparisonViewModelMetadata.From(result));
        }

        private static LadComparisonViewModel BuildLadViewModel()
        {
            var sactResult = new SactCompareResult { State = CompareState.Changed, Content = new SactContentResult() };
            InterfaceSectionSnapshot section = Section("Input", Member("Input", "Alarm", "Bool"));
            var interfaceComparison = new InterfaceSectionComparison(InterfaceChangeKind.Unchanged, section, section,
                Array.Empty<InterfaceMemberComparison>());
            var interfacePresentation = new InterfacePresentation(new[] { interfaceComparison });
            var ladPresentation = new LadPresentation(sactResult, interfacePresentation);

            var result = new PlcComparisonResult(
                PlcArtifactKind.Lad,
                PlcComparisonMode.Visual,
                PlcComparisonMode.Visual,
                PlcSupportLevel.Partial,
                "Ladder rendering is best-effort; interface comparison is precise.",
                Array.Empty<PlcComparisonDiagnostic>(),
                ladPresentation,
                null);

            var metadata = ComparisonViewModelMetadata.From(result);
            var interfaceViewModel = new InterfaceComparisonViewModel(interfacePresentation, metadata);
            var ladDiffViewModel = new TiaGitAddIn.UI.ViewModels.LadDiffViewModel(
                sactResult, interfaceViewModel, new FileLogger(), ImmediateUiDispatcher.Instance);

            return new LadComparisonViewModel(ladDiffViewModel, metadata);
        }

        private static TextComparisonViewModel BuildTextViewModel()
        {
            var lines = new[]
            {
                new TextDiffLine(TextDiffLineKind.Unchanged, 1, 1, "NETWORK 1"),
                new TextDiffLine(TextDiffLineKind.Added, null, 2, "NEW LINE"),
                new TextDiffLine(TextDiffLineKind.Removed, 2, null, "OLD LINE"),
            };
            var presentation = new TextPresentation(lines);
            var rawText = new ComparisonRawText("NETWORK 1\r\nOLD LINE", "NETWORK 1\r\nNEW LINE", false, false);

            var result = new PlcComparisonResult(
                PlcArtifactKind.Scl,
                PlcComparisonMode.Text,
                PlcComparisonMode.Text,
                PlcSupportLevel.Fallback,
                "Fell back to line-based text comparison.",
                Array.Empty<PlcComparisonDiagnostic>(),
                presentation,
                rawText);

            return new TextComparisonViewModel(presentation, ComparisonViewModelMetadata.From(result));
        }

        private static UnsupportedComparisonViewModel BuildUnsupportedViewModel()
        {
            var result = new PlcComparisonResult(
                PlcArtifactKind.Binary,
                PlcComparisonMode.Unsupported,
                PlcComparisonMode.Unsupported,
                PlcSupportLevel.Unsupported,
                "Binary artifacts cannot be compared.",
                Array.Empty<PlcComparisonDiagnostic>(),
                new UnsupportedPresentation(),
                null);

            return new UnsupportedComparisonViewModel(ComparisonViewModelMetadata.From(result));
        }

        private static ErrorComparisonViewModel BuildErrorViewModel()
        {
            var result = new PlcComparisonResult(
                PlcArtifactKind.Unknown,
                PlcComparisonMode.Unsupported,
                PlcComparisonMode.Unsupported,
                PlcSupportLevel.Unsupported,
                "Comparison failed: the parser threw an unexpected exception.",
                Array.Empty<PlcComparisonDiagnostic>(),
                new ErrorPresentation(),
                null);

            return new ErrorComparisonViewModel(ComparisonViewModelMetadata.From(result));
        }

        private static InterfaceSectionSnapshot Section(string name, params InterfaceMemberSnapshot[] members) =>
            new InterfaceSectionSnapshot(name, true, members);

        private static InterfaceMemberSnapshot Member(string section, string name, string datatype) =>
            new InterfaceMemberSnapshot(
                section,
                name,
                name,
                datatype,
                SemanticBoolean.Unspecified,
                null,
                null,
                new Dictionary<string, string>(),
                null,
                SemanticBoolean.Unspecified,
                null,
                new Dictionary<string, string>(),
                Array.Empty<InterfaceMemberSnapshot>());

        private sealed class CollectingTraceListener : TraceListener
        {
            public List<string> Messages { get; } = new();

            public override void Write(string? message)
            {
            }

            public override void WriteLine(string? message)
            {
                if (message != null && message.Length > 0)
                {
                    Messages.Add(message);
                }
            }
        }
    }
}
