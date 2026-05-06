using System;
using System.Collections.Generic;
using System.Linq;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services.SimaticMl
{
    public static class SimaticMlComparer
    {
        public static SactCompareResult Compare(SimaticMlFile? left, SimaticMlFile? right)
        {
            if (left == null && right == null) return new SactCompareResult { State = CompareState.Equal };
            
            if (left == null)
            {
                return SimaticMlToSactMapper.Map(right!, CompareState.MissingOnLeft);
            }

            if (right == null)
            {
                return SimaticMlToSactMapper.Map(left!, CompareState.MissingOnRight);
            }

            var result = new SactCompareResult
            {
                Left = left.Blocks.FirstOrDefault()?.Name ?? "Left",
                Right = right.Blocks.FirstOrDefault()?.Name ?? "Right",
                State = CompareState.Equal
            };

            var leftBlock = left.Blocks.FirstOrDefault();
            var rightBlock = right.Blocks.FirstOrDefault();

            if (leftBlock == null && rightBlock == null) return result;
            
            if (leftBlock == null)
            {
                result.State = CompareState.MissingOnLeft;
                result.Content = SimaticMlToSactMapper.Map(right, CompareState.MissingOnLeft).Content;
                return result;
            }

            if (rightBlock == null)
            {
                result.State = CompareState.MissingOnRight;
                result.Content = SimaticMlToSactMapper.Map(left, CompareState.MissingOnRight).Content;
                return result;
            }

            // Compare Interfaces
            result.Interface = CompareInterfaces(leftBlock, rightBlock);

            // Compare Content (Networks)
            result.Content = CompareContent(leftBlock, rightBlock);

            // Overall state
            if (result.Interface.State != CompareState.Equal || result.Content.State != CompareState.Equal)
            {
                result.State = CompareState.Changed;
            }

            return result;
        }

        private static SactInterfaceResult CompareInterfaces(BlockDefinition left, BlockDefinition right)
        {
            // Simplified: just map right and check if it differs from left
            // For now we map right side and set state to Equal (UI doesn't highlight interface diffs much yet)
            var result = SimaticMlToSactMapper.Map(new SimaticMlFile { Blocks = { right } }).Interface!;
            
            // TODO: Deep compare interface members
            return result;
        }

        private static SactContentResult CompareContent(BlockDefinition left, BlockDefinition right)
        {
            var result = new SactContentResult
            {
                State = CompareState.Equal
            };

            int maxCount = Math.Max(left.CompileUnits.Count, right.CompileUnits.Count);

            for (int i = 0; i < maxCount; i++)
            {
                var leftCu = i < left.CompileUnits.Count ? left.CompileUnits[i] : null;
                var rightCu = i < right.CompileUnits.Count ? right.CompileUnits[i] : null;

                if (leftCu != null && rightCu != null)
                {
                    var networkResult = new SactNetworkResult
                    {
                        State = CompareState.Equal,
                        Title = rightCu.Texts.FirstOrDefault(t => t.CompositionName == "Title")?.Items.FirstOrDefault()?.Text,
                        Comment = rightCu.Texts.FirstOrDefault(t => t.CompositionName == "Comment")?.Items.FirstOrDefault()?.Text
                    };

                    if (rightCu.RawAttributes.TryGetValue("NetworkNumber", out string? numStr) && int.TryParse(numStr, out int num))
                    {
                        networkResult.Number.Right = num;
                    }
                    if (leftCu.RawAttributes.TryGetValue("NetworkNumber", out string? numStrLeft) && int.TryParse(numStrLeft, out int numL))
                    {
                        networkResult.Number.Left = numL;
                    }

                    networkResult.LeftBody = leftCu.Network != null ? SimaticMlToSactMapper.MapNetworkBody(leftCu.Network) : new Dictionary<string, SactComponentData>();
                    networkResult.RightBody = rightCu.Network != null ? SimaticMlToSactMapper.MapNetworkBody(rightCu.Network) : new Dictionary<string, SactComponentData>();
                    
                    // Simple XML comparison for logic
                    if (leftCu.Network?.RawXml != rightCu.Network?.RawXml)
                    {
                        networkResult.State = CompareState.Changed;
                        result.State = CompareState.Changed;
                        ApplyComponentStates(networkResult, leftCu.Network, rightCu.Network);
                    }

                    result.Networks[i.ToString()] = networkResult;
                }
                else if (leftCu == null && rightCu != null)
                {
                    var networkResult = new SactNetworkResult
                    {
                        State = CompareState.MissingOnLeft,
                        Title = rightCu.Texts.FirstOrDefault(t => t.CompositionName == "Title")?.Items.FirstOrDefault()?.Text,
                        Comment = rightCu.Texts.FirstOrDefault(t => t.CompositionName == "Comment")?.Items.FirstOrDefault()?.Text
                    };
                    if (rightCu.RawAttributes.TryGetValue("NetworkNumber", out string? numStr) && int.TryParse(numStr, out int num))
                        networkResult.Number.Right = num;

                    networkResult.RightBody = rightCu.Network != null ? SimaticMlToSactMapper.MapNetworkBody(rightCu.Network) : new Dictionary<string, SactComponentData>();
                    networkResult.RightBody = WithState(networkResult.RightBody, CompareState.MissingOnLeft);
                    result.State = CompareState.Changed;
                    result.Networks[i.ToString()] = networkResult;
                }
                else if (leftCu != null && rightCu == null)
                {
                    var networkResult = new SactNetworkResult
                    {
                        State = CompareState.MissingOnRight,
                        Title = leftCu.Texts.FirstOrDefault(t => t.CompositionName == "Title")?.Items.FirstOrDefault()?.Text,
                        Comment = leftCu.Texts.FirstOrDefault(t => t.CompositionName == "Comment")?.Items.FirstOrDefault()?.Text
                    };
                    if (leftCu.RawAttributes.TryGetValue("NetworkNumber", out string? numStrL) && int.TryParse(numStrL, out int numL))
                        networkResult.Number.Left = numL;

                    networkResult.LeftBody = leftCu.Network != null ? SimaticMlToSactMapper.MapNetworkBody(leftCu.Network) : new Dictionary<string, SactComponentData>();
                    networkResult.LeftBody = WithState(networkResult.LeftBody, CompareState.MissingOnRight);
                    result.State = CompareState.Changed;
                    result.Networks[i.ToString()] = networkResult;
                }
            }

            return result;
        }

        private static void ApplyComponentStates(
            SactNetworkResult networkResult,
            NetworkSourceDefinition? leftNetwork,
            NetworkSourceDefinition? rightNetwork)
        {
            Dictionary<string, string> leftRawByUId = BuildRawComponentXmlByUId(leftNetwork);
            Dictionary<string, string> rightRawByUId = BuildRawComponentXmlByUId(rightNetwork);
            HashSet<string> uids = new HashSet<string>(networkResult.LeftBody.Keys);
            uids.UnionWith(networkResult.RightBody.Keys);

            Dictionary<string, CompareState> leftStates = new Dictionary<string, CompareState>();
            Dictionary<string, CompareState> rightStates = new Dictionary<string, CompareState>();

            foreach (string uid in uids)
            {
                bool hasLeft = networkResult.LeftBody.TryGetValue(uid, out SactComponentData? leftComponent);
                bool hasRight = networkResult.RightBody.TryGetValue(uid, out SactComponentData? rightComponent);

                if (hasLeft && !hasRight)
                {
                    leftStates[uid] = CompareState.MissingOnRight;
                    continue;
                }

                if (!hasLeft && hasRight)
                {
                    rightStates[uid] = CompareState.MissingOnLeft;
                    continue;
                }

                if (leftComponent != null && rightComponent != null &&
                    !ComponentsAreEqual(leftComponent, rightComponent, leftRawByUId, rightRawByUId))
                {
                    leftStates[uid] = CompareState.Changed;
                    rightStates[uid] = CompareState.Changed;
                }
            }

            networkResult.LeftBody = WithStates(networkResult.LeftBody, leftStates);
            networkResult.RightBody = WithStates(networkResult.RightBody, rightStates);
        }

        private static bool ComponentsAreEqual(
            SactComponentData left,
            SactComponentData right,
            Dictionary<string, string> leftRawByUId,
            Dictionary<string, string> rightRawByUId)
        {
            if (IsInfrastructureComponent(left) && IsInfrastructureComponent(right))
            {
                return true;
            }

            bool definitionsAreEqual = ComponentDefinitionsAreEqual(left, right, leftRawByUId, rightRawByUId);

            return definitionsAreEqual &&
                   string.Equals(left.TopOperandConnector?.DisplayName, right.TopOperandConnector?.DisplayName, StringComparison.Ordinal) &&
                   ConnectorSignature(left.inputConnectors) == ConnectorSignature(right.inputConnectors) &&
                   ConnectorSignature(left.outputConnectors) == ConnectorSignature(right.outputConnectors);
        }

        private static bool ComponentDefinitionsAreEqual(
            SactComponentData left,
            SactComponentData right,
            Dictionary<string, string> leftRawByUId,
            Dictionary<string, string> rightRawByUId)
        {
            leftRawByUId.TryGetValue(left.uId, out string? leftRaw);
            rightRawByUId.TryGetValue(right.uId, out string? rightRaw);
            if (leftRaw != null || rightRaw != null)
            {
                return string.Equals(leftRaw, rightRaw, StringComparison.Ordinal);
            }

            return string.Equals(left.name, right.name, StringComparison.Ordinal) &&
                   string.Equals(left.DisplayName, right.DisplayName, StringComparison.Ordinal) &&
                   string.Equals(left.TemplateType, right.TemplateType, StringComparison.Ordinal) &&
                   left.negated == right.negated;
        }

        private static Dictionary<string, string> BuildRawComponentXmlByUId(NetworkSourceDefinition? network)
        {
            Dictionary<string, string> rawByUId = new Dictionary<string, string>();
            if (network == null)
            {
                return rawByUId;
            }

            foreach (PartDefinition part in network.Parts)
            {
                if (part.UId.HasValue)
                {
                    rawByUId[part.UId.Value.ToString()] = part.RawXml ?? string.Empty;
                }
            }

            foreach (CallDefinition call in network.Calls)
            {
                if (call.UId.HasValue)
                {
                    rawByUId[call.UId.Value.ToString()] = call.RawXml ?? string.Empty;
                }
            }

            return rawByUId;
        }

        private static string ConnectorSignature(List<SactConnectorData> connectors)
        {
            return string.Join(
                "|",
                connectors
                    .Select(c => $"{GetComponentIdFromPort(c.uId)}>{GetComponentIdFromPort(c.PartnerUId)}")
                    .OrderBy(c => c, StringComparer.Ordinal));
        }

        private static string GetComponentIdFromPort(string? portId)
        {
            if (string.IsNullOrWhiteSpace(portId))
            {
                return string.Empty;
            }

            string value = portId!;
            int separatorIndex = value.LastIndexOf('_');
            return separatorIndex >= 0 && separatorIndex < value.Length - 1
                ? value.Substring(separatorIndex + 1)
                : value;
        }

        private static bool IsInfrastructureComponent(SactComponentData component)
        {
            return component.name == "BranchWireData" ||
                   component.name == "Powerrail" ||
                   component.name == "LadOrWireData" ||
                   component.name == "OrBranch";
        }

        private static Dictionary<string, SactComponentData> WithStates(
            Dictionary<string, SactComponentData> components,
            Dictionary<string, CompareState> statesByUId)
        {
            return components.ToDictionary(
                kvp => kvp.Key,
                kvp => CopyComponentWithState(
                    kvp.Value,
                    statesByUId.TryGetValue(kvp.Key, out CompareState state) ? state : CompareState.Equal));
        }

        private static Dictionary<string, SactComponentData> WithState(
            Dictionary<string, SactComponentData> components,
            CompareState state)
        {
            return components.ToDictionary(kvp => kvp.Key, kvp => CopyComponentWithState(kvp.Value, state));
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
                outputConnectors = component.outputConnectors
                    .Select(c => new SactConnectorData { uId = c.uId, PartnerUId = c.PartnerUId })
                    .ToList(),
                inputConnectors = component.inputConnectors
                    .Select(c => new SactConnectorData { uId = c.uId, PartnerUId = c.PartnerUId })
                    .ToList()
            };
        }
    }
}
