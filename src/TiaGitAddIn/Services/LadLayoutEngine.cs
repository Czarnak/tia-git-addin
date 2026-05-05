using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Lad;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services
{
    public static class LadLayoutEngine
    {
        public static List<LadNetworkLayout> LayoutAll(SactCompareResult compareResult)
        {
            List<LadNetworkLayout> layouts = new();

            if (compareResult.Content == null || compareResult.Content.Networks == null)
            {
                return layouts;
            }

            List<KeyValuePair<string, SactNetworkResult>> sortedNetworks = compareResult.Content.Networks
                .OrderBy(k => int.TryParse(k.Key, out int i) ? i : int.MaxValue)
                .ToList();

            foreach (var kvp in sortedNetworks)
            {
                var network = kvp.Value;
                if (network != null)
                {
                    var layout = Layout(network);
                    layout.NetworkNumber = network.Number.Right > 0 ? network.Number.Right : network.Number.Left;
                    layouts.Add(layout);
                }
            }

            return layouts;
        }

        public static LadNetworkLayout Layout(SactNetworkResult network)
        {
            LadNetworkLayout layout = new()
            {
                DiffState = network.State
            };

            if (network.Body == null || network.Body.Count == 0)
            {
                return layout;
            }

            var componentsByUId = network.Body;
            Dictionary<string, string> componentOwnerByConnectorId = new();

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

            var startElement = componentsByUId.Values.FirstOrDefault(c => c.name == "BranchWireData" && c.isStartElement == true);
            if (startElement == null)
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

                if (visited.Contains(current.uId))
                {
                    continue;
                }
                visited.Add(current.uId);

                maxCol = Math.Max(maxCol, col);
                maxRow = Math.Max(maxRow, row);

                layout.Elements.Add(new LadElementLayout
                {
                    Column = col,
                    Row = row,
                    ElementType = MapElementType(current),
                    DisplayName = current.DisplayName ?? string.Empty,
                    Operand = current.TopOperandConnector?.DisplayName ?? string.Empty,
                    UId = current.uId,
                    // Assume component state follows network state since there is no individual diff state in SACT models
                    DiffState = network.State
                });

                if (current.name == "LadOrWireData" || current.name == "OrBranch")
                {
                    int branchRow = row;
                    foreach (var output in current.outputConnectors)
                    {
                        if (string.IsNullOrEmpty(output.PartnerUId) || !componentOwnerByConnectorId.TryGetValue(output.PartnerUId, out string? partnerCompId))
                        {
                            continue;
                        }

                        if (componentsByUId.TryGetValue(partnerCompId, out var nextComp))
                        {
                            layout.Wires.Add(new LadWireSegment
                            {
                                FromColumn = col,
                                FromRow = row,
                                ToColumn = col + 1,
                                ToRow = branchRow,
                                IsOrBranch = branchRow != row
                            });

                            queue.Enqueue((nextComp, col + 1, branchRow));
                            branchRow++;
                        }
                    }
                }
                else
                {
                    foreach (var output in current.outputConnectors)
                    {
                        if (string.IsNullOrEmpty(output.PartnerUId) || !componentOwnerByConnectorId.TryGetValue(output.PartnerUId, out string? partnerCompId))
                        {
                            continue;
                        }

                        if (componentsByUId.TryGetValue(partnerCompId, out var nextComp))
                        {
                            layout.Wires.Add(new LadWireSegment
                            {
                                FromColumn = col,
                                FromRow = row,
                                ToColumn = col + 1,
                                ToRow = row,
                                IsOrBranch = false
                            });

                            queue.Enqueue((nextComp, col + 1, row));
                        }
                    }
                }
            }

            layout.ColumnCount = maxCol + 1;
            layout.RowCount = maxRow + 1;

            return layout;
        }

        private static LadElementType MapElementType(SactComponentData component)
        {
            switch (component.name)
            {
                case "BranchWireData": return LadElementType.Powerrail;
                case "LadContactData": return component.negated == true ? LadElementType.NegatedContact : LadElementType.Contact;
                case "LadTemplatedContactData":
                    if (component.TemplateType == "P") return LadElementType.PEdgeContact;
                    if (component.TemplateType == "N") return LadElementType.NEdgeContact;
                    return LadElementType.TemplatedContact;
                case "LadCoilData": return component.negated == true ? LadElementType.NegatedCoil : LadElementType.Coil;
                case "LadComparatorContactData": return LadElementType.ComparatorBox;
                case "LadOrWireData": return LadElementType.OrBranch;
                case "LadTemplatedCoilData": return LadElementType.TemplatedCoil;
                default: return LadElementType.Contact; // Fallback
            }
        }
    }
}