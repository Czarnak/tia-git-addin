using System.Collections.Generic;

namespace TiaGitAddIn.Models.Sact
{
    public sealed class SactComponentData
    {
        public string name { get; set; } = string.Empty;
        public string uId { get; set; } = string.Empty;
        public CompareState State { get; set; }
        public bool? isStartElement { get; set; }
        public bool? negated { get; set; }
        public string? DisplayName { get; set; }
        public string? TemplateType { get; set; }
        public SactOperandConnector? TopOperandConnector { get; set; }
        public List<SactConnectorData> outputConnectors { get; set; } = new List<SactConnectorData>();
        public List<SactConnectorData> inputConnectors { get; set; } = new List<SactConnectorData>();
    }
}