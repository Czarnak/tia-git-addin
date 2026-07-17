using System;
using System.Threading;
using System.Threading.Tasks;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public sealed class LadComparisonStrategyTests
    {
        [Fact]
        public void SupportedKinds_IsExactlyLad()
        {
            LadComparisonStrategy strategy = CreateStrategy();

            Assert.Equal(new[] { PlcArtifactKind.Lad }, strategy.SupportedKinds);
        }

        [Fact]
        public async Task CompareAsync_MalformedXmlOnEitherSide_ThrowsRecoverableComparisonException()
        {
            LadComparisonStrategy strategy = CreateStrategy();
            PlcComparisonContext context = CreateContext("<Document><SW.Blocks.FB ID=\"1\">", ValidLadXml());

            await Assert.ThrowsAsync<RecoverableComparisonException>(() =>
                strategy.CompareAsync(context, CancellationToken.None));
        }

        [Fact]
        public async Task CompareAsync_RecognizedFlgNetNetworks_ReturnsFullVisualLadPresentation()
        {
            LadComparisonStrategy strategy = CreateStrategy();
            PlcComparisonContext context = CreateContext(ValidLadXml("Contact1"), ValidLadXml("Contact2"));

            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcComparisonMode.Visual, result.ActualMode);
            Assert.Equal(PlcSupportLevel.Full, result.SupportLevel);
            Assert.Equal(string.Empty, result.Limitation);
            Assert.IsType<LadPresentation>(result.Presentation);
        }

        [Fact]
        public async Task CompareAsync_UnrecognizedNetworkFormat_ReturnsPartialStructuredInterfaceOnly()
        {
            LadComparisonStrategy strategy = CreateStrategy();
            PlcComparisonContext context = CreateContext(UnrecognizedNetworkFormatXml(), UnrecognizedNetworkFormatXml());

            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcComparisonMode.Structured, result.ActualMode);
            Assert.Equal(PlcSupportLevel.Partial, result.SupportLevel);
            Assert.False(string.IsNullOrWhiteSpace(result.Limitation));
            Assert.IsType<InterfacePresentation>(result.Presentation);
        }

        [Fact]
        public async Task CompareAsync_MissingLeftSide_StillProducesAResultFromTheRightSideAlone()
        {
            LadComparisonStrategy strategy = CreateStrategy();
            PlcComparisonContext context = CreateContext(rightXml: ValidLadXml(), leftMissing: true);

            PlcComparisonResult result = await strategy.CompareAsync(context, CancellationToken.None);

            Assert.Equal(PlcComparisonMode.Visual, result.ActualMode);
            Assert.Equal(PlcSupportLevel.Full, result.SupportLevel);
        }

        [Fact]
        public async Task CompareAsync_AlreadyCancelledToken_PropagatesCancellationUnwrapped()
        {
            LadComparisonStrategy strategy = CreateStrategy();
            PlcComparisonContext context = CreateContext(ValidLadXml(), ValidLadXml());
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                strategy.CompareAsync(context, cts.Token));
        }

        private static LadComparisonStrategy CreateStrategy()
        {
            var textComparer = new LineTextComparer(TextComparisonLimits.Default);
            var resultFactory = new PlcComparisonResultFactory(textComparer);
            var sanitizer = new ComparisonDiagnosticSanitizer();
            return new LadComparisonStrategy(resultFactory, sanitizer);
        }

        private static PlcComparisonContext CreateContext(string? leftXml = null, string? rightXml = null, bool leftMissing = false)
        {
            PlcRevision left = leftMissing
                ? PlcRevision.Missing(PlcRevisionSide.Left, PlcRevisionSource.Head, "Block.xml", PlcRevisionMissingReason.Added)
                : PresentRevision(PlcRevisionSide.Left, leftXml!);
            PlcRevision right = PresentRevision(PlcRevisionSide.Right, rightXml!);

            var pair = new PlcArtifactPairDescriptor(
                leftMissing ? null : new PlcArtifactDescriptor(PlcArtifactKind.Lad, PlcComparisonMode.Visual, Array.Empty<string>()),
                new PlcArtifactDescriptor(PlcArtifactKind.Lad, PlcComparisonMode.Visual, Array.Empty<string>()),
                PlcArtifactKind.Lad, PlcComparisonMode.Visual,
                leftMissing ? PlcPairChangeKind.Added : PlcPairChangeKind.Modified,
                string.Empty);

            var request = new PlcComparisonRequest(left, right, pair);
            var rawText = new ComparisonRawText(leftMissing ? null : leftXml, rightXml, leftMissing, false);
            return new PlcComparisonContext(request, rawText);
        }

        private static PlcRevision PresentRevision(PlcRevisionSide side, string xml)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(xml);
            return PlcRevision.Present(
                side, PlcRevisionSource.Head, "Block.xml", bytes, PlcTextEncoding.Utf8WithoutBom, xml, false, string.Empty);
        }

        private static string ValidLadXml(string contactName = "Contact") => $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Document>
  <Engineering version=""V21"" />
  <SW.Blocks.FB ID=""0"">
    <AttributeList>
      <Name>{contactName}Block</Name>
      <ProgrammingLanguage>LAD</ProgrammingLanguage>
    </AttributeList>
    <ObjectList>
      <SW.Blocks.CompileUnit ID=""1"">
        <AttributeList>
          <ProgrammingLanguage>LAD</ProgrammingLanguage>
          <NetworkSource>
            <FlgNet>
              <Parts>
                <Part Name=""{contactName}"" UId=""21""/>
              </Parts>
              <Wires/>
            </FlgNet>
          </NetworkSource>
        </AttributeList>
      </SW.Blocks.CompileUnit>
    </ObjectList>
  </SW.Blocks.FB>
</Document>";

        private static string UnrecognizedNetworkFormatXml() => @"<?xml version=""1.0"" encoding=""utf-8""?>
<Document>
  <Engineering version=""V21"" />
  <SW.Blocks.FB ID=""0"">
    <AttributeList>
      <Name>Block</Name>
      <ProgrammingLanguage>LAD</ProgrammingLanguage>
    </AttributeList>
    <ObjectList>
      <SW.Blocks.CompileUnit ID=""1"">
        <AttributeList>
          <ProgrammingLanguage>LAD</ProgrammingLanguage>
          <NetworkSource>
            <SomeFutureFormat/>
          </NetworkSource>
        </AttributeList>
      </SW.Blocks.CompileUnit>
    </ObjectList>
  </SW.Blocks.FB>
</Document>";
    }
}
