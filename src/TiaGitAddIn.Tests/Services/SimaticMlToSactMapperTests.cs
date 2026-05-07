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
                Assert.Contains(call.inputParameters, p =>
                    p.Name == "Alarm" && p.Operand == "false");
                Assert.Contains(call.inputParameters, p =>
                    p.Name == "Running" && p.Operand == "#tempOut");
                Assert.Contains(call.inputParameters, p =>
                    p.Name == "Service" && p.Operand == "#FI_SERVICE");
                Assert.Contains(call.inputParameters, p =>
                    p.Name == "deviceIcon" && p.Operand == "#FIQ_Icon");
                Assert.Contains(call.outputParameters, p =>
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
