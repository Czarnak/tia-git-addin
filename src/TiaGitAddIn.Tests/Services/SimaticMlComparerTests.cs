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

        [Fact]
        public void Compare_InterfaceMembersChangedAddedAndRemoved_ProducesTableRows()
        {
            var left = CreateFileWithInterface(
                new InterfaceSection
                {
                    Name = "Input",
                    Members = new List<InterfaceMember>
                    {
                        new InterfaceMember { Name = "Alarm", Datatype = "Bool" },
                        new InterfaceMember { Name = "Mode", Datatype = "Int" }
                    }
                });
            var right = CreateFileWithInterface(
                new InterfaceSection
                {
                    Name = "Input",
                    Members = new List<InterfaceMember>
                    {
                        new InterfaceMember { Name = "Alarm", Datatype = "Word" },
                        new InterfaceMember { Name = "Reset", Datatype = "Bool" }
                    }
                });

            var result = SimaticMlComparer.Compare(left, right);

            Assert.Equal(CompareState.Changed, result.Interface!.State);
            Assert.Contains(result.Interface.Members, r =>
                r.Section == "Input" &&
                r.Name == "Alarm" &&
                r.LeftDatatype == "Bool" &&
                r.RightDatatype == "Word" &&
                r.State == CompareState.Changed);
            Assert.Contains(result.Interface.Members, r =>
                r.Section == "Input" &&
                r.Name == "Mode" &&
                r.LeftDatatype == "Int" &&
                r.RightDatatype == string.Empty &&
                r.State == CompareState.MissingOnRight);
            Assert.Contains(result.Interface.Members, r =>
                r.Section == "Input" &&
                r.Name == "Reset" &&
                r.LeftDatatype == string.Empty &&
                r.RightDatatype == "Bool" &&
                r.State == CompareState.MissingOnLeft);
        }

        [Fact]
        public void Compare_InterfaceMembers_KeepRightDeclarationOrderWithinTiaSections()
        {
            var left = CreateFileWithInterface(
                new InterfaceSection
                {
                    Name = "Input",
                    Members = new List<InterfaceMember>
                    {
                        new InterfaceMember { Name = "Alarm", Datatype = "Bool" },
                        new InterfaceMember { Name = "Warning", Datatype = "Bool" },
                        new InterfaceMember { Name = "Running", Datatype = "Bool" }
                    }
                });
            var right = CreateFileWithInterface(
                new InterfaceSection
                {
                    Name = "Input",
                    Members = new List<InterfaceMember>
                    {
                        new InterfaceMember { Name = "Alarm", Datatype = "Bool" },
                        new InterfaceMember { Name = "Warning", Datatype = "Bool" },
                        new InterfaceMember { Name = "Running", Datatype = "Bool" }
                    }
                },
                new InterfaceSection
                {
                    Name = "InOut",
                    Members = new List<InterfaceMember>
                    {
                        new InterfaceMember { Name = "deviceIcon", Datatype = "Int" }
                    }
                });

            var result = SimaticMlComparer.Compare(left, right);

            Assert.Equal(
                new[] { "Alarm", "Warning", "Running", "deviceIcon" },
                result.Interface!.Members.ConvertAll(r => r.Name).ToArray());
        }

        [Fact]
        public void Compare_InterfaceMembersWithWhitespaceOnlyStartValueDifference_ConsideredUnchanged()
        {
            // Precision improvement from delegating to InterfaceSnapshotBuilder/InterfaceComparer (Task 8):
            // the old comparer's raw ordinal StartValue comparison treated CRLF/whitespace-only differences
            // as a real change. The new normalization matrix (CRLF -> LF, then outer Trim()) recognizes these
            // as the same value, so interface metadata noise from re-exports no longer reports as Changed.
            var left = CreateFileWithInterface(
                new InterfaceSection
                {
                    Name = "Input",
                    Members = new List<InterfaceMember>
                    {
                        new InterfaceMember { Name = "Preset", Datatype = "Int", StartValue = "  10\r\n" }
                    }
                });
            var right = CreateFileWithInterface(
                new InterfaceSection
                {
                    Name = "Input",
                    Members = new List<InterfaceMember>
                    {
                        new InterfaceMember { Name = "Preset", Datatype = "Int", StartValue = "10" }
                    }
                });

            var result = SimaticMlComparer.Compare(left, right);

            Assert.Equal(CompareState.Equal, result.Interface!.State);
            Assert.Contains(result.Interface.Members, r =>
                r.Section == "Input" && r.Name == "Preset" && r.State == CompareState.Equal);
        }

        private static SimaticMlFile CreateFile(params PartDefinition[] parts)
        {
            return CreateFile(parts, new WireDefinition[0]);
        }

        private static SimaticMlFile CreateFileWithInterface(params InterfaceSection[] sections)
        {
            return new SimaticMlFile
            {
                Blocks = new List<BlockDefinition>
                {
                    new BlockDefinition
                    {
                        Name = "Block",
                        InterfaceSections = new List<InterfaceSection>(sections)
                    }
                }
            };
        }

        private static SimaticMlFile CreateFile(PartDefinition[] parts, params WireDefinition[] wires)
        {
            return new SimaticMlFile
            {
                Blocks = new List<BlockDefinition>
                {
                    new BlockDefinition
                    {
                        Name = "Block",
                        CompileUnits = new List<CompileUnitDefinition>
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
                Connections = new List<ConnectionDefinition>
                {
                    new NameConDefinition { UId = sourceUid, Name = sourceName },
                    new NameConDefinition { UId = targetUid, Name = targetName }
                }
            };
        }
    }
}
