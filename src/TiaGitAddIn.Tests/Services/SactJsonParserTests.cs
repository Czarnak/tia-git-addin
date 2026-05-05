using System.Collections.Generic;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public class SactJsonParserTests
    {
        [Fact]
        public void ParseCompareResult_NullOrEmptyInput_ReturnsNull()
        {
            Assert.Null(SactJsonParser.ParseCompareResult(null!));
            Assert.Null(SactJsonParser.ParseCompareResult(""));
            Assert.Null(SactJsonParser.ParseCompareResult("   "));
        }

        [Fact]
        public void ParseCompareResult_ValidMinimalJson_ParsesCorrectly()
        {
            var json = @"{
                ""Left"": ""Block1"",
                ""Right"": ""Block2"",
                ""State"": ""Equal""
            }";

            var result = SactJsonParser.ParseCompareResult(json);

            Assert.NotNull(result);
            Assert.Equal("Block1", result.Left);
            Assert.Equal("Block2", result.Right);
            Assert.Equal(CompareState.Equal, result.State);
            Assert.Null(result.Content);
            Assert.Null(result.Interface);
        }

        [Fact]
        public void ParseCompareResult_WithChangedNetwork_ParsesNetworkStateAndBody()
        {
            var json = @"{
                ""State"": ""Changed"",
                ""Content"": {
                    ""State"": ""Changed"",
                    ""Networks"": {
                        ""0"": {
                            ""State"": ""Changed"",
                            ""Number"": { ""Left"": 1, ""Right"": 1 },
                            ""Body"": {
                                ""uid1"": {
                                    ""name"": ""LadContactData"",
                                    ""uId"": ""uid1"",
                                    ""isStartElement"": false,
                                    ""negated"": true,
                                    ""DisplayName"": ""Contact1""
                                }
                            }
                        }
                    }
                }
            }";

            var result = SactJsonParser.ParseCompareResult(json);

            Assert.NotNull(result);
            Assert.Equal(CompareState.Changed, result.State);
            Assert.NotNull(result.Content);
            Assert.Equal(CompareState.Changed, result.Content.State);
            
            Assert.Single(result.Content.Networks);
            Assert.True(result.Content.Networks.ContainsKey("0"));
            
            var network = result.Content.Networks["0"];
            Assert.Equal(CompareState.Changed, network.State);
            Assert.Equal(1, network.Number.Left);
            Assert.Equal(1, network.Number.Right);
            
            Assert.Single(network.Body);
            Assert.True(network.Body.ContainsKey("uid1"));
            
            var component = network.Body["uid1"];
            Assert.Equal("LadContactData", component.name);
            Assert.Equal("uid1", component.uId);
            Assert.False(component.isStartElement.GetValueOrDefault());
            Assert.True(component.negated.GetValueOrDefault());
            Assert.Equal("Contact1", component.DisplayName);
        }

        [Fact]
        public void ParseCompareResult_OmittedContentSection_HandledGracefully()
        {
            var json = @"{
                ""State"": ""Equal"",
                ""Interface"": {
                    ""State"": ""Equal"",
                    ""Sections"": {}
                }
            }";

            var result = SactJsonParser.ParseCompareResult(json);

            Assert.NotNull(result);
            Assert.Equal(CompareState.Equal, result.State);
            Assert.Null(result.Content);
            Assert.NotNull(result.Interface);
            Assert.Equal(CompareState.Equal, result.Interface.State);
        }

        [Fact]
        public void ParseCompareResult_ComponentWithOutputConnectors_ParsesPartnerUId()
        {
            var json = @"{
                ""Content"": {
                    ""Networks"": {
                        ""0"": {
                            ""Body"": {
                                ""uid1"": {
                                    ""name"": ""BranchWireData"",
                                    ""outputConnectors"": [
                                        { ""uId"": ""conn1"", ""PartnerUId"": ""uid2"" }
                                    ]
                                }
                            }
                        }
                    }
                }
            }";

            var result = SactJsonParser.ParseCompareResult(json);

            Assert.NotNull(result);
            var component = result.Content!.Networks["0"].Body["uid1"];
            
            Assert.Single(component.outputConnectors);
            Assert.Equal("conn1", component.outputConnectors[0].uId);
            Assert.Equal("uid2", component.outputConnectors[0].PartnerUId);
            Assert.Empty(component.inputConnectors);
        }

        [Fact]
        public void ParseCompareResult_WithDifferentState_ParsesSuccessfully()
        {
            var json = @"{""Left"":""deviceState"",""Right"":""deviceState"",""State"":""Different"",""Interface"":{""State"":""Equal"",""Sections"":{}},""Content"":{""State"":""Different"",""Networks"":{""0"":{""State"":""Different"",""Number"":{""Left"":1,""Right"":1},""Properties"":{},""Body"":{""State"":""Different""}}}},""Attributes"":{""State"":""Equal"",""Properties"":{}}}";
            
            var result = SactJsonParser.ParseCompareResult(json);
            Assert.NotNull(result);
            
            var resultWithNewline = SactJsonParser.ParseCompareResult("\r\n\r\n" + json);
            Assert.NotNull(resultWithNewline);

            var resultWithBom = SactJsonParser.ParseCompareResult("\xEF\xBB\xBF" + json);
            Assert.NotNull(resultWithBom);
        }
    }
}