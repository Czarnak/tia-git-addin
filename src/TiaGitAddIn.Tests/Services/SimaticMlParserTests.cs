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
    }
}
