using System;
using System.Reflection;
using TiaGitAddIn.UI;
using Xunit;

namespace TiaGitAddIn.Tests.Entry
{
    public sealed class MenuSelectionContextResolverTests
    {
        [Fact]
        public void ResolveUsesNonGenericSelectionMethodWhenGenericOverloadExists()
        {
            object selected = new object();
            ProviderWithAmbiguousSelectionMethods provider =
                new ProviderWithAmbiguousSelectionMethods(selected);

            object? result = InvokeResolve(provider);

            Assert.Same(selected, result);
        }

        private static object? InvokeResolve(object provider)
        {
            Type resolverType = typeof(GitPanelLaunchService)
                .Assembly
                .GetType("TiaGitAddIn.Entry.MenuSelectionContextResolver")!;
            MethodInfo resolve = resolverType.GetMethod(
                "Resolve",
                BindingFlags.Public | BindingFlags.Static)!;

            return resolve.Invoke(null, new[] { provider });
        }

        private sealed class ProviderWithAmbiguousSelectionMethods
        {
            private readonly object selected;

            public ProviderWithAmbiguousSelectionMethods(object selected)
            {
                this.selected = selected;
            }

            public object GetSelection() => selected;

            public T GetSelection<T>() where T : class => (T)selected;
        }
    }
}
