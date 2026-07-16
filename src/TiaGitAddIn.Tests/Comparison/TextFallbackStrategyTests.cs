using System;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Comparison
{
    // Not one of the brief's literally-named files; added because the self-review checklist for this
    // task requires verifying (not just reading) that TextFallbackStrategy really propagates
    // OperationCanceledException unwrapped, and that it selects the correct limitation text.
    public sealed class TextFallbackStrategyTests
    {
        [Fact]
        public void SupportedKindsAreExactlyTheTextOnlyArtifactKinds()
        {
            var strategy = new TextFallbackStrategy(CreateFactory());

            Assert.Equal(4, strategy.SupportedKinds.Count);
            Assert.Contains(PlcArtifactKind.Text, strategy.SupportedKinds);
            Assert.Contains(PlcArtifactKind.GenericXml, strategy.SupportedKinds);
            Assert.Contains(PlcArtifactKind.Stl, strategy.SupportedKinds);
            Assert.Contains(PlcArtifactKind.Sfc, strategy.SupportedKinds);
        }

        [Fact]
        public async Task CompareAsyncUsesPairLimitationWhenPresent()
        {
            var descriptor = new PlcArtifactDescriptor(PlcArtifactKind.GenericXml, PlcComparisonMode.Text, new[] { "test" });
            var pair = new PlcArtifactPairDescriptor(descriptor, descriptor, PlcArtifactKind.GenericXml,
                PlcComparisonMode.Text, PlcPairChangeKind.Modified, "Custom limitation from classifier.");
            var request = new PlcComparisonRequest(
                ComparisonTestData.TextRevision(PlcRevisionSide.Left, "left"),
                ComparisonTestData.TextRevision(PlcRevisionSide.Right, "right"),
                pair);
            var context = new PlcComparisonContext(request, new ComparisonRawText("left", "right", false, false));
            var strategy = new TextFallbackStrategy(CreateFactory());

            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcSupportLevel.Fallback, result.SupportLevel);
            Assert.Equal("Custom limitation from classifier.", result.Limitation);
        }

        [Fact]
        public async Task CompareAsyncUsesDefaultLimitationWhenPairLimitationIsEmpty()
        {
            PlcComparisonContext context = ComparisonTestData.Context(PlcArtifactKind.Stl, PlcComparisonMode.Text);
            var strategy = new TextFallbackStrategy(CreateFactory());

            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcSupportLevel.Fallback, result.SupportLevel);
            Assert.Equal("Stl semantic comparison is unavailable.", result.Limitation);
        }

        [Fact]
        public async Task CompareAsyncPropagatesCancellationWithoutWrapping()
        {
            PlcComparisonContext context = ComparisonTestData.Context(PlcArtifactKind.Text, PlcComparisonMode.Text);
            var strategy = new TextFallbackStrategy(CreateFactory());
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => strategy.CompareAsync(context, cts.Token));
        }

        [Fact]
        public void ConstructorRejectsNullResultFactory()
        {
            Assert.Throws<ArgumentNullException>(() => new TextFallbackStrategy(null!));
        }

        private static PlcComparisonResultFactory CreateFactory()
            => new PlcComparisonResultFactory(new LineTextComparer(TextComparisonLimits.Default));
    }
}
