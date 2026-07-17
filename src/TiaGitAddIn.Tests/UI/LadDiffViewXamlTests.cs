using System.IO;
using Xunit;

namespace TiaGitAddIn.Tests.UI
{
    public sealed class LadDiffViewXamlTests
    {
        [Fact]
        public void NetworkSideContainersDoNotBindBackgroundToNetworkDiffState()
        {
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.DoesNotContain("Background=\"{Binding Left.DiffState", xaml);
            Assert.DoesNotContain("Background=\"{Binding Right.DiffState", xaml);
        }

        [Fact]
        public void ElementHighlightWrapsRenderedGraphicWithoutFaintOpacityLayer()
        {
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.Contains("Padding=\"6\"", xaml);
            Assert.Contains("Background=\"{Binding DiffState, Converter={StaticResource CompareStateToColorConverter}}\"", xaml);
            Assert.DoesNotContain("Opacity=\"0.3\"", xaml);
        }

        [Fact]
        public void ViewRendersInterfaceComparisonAndDynamicBoxPins()
        {
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.Contains("comparison:InterfaceDiffView", xaml);
            Assert.Contains("DataContext=\"{Binding InterfaceComparison}\"", xaml);
            Assert.Contains("ItemsSource=\"{Binding InputPinRows}\"", xaml);
            Assert.Contains("ItemsSource=\"{Binding OutputPinRows}\"", xaml);
        }

        [Fact]
        public void DynamicBoxPinsRenderAsVisibleConnectorRows()
        {
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.Contains("x:Key=\"LadBoxPinTextStyle\"", xaml);
            Assert.Contains("x:Key=\"LadInputPinRowTemplate\"", xaml);
            Assert.Contains("x:Key=\"LadOutputPinRowTemplate\"", xaml);
            Assert.Contains("Tag=\"PinConnectorLine\"", xaml);
            Assert.Contains("Text=\"{Binding Operand}\"", xaml);
            Assert.Contains("Text=\"{Binding Name}\"", xaml);
        }

        [Fact]
        public void ElementTemplateRendersInstructionCommentAndEquation()
        {
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.Contains("Text=\"{Binding Comment}\"", xaml);
            Assert.Contains("Text=\"{Binding Equation}\"", xaml);
        }

        [Fact]
        public void InterfaceComparisonReplacesLegacyTiaPortalColumnTable()
        {
            // The right-first flat table (Name/Data type/Default value/Comment columns keyed off
            // IsSectionHeader) was replaced by the deep InterfaceDiffView per Task 9; this locks
            // that the legacy markup does not silently return.
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.DoesNotContain("Text=\"{Binding InterfaceTitle}\"", xaml);
            Assert.DoesNotContain("IsSectionHeader", xaml);
            Assert.Contains("xmlns:comparison=\"clr-namespace:TiaGitAddIn.UI.Views.Comparison\"", xaml);
        }

        [Fact]
        public void BoxTemplateBindsContentControlSizeSoRightPinsAreVisible()
        {
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.Contains("Width=\"{Binding Width}\"", xaml);
            Assert.Contains("Height=\"{Binding Height}\"", xaml);
            Assert.Contains("HorizontalContentAlignment=\"Stretch\"", xaml);
            Assert.Contains("HorizontalAlignment=\"Right\"", xaml);
        }

        private static string GetLadDiffViewPath()
        {
            string root = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(
                    root,
                    "src",
                    "TiaGitAddIn",
                    "UI",
                    "Views",
                    "LadDiffView.xaml");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                DirectoryInfo? parent = Directory.GetParent(root);
                if (parent == null)
                {
                    break;
                }

                root = parent.FullName;
            }

            throw new FileNotFoundException("LadDiffView.xaml not found.");
        }
    }
}
