using System.Collections.Generic;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services.SimaticMl;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public class SimaticMlComparerTests
    {
        [Fact]
        public void Compare_AddedPartInChangedNetwork_MarksOnlyAddedComponent()
        {
            var left = CreateFile(
                new PartDefinition { UId = 1, Name = "Contact", RawXml = "<Part UId=\"1\" Name=\"Contact\" />" });
            var right = CreateFile(
                new PartDefinition { UId = 1, Name = "Contact", RawXml = "<Part UId=\"1\" Name=\"Contact\" />" },
                new PartDefinition { UId = 2, Name = "Coil", RawXml = "<Part UId=\"2\" Name=\"Coil\" />" });

            var result = SimaticMlComparer.Compare(left, right);
            var network = result.Content!.Networks["0"];

            Assert.Equal(CompareState.Changed, network.State);
            Assert.Equal(CompareState.Equal, network.LeftBody["1"].State);
            Assert.Equal(CompareState.Equal, network.RightBody["1"].State);
            Assert.Equal(CompareState.MissingOnLeft, network.RightBody["2"].State);
        }

        [Fact]
        public void Compare_ModifiedPartInChangedNetwork_MarksOnlyModifiedComponent()
        {
            var left = CreateFile(
                new PartDefinition { UId = 1, Name = "Contact", RawXml = "<Part UId=\"1\" Name=\"Contact\" />" },
                new PartDefinition { UId = 2, Name = "Coil", RawXml = "<Part UId=\"2\" Name=\"Coil\" />" });
            var right = CreateFile(
                new PartDefinition { UId = 1, Name = "Contact", RawXml = "<Part UId=\"1\" Name=\"Contact\" />" },
                new PartDefinition { UId = 2, Name = "Coil", RawXml = "<Part UId=\"2\" Name=\"Coil\" Negated=\"true\" />" });

            var result = SimaticMlComparer.Compare(left, right);
            var network = result.Content!.Networks["0"];

            Assert.Equal(CompareState.Equal, network.LeftBody["1"].State);
            Assert.Equal(CompareState.Equal, network.RightBody["1"].State);
            Assert.Equal(CompareState.Changed, network.LeftBody["2"].State);
            Assert.Equal(CompareState.Changed, network.RightBody["2"].State);
        }

        [Fact]
        public void Compare_RewiredPartInChangedNetwork_MarksConnectedComponentsChanged()
        {
            var parts = new[]
            {
                new PartDefinition { UId = 1, Name = "Contact", RawXml = "<Part UId=\"1\" Name=\"Contact\" />" },
                new PartDefinition { UId = 2, Name = "Coil", RawXml = "<Part UId=\"2\" Name=\"Coil\" />" },
                new PartDefinition { UId = 3, Name = "Box", RawXml = "<Part UId=\"3\" Name=\"Box\" />" }
            };
            var left = CreateFile(
                parts,
                CreateWire(10, 1, "out", 2, "in"));
            var right = CreateFile(
                parts,
                CreateWire(10, 1, "out", 3, "in"));

            var result = SimaticMlComparer.Compare(left, right);
            var network = result.Content!.Networks["0"];

            Assert.Equal(CompareState.Changed, network.LeftBody["1"].State);
            Assert.Equal(CompareState.Changed, network.RightBody["1"].State);
            Assert.Equal(CompareState.Changed, network.LeftBody["2"].State);
            Assert.Equal(CompareState.Changed, network.RightBody["2"].State);
            Assert.Equal(CompareState.Changed, network.LeftBody["3"].State);
            Assert.Equal(CompareState.Changed, network.RightBody["3"].State);
        }

        private static SimaticMlFile CreateFile(params PartDefinition[] parts)
        {
            return CreateFile(parts, new WireDefinition[0]);
        }

        private static SimaticMlFile CreateFile(PartDefinition[] parts, params WireDefinition[] wires)
        {
            return new SimaticMlFile
            {
                Blocks =
                {
                    new BlockDefinition
                    {
                        Name = "Block",
                        CompileUnits =
                        {
                            new CompileUnitDefinition
                            {
                                RawAttributes = new Dictionary<string, string?> { { "NetworkNumber", "1" } },
                                Network = new NetworkSourceDefinition
                                {
                                    RawXml = string.Join("", System.Array.ConvertAll(parts, p => p.RawXml)) +
                                             string.Join("", System.Array.ConvertAll(wires, w => w.RawXml)),
                                    Parts = new List<PartDefinition>(parts),
                                    Wires = new List<WireDefinition>(wires)
                                }
                            }
                        }
                    }
                }
            };
        }

        private static WireDefinition CreateWire(
            int uid,
            int sourceUid,
            string sourceName,
            int targetUid,
            string targetName)
        {
            return new WireDefinition
            {
                UId = uid,
                RawXml = $"<Wire UId=\"{uid}\"><NameCon UId=\"{sourceUid}\" Name=\"{sourceName}\" /><NameCon UId=\"{targetUid}\" Name=\"{targetName}\" /></Wire>",
                Connections =
                {
                    new NameConDefinition { UId = sourceUid, Name = sourceName },
                    new NameConDefinition { UId = targetUid, Name = targetName }
                }
            };
        }
    }
}
