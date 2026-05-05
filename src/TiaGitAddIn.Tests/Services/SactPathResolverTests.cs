using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public class SactPathResolverTests
    {
        [Fact]
        public void ResolveNodePath_ReturnsNode_WhenNodeIsAvailable()
        {
            var resolver = new SactPathResolver();
            var path = resolver.ResolveNodePath();
            
            // In this environment, node -v succeeded, so it should return "node"
            Assert.Equal("node", path);
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
