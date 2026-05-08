using System;
using System.IO;
using System.Linq;
using TiaGitAddIn.Services.SimaticMl;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public sealed class SimaticMlParserTests
    {
        [Fact]
        public void Parse_CallInfoParameters_CapturesVisibleCallPins()
        {
            string path = Path.Combine(Path.GetTempPath(), $"tia-git-addin-{Guid.NewGuid():N}.xml");
            File.WriteAllText(path, CreateSimaticMlWithCallParameters());

            try
            {
                var file = SimaticMlParser.Parse(path);
                var callInfo = file.Blocks.Single().CompileUnits.Single().Network!.Calls.Single().CallInfo!;

                Assert.Equal("SCALE_R", callInfo.Name);
                Assert.Contains(callInfo.Parameters, p =>
                    p.Name == "X_REAL" && p.Section == "Input" && p.Type == "Real");
                Assert.Contains(callInfo.Parameters, p =>
                    p.Name == "Ret_Val" && p.Section == "Return" && p.Type == "Real");
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
        public void Parse_FlgNetMetadata_PreservesLadVisualizationSemantics()
        {
            string path = Path.Combine(Path.GetTempPath(), $"tia-git-addin-{Guid.NewGuid():N}.xml");
            File.WriteAllText(path, CreateRichLadSimaticMl());

            try
            {
                var file = SimaticMlParser.Parse(path);
                var network = file.Blocks.Single().CompileUnits.Single().Network!;

                var access = network.Accesses.Single(a => a.UId == 11);
                Assert.Equal("LocalVariable", access.Scope);
                Assert.Equal(new[] { "motor", "status" }, access.SymbolComponents);
                Assert.Contains(access.Components, c =>
                    c.Name == "status" &&
                    c.SliceAccessModifier == "X0" &&
                    c.AccessModifier == "Array");

                var part = network.Parts.Single(p => p.UId == 21);
                Assert.Equal("Contact", part.Name);
                Assert.Contains(part.Negated, n => n == "in");
                Assert.Contains(part.Invisible, n => n == "eno");
                Assert.Equal("a + b", part.Equation);
                Assert.Contains(part.CommentText ?? string.Empty, "Start condition");
                Assert.Equal("SystemTimer", part.Instance?.Name);

                var call = network.Calls.Single(c => c.UId == 31);
                Assert.Equal("MotorFb", call.CallInfo!.Name);
                Assert.Equal("FB", call.CallInfo.BlockType);
                Assert.Equal("MotorDb", call.CallInfo.Instance);
                Assert.Contains(call.CallInfo.Parameters, p =>
                    p.Name == "Run" &&
                    p.Section == "Input" &&
                    p.TemplateReference == "T" &&
                    p.Informative == true);
                Assert.Contains(call.CommentText ?? string.Empty, "User block call");

                Assert.Contains(network.Wires, w =>
                    w.Connections.Any(c => c is PowerrailConDefinition) &&
                    w.Connections.OfType<NameConDefinition>().Any(c => c.UId == 21 && c.Name == "in"));
                Assert.Contains(network.Wires, w => w.Connections.Any(c => c is OpenConDefinition));
                Assert.Contains(network.Wires, w => w.Connections.Any(c => c is OpenbranchConDefinition));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static string CreateSimaticMlWithCallParameters()
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
                <Call UId=""30"">
                  <CallInfo Name=""SCALE_R"" BlockType=""FC"">
                    <Parameter Name=""X_REAL"" Section=""Input"" Type=""Real"" />
                    <Parameter Name=""Ret_Val"" Section=""Return"" Type=""Real"" />
                  </CallInfo>
                </Call>
              </Parts>
              <Wires />
            </FlgNet>
          </NetworkSource>
        </AttributeList>
      </SW.Blocks.CompileUnit>
    </ObjectList>
  </SW.Blocks.FC>
</Document>";
        }

        private static string CreateRichLadSimaticMl()
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
                <Access Scope=""LocalVariable"" UId=""11"">
                  <Symbol>
                    <Component Name=""motor"" />
                    <Component Name=""status"" SliceAccessModifier=""X0"" AccessModifier=""Array"" />
                  </Symbol>
                </Access>
                <Part UId=""21"" Name=""Contact"" Version=""1.0"" DisabledENO=""true"">
                  <TemplateValue Name=""Card"" Type=""Cardinality"">2</TemplateValue>
                  <AutomaticTyped Name=""in"" />
                  <Negated Name=""in"" />
                  <Invisible Name=""eno"" />
                  <Equation>a + b</Equation>
                  <Instance Name=""SystemTimer"" Scope=""GlobalVariable"" />
                  <Comment>
                    <MultiLanguageText Lang=""en-US"">Start condition</MultiLanguageText>
                  </Comment>
                </Part>
                <Call UId=""31"">
                  <CallInfo Name=""MotorFb"" BlockType=""FB"" Instance=""MotorDb"">
                    <Parameter Name=""Run"" Section=""Input"" Type=""Bool"" TemplateReference=""T"" Informative=""true"" />
                    <Parameter Name=""Done"" Section=""Output"" Type=""Bool"" />
                  </CallInfo>
                  <Comment>
                    <MultiLanguageText Lang=""en-US"">User block call</MultiLanguageText>
                  </Comment>
                </Call>
              </Parts>
              <Wires>
                <Wire UId=""100""><Powerrail /><NameCon UId=""21"" Name=""in"" /></Wire>
                <Wire UId=""101""><IdentCon UId=""11"" /><NameCon UId=""21"" Name=""operand"" /></Wire>
                <Wire UId=""102""><NameCon UId=""21"" Name=""out"" /><OpenCon UId=""91"" /></Wire>
                <Wire UId=""103""><NameCon UId=""21"" Name=""out"" /><Openbranch /></Wire>
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
