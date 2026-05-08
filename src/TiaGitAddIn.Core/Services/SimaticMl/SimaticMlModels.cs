using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaGitAddIn.Services.SimaticMl
{
    public sealed class SimaticMlFile
    {
        public string? EngineeringVersion { get; set; }
        public List<BlockDefinition> Blocks { get; set; } = new List<BlockDefinition>();
    }

    public sealed class BlockDefinition
    {
        public string? XmlElementName { get; set; }
        public string? BlockKind { get; set; }
        public string? Id { get; set; }

        public string? Name { get; set; }
        public string? Namespace { get; set; }
        public int? Number { get; set; }
        public string? ProgrammingLanguage { get; set; }
        public string? MemoryLayout { get; set; }

        public Dictionary<string, string?> RawAttributes { get; set; } = new Dictionary<string, string?>();
        public List<InterfaceSection> InterfaceSections { get; set; } = new List<InterfaceSection>();
        public List<MultilingualTextDefinition> Texts { get; set; } = new List<MultilingualTextDefinition>();
        public List<CompileUnitDefinition> CompileUnits { get; set; } = new List<CompileUnitDefinition>();
    }

    public sealed class InterfaceSection
    {
        public string Name { get; set; } = "";
        public List<InterfaceMember> Members { get; set; } = new List<InterfaceMember>();
    }

    public sealed class InterfaceMember
    {
        public string Name { get; set; } = "";
        public string Datatype { get; set; } = "";
        public string? Version { get; set; }
        public string? Remanence { get; set; }
        public string? Accessibility { get; set; }
        public bool? Informative { get; set; }
        public string? StartValue { get; set; }
        public string? CommentRawXml { get; set; }

        public Dictionary<string, string?> RawAttributes { get; set; } = new Dictionary<string, string?>();
        public Dictionary<string, string?> AttributeList { get; set; } = new Dictionary<string, string?>();
        public List<InterfaceMember> Children { get; set; } = new List<InterfaceMember>();
    }

    public sealed class CompileUnitDefinition
    {
        public string? Id { get; set; }
        public string? CompositionName { get; set; }
        public string? ProgrammingLanguage { get; set; }

        public Dictionary<string, string?> RawAttributes { get; set; } = new Dictionary<string, string?>();
        public NetworkSourceDefinition? Network { get; set; }
        public List<MultilingualTextDefinition> Texts { get; set; } = new List<MultilingualTextDefinition>();
    }

    public sealed class NetworkSourceDefinition
    {
        public string? Format { get; set; }
        public string? RawXml { get; set; }

        public List<AccessDefinition> Accesses { get; set; } = new List<AccessDefinition>();
        public List<PartDefinition> Parts { get; set; } = new List<PartDefinition>();
        public List<CallDefinition> Calls { get; set; } = new List<CallDefinition>();
        public List<OpenbranchDefinition> Openbranches { get; set; } = new List<OpenbranchDefinition>();
        public PowerrailDefinition? Powerrail { get; set; }
        public List<WireDefinition> Wires { get; set; } = new List<WireDefinition>();
    }

    public sealed class AccessDefinition
    {
        public int? UId { get; set; }
        public string? Scope { get; set; }

        public string? SymbolPath { get; set; }
        public List<string> SymbolComponents { get; set; } = new List<string>();
        public List<AccessComponentDefinition> Components { get; set; } = new List<AccessComponentDefinition>();

        public string? ConstantType { get; set; }
        public string? ConstantValue { get; set; }

        public string? RawXml { get; set; }
    }

    public sealed class AccessComponentDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string? AccessModifier { get; set; }
        public string? SliceAccessModifier { get; set; }
        public string? SimpleAccessModifier { get; set; }
        public Dictionary<string, string?> RawAttributes { get; set; } = new Dictionary<string, string?>();
    }

    public sealed class PartDefinition
    {
        public int? UId { get; set; }
        public string? Name { get; set; }
        public string? Version { get; set; }
        public bool DisabledENO { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
        public List<TemplateValueDefinition> TemplateValues { get; set; } = new List<TemplateValueDefinition>();
        public List<string> AutomaticTyped { get; set; } = new List<string>();
        public List<string> Negated { get; set; } = new List<string>();
        public List<string> Invisible { get; set; } = new List<string>();
        
        public string? Equation { get; set; }
        public string? CommentRawXml { get; set; }
        public string? CommentText { get; set; }
        public InstanceDefinition? Instance { get; set; }

        public string? RawXml { get; set; }
    }

    public sealed class CallDefinition
    {
        public int? UId { get; set; }
        public CallInfoDefinition? CallInfo { get; set; }
        
        public List<TemplateValueDefinition> TemplateValues { get; set; } = new List<TemplateValueDefinition>();
        public List<string> AutomaticTyped { get; set; } = new List<string>();
        public string? CommentRawXml { get; set; }
        public string? CommentText { get; set; }
        public InstanceDefinition? Instance { get; set; }

        public string? RawXml { get; set; }
    }

    public sealed class InstanceDefinition
    {
        public string? Name { get; set; }
        public string? Scope { get; set; }
        public int? UId { get; set; }
        public string? RawXml { get; set; }
    }

    public sealed class CallInfoDefinition
    {
        public string? Name { get; set; }
        public string? BlockType { get; set; }
        public string? Instance { get; set; }
        public List<CallParameterDefinition> Parameters { get; set; } = new List<CallParameterDefinition>();
    }

    public sealed class CallParameterDefinition
    {
        public string? Name { get; set; }
        public string? Section { get; set; }
        public string? Type { get; set; }
        public string? TemplateReference { get; set; }
        public bool? Informative { get; set; }
    }

    public sealed class PowerrailDefinition
    {
        public string? RawXml { get; set; }
    }

    public sealed class OpenbranchDefinition
    {
        public string? RawXml { get; set; }
    }

    public sealed class TemplateValueDefinition
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Value { get; set; }
    }

    public sealed class WireDefinition
    {
        public int? UId { get; set; }
        public List<ConnectionDefinition> Connections { get; set; } = new List<ConnectionDefinition>();
        public string? RawXml { get; set; }
    }

    public abstract class ConnectionDefinition
    {
        public string? Kind { get; set; }
    }

    public sealed class NameConDefinition : ConnectionDefinition
    {
        public int? UId { get; set; }
        public string? Name { get; set; }
    }

    public sealed class IdentConDefinition : ConnectionDefinition
    {
        public int? UId { get; set; }
    }

    public sealed class OpenConDefinition : ConnectionDefinition
    {
        public int? UId { get; set; }
    }

    public sealed class PowerrailConDefinition : ConnectionDefinition
    {
    }

    public sealed class OpenbranchConDefinition : ConnectionDefinition
    {
    }

    public sealed class MultilingualTextDefinition
    {
        public string? Id { get; set; }
        public string? CompositionName { get; set; }
        public List<MultilingualTextItemDefinition> Items { get; set; } = new List<MultilingualTextItemDefinition>();
    }

    public sealed class MultilingualTextItemDefinition
    {
        public string? Id { get; set; }
        public string? Culture { get; set; }
        public string? Text { get; set; }
    }
}
