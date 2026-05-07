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
        public void ViewRendersInterfaceRowsAndDynamicBoxPins()
        {
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.Contains("ItemsSource=\"{Binding InterfaceRows}\"", xaml);
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
        public void InterfaceTableUsesTiaPortalColumnsAndSectionRows()
        {
            string xaml = File.ReadAllText(GetLadDiffViewPath());

            Assert.Contains("Text=\"{Binding InterfaceTitle}\"", xaml);
            Assert.Contains("Text=\"Name\"", xaml);
            Assert.Contains("Text=\"Data type\"", xaml);
            Assert.Contains("Text=\"Default value\"", xaml);
            Assert.Contains("Text=\"Comment\"", xaml);
            Assert.Contains("IsSectionHeader", xaml);
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
