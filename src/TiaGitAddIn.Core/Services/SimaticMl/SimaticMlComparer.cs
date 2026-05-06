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
                    result.State = CompareState.Changed;
                    result.Networks[i.ToString()] = networkResult;
                }
            }

            return result;
        }
    }
}
