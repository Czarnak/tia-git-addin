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

            result.Interface = MapInterface(block, fileState);
            
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

                    int displayNum = i + 1;
                    if (cu.RawAttributes.TryGetValue("NetworkNumber", out string? numStr) && int.TryParse(numStr, out int num))
                    {
                        displayNum = num;
                    }
                    else if (cu.RawAttributes.TryGetValue("Number", out string? numStr2) && int.TryParse(numStr2, out int num2))
                    {
                        displayNum = num2;
                    }

                    networkResult.Number.Right = displayNum;
                    networkResult.Number.Left = displayNum;

                    var body = WithComponentState(MapNetworkBody(cu.Network), fileState);
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

        private static Dictionary<string, SactComponentData> WithComponentState(
            Dictionary<string, SactComponentData> componentsByUid,
            CompareState state)
        {
            if (state == CompareState.Equal)
            {
                return componentsByUid;
            }

            return componentsByUid.ToDictionary(
                kvp => kvp.Key,
                kvp => CopyComponentWithState(kvp.Value, state));
        }

        private static SactComponentData CopyComponentWithState(SactComponentData component, CompareState state)
        {
            return new SactComponentData
            {
                name = component.name,
                uId = component.uId,
                State = state,
                isStartElement = component.isStartElement,
                negated = component.negated,
                DisplayName = component.DisplayName,
                TemplateType = component.TemplateType,
                TopOperandConnector = component.TopOperandConnector == null
                    ? null
                    : new SactOperandConnector { DisplayName = component.TopOperandConnector.DisplayName },
                inputParameters = component.inputParameters
                    .Select(CopyParameter)
                    .ToList(),
                outputParameters = component.outputParameters
                    .Select(CopyParameter)
                    .ToList(),
                outputConnectors = component.outputConnectors
                    .Select(CopyConnector)
                    .ToList(),
                inputConnectors = component.inputConnectors
                    .Select(CopyConnector)
                    .ToList()
            };
        }

        private static SactInterfaceResult MapInterface(BlockDefinition block, CompareState state)
        {
            var sections = new Dictionary<string, object>();
            foreach (var section in block.InterfaceSections)
            {
                sections[section.Name] = MapMembers(section.Members);
            }

            return new SactInterfaceResult
            {
                State = state,
                Sections = sections,
                Members = CreateInterfaceRows(block, state)
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
                    DisplayName = part.Name,
                    negated = false 
                };

                var templateValue = part.TemplateValues.FirstOrDefault();
                if (templateValue != null)
                {
                    comp.TemplateType = templateValue.Value;
                }

                componentsByUid[comp.uId] = comp;
            }

            // 1b. Create components for Calls
            foreach (var call in network.Calls)
            {
                if (!call.UId.HasValue) continue;

                var comp = new SactComponentData
                {
                    uId = call.UId.Value.ToString(),
                    name = "LadBoxData",
                    DisplayName = call.CallInfo?.Name ?? "Call",
                    inputParameters = MapCallParameters(call, IsInputParameter),
                    outputParameters = MapCallParameters(call, IsOutputParameter)
                };

                componentsByUid[comp.uId] = comp;
            }

            // 2. Create components for Accesses (Tags/Operands)
            var accessesByUid = new Dictionary<string, AccessDefinition>();
            foreach (var access in network.Accesses)
            {
                if (!access.UId.HasValue) continue;
                accessesByUid[access.UId.Value.ToString()] = access;
            }

            // 3. Handle IdentCon connections (linking Accesses to Parts)
            foreach (var wire in network.Wires)
            {
                // Find if this wire connects a Part/Call and an Access (IdentCon)
                var identCons = wire.Connections.OfType<IdentConDefinition>().ToList();
                if (identCons.Count == 0) continue;

                // A wire with IdentCon usually connects a specific Part and an Access
                // Example: Wire connects Part (UId=1) and Access (UId=2) via IdentCon
                foreach (var idCon in identCons)
                {
                    if (!idCon.UId.HasValue) continue;
                    string targetId = idCon.UId.Value.ToString();

                    // If target is an Access, find what else this wire connects to
                    if (accessesByUid.TryGetValue(targetId, out var access))
                    {
                        // Look for other connections in the same wire that are NOT this Access
                        foreach (var otherConn in wire.Connections.Where(c => c != idCon))
                        {
                            string otherId = GetCompId(otherConn);
                            if (componentsByUid.TryGetValue(otherId, out var component))
                            {
                                // Attach tag name to the component
                                string tagName = access.SymbolPath ?? access.ConstantValue ?? "Access";
                                component.TopOperandConnector = new SactOperandConnector
                                {
                                    DisplayName = tagName
                                };
                            }
                        }
                    }
                }
            }

            // 4. Process Logic Wires (Powerrail, logic flow)
            bool hasPowerrail = network.Powerrail != null || network.Wires.Any(w => w.Connections.Any(c => c is PowerrailConDefinition));
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

            // Add virtual Openbranches and OpenCons
            foreach (var wire in network.Wires)
            {
                foreach (var conn in wire.Connections)
                {
                    string id = GetCompId(conn);
                    if (string.IsNullOrEmpty(id) || componentsByUid.ContainsKey(id)) continue;

                    if (conn is OpenConDefinition)
                    {
                        componentsByUid[id] = new SactComponentData
                        {
                            uId = id,
                            name = "LadOrWireData",
                            DisplayName = "OpenCon"
                        };
                    }
                }
            }

            foreach (var wire in network.Wires)
            {
                // Skip wires used for IdentCon (operands) in logic flow processing
                if (wire.Connections.Any(c => c is IdentConDefinition)) continue;

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
                        PinName = GetPinName(sourceCon),
                        PartnerUId = targetPortId
                    });

                    targetComp.inputConnectors.Add(new SactConnectorData
                    {
                        uId = targetPortId,
                        PinName = GetPinName(targetCon),
                        PartnerUId = sourcePortId
                    });
                }
            }

            return componentsByUid;
        }

        private static string GetCompId(ConnectionDefinition conn)
        {
            if (conn is PowerrailConDefinition) return "Powerrail";
            if (conn is IdentConDefinition idCon) return idCon.UId?.ToString() ?? "";
            if (conn is NameConDefinition nameCon) return nameCon.UId?.ToString() ?? "";
            if (conn is OpenConDefinition openCon) return openCon.UId?.ToString() ?? "";
            return "";
        }

        private static string GetPinName(ConnectionDefinition conn)
        {
            return conn is NameConDefinition nameCon ? nameCon.Name ?? string.Empty : string.Empty;
        }

        private static bool IsSourceConnection(ConnectionDefinition conn, Dictionary<string, SactComponentData> components)
        {
            if (conn is PowerrailConDefinition) return true;

            int? uId = null;
            if (conn is IdentConDefinition idCon) uId = idCon.UId;
            else if (conn is NameConDefinition nCon) uId = nCon.UId;
            else if (conn is OpenConDefinition oCon) uId = oCon.UId;

            if (!uId.HasValue) return false;

            string id = uId.Value.ToString();
            if (components.TryGetValue(id, out var comp))
            {
                if (conn is NameConDefinition nameCon && IsOutputPin(nameCon.Name)) return true;
                
                if (comp.name == "LadLiteralData" || comp.name == "LadOperandData") return true;
            }

            return false;
        }

        private static bool IsOutputPin(string? pin)
        {
            return !string.IsNullOrWhiteSpace(pin) && OutputPins.Contains(pin!);
        }

        private static List<SactInterfaceMemberComparison> CreateInterfaceRows(BlockDefinition block, CompareState state)
        {
            return block.InterfaceSections
                .SelectMany(section => FlattenMembers(section.Name, section.Members, string.Empty))
                .Select(member => new SactInterfaceMemberComparison
                {
                    Section = member.Section,
                    Name = member.Path,
                    LeftDatatype = state == CompareState.MissingOnLeft ? string.Empty : member.Member.Datatype,
                    RightDatatype = state == CompareState.MissingOnRight ? string.Empty : member.Member.Datatype,
                    LeftStartValue = state == CompareState.MissingOnLeft ? string.Empty : member.Member.StartValue ?? string.Empty,
                    RightStartValue = state == CompareState.MissingOnRight ? string.Empty : member.Member.StartValue ?? string.Empty,
                    State = state
                })
                .ToList();
        }

        private static IEnumerable<(string Section, string Path, InterfaceMember Member)> FlattenMembers(
            string section,
            IEnumerable<InterfaceMember> members,
            string parentPath)
        {
            foreach (InterfaceMember member in members)
            {
                string path = string.IsNullOrEmpty(parentPath)
                    ? member.Name
                    : parentPath + "." + member.Name;

                yield return (section, path, member);

                foreach (var child in FlattenMembers(section, member.Children, path))
                {
                    yield return child;
                }
            }
        }

        private static List<SactParameterData> MapCallParameters(
            CallDefinition call,
            Func<CallParameterDefinition, bool> predicate)
        {
            if (call.CallInfo == null)
            {
                return new List<SactParameterData>();
            }

            return call.CallInfo.Parameters
                .Where(predicate)
                .Select(p => new SactParameterData
                {
                    Name = p.Name ?? string.Empty,
                    Section = p.Section ?? string.Empty,
                    Type = p.Type ?? string.Empty
                })
                .ToList();
        }

        private static bool IsInputParameter(CallParameterDefinition parameter)
        {
            return string.Equals(parameter.Section, "Input", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(parameter.Section, "InOut", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOutputParameter(CallParameterDefinition parameter)
        {
            return string.Equals(parameter.Section, "Output", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(parameter.Section, "Return", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(parameter.Section, "InOut", StringComparison.OrdinalIgnoreCase);
        }

        private static SactParameterData CopyParameter(SactParameterData parameter)
        {
            return new SactParameterData
            {
                Name = parameter.Name,
                Section = parameter.Section,
                Type = parameter.Type
            };
        }

        private static SactConnectorData CopyConnector(SactConnectorData connector)
        {
            return new SactConnectorData
            {
                uId = connector.uId,
                PinName = connector.PinName,
                PartnerUId = connector.PartnerUId
            };
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
