using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services.SimaticMl
{
    public static class LadVisualGraphBuilder
    {
        private static readonly HashSet<string> OutputPins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "eno", "out", "out1", "out2", "q", "ret_val", "output"
        };

        public static Dictionary<string, SactComponentData> Build(NetworkSourceDefinition network)
        {
            var componentsByUid = CreateInstructionComponents(network);
            var accessesByUid = network.Accesses
                .Where(a => a.UId.HasValue)
                .ToDictionary(a => a.UId!.Value.ToString(), a => a);

            AssignOperands(network, componentsByUid, accessesByUid);
            AddPowerrail(network, componentsByUid);
            AddOpenEndpointComponents(network, componentsByUid);
            AddLogicConnections(network, componentsByUid);

            return componentsByUid;
        }

        private static Dictionary<string, SactComponentData> CreateInstructionComponents(NetworkSourceDefinition network)
        {
            var componentsByUid = new Dictionary<string, SactComponentData>();

            foreach (var part in network.Parts.Where(p => p.UId.HasValue))
            {
                var comp = new SactComponentData
                {
                    uId = part.UId!.Value.ToString(),
                    name = MapSimaticPartToSactName(part),
                    DisplayName = part.Name,
                    negated = part.Negated.Count > 0,
                    DisabledENO = part.DisabledENO,
                    Comment = part.CommentText,
                    Equation = part.Equation,
                    InvisiblePins = new List<string>(part.Invisible),
                    NegatedPins = new List<string>(part.Negated)
                };

                var templateValue = part.TemplateValues.FirstOrDefault();
                if (templateValue != null)
                {
                    comp.TemplateType = templateValue.Value;
                }

                componentsByUid[comp.uId] = comp;
            }

            foreach (var call in network.Calls.Where(c => c.UId.HasValue))
            {
                var comp = new SactComponentData
                {
                    uId = call.UId!.Value.ToString(),
                    name = "LadBoxData",
                    DisplayName = call.CallInfo?.Name ?? "Call",
                    Comment = call.CommentText,
                    inputParameters = MapCallParameters(call, IsInputParameter),
                    outputParameters = MapCallParameters(call, IsOutputParameter)
                };

                componentsByUid[comp.uId] = comp;
            }

            return componentsByUid;
        }

        private static void AssignOperands(
            NetworkSourceDefinition network,
            Dictionary<string, SactComponentData> componentsByUid,
            Dictionary<string, AccessDefinition> accessesByUid)
        {
            foreach (var wire in network.Wires)
            {
                var identCons = wire.Connections.OfType<IdentConDefinition>().ToList();
                if (identCons.Count == 0)
                {
                    continue;
                }

                foreach (var idCon in identCons.Where(c => c.UId.HasValue))
                {
                    string accessId = idCon.UId!.Value.ToString();
                    if (!accessesByUid.TryGetValue(accessId, out var access))
                    {
                        continue;
                    }

                    string operand = FormatAccessDisplayName(access);
                    foreach (var otherConn in wire.Connections.Where(c => c != idCon))
                    {
                        string otherId = GetComponentId(wire, otherConn);
                        if (!componentsByUid.TryGetValue(otherId, out var component))
                        {
                            continue;
                        }

                        if (otherConn is NameConDefinition nameCon &&
                            TryAssignParameterOperand(component, nameCon.Name, operand))
                        {
                            continue;
                        }

                        if (otherConn is NameConDefinition namedPin &&
                            TryAssignBoxPinOperand(component, namedPin.Name, operand))
                        {
                            continue;
                        }

                        component.TopOperandConnector = new SactOperandConnector { DisplayName = operand };
                    }
                }
            }
        }

        private static void AddPowerrail(
            NetworkSourceDefinition network,
            Dictionary<string, SactComponentData> componentsByUid)
        {
            bool hasPowerrail = network.Powerrail != null ||
                                network.Wires.Any(w => w.Connections.Any(c => c is PowerrailConDefinition));
            if (!hasPowerrail)
            {
                return;
            }

            componentsByUid["Powerrail"] = new SactComponentData
            {
                uId = "Powerrail",
                name = "BranchWireData",
                DisplayName = "Powerrail",
                isStartElement = true
            };
        }

        private static void AddOpenEndpointComponents(
            NetworkSourceDefinition network,
            Dictionary<string, SactComponentData> componentsByUid)
        {
            foreach (var wire in network.Wires)
            {
                foreach (var conn in wire.Connections)
                {
                    string id = GetComponentId(wire, conn);
                    if (string.IsNullOrEmpty(id) || componentsByUid.ContainsKey(id))
                    {
                        continue;
                    }

                    if (conn is OpenConDefinition || conn is OpenbranchConDefinition)
                    {
                        componentsByUid[id] = new SactComponentData
                        {
                            uId = id,
                            name = "LadOrWireData",
                            DisplayName = conn.Kind ?? "Open"
                        };
                    }
                }
            }
        }

        private static void AddLogicConnections(
            NetworkSourceDefinition network,
            Dictionary<string, SactComponentData> componentsByUid)
        {
            foreach (var wire in network.Wires)
            {
                if (wire.Connections.Any(c => c is IdentConDefinition))
                {
                    continue;
                }

                var connections = wire.Connections.ToList();
                if (connections.Count < 2)
                {
                    continue;
                }

                var sourceCon = connections.FirstOrDefault(c => IsSourceConnection(wire, c, componentsByUid));
                var targetCons = connections.Where(c => c != sourceCon).ToList();

                if (sourceCon == null)
                {
                    sourceCon = connections[0];
                    targetCons = connections.Skip(1).ToList();
                }

                string sourceCompId = GetComponentId(wire, sourceCon);
                if (!componentsByUid.TryGetValue(sourceCompId, out var sourceComp))
                {
                    continue;
                }

                foreach (var targetCon in targetCons)
                {
                    string targetCompId = GetComponentId(wire, targetCon);
                    if (!componentsByUid.TryGetValue(targetCompId, out var targetComp))
                    {
                        continue;
                    }

                    string sourcePin = GetPinName(sourceCon);
                    string targetPin = GetPinName(targetCon);
                    if (IsHiddenPin(sourceComp, sourcePin) || IsHiddenPin(targetComp, targetPin))
                    {
                        continue;
                    }

                    string wireId = wire.UId?.ToString() ?? Guid.NewGuid().ToString();
                    string sourcePortId = $"{wireId}_src_{sourceCompId}";
                    string targetPortId = $"{wireId}_tgt_{targetCompId}";

                    sourceComp.outputConnectors.Add(new SactConnectorData
                    {
                        uId = sourcePortId,
                        PinName = sourcePin,
                        PartnerUId = targetPortId
                    });

                    targetComp.inputConnectors.Add(new SactConnectorData
                    {
                        uId = targetPortId,
                        PinName = targetPin,
                        PartnerUId = sourcePortId
                    });
                }
            }
        }

        private static string GetComponentId(WireDefinition wire, ConnectionDefinition conn)
        {
            if (conn is PowerrailConDefinition)
            {
                return "Powerrail";
            }

            if (conn is IdentConDefinition idCon)
            {
                return idCon.UId?.ToString() ?? string.Empty;
            }

            if (conn is NameConDefinition nameCon)
            {
                return nameCon.UId?.ToString() ?? string.Empty;
            }

            if (conn is OpenConDefinition openCon)
            {
                return openCon.UId.HasValue
                    ? "OpenCon_" + openCon.UId.Value.ToString()
                    : "OpenCon_" + (wire.UId?.ToString() ?? Guid.NewGuid().ToString());
            }

            if (conn is OpenbranchConDefinition)
            {
                return "Openbranch_" + (wire.UId?.ToString() ?? Guid.NewGuid().ToString());
            }

            return string.Empty;
        }

        private static string GetPinName(ConnectionDefinition conn)
        {
            return conn is NameConDefinition nameCon ? nameCon.Name ?? string.Empty : string.Empty;
        }

        private static bool IsSourceConnection(
            WireDefinition wire,
            ConnectionDefinition conn,
            Dictionary<string, SactComponentData> components)
        {
            if (conn is PowerrailConDefinition)
            {
                return true;
            }

            string id = GetComponentId(wire, conn);
            if (!components.TryGetValue(id, out var comp))
            {
                return false;
            }

            if (conn is NameConDefinition nameCon && OutputPins.Contains(nameCon.Name ?? string.Empty))
            {
                return true;
            }

            return comp.name == "LadLiteralData" || comp.name == "LadOperandData";
        }

        private static bool IsHiddenPin(SactComponentData component, string pinName)
        {
            return !string.IsNullOrWhiteSpace(pinName) &&
                   component.InvisiblePins.Any(p => string.Equals(p, pinName, StringComparison.OrdinalIgnoreCase));
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
                    Type = p.Type ?? string.Empty,
                    Operand = string.Empty,
                    IsVisible = p.Informative != true
                })
                .ToList();
        }

        private static bool TryAssignParameterOperand(SactComponentData component, string? pinName, string operand)
        {
            if (string.IsNullOrWhiteSpace(pinName))
            {
                return false;
            }

            bool assigned = false;
            foreach (var parameter in component.inputParameters.Concat(component.outputParameters))
            {
                if (string.Equals(parameter.Name, pinName, StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Operand = operand;
                    assigned = true;
                }
            }

            return assigned;
        }

        private static bool TryAssignBoxPinOperand(SactComponentData component, string? pinName, string operand)
        {
            if (!IsBoxLike(component) || string.IsNullOrWhiteSpace(pinName))
            {
                return false;
            }

            var parameters = OutputPins.Contains(pinName!)
                ? component.outputParameters
                : component.inputParameters;
            var existing = parameters.FirstOrDefault(p =>
                string.Equals(p.Name, pinName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Operand = operand;
            }
            else
            {
                parameters.Add(new SactParameterData
                {
                    Name = pinName!.ToUpperInvariant(),
                    Section = OutputPins.Contains(pinName!) ? "Output" : "Input",
                    Operand = operand
                });
            }

            return true;
        }

        private static bool IsBoxLike(SactComponentData component)
        {
            return component.name.IndexOf("Box", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   component.name.IndexOf("Comparator", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatAccessDisplayName(AccessDefinition access)
        {
            if (!string.IsNullOrWhiteSpace(access.ConstantValue))
            {
                return access.ConstantValue!;
            }

            string symbol = FormatSymbolPath(access);
            if (string.Equals(access.Scope, "LocalVariable", StringComparison.OrdinalIgnoreCase))
            {
                return "#" + symbol;
            }

            return symbol;
        }

        private static string FormatSymbolPath(AccessDefinition access)
        {
            if (access.Components.Count == 0)
            {
                return access.SymbolPath ?? "Access";
            }

            return string.Join(".", access.Components.Select(FormatComponent));
        }

        private static string FormatComponent(AccessComponentDefinition component)
        {
            string suffix = component.SliceAccessModifier ?? component.SimpleAccessModifier ?? string.Empty;
            return string.IsNullOrWhiteSpace(suffix)
                ? component.Name
                : component.Name + "." + suffix;
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

        private static string MapSimaticPartToSactName(PartDefinition part)
        {
            switch (part.Name)
            {
                case "Contact":
                    return part.TemplateValues.Count > 0 ? "LadTemplatedContactData" : "LadContactData";
                case "PContact":
                    return "LadTemplatedContactData";
                case "Coil":
                    return part.TemplateValues.Count > 0 ? "LadTemplatedCoilData" : "LadCoilData";
                case "SCoil":
                case "RCoil":
                    return "LadTemplatedCoilData";
                case "Or":
                case "O":
                    return "LadOrWireData";
                case "Lt":
                case "Le":
                case "Eq":
                case "Ne":
                case "Ge":
                case "Gt":
                    return "LadComparatorContactData";
                case "Box":
                case "Move":
                case "Add":
                case "Sub":
                case "Mul":
                case "Div":
                case "Inc":
                case "Calculate":
                    return "LadBoxData";
                default:
                    return part.Name ?? string.Empty;
            }
        }
    }
}
