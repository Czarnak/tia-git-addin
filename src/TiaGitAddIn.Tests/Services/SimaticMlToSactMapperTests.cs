using System;
using System.IO;
using System.Linq;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services.SimaticMl;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public sealed class SimaticMlToSactMapperTests
    {
        [Fact]
        public void Map_CallParameterIdentConnections_AttachesOperandsToPins()
        {
            string path = Path.Combine(Path.GetTempPath(), $"tia-git-addin-{Guid.NewGuid():N}.xml");
            File.WriteAllText(path, CreateSimaticMlWithCallParameterOperands());

            try
            {
                var file = SimaticMlParser.Parse(path);
                var result = SimaticMlToSactMapper.Map(file);
                var call = result.Content!.Networks.Single().Value.Body["27"];

                Assert.Equal("deviceState", call.DisplayName);
                Assert.Empty(call.TopOperandConnector?.DisplayName ?? string.Empty);
                Assert.Contains(call.InputParameters, p =>
                    p.Name == "Alarm" && p.Operand == "false");
                Assert.Contains(call.InputParameters, p =>
                    p.Name == "Running" && p.Operand == "#tempOut");
                Assert.Contains(call.InputParameters, p =>
                    p.Name == "Service" && p.Operand == "#FI_SERVICE");
                Assert.Contains(call.InputParameters, p =>
                    p.Name == "deviceIcon" && p.Operand == "#FIQ_Icon");
                Assert.Contains(call.OutputParameters, p =>
                    p.Name == "deviceIcon" && p.Operand == "#FIQ_Icon");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void Map_RichLadPartMetadata_BuildsRenderableSemantics()
        {
            var network = new NetworkSourceDefinition
            {
                Accesses =
                {
                    new AccessDefinition
                    {
                        UId = 10,
                        Scope = "LocalVariable",
                        SymbolComponents = { "startSignal" },
                        SymbolPath = "startSignal"
                    }
                },
                Parts =
                {
                    new PartDefinition
                    {
                        UId = 20,
                        Name = "Contact",
                        CommentText = "Start condition",
                        Negated = { "in" },
                        Invisible = { "eno" },
                        RawXml = "<Part UId=\"20\" Name=\"Contact\"><Negated Name=\"in\" /><Invisible Name=\"eno\" /></Part>"
                    },
                    new PartDefinition
                    {
                        UId = 30,
                        Name = "SCoil",
                        RawXml = "<Part UId=\"30\" Name=\"SCoil\" />"
                    }
                },
                Wires =
                {
                    new WireDefinition
                    {
                        UId = 1,
                        Connections =
                        {
                            new PowerrailConDefinition(),
                            new NameConDefinition { UId = 20, Name = "in" }
                        }
                    },
                    new WireDefinition
                    {
                        UId = 2,
                        Connections =
                        {
                            new IdentConDefinition { UId = 10 },
                            new NameConDefinition { UId = 20, Name = "operand" }
                        }
                    },
                    new WireDefinition
                    {
                        UId = 3,
                        Connections =
                        {
                            new NameConDefinition { UId = 20, Name = "out" },
                            new NameConDefinition { UId = 30, Name = "in" },
                            new OpenConDefinition { UId = 40 }
                        }
                    }
                }
            };

            var body = SimaticMlToSactMapper.MapNetworkBody(network);

            Assert.True(body["Powerrail"].IsStartElement);
            Assert.True(body["20"].Negated);
            Assert.Equal("#startSignal", body["20"].TopOperandConnector!.DisplayName);
            Assert.Equal("Start condition", body["20"].Comment);
            Assert.DoesNotContain(body["20"].OutputConnectors, c => c.PinName == "eno");
            Assert.Equal("SCoil", body["30"].DisplayName);
            Assert.Equal("LadTemplatedCoilData", body["30"].Name);
            Assert.Contains(body["20"].OutputConnectors, c => c.PartnerUId != null);
            Assert.Contains(body.Keys, key => key.StartsWith("OpenCon_", System.StringComparison.Ordinal));
        }

        [Fact]
        public void Map_BoxPartIdentConnections_AttachesLiteralConstantsToNamedPins()
        {
            var network = new NetworkSourceDefinition
            {
                Accesses =
                {
                    new AccessDefinition
                    {
                        UId = 30,
                        Scope = "LocalVariable",
                        SymbolComponents = { "deviceIcon" },
                        SymbolPath = "deviceIcon"
                    },
                    new AccessDefinition
                    {
                        UId = 31,
                        Scope = "LiteralConstant",
                        ConstantValue = "10"
                    }
                },
                Parts =
                {
                    new PartDefinition
                    {
                        UId = 47,
                        Name = "Add",
                        TemplateValues =
                        {
                            new TemplateValueDefinition
                            {
                                Name = "Card",
                                Type = "Cardinality",
                                Value = "2"
                            }
                        }
                    }
                },
                Wires =
                {
                    new WireDefinition
                    {
                        UId = 65,
                        Connections =
                        {
                            new IdentConDefinition { UId = 30 },
                            new NameConDefinition { UId = 47, Name = "in1" }
                        }
                    },
                    new WireDefinition
                    {
                        UId = 66,
                        Connections =
                        {
                            new IdentConDefinition { UId = 31 },
                            new NameConDefinition { UId = 47, Name = "in2" }
                        }
                    },
                    new WireDefinition
                    {
                        UId = 68,
                        Connections =
                        {
                            new NameConDefinition { UId = 47, Name = "out" },
                            new IdentConDefinition { UId = 30 }
                        }
                    }
                }
            };

            var add = SimaticMlToSactMapper.MapNetworkBody(network)["47"];

            Assert.Null(add.TopOperandConnector);
            Assert.Contains(add.InputParameters, p => p.Name == "IN1" && p.Operand == "#deviceIcon");
            Assert.Contains(add.InputParameters, p => p.Name == "IN2" && p.Operand == "10");
            Assert.Contains(add.OutputParameters, p => p.Name == "OUT" && p.Operand == "#deviceIcon");
        }

        private static string CreateSimaticMlWithCallParameterOperands()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Document>
  <Engineering version=""V21"" />
  <SW.Blocks.FC ID=""0"">
    <AttributeList>
      <Name>Block</Name>
      <ProgrammingLanguage>LAD</ProgrammingLanguage>
    </AttributeList>
    <ObjectList>
      <SW.Blocks.CompileUnit ID=""1"">
        <AttributeList>
          <ProgrammingLanguage>LAD</ProgrammingLanguage>
          <NetworkSource>
            <FlgNet>
              <Parts>
                <Access Scope=""LiteralConstant"" UId=""21"">
                  <Constant><ConstantValue>false</ConstantValue></Constant>
                </Access>
                <Access Scope=""LocalVariable"" UId=""22"">
                  <Symbol><Component Name=""tempOut"" /></Symbol>
                </Access>
                <Access Scope=""LocalVariable"" UId=""23"">
                  <Symbol><Component Name=""FI_SERVICE"" /></Symbol>
                </Access>
                <Access Scope=""LocalVariable"" UId=""24"">
                  <Symbol><Component Name=""FIQ_Icon"" /></Symbol>
                </Access>
                <Call UId=""27"">
                  <CallInfo Name=""deviceState"" BlockType=""FC"">
                    <Parameter Name=""Alarm"" Section=""Input"" Type=""Bool"" />
                    <Parameter Name=""Running"" Section=""Input"" Type=""Bool"" />
                    <Parameter Name=""Service"" Section=""Input"" Type=""Bool"" />
                    <Parameter Name=""deviceIcon"" Section=""InOut"" Type=""Int"" />
                  </CallInfo>
                </Call>
              </Parts>
              <Wires>
                <Wire UId=""1""><Powerrail /><NameCon UId=""27"" Name=""en"" /></Wire>
                <Wire UId=""2""><IdentCon UId=""21"" /><NameCon UId=""27"" Name=""Alarm"" /></Wire>
                <Wire UId=""3""><IdentCon UId=""22"" /><NameCon UId=""27"" Name=""Running"" /></Wire>
                <Wire UId=""4""><IdentCon UId=""23"" /><NameCon UId=""27"" Name=""Service"" /></Wire>
                <Wire UId=""5""><IdentCon UId=""24"" /><NameCon UId=""27"" Name=""deviceIcon"" /></Wire>
              </Wires>
            </FlgNet>
          </NetworkSource>
        </AttributeList>
      </SW.Blocks.CompileUnit>
    </ObjectList>
  </SW.Blocks.FC>
</Document>";
        }
    }
}
