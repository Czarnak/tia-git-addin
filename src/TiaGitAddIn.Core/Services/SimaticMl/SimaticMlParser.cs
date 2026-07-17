using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.SimaticMl
{
    /// <summary>
    /// Domain extraction half of the SimaticML parser (block/interface/network/wire structure). The safe
    /// XML-loading boundary (DTD/entity rejection, character/element/depth limits, cancellation) lives in the
    /// other half of this <c>partial class</c>, <see cref="SimaticMlReader"/>'s counterpart file.
    /// </summary>
    public static partial class SimaticMlParser
    {
        /// <summary>
        /// Legacy, path-based entry point. Kept only for existing callers (e.g. <c>SactService</c>); it reads
        /// the file with a bounded <see cref="StreamReader"/>, delegates to the safe <see cref="ParseText"/>
        /// overload using the default limits, and throws <see cref="InvalidDataException"/> for legacy callers
        /// when the safe parse does not succeed. New comparison strategies should call <see cref="ParseText"/>
        /// directly and inspect <see cref="SimaticMlParseResult.IsSuccess"/> instead of catching exceptions.
        /// </summary>
        public static SimaticMlFile Parse(string xmlPath)
        {
            if (xmlPath == null) throw new ArgumentNullException(nameof(xmlPath));
            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException("SimaticML file not found.", xmlPath);
            }

            SimaticMlParserLimits limits = SimaticMlParserLimits.Default;

            var fileInfo = new FileInfo(xmlPath);
            if (fileInfo.Length > limits.MaximumCharactersInDocument)
            {
                throw new InvalidDataException("SimaticML document exceeds the maximum supported size.");
            }

            string xml;
            using (var streamReader = new StreamReader(xmlPath))
            {
                xml = streamReader.ReadToEnd();
            }

            SimaticMlParseResult result = ParseText(xml, limits, PlcRevisionSide.Left, CancellationToken.None);

            if (!result.IsSuccess || result.Model == null)
            {
                string reason = result.Diagnostics.Count > 0 ? result.Diagnostics[0].Code : "unknown";
                throw new InvalidDataException($"SimaticML document could not be parsed safely ({reason}).");
            }

            return result.Model;
        }

        private static BlockDefinition ParseBlock(XElement blockElement)
        {
            XElement? attr = Child(blockElement, "AttributeList");

            XElement? interfaceElement = Child(attr, "Interface");
            List<InterfaceSection> interfaceSections = ParseInterface(interfaceElement);

            XElement? objectList = Child(blockElement, "ObjectList");
            List<MultilingualTextDefinition> texts = ParseDirectMultilingualTexts(objectList);

            var compileUnits = new List<CompileUnitDefinition>();
            foreach (XElement compileUnit in Children(objectList, "SW.Blocks.CompileUnit"))
            {
                compileUnits.Add(ParseCompileUnit(compileUnit));
            }

            return new BlockDefinition
            {
                XmlElementName = blockElement.Name.LocalName,
                BlockKind = blockElement.Name.LocalName.Replace("SW.Blocks.", ""),
                Id = Attr(blockElement, "ID"),
                RawAttributes = ReadScalarChildren(attr),
                Name = Value(attr, "Name"),
                Namespace = Value(attr, "Namespace"),
                Number = IntValue(attr, "Number"),
                ProgrammingLanguage = Value(attr, "ProgrammingLanguage"),
                MemoryLayout = Value(attr, "MemoryLayout"),
                InterfaceSections = interfaceSections.ToArray(),
                Texts = texts.ToArray(),
                CompileUnits = compileUnits.ToArray(),
            };
        }

        private static List<InterfaceSection> ParseInterface(XElement? interfaceElement)
        {
            var sections = new List<InterfaceSection>();

            if (interfaceElement == null)
            {
                return sections;
            }

            XElement? sectionsRoot = Descendants(interfaceElement, "Sections").FirstOrDefault();

            if (sectionsRoot == null)
            {
                return sections;
            }

            foreach (XElement sectionElement in Children(sectionsRoot, "Section"))
            {
                var members = new List<InterfaceMember>();
                foreach (XElement memberElement in Children(sectionElement, "Member"))
                {
                    members.Add(ParseMember(memberElement));
                }

                sections.Add(new InterfaceSection
                {
                    Name = Attr(sectionElement, "Name") ?? "(unnamed)",
                    Members = members.ToArray(),
                });
            }

            return sections;
        }

        private static InterfaceMember ParseMember(XElement memberElement)
        {
            XElement? attributeList = Child(memberElement, "AttributeList");
            IReadOnlyDictionary<string, string?> attributeListMap = ReadScalarChildren(attributeList);

            XElement? comment = Child(memberElement, "Comment");
            string? commentRawXml = comment?.ToString(SaveOptions.DisableFormatting);

            var children = new List<InterfaceMember>();
            foreach (XElement childMember in Children(memberElement, "Member"))
            {
                children.Add(ParseMember(childMember));
            }

            return new InterfaceMember
            {
                Name = Attr(memberElement, "Name") ?? "(unnamed)",
                Datatype = Attr(memberElement, "Datatype") ?? "(unknown)",
                Version = Attr(memberElement, "Version"),
                Remanence = Attr(memberElement, "Remanence"),
                Accessibility = Attr(memberElement, "Accessibility"),
                Informative = BoolAttr(memberElement, "Informative"),
                StartValue = Value(memberElement, "StartValue"),
                RawAttributes = ToReadOnlyMap(memberElement.Attributes()
                    .ToDictionary(a => a.Name.LocalName, a => (string?)a.Value, StringComparer.Ordinal)),
                AttributeList = attributeListMap,
                CommentRawXml = commentRawXml,
                Children = children.ToArray(),
            };
        }

        private static CompileUnitDefinition ParseCompileUnit(XElement compileUnitElement)
        {
            XElement? attr = Child(compileUnitElement, "AttributeList");
            XElement? networkSource = Child(attr, "NetworkSource");
            NetworkSourceDefinition? network = networkSource != null ? ParseNetworkSource(networkSource) : null;

            XElement? objectList = Child(compileUnitElement, "ObjectList");
            List<MultilingualTextDefinition> texts = ParseDirectMultilingualTexts(objectList);

            return new CompileUnitDefinition
            {
                Id = Attr(compileUnitElement, "ID"),
                CompositionName = Attr(compileUnitElement, "CompositionName"),
                ProgrammingLanguage = Value(attr, "ProgrammingLanguage"),
                RawAttributes = ReadScalarChildren(attr),
                Network = network,
                Texts = texts.ToArray(),
            };
        }

        private static NetworkSourceDefinition? ParseNetworkSource(XElement networkSource)
        {
            XElement? flgNet = Descendants(networkSource, "FlgNet").FirstOrDefault();

            if (flgNet == null)
            {
                return new NetworkSourceDefinition
                {
                    Format = "Unknown",
                    RawXml = networkSource.ToString(SaveOptions.DisableFormatting),
                };
            }

            var accesses = new List<AccessDefinition>();
            var parts = new List<PartDefinition>();
            var calls = new List<CallDefinition>();

            XElement? partsRoot = Child(flgNet, "Parts");
            if (partsRoot != null)
            {
                foreach (XElement node in partsRoot.Elements())
                {
                    switch (node.Name.LocalName)
                    {
                        case "Access":
                            accesses.Add(ParseAccess(node));
                            break;

                        case "Part":
                            parts.Add(ParsePart(node));
                            break;

                        case "Call":
                            calls.Add(ParseCall(node));
                            break;
                    }
                }
            }

            var wires = new List<WireDefinition>();
            XElement? wiresRoot = Child(flgNet, "Wires");
            if (wiresRoot != null)
            {
                foreach (XElement wireElement in Children(wiresRoot, "Wire"))
                {
                    wires.Add(ParseWire(wireElement));
                }
            }

            // FlgNet can directly contain Openbranch and Powerrail.
            var openbranches = new List<OpenbranchDefinition>();
            PowerrailDefinition? powerrail = null;
            foreach (XElement node in flgNet.Elements())
            {
                switch (node.Name.LocalName)
                {
                    case "Openbranch":
                        openbranches.Add(new OpenbranchDefinition { RawXml = node.ToString(SaveOptions.DisableFormatting) });
                        break;
                    case "Powerrail":
                        powerrail = new PowerrailDefinition { RawXml = node.ToString(SaveOptions.DisableFormatting) };
                        break;
                }
            }

            return new NetworkSourceDefinition
            {
                Format = "FlgNet",
                RawXml = flgNet.ToString(SaveOptions.DisableFormatting),
                Accesses = accesses.ToArray(),
                Parts = parts.ToArray(),
                Calls = calls.ToArray(),
                Wires = wires.ToArray(),
                Openbranches = openbranches.ToArray(),
                Powerrail = powerrail,
            };
        }

        private static AccessDefinition ParseAccess(XElement accessElement)
        {
            XElement? constant = Child(accessElement, "Constant");
            XElement? symbol = Child(accessElement, "Symbol");

            List<AccessComponentDefinition> components = symbol != null
                ? Descendants(symbol, "Component")
                    .Select(ParseAccessComponent)
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                    .ToList()
                : new List<AccessComponentDefinition>();

            List<string> symbolComponents = components.Select(c => c.Name).ToList();
            string? symbolPath = symbolComponents.Count > 0 ? string.Join(".", symbolComponents) : null;

            return new AccessDefinition
            {
                UId = IntAttr(accessElement, "UId"),
                Scope = Attr(accessElement, "Scope"),
                ConstantType = Value(constant, "ConstantType"),
                ConstantValue = Value(constant, "ConstantValue"),
                RawXml = accessElement.ToString(SaveOptions.DisableFormatting),
                Components = components.ToArray(),
                SymbolComponents = symbolComponents.ToArray(),
                SymbolPath = symbolPath,
            };
        }

        private static AccessComponentDefinition ParseAccessComponent(XElement componentElement)
        {
            return new AccessComponentDefinition
            {
                Name = Attr(componentElement, "Name") ?? string.Empty,
                AccessModifier = Attr(componentElement, "AccessModifier"),
                SliceAccessModifier = Attr(componentElement, "SliceAccessModifier"),
                SimpleAccessModifier = Attr(componentElement, "SimpleAccessModifier"),
                RawAttributes = ToReadOnlyMap(componentElement.Attributes()
                    .ToDictionary(a => a.Name.LocalName, a => (string?)a.Value, StringComparer.Ordinal)),
            };
        }

        private static PartDefinition ParsePart(XElement partElement)
        {
            var templateValues = new List<TemplateValueDefinition>();
            foreach (XElement tv in Children(partElement, "TemplateValue"))
            {
                templateValues.Add(new TemplateValueDefinition
                {
                    Name = Attr(tv, "Name"),
                    Type = Attr(tv, "Type"),
                    Value = tv.Value?.Trim(),
                });
            }

            var automaticTyped = new List<string>();
            foreach (XElement at in Children(partElement, "AutomaticTyped"))
            {
                automaticTyped.Add(Attr(at, "Name") ?? "");
            }

            var negated = new List<string>();
            foreach (XElement negatedElement in Children(partElement, "Negated"))
            {
                AddNamedPin(negated, negatedElement);
            }

            var invisible = new List<string>();
            foreach (XElement invisibleElement in Children(partElement, "Invisible"))
            {
                AddNamedPin(invisible, invisibleElement);
            }

            XElement? comment = Child(partElement, "Comment");
            string? commentRawXml = comment?.ToString(SaveOptions.DisableFormatting);
            string? commentText = comment != null ? ReadTextContent(comment) : null;

            return new PartDefinition
            {
                UId = IntAttr(partElement, "UId"),
                Name = Attr(partElement, "Name"),
                Version = Attr(partElement, "Version"),
                DisabledENO = BoolAttr(partElement, "DisabledENO") ?? false,
                Attributes = new ReadOnlyDictionary<string, string>(
                    partElement.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value, StringComparer.Ordinal)),
                Equation = Value(partElement, "Equation"),
                RawXml = partElement.ToString(SaveOptions.DisableFormatting),
                TemplateValues = templateValues.ToArray(),
                AutomaticTyped = automaticTyped.ToArray(),
                Negated = negated.ToArray(),
                Invisible = invisible.ToArray(),
                CommentRawXml = commentRawXml,
                CommentText = commentText,
                Instance = ParseInstance(Child(partElement, "Instance")),
            };
        }

        private static CallDefinition ParseCall(XElement callElement)
        {
            CallInfoDefinition? callInfo = ParseCallInfo(Child(callElement, "CallInfo"));

            var templateValues = new List<TemplateValueDefinition>();
            foreach (XElement tv in Children(callElement, "TemplateValue"))
            {
                templateValues.Add(new TemplateValueDefinition
                {
                    Name = Attr(tv, "Name"),
                    Type = Attr(tv, "Type"),
                    Value = tv.Value?.Trim(),
                });
            }

            var automaticTyped = new List<string>();
            foreach (XElement at in Children(callElement, "AutomaticTyped"))
            {
                automaticTyped.Add(Attr(at, "Name") ?? "");
            }

            XElement? comment = Child(callElement, "Comment");
            string? commentRawXml = comment?.ToString(SaveOptions.DisableFormatting);
            string? commentText = comment != null ? ReadTextContent(comment) : null;

            return new CallDefinition
            {
                UId = IntAttr(callElement, "UId"),
                RawXml = callElement.ToString(SaveOptions.DisableFormatting),
                CallInfo = callInfo,
                TemplateValues = templateValues.ToArray(),
                AutomaticTyped = automaticTyped.ToArray(),
                CommentRawXml = commentRawXml,
                CommentText = commentText,
                Instance = ParseInstance(Child(callElement, "Instance")),
            };
        }

        private static CallInfoDefinition? ParseCallInfo(XElement? callInfoElement)
        {
            if (callInfoElement == null)
            {
                return null;
            }

            var parameters = new List<CallParameterDefinition>();
            foreach (XElement parameter in Children(callInfoElement, "Parameter"))
            {
                parameters.Add(new CallParameterDefinition
                {
                    Name = Attr(parameter, "Name"),
                    Section = Attr(parameter, "Section"),
                    Type = Attr(parameter, "Type"),
                    TemplateReference = Attr(parameter, "TemplateReference"),
                    Informative = BoolAttr(parameter, "Informative"),
                });
            }

            return new CallInfoDefinition
            {
                Name = Attr(callInfoElement, "Name"),
                BlockType = Attr(callInfoElement, "BlockType"),
                Instance = Attr(callInfoElement, "Instance"),
                Parameters = parameters.ToArray(),
            };
        }

        private static void AddNamedPin(List<string> target, XElement pinElement)
        {
            string? name = Attr(pinElement, "Name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                target.Add(name!);
            }
        }

        private static InstanceDefinition? ParseInstance(XElement? instanceElement)
        {
            if (instanceElement == null)
            {
                return null;
            }

            return new InstanceDefinition
            {
                Name = Attr(instanceElement, "Name"),
                Scope = Attr(instanceElement, "Scope"),
                UId = IntAttr(instanceElement, "UId"),
                RawXml = instanceElement.ToString(SaveOptions.DisableFormatting),
            };
        }

        private static string? ReadTextContent(XElement element)
        {
            string text = string.Join(
                " ",
                element
                    .DescendantNodes()
                    .OfType<XText>()
                    .Select(t => t.Value.Trim())
                    .Where(v => !string.IsNullOrWhiteSpace(v)));

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static WireDefinition ParseWire(XElement wireElement)
        {
            var connections = new List<ConnectionDefinition>();

            foreach (XElement connectionElement in wireElement.Elements())
            {
                ConnectionDefinition connection;
                switch (connectionElement.Name.LocalName)
                {
                    case "NameCon":
                        connection = new NameConDefinition { Name = Attr(connectionElement, "Name"), UId = IntAttr(connectionElement, "UId") };
                        break;
                    case "IdentCon":
                        connection = new IdentConDefinition { UId = IntAttr(connectionElement, "UId") };
                        break;
                    case "OpenCon":
                        connection = new OpenConDefinition { UId = IntAttr(connectionElement, "UId") };
                        break;
                    case "Powerrail":
                        connection = new PowerrailConDefinition();
                        break;
                    case "Openbranch":
                        connection = new OpenbranchConDefinition();
                        break;
                    default:
                        // Fallback for unknown connection types.
                        connection = new IdentConDefinition { UId = IntAttr(connectionElement, "UId") };
                        break;
                }

                connection.Kind = connectionElement.Name.LocalName;
                connections.Add(connection);
            }

            return new WireDefinition
            {
                UId = IntAttr(wireElement, "UId"),
                RawXml = wireElement.ToString(SaveOptions.DisableFormatting),
                Connections = connections.ToArray(),
            };
        }

        private static List<MultilingualTextDefinition> ParseDirectMultilingualTexts(XElement? objectList)
        {
            var result = new List<MultilingualTextDefinition>();

            if (objectList == null)
            {
                return result;
            }

            foreach (XElement textElement in Children(objectList, "MultilingualText"))
            {
                var items = new List<MultilingualTextItemDefinition>();
                foreach (XElement item in Descendants(textElement, "MultilingualTextItem"))
                {
                    XElement? attr = Child(item, "AttributeList");

                    items.Add(new MultilingualTextItemDefinition
                    {
                        Id = Attr(item, "ID"),
                        Culture = Value(attr, "Culture"),
                        Text = Value(attr, "Text") ?? "",
                    });
                }

                result.Add(new MultilingualTextDefinition
                {
                    Id = Attr(textElement, "ID"),
                    CompositionName = Attr(textElement, "CompositionName"),
                    Items = items.ToArray(),
                });
            }

            return result;
        }

        private static IReadOnlyDictionary<string, string?> ReadScalarChildren(XElement? element)
        {
            var result = new Dictionary<string, string?>(StringComparer.Ordinal);

            if (element == null)
            {
                return ToReadOnlyMap(result);
            }

            foreach (XElement child in element.Elements())
            {
                if (!child.HasElements)
                {
                    string key = child.Name.LocalName;
                    // If it's a generic attribute type, use its 'Name' attribute as the key.
                    if (key.EndsWith("Attribute", StringComparison.Ordinal) || key == "Attribute")
                    {
                        string? nameAttr = Attr(child, "Name");
                        if (!string.IsNullOrEmpty(nameAttr))
                        {
                            key = nameAttr!;
                        }
                    }
                    result[key] = child.Value?.Trim();
                }
            }

            return ToReadOnlyMap(result);
        }

        private static IReadOnlyDictionary<string, string?> ToReadOnlyMap(Dictionary<string, string?> source)
            => new ReadOnlyDictionary<string, string?>(source);

        private static XElement? Child(XElement? element, string localName)
        {
            return element?
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName == localName);
        }

        private static IEnumerable<XElement> Children(XElement? element, string localName)
        {
            if (element == null)
            {
                return Enumerable.Empty<XElement>();
            }

            return element.Elements().Where(e => e.Name.LocalName == localName);
        }

        private static IEnumerable<XElement> Descendants(XElement? element, string localName)
        {
            if (element == null)
            {
                return Enumerable.Empty<XElement>();
            }

            return element.Descendants().Where(e => e.Name.LocalName == localName);
        }

        private static string? Attr(XElement? element, string name)
        {
            return element?.Attribute(name)?.Value;
        }

        private static int? IntAttr(XElement? element, string name)
        {
            string? raw = Attr(element, name);

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }

            return null;
        }

        private static string? Value(XElement? element, string localName)
        {
            return Child(element, localName)?.Value?.Trim();
        }

        private static int? IntValue(XElement? element, string localName)
        {
            string? raw = Value(element, localName);

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                return value;
            }

            return null;
        }

        private static bool? BoolAttr(XElement? element, string name)
        {
            string? raw = Attr(element, name);

            if (bool.TryParse(raw, out bool value))
            {
                return value;
            }

            return null;
        }
    }
}
