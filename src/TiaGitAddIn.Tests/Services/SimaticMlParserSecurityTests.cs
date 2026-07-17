using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.SimaticMl;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    /// <summary>
    /// Locks the safe-XML-loading boundary of <see cref="SimaticMlParser.ParseText"/>: DTD/external-entity
    /// rejection, exact character/element/depth limits, cooperative cancellation, and the read-only shape of
    /// the parsed model. No test here may assert on raw XML, entity content, file paths, or stack traces
    /// appearing in a diagnostic message - only on stable diagnostic codes and safe side/line/column data.
    /// </summary>
    public sealed class SimaticMlParserSecurityTests
    {
        private const string MinimalFbXml =
            "<Document><Engineering version=\"V21\" /><SW.Blocks.FC ID=\"0\"><AttributeList><Name>Block</Name></AttributeList></SW.Blocks.FC></Document>";

        [Fact]
        public void ParseTextRejectsDtdWithoutResolvingExternalContent()
        {
            string xml = "<!DOCTYPE x [<!ENTITY leak SYSTEM 'file:///C:/Windows/win.ini'>]>" +
                         "<Document><SW.Blocks.FB><AttributeList><Name>&leak;</Name></AttributeList></SW.Blocks.FB></Document>";

            SimaticMlParseResult result = SimaticMlParser.ParseText(xml, SimaticMlParserLimits.Default,
                PlcRevisionSide.Left, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Model);
            Assert.Contains(result.Diagnostics, d => d.Code == "CMP-XML-DTD");
            Assert.DoesNotContain("Windows", string.Join(" ", result.Diagnostics.Select(d => d.Message)), StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("characters")]
        [InlineData("elements")]
        [InlineData("depth")]
        public void ParseTextAcceptsNAndRejectsNPlusOne(string boundary)
        {
            ParserBoundaryCase fixture = ParserBoundaryCase.Create(boundary);

            Assert.True(SimaticMlParser.ParseText(fixture.AtLimitXml, fixture.Limits,
                PlcRevisionSide.Left, CancellationToken.None).IsSuccess);

            SimaticMlParseResult over = SimaticMlParser.ParseText(fixture.OverLimitXml, fixture.Limits,
                PlcRevisionSide.Left, CancellationToken.None);

            Assert.False(over.IsSuccess);
            Assert.Contains(over.Diagnostics, d => d.Code == fixture.ExpectedDiagnosticCode);
        }

        [Fact]
        public void ParseTextObservesPreCancelledToken()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.ThrowsAny<OperationCanceledException>(() => SimaticMlParser.ParseText(
                MinimalFbXml, SimaticMlParserLimits.Default, PlcRevisionSide.Right, cts.Token));
        }

        [Fact]
        public void ParseTextParsesWellFormedDocumentSuccessfully()
        {
            SimaticMlParseResult result = SimaticMlParser.ParseText(MinimalFbXml, SimaticMlParserLimits.Default,
                PlcRevisionSide.Left, CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Model);
            Assert.Empty(result.Diagnostics);
            Assert.Equal("V21", result.Model!.EngineeringVersion);
        }

        [Fact]
        public void ParseTextRejectsMalformedXmlWithSyntaxDiagnostic()
        {
            // Mismatched end tag: never a DTD problem, must classify as CMP-XML-SYNTAX.
            string malformedXml = "<Document><SW.Blocks.FC ID=\"0\"></Document>";

            SimaticMlParseResult result = SimaticMlParser.ParseText(malformedXml, SimaticMlParserLimits.Default,
                PlcRevisionSide.Left, CancellationToken.None);

            Assert.False(result.IsSuccess);
            Assert.Null(result.Model);
            Assert.Contains(result.Diagnostics, d => d.Code == "CMP-XML-SYNTAX");
        }

        [Fact]
        public void ParsedModelIsImmutableAndUnaffectedByRepeatedComparison()
        {
            const string richXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Document>
  <Engineering version=""V21"" />
  <SW.Blocks.FC ID=""0"">
    <AttributeList>
      <Name>Block</Name>
      <ProgrammingLanguage>LAD</ProgrammingLanguage>
      <Interface>
        <Sections>
          <Section Name=""Input"">
            <Member Name=""Start"" Datatype=""Bool"" />
          </Section>
        </Sections>
      </Interface>
    </AttributeList>
    <ObjectList>
      <SW.Blocks.CompileUnit ID=""1"">
        <AttributeList>
          <ProgrammingLanguage>LAD</ProgrammingLanguage>
          <NetworkSource>
            <FlgNet>
              <Parts>
                <Access Scope=""LocalVariable"" UId=""11"">
                  <Symbol>
                    <Component Name=""motor"" />
                  </Symbol>
                </Access>
                <Part UId=""21"" Name=""Contact"" />
              </Parts>
              <Wires>
                <Wire UId=""100""><Powerrail /><NameCon UId=""21"" Name=""in"" /></Wire>
              </Wires>
            </FlgNet>
          </NetworkSource>
        </AttributeList>
      </SW.Blocks.CompileUnit>
    </ObjectList>
  </SW.Blocks.FC>
</Document>";

            SimaticMlParseResult result = SimaticMlParser.ParseText(richXml, SimaticMlParserLimits.Default,
                PlcRevisionSide.Left, CancellationToken.None);
            Assert.True(result.IsSuccess);
            SimaticMlFile model = result.Model!;

            string beforeJson = JsonConvert.SerializeObject(model);

            SimaticMlComparer.Compare(model, model);
            SimaticMlComparer.Compare(model, model);

            string afterJson = JsonConvert.SerializeObject(model);

            Assert.Equal(beforeJson, afterJson);
        }

        [Fact]
        public void ModelCollectionPropertiesAreNeverAssignableToMutableCollectionInterfaces()
        {
            Type[] modelTypes =
            {
                typeof(SimaticMlFile),
                typeof(BlockDefinition),
                typeof(InterfaceSection),
                typeof(InterfaceMember),
                typeof(CompileUnitDefinition),
                typeof(NetworkSourceDefinition),
                typeof(AccessDefinition),
                typeof(AccessComponentDefinition),
                typeof(PartDefinition),
                typeof(CallDefinition),
                typeof(InstanceDefinition),
                typeof(CallInfoDefinition),
                typeof(CallParameterDefinition),
                typeof(PowerrailDefinition),
                typeof(OpenbranchDefinition),
                typeof(TemplateValueDefinition),
                typeof(WireDefinition),
                typeof(ConnectionDefinition),
                typeof(NameConDefinition),
                typeof(IdentConDefinition),
                typeof(OpenConDefinition),
                typeof(PowerrailConDefinition),
                typeof(OpenbranchConDefinition),
                typeof(MultilingualTextDefinition),
                typeof(MultilingualTextItemDefinition),
            };

            foreach (Type modelType in modelTypes)
            {
                foreach (PropertyInfo property in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    Type propertyType = property.PropertyType;

                    Assert.False(ImplementsOpenGeneric(propertyType, typeof(IList<>)),
                        $"{modelType.Name}.{property.Name} must not be assignable to IList<T>.");
                    Assert.False(ImplementsOpenGeneric(propertyType, typeof(IDictionary<,>)),
                        $"{modelType.Name}.{property.Name} must not be assignable to IDictionary<TKey,TValue>.");

                    Assert.False(property.CanWrite && property.SetMethod!.IsPublic,
                        $"{modelType.Name}.{property.Name} must not expose a public setter.");
                }
            }
        }

        private static bool ImplementsOpenGeneric(Type type, Type openGenericInterface)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == openGenericInterface)
            {
                return true;
            }

            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);
        }
    }

    /// <summary>
    /// Builds matched at-limit/over-limit XML documents plus a tight <see cref="SimaticMlParserLimits"/> so
    /// exactly one dimension (characters, elements, or depth) is the binding constraint for a given case.
    /// </summary>
    internal sealed class ParserBoundaryCase
    {
        private ParserBoundaryCase(string atLimitXml, string overLimitXml, SimaticMlParserLimits limits, string expectedDiagnosticCode)
        {
            AtLimitXml = atLimitXml;
            OverLimitXml = overLimitXml;
            Limits = limits;
            ExpectedDiagnosticCode = expectedDiagnosticCode;
        }

        public string AtLimitXml { get; }
        public string OverLimitXml { get; }
        public SimaticMlParserLimits Limits { get; }
        public string ExpectedDiagnosticCode { get; }

        public static ParserBoundaryCase Create(string boundary)
        {
            return boundary switch
            {
                "characters" => CreateCharactersCase(),
                "elements" => CreateElementsCase(),
                "depth" => CreateDepthCase(),
                _ => throw new ArgumentOutOfRangeException(nameof(boundary), boundary, "Unknown parser boundary case."),
            };
        }

        private static ParserBoundaryCase CreateCharactersCase()
        {
            string atLimitXml = "<Document><Engineering version=\"V21\" /></Document>";
            string overLimitXml = atLimitXml + " ";
            var limits = new SimaticMlParserLimits(atLimitXml.Length, 250_000, 128);
            return new ParserBoundaryCase(atLimitXml, overLimitXml, limits, "CMP-XML-LIMIT-CHARACTERS");
        }

        private static ParserBoundaryCase CreateElementsCase()
        {
            const int fillerCount = 5;
            string atLimitXml = BuildDocumentWithFillers(fillerCount);
            string overLimitXml = BuildDocumentWithFillers(fillerCount + 1);
            int elementCountAtLimit = fillerCount + 1; // <Document> itself plus each filler element.
            var limits = new SimaticMlParserLimits(1_000_000, elementCountAtLimit, 128);
            return new ParserBoundaryCase(atLimitXml, overLimitXml, limits, "CMP-XML-LIMIT-ELEMENTS");
        }

        private static ParserBoundaryCase CreateDepthCase()
        {
            const int depth = 5;
            string atLimitXml = BuildNestedDocument(depth);
            string overLimitXml = BuildNestedDocument(depth + 1);
            var limits = new SimaticMlParserLimits(1_000_000, 250_000, depth);
            return new ParserBoundaryCase(atLimitXml, overLimitXml, limits, "CMP-XML-LIMIT-DEPTH");
        }

        private static string BuildDocumentWithFillers(int fillerCount)
        {
            var builder = new StringBuilder("<Document>");
            for (int i = 0; i < fillerCount; i++)
            {
                builder.Append("<F/>");
            }

            builder.Append("</Document>");
            return builder.ToString();
        }

        private static string BuildNestedDocument(int depth)
        {
            var builder = new StringBuilder("<Document>");
            for (int i = 0; i < depth; i++)
            {
                builder.Append("<A>");
            }

            for (int i = 0; i < depth; i++)
            {
                builder.Append("</A>");
            }

            builder.Append("</Document>");
            return builder.ToString();
        }
    }
}
