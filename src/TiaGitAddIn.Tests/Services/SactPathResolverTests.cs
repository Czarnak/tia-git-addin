using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public class SactPathResolverTests
    {
        [Fact]
        public void ResolveNodePath_ReturnsOverride_WhenProvided()
        {
            var resolver = new SactPathResolver(nodeOverride: "custom_node");
            Assert.Equal("custom_node", resolver.ResolveNodePath());
        }

        [Fact]
        public void ResolveSiemensInstallPath_DoesNotThrow()
        {
            var resolver = new SactPathResolver();
            var path = resolver.ResolveSiemensInstallPath();
            
            // We don't know if it's installed, but it should not throw.
            // If it's not installed, it should be null.
        }
    }
}
