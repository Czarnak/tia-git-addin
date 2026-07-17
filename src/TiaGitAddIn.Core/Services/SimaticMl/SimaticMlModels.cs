using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("TiaGitAddIn.Tests")]

namespace TiaGitAddIn.Services.SimaticMl
{
    /// <summary>
    /// Shared, ordinal-compared empty read-only collection singletons. Every model property below defaults
    /// to one of these instead of allocating a fresh empty collection per instance, and instead of ever
    /// exposing <c>null</c> where a caller would reasonably expect an empty collection.
    /// </summary>
    internal static class SimaticMlEmptyCollections
    {
        public static readonly IReadOnlyDictionary<string, string?> NullableStringMap =
            new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>(StringComparer.Ordinal));

        public static readonly IReadOnlyDictionary<string, string> StringMap =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal));
    }

    public sealed class SimaticMlFile
    {
        public string? EngineeringVersion { get; internal set; }
        public IReadOnlyList<BlockDefinition> Blocks { get; internal set; } = Array.Empty<BlockDefinition>();
    }

    public sealed class BlockDefinition
    {
        public string? XmlElementName { get; internal set; }
        public string? BlockKind { get; internal set; }
        public string? Id { get; internal set; }

        public string? Name { get; internal set; }
        public string? Namespace { get; internal set; }
        public int? Number { get; internal set; }
        public string? ProgrammingLanguage { get; internal set; }
        public string? MemoryLayout { get; internal set; }

        public IReadOnlyDictionary<string, string?> RawAttributes { get; internal set; } = SimaticMlEmptyCollections.NullableStringMap;
        public IReadOnlyList<InterfaceSection> InterfaceSections { get; internal set; } = Array.Empty<InterfaceSection>();
        public IReadOnlyList<MultilingualTextDefinition> Texts { get; internal set; } = Array.Empty<MultilingualTextDefinition>();
        public IReadOnlyList<CompileUnitDefinition> CompileUnits { get; internal set; } = Array.Empty<CompileUnitDefinition>();
    }

    public sealed class InterfaceSection
    {
        public string Name { get; internal set; } = "";
        public IReadOnlyList<InterfaceMember> Members { get; internal set; } = Array.Empty<InterfaceMember>();
    }

    public sealed class InterfaceMember
    {
        public string Name { get; internal set; } = "";
        public string Datatype { get; internal set; } = "";
        public string? Version { get; internal set; }
        public string? Remanence { get; internal set; }
        public string? Accessibility { get; internal set; }
        public bool? Informative { get; internal set; }
        public string? StartValue { get; internal set; }
        public string? CommentRawXml { get; internal set; }

        public IReadOnlyDictionary<string, string?> RawAttributes { get; internal set; } = SimaticMlEmptyCollections.NullableStringMap;
        public IReadOnlyDictionary<string, string?> AttributeList { get; internal set; } = SimaticMlEmptyCollections.NullableStringMap;
        public IReadOnlyList<InterfaceMember> Children { get; internal set; } = Array.Empty<InterfaceMember>();
    }

    public sealed class CompileUnitDefinition
    {
        public string? Id { get; internal set; }
        public string? CompositionName { get; internal set; }
        public string? ProgrammingLanguage { get; internal set; }

        public IReadOnlyDictionary<string, string?> RawAttributes { get; internal set; } = SimaticMlEmptyCollections.NullableStringMap;
        public NetworkSourceDefinition? Network { get; internal set; }
        public IReadOnlyList<MultilingualTextDefinition> Texts { get; internal set; } = Array.Empty<MultilingualTextDefinition>();
    }

    public sealed class NetworkSourceDefinition
    {
        public string? Format { get; internal set; }
        public string? RawXml { get; internal set; }

        public IReadOnlyList<AccessDefinition> Accesses { get; internal set; } = Array.Empty<AccessDefinition>();
        public IReadOnlyList<PartDefinition> Parts { get; internal set; } = Array.Empty<PartDefinition>();
        public IReadOnlyList<CallDefinition> Calls { get; internal set; } = Array.Empty<CallDefinition>();
        public IReadOnlyList<OpenbranchDefinition> Openbranches { get; internal set; } = Array.Empty<OpenbranchDefinition>();
        public PowerrailDefinition? Powerrail { get; internal set; }
        public IReadOnlyList<WireDefinition> Wires { get; internal set; } = Array.Empty<WireDefinition>();
    }

    public sealed class AccessDefinition
    {
        public int? UId { get; internal set; }
        public string? Scope { get; internal set; }

        public string? SymbolPath { get; internal set; }
        public IReadOnlyList<string> SymbolComponents { get; internal set; } = Array.Empty<string>();
        public IReadOnlyList<AccessComponentDefinition> Components { get; internal set; } = Array.Empty<AccessComponentDefinition>();

        public string? ConstantType { get; internal set; }
        public string? ConstantValue { get; internal set; }

        public string? RawXml { get; internal set; }
    }

    public sealed class AccessComponentDefinition
    {
        public string Name { get; internal set; } = string.Empty;
        public string? AccessModifier { get; internal set; }
        public string? SliceAccessModifier { get; internal set; }
        public string? SimpleAccessModifier { get; internal set; }
        public IReadOnlyDictionary<string, string?> RawAttributes { get; internal set; } = SimaticMlEmptyCollections.NullableStringMap;
    }

    public sealed class PartDefinition
    {
        public int? UId { get; internal set; }
        public string? Name { get; internal set; }
        public string? Version { get; internal set; }
        public bool DisabledENO { get; internal set; }

        public IReadOnlyDictionary<string, string> Attributes { get; internal set; } = SimaticMlEmptyCollections.StringMap;
        public IReadOnlyList<TemplateValueDefinition> TemplateValues { get; internal set; } = Array.Empty<TemplateValueDefinition>();
        public IReadOnlyList<string> AutomaticTyped { get; internal set; } = Array.Empty<string>();
        public IReadOnlyList<string> Negated { get; internal set; } = Array.Empty<string>();
        public IReadOnlyList<string> Invisible { get; internal set; } = Array.Empty<string>();

        public string? Equation { get; internal set; }
        public string? CommentRawXml { get; internal set; }
        public string? CommentText { get; internal set; }
        public InstanceDefinition? Instance { get; internal set; }

        public string? RawXml { get; internal set; }
    }

    public sealed class CallDefinition
    {
        public int? UId { get; internal set; }
        public CallInfoDefinition? CallInfo { get; internal set; }

        public IReadOnlyList<TemplateValueDefinition> TemplateValues { get; internal set; } = Array.Empty<TemplateValueDefinition>();
        public IReadOnlyList<string> AutomaticTyped { get; internal set; } = Array.Empty<string>();
        public string? CommentRawXml { get; internal set; }
        public string? CommentText { get; internal set; }
        public InstanceDefinition? Instance { get; internal set; }

        public string? RawXml { get; internal set; }
    }

    public sealed class InstanceDefinition
    {
        public string? Name { get; internal set; }
        public string? Scope { get; internal set; }
        public int? UId { get; internal set; }
        public string? RawXml { get; internal set; }
    }

    public sealed class CallInfoDefinition
    {
        public string? Name { get; internal set; }
        public string? BlockType { get; internal set; }
        public string? Instance { get; internal set; }
        public IReadOnlyList<CallParameterDefinition> Parameters { get; internal set; } = Array.Empty<CallParameterDefinition>();
    }

    public sealed class CallParameterDefinition
    {
        public string? Name { get; internal set; }
        public string? Section { get; internal set; }
        public string? Type { get; internal set; }
        public string? TemplateReference { get; internal set; }
        public bool? Informative { get; internal set; }
    }

    public sealed class PowerrailDefinition
    {
        public string? RawXml { get; internal set; }
    }

    public sealed class OpenbranchDefinition
    {
        public string? RawXml { get; internal set; }
    }

    public sealed class TemplateValueDefinition
    {
        public string? Name { get; internal set; }
        public string? Type { get; internal set; }
        public string? Value { get; internal set; }
    }

    public sealed class WireDefinition
    {
        public int? UId { get; internal set; }
        public IReadOnlyList<ConnectionDefinition> Connections { get; internal set; } = Array.Empty<ConnectionDefinition>();
        public string? RawXml { get; internal set; }
    }

    public abstract class ConnectionDefinition
    {
        public string? Kind { get; internal set; }
    }

    public sealed class NameConDefinition : ConnectionDefinition
    {
        public int? UId { get; internal set; }
        public string? Name { get; internal set; }
    }

    public sealed class IdentConDefinition : ConnectionDefinition
    {
        public int? UId { get; internal set; }
    }

    public sealed class OpenConDefinition : ConnectionDefinition
    {
        public int? UId { get; internal set; }
    }

    public sealed class PowerrailConDefinition : ConnectionDefinition
    {
    }

    public sealed class OpenbranchConDefinition : ConnectionDefinition
    {
    }

    public sealed class MultilingualTextDefinition
    {
        public string? Id { get; internal set; }
        public string? CompositionName { get; internal set; }
        public IReadOnlyList<MultilingualTextItemDefinition> Items { get; internal set; } = Array.Empty<MultilingualTextItemDefinition>();
    }

    public sealed class MultilingualTextItemDefinition
    {
        public string? Id { get; internal set; }
        public string? Culture { get; internal set; }
        public string? Text { get; internal set; }
    }
}
