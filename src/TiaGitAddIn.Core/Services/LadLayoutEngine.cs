using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services
{
    public static class LadLayoutEngine
    {
        public static List<LadNetworkPairLayout> LayoutAll(SactCompareResult compareResult)
        {
            List<LadNetworkPairLayout> pairs = new();

            if (compareResult.Content == null || compareResult.Content.Networks == null)
            {
                return pairs;
            }

            List<KeyValuePair<string, SactNetworkResult>> sortedNetworks = compareResult.Content.Networks
                .OrderBy(k => int.TryParse(k.Key, out int i) ? i : int.MaxValue)
                .ToList();

            int displayNumber = 1;
            foreach (var kvp in sortedNetworks)
            {
                var network = kvp.Value;
                if (network != null)
                {
                    var pair = new LadNetworkPairLayout
                    {
                        NetworkNumber = displayNumber,
                        DiffState = network.State,
                        Title = network.Title
                    };

                    if (network.LeftBody != null && network.LeftBody.Count > 0)
                    {
                        pair.Left = LayoutBody(network.LeftBody, network.State);
                        pair.Left.NetworkNumber = pair.NetworkNumber;
                    }

                    if (network.RightBody != null && network.RightBody.Count > 0)
                    {
                        pair.Right = LayoutBody(network.RightBody, network.State);
                        pair.Right.NetworkNumber = pair.NetworkNumber;
                    }

                    // Fallback to Body if both side bodies are empty (legacy or merged result)
                    if (pair.Left == null && pair.Right == null && network.Body != null && network.Body.Count > 0)
                    {
                        pair.Right = LayoutBody(network.Body, network.State);
                        pair.Right.NetworkNumber = pair.NetworkNumber;
                    }

                    pairs.Add(pair);
                    displayNumber++;
                }
            }

            return pairs;
        }

        public static LadNetworkLayout LayoutBody(Dictionary<string, SactComponentData> componentsByUId, CompareState state)
        {
            LadNetworkLayout layout = new()
            {
                DiffState = state
            };

            if (componentsByUId == null || componentsByUId.Count == 0)
            {
                return layout;
            }

            Dictionary<string, string> componentOwnerByConnectorId = new();
            // ... (rest of layout logic)


            foreach (var component in componentsByUId.Values)
            {
                foreach (var input in component.inputConnectors)
                {
                    if (!string.IsNullOrEmpty(input.uId))
                    {
                        componentOwnerByConnectorId[input.uId] = component.uId;
                    }
                }
                foreach (var output in component.outputConnectors)
                {
                    if (!string.IsNullOrEmpty(output.uId))
                    {
                        componentOwnerByConnectorId[output.uId] = component.uId;
                    }
                }
            }

            // Find start element (Powerrail)
            var startElement = componentsByUId.Values.FirstOrDefault(c => 
                (c.name != null && (c.name.IndexOf("BranchWire", StringComparison.OrdinalIgnoreCase) >= 0 || c.name.IndexOf("Powerrail", StringComparison.OrdinalIgnoreCase) >= 0)) ||
                (c.DisplayName != null && c.DisplayName.IndexOf("Powerrail", StringComparison.OrdinalIgnoreCase) >= 0) ||
                c.isStartElement == true);

            if (startElement == null)
            {
                // Fallback: pick any element that has no inputs or just the first element
                startElement = componentsByUId.Values.FirstOrDefault(c => c.inputConnectors.Count == 0)
                             ?? componentsByUId.Values.FirstOrDefault();
            }

            if (startElement == null || string.IsNullOrEmpty(startElement.uId))
            {
                return layout;
            }

            HashSet<string> visited = new();
            Queue<(SactComponentData component, int col, int row)> queue = new();

            queue.Enqueue((startElement, 0, 0));

            int maxCol = 0;
            int maxRow = 0;

            while (queue.Count > 0)
            {
                var (current, col, row) = queue.Dequeue();

                if (string.IsNullOrEmpty(current.uId) || visited.Contains(current.uId))
                {
                    continue;
                }
                visited.Add(current.uId);

                maxCol = Math.Max(maxCol, col);
                maxRow = Math.Max(maxRow, row);

                if (!IsRoutingOnlyElement(current))
                {
                    layout.Elements.Add(new LadElementLayout
                    {
                        Column = col,
                        Row = row,
                        ElementType = MapElementType(current),
                        DisplayName = current.DisplayName ?? string.Empty,
                        Operand = current.TopOperandConnector?.DisplayName ?? string.Empty,
                        UId = current.uId,
                        DiffState = current.State
                    });
                }

                bool isBranchingElement = current.name == "LadOrWireData" || 
                                          current.name == "OrBranch" || 
                                          current.name == "BranchWireData" || 
                                          current.name == "Powerrail";

                if (isBranchingElement)
                {
                    int branchRow = row;
                    foreach (var output in current.outputConnectors)
                    {
                        string partnerUId = output.PartnerUId ?? string.Empty;
                        if (partnerUId.Length == 0 || !componentOwnerByConnectorId.TryGetValue(partnerUId, out string partnerCompId))
                        {
                            continue;
                        }

                        if (componentsByUId.TryGetValue(partnerCompId, out var nextComp))
                        {
                            AddOrthogonalWire(layout, col, row, col + 1, branchRow);

                            queue.Enqueue((nextComp, col + 1, branchRow));
                            branchRow++;
                        }
                    }
                }
                else
                {
                    foreach (var output in current.outputConnectors)
                    {
                        string partnerUId = output.PartnerUId ?? string.Empty;
                        if (partnerUId.Length == 0 || !componentOwnerByConnectorId.TryGetValue(partnerUId, out string partnerCompId))
                        {
                            continue;
                        }

                        if (componentsByUId.TryGetValue(partnerCompId, out var nextComp))
                        {
                            AddOrthogonalWire(layout, col, row, col + 1, row);

                            queue.Enqueue((nextComp, col + 1, row));
                        }
                    }
                }
            }

            layout.ColumnCount = maxCol + 1;
            layout.RowCount = maxRow + 1;

            return layout;
        }

        private static void AddOrthogonalWire(LadNetworkLayout layout, int fromColumn, int fromRow, int toColumn, int toRow)
        {
            if (fromRow != toRow)
            {
                layout.Wires.Add(new LadWireSegment
                {
                    FromColumn = fromColumn,
                    FromRow = fromRow,
                    ToColumn = fromColumn,
                    ToRow = toRow,
                    IsOrBranch = true
                });
            }

            layout.Wires.Add(new LadWireSegment
            {
                FromColumn = fromColumn,
                FromRow = toRow,
                ToColumn = toColumn,
                ToRow = toRow,
                IsOrBranch = fromRow != toRow
            });
        }

        private static bool IsRoutingOnlyElement(SactComponentData component)
        {
            return component.name == "LadOrWireData" || component.name == "OrBranch";
        }

        private static LadElementType MapElementType(SactComponentData component)
        {
            string name = component.name?.ToLowerInvariant() ?? "";
            string displayName = component.DisplayName?.ToLowerInvariant() ?? "";

            if (name == "branchwiredata" || name == "powerrail" || displayName == "powerrail") 
                return LadElementType.Powerrail;

            if (name.Contains("contact") || displayName.Contains("contact")) 
                return component.negated == true ? LadElementType.NegatedContact : LadElementType.Contact;

            if (name.Contains("coil") || displayName.Contains("coil")) 
                return component.negated == true ? LadElementType.NegatedCoil : LadElementType.Coil;

            switch (component.name)
            {
                case "LadTemplatedContactData":
                    if (component.TemplateType == "P") return LadElementType.PEdgeContact;
                    if (component.TemplateType == "N") return LadElementType.NEdgeContact;
                    return LadElementType.TemplatedContact;
                case "LadComparatorContactData": return LadElementType.ComparatorBox;
                case "LadBoxData": return LadElementType.Box;
                case "LadOrWireData": 
                case "OrBranch":
                    return LadElementType.OrBranch;
                case "LadTemplatedCoilData": return LadElementType.TemplatedCoil;
                default: 
                    // Special names that often appear as DisplayName
                    if (displayName == "move" || displayName == "add" || displayName == "sub" || displayName == "mul" || displayName == "div")
                        return LadElementType.ComparatorBox;
                    
                    if (displayName.StartsWith("call")) return LadElementType.Call;

                    return LadElementType.Contact; // Fallback
            }
        }
    }
}
