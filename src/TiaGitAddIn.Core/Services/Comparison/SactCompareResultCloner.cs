using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// Recursively deep-clones the legacy, mutable <see cref="SactCompareResult"/> object graph. Exists so
    /// <see cref="Models.Comparison.LadPresentation"/> can hand out and accept <see cref="SactCompareResult"/>
    /// instances without ever sharing mutable state with a caller: every collection, nested record, and
    /// attribute value is copied, never referenced. Unsupported mutable attribute values (anything other than
    /// a primitive/string) are converted to an invariant string so no caller can reach back into shared state
    /// through an opaque <c>object</c> value.
    /// </summary>
    public static class SactCompareResultCloner
    {
        public static SactCompareResult Clone(SactCompareResult source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return new SactCompareResult
            {
                Left = source.Left,
                Right = source.Right,
                State = source.State,
                Interface = CloneInterface(source.Interface),
                Content = CloneContent(source.Content),
                Attributes = CloneAttributeMap(source.Attributes)
            };
        }

        private static SactInterfaceResult? CloneInterface(SactInterfaceResult? source)
        {
            if (source == null) return null;

            return new SactInterfaceResult
            {
                State = source.State,
                Sections = CloneAttributeMap(source.Sections),
                Members = source.Members.Select(CloneMember).ToList()
            };
        }

        private static SactInterfaceMemberComparison CloneMember(SactInterfaceMemberComparison member) => new SactInterfaceMemberComparison
        {
            Section = member.Section,
            Name = member.Name,
            LeftDatatype = member.LeftDatatype,
            RightDatatype = member.RightDatatype,
            LeftStartValue = member.LeftStartValue,
            RightStartValue = member.RightStartValue,
            State = member.State
        };

        private static SactContentResult? CloneContent(SactContentResult? source)
        {
            if (source == null) return null;

            var result = new SactContentResult { State = source.State };
            foreach (KeyValuePair<string, SactNetworkResult> entry in source.Networks)
            {
                result.Networks[entry.Key] = CloneNetwork(entry.Value);
            }

            return result;
        }

        private static SactNetworkResult CloneNetwork(SactNetworkResult network)
        {
            var result = new SactNetworkResult
            {
                State = network.State,
                Title = network.Title,
                Comment = network.Comment,
                Number = new SactNumberPair { Left = network.Number.Left, Right = network.Number.Right },
                Body = CloneComponentMap(network.Body),
                LeftBody = CloneComponentMap(network.LeftBody),
                RightBody = CloneComponentMap(network.RightBody)
            };

            return result;
        }

        private static Dictionary<string, SactComponentData> CloneComponentMap(Dictionary<string, SactComponentData> source)
        {
            var result = new Dictionary<string, SactComponentData>(source.Count);
            foreach (KeyValuePair<string, SactComponentData> entry in source)
            {
                result[entry.Key] = CloneComponent(entry.Value);
            }

            return result;
        }

        private static SactComponentData CloneComponent(SactComponentData component) => new SactComponentData
        {
            Name = component.Name,
            UId = component.UId,
            State = component.State,
            IsStartElement = component.IsStartElement,
            Negated = component.Negated,
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
            InputParameters = component.InputParameters.Select(CloneParameter).ToList(),
            OutputParameters = component.OutputParameters.Select(CloneParameter).ToList(),
            OutputConnectors = component.OutputConnectors.Select(CloneConnector).ToList(),
            InputConnectors = component.InputConnectors.Select(CloneConnector).ToList()
        };

        private static SactParameterData CloneParameter(SactParameterData parameter) => new SactParameterData
        {
            Name = parameter.Name,
            Section = parameter.Section,
            Type = parameter.Type,
            Operand = parameter.Operand,
            IsVisible = parameter.IsVisible
        };

        private static SactConnectorData CloneConnector(SactConnectorData connector) => new SactConnectorData
        {
            UId = connector.UId,
            PinName = connector.PinName,
            PartnerUId = connector.PartnerUId
        };

        private static Dictionary<string, object>? CloneAttributeMap(Dictionary<string, object>? source)
        {
            if (source == null) return null;

            var result = new Dictionary<string, object>(source.Count);
            foreach (KeyValuePair<string, object> entry in source)
            {
                result[entry.Key] = ToInvariantValue(entry.Value);
            }

            return result;
        }

        /// <summary>
        /// Copies primitive/string attribute values as-is; anything else (nested dictionaries, lists, or any
        /// other mutable reference type the legacy mapper may have stashed here) is converted to an invariant,
        /// immutable string representation so no shared mutable reference can ever leak out of the clone.
        /// </summary>
        private static object ToInvariantValue(object? value)
        {
            switch (value)
            {
                case null: return string.Empty;
                case string s: return s;
                case bool b: return b;
                case int i: return i;
                case long l: return l;
                case double d: return d;
                case decimal m: return m;
                default: return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            }
        }
    }
}
