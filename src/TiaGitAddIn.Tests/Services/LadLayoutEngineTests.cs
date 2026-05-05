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

            var layout = LadLayoutEngine.Layout(network);

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

            var layout = LadLayoutEngine.Layout(network);

            Assert.Equal(4, layout.Elements.Count);
            Assert.Equal(3, layout.ColumnCount); // pr (0), or (1), contacts (2)
            Assert.Equal(2, layout.RowCount); // two branches
            
            var c1Element = layout.Elements.First(e => e.UId == "c1");
            var c2Element = layout.Elements.First(e => e.UId == "c2");

            Assert.Equal(2, c1Element.Column);
            Assert.Equal(2, c2Element.Column);
            Assert.NotEqual(c1Element.Row, c2Element.Row);
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

            var layout = LadLayoutEngine.Layout(network);
            var element = layout.Elements.First(e => e.UId == "c");
            Assert.Equal(LadElementType.NegatedContact, element.ElementType);
        }

        [Fact]
        public void Layout_NullOrEmptyBody_ReturnsEmptyLayout()
        {
            var layout1 = LadLayoutEngine.Layout(new SactNetworkResult { Body = null! });
            Assert.Empty(layout1.Elements);

            var layout2 = LadLayoutEngine.Layout(new SactNetworkResult { Body = new Dictionary<string, SactComponentData>() });
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