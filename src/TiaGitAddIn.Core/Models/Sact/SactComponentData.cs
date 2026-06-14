using System.Collections.Generic;

namespace TiaGitAddIn.Models.Sact
{
    public sealed class SactComponentData
    {
        public string Name { get; set; } = string.Empty;
        public string UId { get; set; } = string.Empty;
        public CompareState State { get; set; }
        public bool? IsStartElement { get; set; }
        public bool? Negated { get; set; }
        public string? DisplayName { get; set; }
        public string? TemplateType { get; set; }
        public string? Comment { get; set; }
        public string? Equation { get; set; }
        public bool DisabledENO { get; set; }
        public List<string> InvisiblePins { get; set; } = new List<string>();
        public List<string> NegatedPins { get; set; } = new List<string>();
        public SactOperandConnector? TopOperandConnector { get; set; }
        public List<SactParameterData> InputParameters { get; set; } = new List<SactParameterData>();
        public List<SactParameterData> OutputParameters { get; set; } = new List<SactParameterData>();
        public List<SactConnectorData> OutputConnectors { get; set; } = new List<SactConnectorData>();
        public List<SactConnectorData> InputConnectors { get; set; } = new List<SactConnectorData>();
    }
}
