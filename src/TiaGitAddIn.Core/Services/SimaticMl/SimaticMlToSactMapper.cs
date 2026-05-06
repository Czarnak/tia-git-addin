using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services.SimaticMl
{
    public static class SimaticMlToSactMapper
    {
        private static readonly HashSet<string> InputPins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "en", "in", "in1", "in2", "in3", "in4",
            "operand",
            "i", "i0", "i1",
            "src", "src1", "src2",
            "input"
        };

        private static readonly HashSet<string> OutputPins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "eno", "out", "out1", "out2", "q",
            "ret_val",
            "output"
        };

        public static SactCompareResult Map(SimaticMlFile file, CompareState fileState = CompareState.Equal)
        {
            var result = new SactCompareResult
            {
                State = fileState
            };

            var block = file.Blocks.FirstOrDefault();
            if (block == null) return result;

            result.Interface = MapInterface(block);
            
            var content = new SactContentResult { State = fileState };
            for (int i = 0; i < block.CompileUnits.Count; i++)
            {
                var cu = block.CompileUnits[i];
                if (cu.Network != null)
                {
                    var networkResult = new SactNetworkResult
                    {
                        State = fileState,
                        Title = cu.Texts.FirstOrDefault(t => t.CompositionName == "Title")?.Items.FirstOrDefault()?.Text,
                        Comment = cu.Texts.FirstOrDefault(t => t.CompositionName == "Comment")?.Items.FirstOrDefault()?.Text
                    };

                    if (cu.RawAttributes.TryGetValue("NetworkNumber", out string? numStr) && int.TryParse(numStr, out int num))
                    {
                        networkResult.Number.Right = num;
                        networkResult.Number.Left = num;
                    }

                    var body = MapNetworkBody(cu.Network);
                    networkResult.Body = body;
                    if (fileState == CompareState.MissingOnLeft) networkResult.RightBody = body;
                    else if (fileState == CompareState.MissingOnRight) networkResult.LeftBody = body;
                    else { networkResult.LeftBody = body; networkResult.RightBody = body; }

                    content.Networks[i.ToString()] = networkResult;
                }
            }
            result.Content = content;

            return result;
        }

        private static SactInterfaceResult MapInterface(BlockDefinition block)
        {
            var sections = new Dictionary<string, object>();
            foreach (var section in block.InterfaceSections)
            {
                sections[section.Name] = MapMembers(section.Members);
            }

            return new SactInterfaceResult
            {
                State = CompareState.Equal,
                Sections = sections
            };
        }

        private static List<Dictionary<string, object>> MapMembers(List<InterfaceMember> members)
        {
            var result = new List<Dictionary<string, object>>();
            foreach (var member in members)
            {
                var dict = new Dictionary<string, object>
                {
                    { "Name", member.Name },
                    { "Datatype", member.Datatype },
                    { "StartValue", member.StartValue ?? "" }
                };

                if (member.Children.Count > 0)
                {
                    dict["Members"] = MapMembers(member.Children);
                }

                result.Add(dict);
            }
            return result;
        }

        public static Dictionary<string, SactComponentData> MapNetworkBody(NetworkSourceDefinition network)
        {
            var componentsByUid = new Dictionary<string, SactComponentData>();
            
            // 1. Create components for Parts
            foreach (var part in network.Parts)
            {
                if (!part.UId.HasValue) continue;

                var comp = new SactComponentData
                {
                    uId = part.UId.Value.ToString(),
                    name = MapSimaticPartToSactName(part.Name ?? ""),
                    DisplayName = part.Name
                };

                var templateValue = part.TemplateValues.FirstOrDefault();
                if (templateValue != null)
                {
                    comp.TemplateType = templateValue.Value;
                }

                componentsByUid[comp.uId] = comp;
            }

            // 2. Create components for Accesses
            foreach (var access in network.Accesses)
            {
                if (!access.UId.HasValue) continue;

                var comp = new SactComponentData
                {
                    uId = access.UId.Value.ToString(),
                    name = access.Scope == "LiteralConstant" ? "LadLiteralData" : "LadOperandData",
                    DisplayName = access.SymbolPath ?? access.ConstantValue ?? "Access"
                };

                comp.TopOperandConnector = new SactOperandConnector
                {
                    DisplayName = comp.DisplayName
                };

                componentsByUid[comp.uId] = comp;
            }

            // 3. Process Wires
            bool hasPowerrail = network.Wires.Any(w => w.Connections.Any(c => c.Kind == "Powerrail"));
            if (hasPowerrail)
            {
                var pr = new SactComponentData
                {
                    uId = "Powerrail",
                    name = "BranchWireData",
                    DisplayName = "Powerrail",
                    isStartElement = true
                };
                componentsByUid[pr.uId] = pr;
            }

            foreach (var wire in network.Wires)
            {
                var connections = wire.Connections.ToList();
                if (connections.Count < 2) continue;

                var sourceCon = connections.FirstOrDefault(c => IsSourceConnection(c, componentsByUid));
                var targetCons = connections.Where(c => c != sourceCon).ToList();

                if (sourceCon == null)
                {
                    sourceCon = connections[0];
                    targetCons = connections.Skip(1).ToList();
                }

                string sourceCompId = GetCompId(sourceCon);
                if (!componentsByUid.TryGetValue(sourceCompId, out var sourceComp)) continue;

                foreach (var targetCon in targetCons)
                {
                    string targetCompId = GetCompId(targetCon);
                    if (!componentsByUid.TryGetValue(targetCompId, out var targetComp)) continue;

                    string wireId = wire.UId?.ToString() ?? Guid.NewGuid().ToString();
                    string sourcePortId = $"{wireId}_src_{sourceCompId}";
                    string targetPortId = $"{wireId}_tgt_{targetCompId}";

                    sourceComp.outputConnectors.Add(new SactConnectorData
                    {
                        uId = sourcePortId,
                        PartnerUId = targetPortId
                    });

                    targetComp.inputConnectors.Add(new SactConnectorData
                    {
                        uId = targetPortId,
                        PartnerUId = sourcePortId
                    });
                }
            }

            return componentsByUid;
        }

        private static string GetCompId(ConnectionDefinition conn)
        {
            if (conn.Kind == "Powerrail") return "Powerrail";
            return conn.UId?.ToString() ?? "";
        }

        private static bool IsSourceConnection(ConnectionDefinition conn, Dictionary<string, SactComponentData> components)
        {
            if (conn.Kind == "Powerrail") return true;
            if (!conn.UId.HasValue) return false;

            string id = conn.UId.Value.ToString();
            if (components.TryGetValue(id, out var comp))
            {
                // Accesses (operands) are sources if connected to an input pin? 
                // Or targets if connected from an output pin?
                // In SimaticML, NameCon has a Name (pin). If pin is an output pin, it's a source.
                if (conn.Kind == "NameCon" && IsOutputPin(conn.Name)) return true;
                
                // Literals/Operands are usually sources for the logic
                if (comp.name == "LadLiteralData" || comp.name == "LadOperandData") return true;
            }

            return false;
        }

        private static bool IsOutputPin(string? pin)
        {
            return !string.IsNullOrWhiteSpace(pin) && OutputPins.Contains(pin!);
        }

        private static string MapSimaticPartToSactName(string simaticName)
        {
            switch (simaticName)
            {
                case "Contact": return "LadContactData";
                case "Coil": return "LadCoilData";
                case "Or": return "LadOrWireData";
                case "Box": return "LadBoxData";
                default: return simaticName;
            }
        }
    }
}
