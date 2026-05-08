using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TiaGitAddIn.Services.SimaticMl
{
    public static class SimaticMlParser
    {
        public static SimaticMlFile Parse(string xmlPath)
        {
            if (!File.Exists(xmlPath))
            {
                throw new FileNotFoundException("SimaticML file not found.", xmlPath);
            }

            XDocument doc = XDocument.Load(xmlPath, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            XElement root = doc.Root ?? throw new InvalidDataException("XML document has no root element.");

            var result = new SimaticMlFile
            {
                EngineeringVersion = Child(root, "Engineering")?.Attribute("version")?.Value
            };

            foreach (XElement blockElement in root.Elements().Where(e => e.Name.LocalName.StartsWith("SW.Blocks.", StringComparison.Ordinal)))
            {
                result.Blocks.Add(ParseBlock(blockElement));
            }

            return result;
        }

        private static BlockDefinition ParseBlock(XElement blockElement)
        {
            XElement? attr = Child(blockElement, "AttributeList");

            var block = new BlockDefinition
            {
                XmlElementName = blockElement.Name.LocalName,
                BlockKind = blockElement.Name.LocalName.Replace("SW.Blocks.", ""),
                Id = Attr(blockElement, "ID"),
                RawAttributes = ReadScalarChildren(attr)
            };

            block.Name = Value(attr, "Name");
            block.Namespace = Value(attr, "Namespace");
            block.Number = IntValue(attr, "Number");
            block.ProgrammingLanguage = Value(attr, "ProgrammingLanguage");
            block.MemoryLayout = Value(attr, "MemoryLayout");

            XElement? interfaceElement = Child(attr, "Interface");
            block.InterfaceSections.AddRange(ParseInterface(interfaceElement));

            XElement? objectList = Child(blockElement, "ObjectList");
            block.Texts.AddRange(ParseDirectMultilingualTexts(objectList));

            foreach (XElement compileUnit in Children(objectList, "SW.Blocks.CompileUnit"))
            {
                block.CompileUnits.Add(ParseCompileUnit(compileUnit));
            }

            return block;
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
                var section = new InterfaceSection
                {
                    Name = Attr(sectionElement, "Name") ?? "(unnamed)"
                };

                foreach (XElement memberElement in Children(sectionElement, "Member"))
                {
                    section.Members.Add(ParseMember(memberElement));
                }

                sections.Add(section);
            }

            return sections;
        }

        private static InterfaceMember ParseMember(XElement memberElement)
        {
            var member = new InterfaceMember
            {
                Name = Attr(memberElement, "Name") ?? "(unnamed)",
                Datatype = Attr(memberElement, "Datatype") ?? "(unknown)",
                Version = Attr(memberElement, "Version"),
                Remanence = Attr(memberElement, "Remanence"),
                Accessibility = Attr(memberElement, "Accessibility"),
                Informative = BoolAttr(memberElement, "Informative"),
                StartValue = Value(memberElement, "StartValue"),
                RawAttributes = memberElement.Attributes()
                    .ToDictionary(a => a.Name.LocalName, a => (string?)a.Value)
            };

            XElement? attributeList = Child(memberElement, "AttributeList");
            member.AttributeList = ReadScalarChildren(attributeList);

            XElement? comment = Child(memberElement, "Comment");
            if (comment != null)
            {
                member.CommentRawXml = comment.ToString(SaveOptions.DisableFormatting);
            }

            foreach (XElement childMember in Children(memberElement, "Member"))
            {
                member.Children.Add(ParseMember(childMember));
            }

            return member;
        }

        private static CompileUnitDefinition ParseCompileUnit(XElement compileUnitElement)
        {
            XElement? attr = Child(compileUnitElement, "AttributeList");

            var cu = new CompileUnitDefinition
            {
                Id = Attr(compileUnitElement, "ID"),
                CompositionName = Attr(compileUnitElement, "CompositionName"),
                ProgrammingLanguage = Value(attr, "ProgrammingLanguage"),
                RawAttributes = ReadScalarChildren(attr)
            };

            XElement? networkSource = Child(attr, "NetworkSource");

            if (networkSource != null)
            {
                cu.Network = ParseNetworkSource(networkSource);
            }

            XElement? objectList = Child(compileUnitElement, "ObjectList");
            cu.Texts.AddRange(ParseDirectMultilingualTexts(objectList));

            return cu;
        }

        private static NetworkSourceDefinition? ParseNetworkSource(XElement networkSource)
        {
            XElement? flgNet = Descendants(networkSource, "FlgNet").FirstOrDefault();

            if (flgNet == null)
            {
                return new NetworkSourceDefinition
                {
                    Format = "Unknown",
                    RawXml = networkSource.ToString(SaveOptions.DisableFormatting)
                };
            }

            var network = new NetworkSourceDefinition
            {
                Format = "FlgNet",
                RawXml = flgNet.ToString(SaveOptions.DisableFormatting)
            };

            XElement? partsRoot = Child(flgNet, "Parts");

            if (partsRoot != null)
            {
                foreach (XElement node in partsRoot.Elements())
                {
                    switch (node.Name.LocalName)
                    {
                        case "Access":
                            network.Accesses.Add(ParseAccess(node));
                            break;

                        case "Part":
                            network.Parts.Add(ParsePart(node));
                            break;

                        case "Call":
                            network.Calls.Add(ParseCall(node));
                            break;
                    }
                }
            }

            XElement? wiresRoot = Child(flgNet, "Wires");

            if (wiresRoot != null)
            {
                foreach (XElement wireElement in Children(wiresRoot, "Wire"))
                {
                    network.Wires.Add(ParseWire(wireElement));
                }
            }

            // FlgNet can directly contain Openbranch and Powerrail
            foreach (XElement node in flgNet.Elements())
            {
                 switch (node.Name.LocalName)
                 {
                     case "Openbranch":
                         network.Openbranches.Add(new OpenbranchDefinition { RawXml = node.ToString(SaveOptions.DisableFormatting) });
                         break;
                     case "Powerrail":
                         network.Powerrail = new PowerrailDefinition { RawXml = node.ToString(SaveOptions.DisableFormatting) };
                         break;
                 }
            }

            return network;
        }

        private static AccessDefinition ParseAccess(XElement accessElement)
        {
            XElement? constant = Child(accessElement, "Constant");
            XElement? symbol = Child(accessElement, "Symbol");

            var access = new AccessDefinition
            {
                UId = IntAttr(accessElement, "UId"),
                Scope = Attr(accessElement, "Scope"),
                ConstantType = Value(constant, "ConstantType"),
                ConstantValue = Value(constant, "ConstantValue"),
                RawXml = accessElement.ToString(SaveOptions.DisableFormatting)
            };

            if (symbol != null)
            {
                access.Components = Descendants(symbol, "Component")
                    .Select(ParseAccessComponent)
                    .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                    .ToList();
                access.SymbolComponents = access.Components
                    .Select(c => c.Name)
                    .ToList();

                access.SymbolPath = access.SymbolComponents.Count > 0
                    ? string.Join(".", access.SymbolComponents)
                    : null;
            }

            return access;
        }

        private static AccessComponentDefinition ParseAccessComponent(XElement componentElement)
        {
            return new AccessComponentDefinition
            {
                Name = Attr(componentElement, "Name") ?? string.Empty,
                AccessModifier = Attr(componentElement, "AccessModifier"),
                SliceAccessModifier = Attr(componentElement, "SliceAccessModifier"),
                SimpleAccessModifier = Attr(componentElement, "SimpleAccessModifier"),
                RawAttributes = componentElement.Attributes()
                    .ToDictionary(a => a.Name.LocalName, a => (string?)a.Value)
            };
        }

        private static PartDefinition ParsePart(XElement partElement)
        {
            var part = new PartDefinition
            {
                UId = IntAttr(partElement, "UId"),
                Name = Attr(partElement, "Name"),
                Version = Attr(partElement, "Version"),
                DisabledENO = BoolAttr(partElement, "DisabledENO") ?? false,
                Attributes = partElement.Attributes()
                    .ToDictionary(a => a.Name.LocalName, a => a.Value),
                Equation = Value(partElement, "Equation"),
                RawXml = partElement.ToString(SaveOptions.DisableFormatting)
            };

            foreach (XElement tv in Children(partElement, "TemplateValue"))
            {
                part.TemplateValues.Add(new TemplateValueDefinition
                {
                    Name = Attr(tv, "Name"),
                    Type = Attr(tv, "Type"),
                    Value = tv.Value?.Trim()
                });
            }

            foreach (XElement at in Children(partElement, "AutomaticTyped"))
            {
                part.AutomaticTyped.Add(Attr(at, "Name") ?? "");
            }

            foreach (XElement negated in Children(partElement, "Negated"))
            {
                AddNamedPin(part.Negated, negated);
            }

            foreach (XElement invisible in Children(partElement, "Invisible"))
            {
                AddNamedPin(part.Invisible, invisible);
            }

            XElement? comment = Child(partElement, "Comment");
            if (comment != null)
            {
                part.CommentRawXml = comment.ToString(SaveOptions.DisableFormatting);
                part.CommentText = ReadTextContent(comment);
            }

            part.Instance = ParseInstance(Child(partElement, "Instance"));

            return part;
        }

        private static CallDefinition ParseCall(XElement callElement)
        {
            var call = new CallDefinition
            {
                UId = IntAttr(callElement, "UId"),
                RawXml = callElement.ToString(SaveOptions.DisableFormatting)
            };

            XElement? callInfo = Child(callElement, "CallInfo");
            if (callInfo != null)
            {
                call.CallInfo = new CallInfoDefinition
                {
                    Name = Attr(callInfo, "Name"),
                    BlockType = Attr(callInfo, "BlockType"),
                    Instance = Attr(callInfo, "Instance")
                };

                foreach (XElement parameter in Children(callInfo, "Parameter"))
                {
                    call.CallInfo.Parameters.Add(new CallParameterDefinition
                    {
                        Name = Attr(parameter, "Name"),
                        Section = Attr(parameter, "Section"),
                        Type = Attr(parameter, "Type"),
                        TemplateReference = Attr(parameter, "TemplateReference"),
                        Informative = BoolAttr(parameter, "Informative")
                    });
                }
            }

            foreach (XElement tv in Children(callElement, "TemplateValue"))
            {
                call.TemplateValues.Add(new TemplateValueDefinition
                {
                    Name = Attr(tv, "Name"),
                    Type = Attr(tv, "Type"),
                    Value = tv.Value?.Trim()
                });
            }

            foreach (XElement at in Children(callElement, "AutomaticTyped"))
            {
                call.AutomaticTyped.Add(Attr(at, "Name") ?? "");
            }

            XElement? comment = Child(callElement, "Comment");
            if (comment != null)
            {
                call.CommentRawXml = comment.ToString(SaveOptions.DisableFormatting);
                call.CommentText = ReadTextContent(comment);
            }

            call.Instance = ParseInstance(Child(callElement, "Instance"));

            return call;
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
                RawXml = instanceElement.ToString(SaveOptions.DisableFormatting)
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
            var wire = new WireDefinition
            {
                UId = IntAttr(wireElement, "UId"),
                RawXml = wireElement.ToString(SaveOptions.DisableFormatting)
            };

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
                        // Fallback for unknown connection types
                        connection = new IdentConDefinition { UId = IntAttr(connectionElement, "UId") }; 
                        break;
                }

                connection.Kind = connectionElement.Name.LocalName;
                wire.Connections.Add(connection);
            }

            return wire;
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
                var text = new MultilingualTextDefinition
                {
                    Id = Attr(textElement, "ID"),
                    CompositionName = Attr(textElement, "CompositionName")
                };

                foreach (XElement item in Descendants(textElement, "MultilingualTextItem"))
                {
                    XElement? attr = Child(item, "AttributeList");

                    text.Items.Add(new MultilingualTextItemDefinition
                    {
                        Id = Attr(item, "ID"),
                        Culture = Value(attr, "Culture"),
                        Text = Value(attr, "Text") ?? ""
                    });
                }

                result.Add(text);
            }

            return result;
        }

        private static Dictionary<string, string?> ReadScalarChildren(XElement? element)
        {
            var result = new Dictionary<string, string?>();

            if (element == null)
            {
                return result;
            }

            foreach (XElement child in element.Elements())
            {
                if (!child.HasElements)
                {
                    string key = child.Name.LocalName;
                    // If it's a generic attribute type, use its 'Name' attribute as the key
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

            return result;
        }

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
