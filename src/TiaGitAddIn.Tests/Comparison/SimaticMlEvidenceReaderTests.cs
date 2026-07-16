using System;
using TiaGitAddIn.Services.Comparison;
using Xunit;

namespace TiaGitAddIn.Tests.Comparison
{
    public sealed class SimaticMlEvidenceReaderTests
    {
        [Fact]
        public void ProbeFindsBlockElementAndProgrammingLanguage()
        {
            const string xml = "<SW.Blocks.FB><AttributeList><ProgrammingLanguage>LAD</ProgrammingLanguage></AttributeList></SW.Blocks.FB>";

            SimaticMlEvidence evidence = SimaticMlEvidenceReader.Probe(xml);

            Assert.True(evidence.IsWellFormed);
            Assert.Equal("SW.Blocks.FB", evidence.RootElementName);
            Assert.Equal("SW.Blocks.FB", evidence.BlockElementName);
            Assert.Equal("LAD", evidence.ProgrammingLanguageValue);
        }

        [Fact]
        public void ProbeReportsWellFormedNonSimaticXmlWithoutABlock()
        {
            const string xml = "<root><value>plain xml</value></root>";

            SimaticMlEvidence evidence = SimaticMlEvidenceReader.Probe(xml);

            Assert.True(evidence.IsWellFormed);
            Assert.Equal("root", evidence.RootElementName);
            Assert.Null(evidence.BlockElementName);
            Assert.Null(evidence.ProgrammingLanguageValue);
        }

        [Fact]
        public void ProbeReportsNotWellFormedForNonXmlText()
        {
            SimaticMlEvidence evidence = SimaticMlEvidenceReader.Probe("FUNCTION_BLOCK Motor\nEND_FUNCTION_BLOCK");

            Assert.False(evidence.IsWellFormed);
            Assert.Null(evidence.RootElementName);
        }

        [Fact]
        public void ProbeDoesNotExpandDoctypeDeclarations()
        {
            // DtdProcessing.Prohibit must reject any DOCTYPE outright rather than attempt expansion —
            // this is the guardrail against XXE/billion-laughs-style payloads riding in as revision text.
            const string maliciousXml = "<!DOCTYPE foo [<!ENTITY xxe SYSTEM \"file:///etc/passwd\">]><root>&xxe;</root>";

            SimaticMlEvidence evidence = SimaticMlEvidenceReader.Probe(maliciousXml);

            Assert.False(evidence.IsWellFormed);
        }

        [Fact]
        public void ProbeThrowsForNullText()
            => Assert.Throws<ArgumentNullException>(() => SimaticMlEvidenceReader.Probe(null!));
    }
}
