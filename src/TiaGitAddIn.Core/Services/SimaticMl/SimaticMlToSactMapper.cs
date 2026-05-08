using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services.SimaticMl
{
    public static class SimaticMlToSactMapper
    {
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
                Comment = component.Comment,
                Equation = component.Equation,
                DisabledENO = component.DisabledENO,
                InvisiblePins = new List<string>(component.InvisiblePins),
                NegatedPins = new List<string>(component.NegatedPins),
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
            return LadVisualGraphBuilder.Build(network);
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

        private static SactParameterData CopyParameter(SactParameterData parameter)
        {
            return new SactParameterData
            {
                Name = parameter.Name,
                Section = parameter.Section,
                Type = parameter.Type,
                Operand = parameter.Operand,
                IsVisible = parameter.IsVisible
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

    }
}
