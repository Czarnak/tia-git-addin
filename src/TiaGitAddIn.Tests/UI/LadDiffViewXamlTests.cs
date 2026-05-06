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
