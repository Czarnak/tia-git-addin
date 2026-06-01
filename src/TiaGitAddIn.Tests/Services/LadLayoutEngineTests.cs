using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;
using TiaGitAddIn.Services;
using Xunit;

namespace TiaGitAddIn.Tests.Services
{
    public class LadLayoutEngineTests
    {
        [Fact]
        public void Layout_SingleRungNetwork_ProducesCorrectLayout()
        {
            // powerrail -> contact -> coil
            var network = new SactNetworkResult
            {
                State = CompareState.Equal,
                Body = new Dictionary<string, SactComponentData>
                {
                    {
                        "pr", new SactComponentData
                        {
                            uId = "pr",
                            name = "BranchWireData",
                            isStartElement = true,
                            outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "pr_out", PartnerUId = "contact_in" } }
                        }
                    },
                    {
                        "contact", new SactComponentData
                        {
                            uId = "contact",
                            name = "LadContactData",
                            inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "contact_in" } },
                            outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "contact_out", PartnerUId = "coil_in" } }
                        }
                    },
                    {
                        "coil", new SactComponentData
                        {
                            uId = "coil",
                            name = "LadCoilData",
                            inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "coil_in" } },
                            outputConnectors = new List<SactConnectorData>()
                        }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(network.Body, network.State);

            Assert.Equal(3, layout.Elements.Count);
            Assert.Equal(2, layout.Wires.Count);
            Assert.Equal(3, layout.ColumnCount);
            Assert.Equal(1, layout.RowCount);

            var prElement = layout.Elements.First(e => e.UId == "pr");
            Assert.Equal(0, prElement.Column);
            Assert.Equal(LadElementType.Powerrail, prElement.ElementType);

            var contactElement = layout.Elements.First(e => e.UId == "contact");
            Assert.Equal(1, contactElement.Column);
            Assert.Equal(LadElementType.Contact, contactElement.ElementType);

            var coilElement = layout.Elements.First(e => e.UId == "coil");
            Assert.Equal(2, coilElement.Column);
            Assert.Equal(LadElementType.Coil, coilElement.ElementType);
        }

        [Fact]
        public void Layout_ParallelBranch_ProducesMultipleRows()
        {
            var network = new SactNetworkResult
            {
                State = CompareState.Equal,
                Body = new Dictionary<string, SactComponentData>
                {
                    {
                        "pr", new SactComponentData
                        {
                            uId = "pr",
                            name = "BranchWireData",
                            isStartElement = true,
                            outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "pr_out", PartnerUId = "or_in" } }
                        }
                    },
                    {
                        "or", new SactComponentData
                        {
                            uId = "or",
                            name = "LadOrWireData",
                            inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "or_in" } },
                            outputConnectors = new List<SactConnectorData> 
                            { 
                                new SactConnectorData { uId = "or_out1", PartnerUId = "c1_in" },
                                new SactConnectorData { uId = "or_out2", PartnerUId = "c2_in" }
                            }
                        }
                    },
                    {
                        "c1", new SactComponentData
                        {
                            uId = "c1",
                            name = "LadContactData",
                            inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "c1_in" } }
                        }
                    },
                    {
                        "c2", new SactComponentData
                        {
                            uId = "c2",
                            name = "LadContactData",
                            inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "c2_in" } }
                        }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(network.Body, network.State);

            Assert.Equal(3, layout.Elements.Count);
            Assert.Equal(3, layout.ColumnCount); // pr (0), or (1), contacts (2)
            Assert.Equal(2, layout.RowCount); // two branches
            Assert.DoesNotContain(layout.Elements, e => e.ElementType == LadElementType.OrBranch);
            Assert.All(layout.Wires, w => Assert.True(w.FromColumn == w.ToColumn || w.FromRow == w.ToRow));
            
            var c1Element = layout.Elements.First(e => e.UId == "c1");
            var c2Element = layout.Elements.First(e => e.UId == "c2");

            Assert.Equal(2, c1Element.Column);
            Assert.Equal(2, c2Element.Column);
            Assert.NotEqual(c1Element.Row, c2Element.Row);
        }

        [Fact]
        public void Layout_MergeOfUnequalBranches_KeepsDownstreamOnMainLineAndAligned()
        {
            // Mirrors the SimpleDevice "OUTPUT" network:
            //   |--[FI_START]--[/FI_SERVICE]--+--(tempOut)--(FQ_OUTPUT)
            //   |--[FIQ_MANUAL]---------------+
            // The two branches are of unequal length (2 hops vs 1 hop) and merge into the
            // output coils. The coils must stay on the main line and align after the merge,
            // not drift onto the shorter branch's row/column.
            var body = new Dictionary<string, SactComponentData>
            {
                {
                    "pr", new SactComponentData
                    {
                        uId = "pr",
                        name = "BranchWireData",
                        isStartElement = true,
                        outputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "pr_out1", PartnerUId = "fistart_in" },
                            new SactConnectorData { uId = "pr_out2", PartnerUId = "fiqman_in" }
                        }
                    }
                },
                {
                    "fistart", new SactComponentData
                    {
                        uId = "fistart",
                        name = "LadContactData",
                        DisplayName = "FI_START",
                        inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "fistart_in" } },
                        outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "fistart_out", PartnerUId = "fiservice_in" } }
                    }
                },
                {
                    "fiservice", new SactComponentData
                    {
                        uId = "fiservice",
                        name = "LadContactData",
                        DisplayName = "FI_SERVICE",
                        negated = true,
                        inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "fiservice_in" } },
                        outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "fiservice_out", PartnerUId = "coil1_in_a" } }
                    }
                },
                {
                    "fiqman", new SactComponentData
                    {
                        uId = "fiqman",
                        name = "LadContactData",
                        DisplayName = "FIQ_MANUAL",
                        inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "fiqman_in" } },
                        outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "fiqman_out", PartnerUId = "coil1_in_b" } }
                    }
                },
                {
                    "coil1", new SactComponentData
                    {
                        uId = "coil1",
                        name = "LadCoilData",
                        DisplayName = "tempOut",
                        inputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "coil1_in_a" },
                            new SactConnectorData { uId = "coil1_in_b" }
                        },
                        outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "coil1_out", PartnerUId = "coil2_in" } }
                    }
                },
                {
                    "coil2", new SactComponentData
                    {
                        uId = "coil2",
                        name = "LadCoilData",
                        DisplayName = "FQ_OUTPUT",
                        inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "coil2_in" } }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(body, CompareState.Equal);

            var fiStart = layout.Elements.First(e => e.UId == "fistart");
            var fiService = layout.Elements.First(e => e.UId == "fiservice");
            var fiqMan = layout.Elements.First(e => e.UId == "fiqman");
            var coil1 = layout.Elements.First(e => e.UId == "coil1");
            var coil2 = layout.Elements.First(e => e.UId == "coil2");

            // Coils stay on the main line (same row as the longer branch), not the FIQ branch.
            Assert.Equal(fiStart.Row, coil1.Row);
            Assert.Equal(fiStart.Row, coil2.Row);
            Assert.NotEqual(fiStart.Row, fiqMan.Row);

            // Columns follow the longest path, so the merge sits past the longer branch and
            // the coils are aligned on a global grid (not pulled left by the shorter branch).
            Assert.Equal(1, fiStart.Column);
            Assert.Equal(2, fiService.Column);
            Assert.Equal(1, fiqMan.Column);
            Assert.Equal(3, coil1.Column);
            Assert.Equal(4, coil2.Column);

            // The terminal coil is the right-most element in the network.
            Assert.Equal(coil2.Column, layout.ColumnCount - 1);
        }

        [Fact]
        public void LayoutAll_NetworkNumbers_AreSequentialFromOne()
        {
            var result = new SactCompareResult
            {
                Content = new SactContentResult
                {
                    Networks = new Dictionary<string, SactNetworkResult>
                    {
                        { "3", new SactNetworkResult { State = CompareState.Equal, Number = new SactNumberPair { Left = 10, Right = 10 } } },
                        { "8", new SactNetworkResult { State = CompareState.Equal, Number = new SactNumberPair { Left = 20, Right = 20 } } }
                    }
                }
            };

            var layouts = LadLayoutEngine.LayoutAll(result);

            Assert.Equal(new[] { 1, 2 }, layouts.Select(l => l.NetworkNumber).ToArray());
        }

        [Fact]
        public void Layout_ChangedNetworkWithEqualElements_DoesNotPaintEveryElement()
        {
            var body = new Dictionary<string, SactComponentData>
            {
                {
                    "pr", new SactComponentData
                    {
                        uId = "pr",
                        name = "BranchWireData",
                        isStartElement = true,
                        outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "out", PartnerUId = "contact_in" } }
                    }
                },
                {
                    "contact", new SactComponentData
                    {
                        uId = "contact",
                        name = "LadContactData",
                        State = CompareState.Equal,
                        inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "contact_in" } }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(body, CompareState.Changed);

            Assert.All(layout.Elements, e => Assert.Equal(CompareState.Equal, e.DiffState));
        }

        [Fact]
        public void Layout_NegatedContact_MapsToNegatedContactType()
        {
            var network = new SactNetworkResult
            {
                Body = new Dictionary<string, SactComponentData>
                {
                    {
                        "pr", new SactComponentData
                        {
                            uId = "pr",
                            name = "BranchWireData",
                            isStartElement = true,
                            outputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "out", PartnerUId = "c_in" } }
                        }
                    },
                    {
                        "c", new SactComponentData
                        {
                            uId = "c",
                            name = "LadContactData",
                            negated = true,
                            inputConnectors = new List<SactConnectorData> { new SactConnectorData { uId = "c_in" } }
                        }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(network.Body, network.State);
            var element = layout.Elements.First(e => e.UId == "c");
            Assert.Equal(LadElementType.NegatedContact, element.ElementType);
        }

        [Fact]
        public void Layout_CallWithParameters_ExposesPinsAndGrowsBoxHeight()
        {
            var body = new Dictionary<string, SactComponentData>
            {
                {
                    "pr", new SactComponentData
                    {
                        uId = "pr",
                        name = "BranchWireData",
                        isStartElement = true,
                        outputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "out", PartnerUId = "call_in" }
                        }
                    }
                },
                {
                    "call", new SactComponentData
                    {
                        uId = "call",
                        name = "LadBoxData",
                        DisplayName = "SCALE_R",
                        inputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "call_in" }
                        },
                        inputParameters = new List<SactParameterData>
                        {
                            new SactParameterData { Name = "X_REAL", Section = "Input", Type = "Real" },
                            new SactParameterData { Name = "I_LO", Section = "Input", Type = "Real" },
                            new SactParameterData { Name = "I_HI", Section = "Input", Type = "Real" }
                        },
                        outputParameters = new List<SactParameterData>
                        {
                            new SactParameterData { Name = "Ret_Val", Section = "Return", Type = "Real" }
                        }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(body, CompareState.Equal);
            var call = layout.Elements.First(e => e.UId == "call");

            Assert.Equal(new[] { "X_REAL", "I_LO", "I_HI" }, call.InputPins);
            Assert.Equal(new[] { "Ret_Val" }, call.OutputPins);
            Assert.True(call.Height > 90);
        }

        [Fact]
        public void Layout_CallWithManyPins_LeavesRoomForReadablePinRows()
        {
            var body = new Dictionary<string, SactComponentData>
            {
                {
                    "pr", new SactComponentData
                    {
                        uId = "pr",
                        name = "BranchWireData",
                        isStartElement = true,
                        outputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "out", PartnerUId = "call_in" }
                        }
                    }
                },
                {
                    "call", new SactComponentData
                    {
                        uId = "call",
                        name = "LadBoxData",
                        DisplayName = "deviceState",
                        inputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "call_in", PinName = "en" }
                        },
                        inputParameters = new List<SactParameterData>
                        {
                            new SactParameterData { Name = "Alarm", Section = "Input", Type = "Bool", Operand = "false" },
                            new SactParameterData { Name = "Warning", Section = "Input", Type = "Bool", Operand = "false" },
                            new SactParameterData { Name = "Running", Section = "Input", Type = "Bool", Operand = "#tempOut" },
                            new SactParameterData { Name = "Reverse", Section = "Input", Type = "Bool", Operand = "false" },
                            new SactParameterData { Name = "Service", Section = "Input", Type = "Bool", Operand = "#FI_SERVICE" },
                            new SactParameterData { Name = "deviceIcon", Section = "InOut", Type = "Int", Operand = "#FIQ_Icon" }
                        },
                        outputParameters = new List<SactParameterData>
                        {
                            new SactParameterData { Name = "deviceIcon", Section = "InOut", Type = "Int", Operand = "#FIQ_Icon" }
                        }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(body, CompareState.Equal);
            var call = layout.Elements.First(e => e.UId == "call");

            Assert.Equal(7, call.InputPins.Count);
            Assert.Equal("EN", call.InputPins[0]);
            Assert.Contains("deviceIcon", call.InputPins);
            Assert.Contains(call.InputPinRows, p => p.Name == "Running" && p.Operand == "#tempOut");
            Assert.Contains(call.InputPinRows, p => p.Name == "Service" && p.Operand == "#FI_SERVICE");
            Assert.Contains("deviceIcon", call.OutputPins);
            Assert.Equal("ENO", call.OutputPins[0]);
            Assert.Contains(call.OutputPinRows, p => p.Name == "deviceIcon" && p.Operand == "#FIQ_Icon");
            Assert.True(call.Width >= 230);
            Assert.True(call.Height >= 180);
        }

        [Fact]
        public void Layout_BoxWithNamedConnectors_UsesInputAndOutputPinNames()
        {
            var body = new Dictionary<string, SactComponentData>
            {
                {
                    "pr", new SactComponentData
                    {
                        uId = "pr",
                        name = "BranchWireData",
                        isStartElement = true,
                        outputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "out", PartnerUId = "box_in1" }
                        }
                    }
                },
                {
                    "box", new SactComponentData
                    {
                        uId = "box",
                        name = "LadBoxData",
                        DisplayName = "Add",
                        inputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "box_in1", PinName = "in1" },
                            new SactConnectorData { uId = "box_in2", PinName = "in2" }
                        },
                        outputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "box_out", PinName = "out" }
                        }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(body, CompareState.Equal);
            var box = layout.Elements.First(e => e.UId == "box");

            Assert.Equal(new[] { "IN1", "IN2" }, box.InputPins);
            Assert.Equal(new[] { "OUT" }, box.OutputPins);
        }

        [Fact]
        public void Layout_ElementMetadata_ExposesCommentAndEquation()
        {
            var body = new Dictionary<string, SactComponentData>
            {
                {
                    "pr", new SactComponentData
                    {
                        uId = "pr",
                        name = "BranchWireData",
                        isStartElement = true,
                        outputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "out", PartnerUId = "box_in" }
                        }
                    }
                },
                {
                    "box", new SactComponentData
                    {
                        uId = "box",
                        name = "LadBoxData",
                        DisplayName = "Calculate",
                        Equation = "#out := #in + 1",
                        Comment = "Normalize value",
                        inputConnectors = new List<SactConnectorData>
                        {
                            new SactConnectorData { uId = "box_in", PinName = "in" }
                        }
                    }
                }
            };

            var layout = LadLayoutEngine.LayoutBody(body, CompareState.Equal);
            var box = layout.Elements.First(e => e.UId == "box");

            Assert.Equal("#out := #in + 1", box.Equation);
            Assert.Equal("Normalize value", box.Comment);
        }

        [Fact]
        public void Layout_NullOrEmptyBody_ReturnsEmptyLayout()
        {
            var layout1 = LadLayoutEngine.LayoutBody(null!, CompareState.Equal);
            Assert.Empty(layout1.Elements);

            var layout2 = LadLayoutEngine.LayoutBody(new Dictionary<string, SactComponentData>(), CompareState.Equal);
            Assert.Empty(layout2.Elements);
        }

        [Fact]
        public void LayoutAll_SkipsNullContent()
        {
            var result = new SactCompareResult { Content = null };
            var layouts = LadLayoutEngine.LayoutAll(result);
            Assert.Empty(layouts);
        }
    }
}
