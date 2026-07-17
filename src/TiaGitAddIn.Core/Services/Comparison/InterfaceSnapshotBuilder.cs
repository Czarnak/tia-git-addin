using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiaGitAddIn.Models.Comparison;
using TiaGitAddIn.Services.SimaticMl;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// Builds an immutable, fully-normalized <see cref="InterfaceSnapshot"/> from a parsed
    /// <see cref="BlockDefinition"/>. Implements the exact normalization matrix from the Task 8 brief: every
    /// field is normalized exactly once, here, so <see cref="InterfaceComparer"/> can compare already-clean
    /// values with plain ordinal equality.
    /// </summary>
    public static class InterfaceSnapshotBuilder
    {
        private static readonly string[] SemanticAttributeWhitelist =
        {
            "ExternalAccessible", "ExternalVisible", "ExternalWritable", "SetPoint"
        };

        private static readonly string[] AccessibilityCanonicalValues =
        {
            "Public", "Protected", "Private", "ReadOnly"
        };

        public static InterfaceSnapshot Build(BlockDefinition? block)
        {
            if (block == null)
            {
                return new InterfaceSnapshot(Array.Empty<InterfaceSectionSnapshot>());
            }

            var declaredByCanonicalName = new Dictionary<string, InterfaceSection>(StringComparer.OrdinalIgnoreCase);
            var extraSections = new List<InterfaceSection>();

            foreach (InterfaceSection section in block.InterfaceSections)
            {
                string? canonicalName = MatchCanonicalSectionName(section.Name);
                if (canonicalName != null)
                {
                    declaredByCanonicalName[canonicalName] = section;
                }
                else
                {
                    extraSections.Add(section);
                }
            }

            var sections = new List<InterfaceSectionSnapshot>();
            foreach (string canonicalName in InterfaceSectionOrder.Canonical)
            {
                sections.Add(declaredByCanonicalName.TryGetValue(canonicalName, out InterfaceSection? declared)
                    ? BuildSection(canonicalName, isPresent: true, declared.Members)
                    : BuildSection(canonicalName, isPresent: false, Array.Empty<InterfaceMember>()));
            }

            foreach (InterfaceSection extra in extraSections)
            {
                sections.Add(BuildSection(NormalizeName(extra.Name), isPresent: true, extra.Members));
            }

            return new InterfaceSnapshot(sections);
        }

        private static string? MatchCanonicalSectionName(string? rawName)
        {
            if (rawName == null) return null;
            string trimmed = rawName.Trim();
            foreach (string canonical in InterfaceSectionOrder.Canonical)
            {
                if (string.Equals(trimmed, canonical, StringComparison.OrdinalIgnoreCase))
                {
                    return canonical;
                }
            }

            return null;
        }

        private static InterfaceSectionSnapshot BuildSection(string name, bool isPresent, IReadOnlyList<InterfaceMember> members)
        {
            IEnumerable<InterfaceMemberSnapshot> memberSnapshots = members.Select(member => BuildMember(name, string.Empty, member));
            return new InterfaceSectionSnapshot(name, isPresent, memberSnapshots);
        }

        private static InterfaceMemberSnapshot BuildMember(string sectionName, string parentPath, InterfaceMember member)
        {
            string normalizedName = NormalizeName(member.Name);
            string path = parentPath.Length == 0 ? normalizedName : parentPath + "/" + normalizedName;

            IEnumerable<InterfaceMemberSnapshot> children = member.Children.Select(child => BuildMember(sectionName, path, child));

            return new InterfaceMemberSnapshot(
                section: sectionName,
                path: path,
                name: normalizedName,
                datatype: NormalizeDatatype(member.Datatype),
                retain: NormalizeRetain(member.Remanence),
                defaultValue: NormalizeMultilineValue(member.DefaultValue),
                startValue: NormalizeMultilineValue(member.StartValue),
                comments: NormalizeComments(member.Comments),
                accessibility: NormalizeAccessibility(member.Accessibility),
                informative: NormalizeInformative(member.Informative),
                version: member.Version?.Trim(),
                semanticAttributes: NormalizeSemanticAttributes(member.AttributeList),
                children: children);
        }

        private static string NormalizeName(string? rawName)
            => (rawName ?? string.Empty).Normalize(NormalizationForm.FormC);

        private static string NormalizeDatatype(string? rawDatatype)
            => (rawDatatype ?? string.Empty).Trim();

        /// <summary>CRLF/CR normalized to LF, then outer <see cref="string.Trim()"/>; every internal character preserved.</summary>
        private static string? NormalizeMultilineValue(string? raw)
        {
            if (raw == null) return null;
            return raw.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        }

        private static string? NormalizeAccessibility(string? raw)
        {
            if (raw == null) return null;
            string trimmed = raw.Trim();
            foreach (string canonical in AccessibilityCanonicalValues)
            {
                if (string.Equals(trimmed, canonical, StringComparison.OrdinalIgnoreCase))
                {
                    return canonical;
                }
            }

            return trimmed;
        }

        private static SemanticBoolean NormalizeRetain(string? raw)
        {
            string trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0) return SemanticBoolean.Unspecified;
            if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "retain", StringComparison.OrdinalIgnoreCase))
            {
                return SemanticBoolean.True;
            }

            if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "nonretain", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "non-retain", StringComparison.OrdinalIgnoreCase))
            {
                return SemanticBoolean.False;
            }

            return SemanticBoolean.Unspecified;
        }

        private static SemanticBoolean NormalizeInformative(bool? raw) => raw switch
        {
            null => SemanticBoolean.Unspecified,
            true => SemanticBoolean.True,
            false => SemanticBoolean.False
        };

        /// <summary>
        /// Keys by normalized (NFC, ordinal) language code. Values: line endings normalized to LF, then
        /// trailing spaces/tabs removed from each line -- leading and internal whitespace on every line is
        /// preserved exactly.
        /// </summary>
        private static IReadOnlyDictionary<string, string> NormalizeComments(IReadOnlyDictionary<string, string> rawComments)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> entry in rawComments)
            {
                string key = NormalizeName(entry.Key);
                result[key] = NormalizeCommentValue(entry.Value);
            }

            return result;
        }

        private static string NormalizeCommentValue(string raw)
        {
            string lineEndingsNormalized = raw.Replace("\r\n", "\n").Replace('\r', '\n');
            string[] lines = lineEndingsNormalized.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimEnd(' ', '\t');
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Filters to the whitelist (<see cref="SemanticAttributeWhitelist"/>); normalizes true/1 -&gt; "true",
        /// false/0 -&gt; "false", otherwise preserves the trimmed text exactly. Every non-whitelisted attribute
        /// (UId, composition IDs/names, timestamps, export order/date/version, document version, order,
        /// ordinal, ...) is ignored.
        /// </summary>
        private static IReadOnlyDictionary<string, string> NormalizeSemanticAttributes(IReadOnlyDictionary<string, string?> attributeList)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string name in SemanticAttributeWhitelist)
            {
                if (attributeList.TryGetValue(name, out string? raw) && raw != null)
                {
                    result[name] = NormalizeSemanticAttributeValue(raw);
                }
            }

            return result;
        }

        private static string NormalizeSemanticAttributeValue(string raw)
        {
            string trimmed = raw.Trim();
            if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) || trimmed == "1") return "true";
            if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) || trimmed == "0") return "false";
            return trimmed;
        }
    }
}
