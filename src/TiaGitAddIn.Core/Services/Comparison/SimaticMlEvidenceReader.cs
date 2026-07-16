using System;
using System.IO;
using System.Text;
using System.Xml;

namespace TiaGitAddIn.Services.Comparison
{
    /// <summary>
    /// Bounded, streaming, DOM-free SimaticML evidence probe used by <see cref="PlcArtifactClassifier"/>.
    /// A single <see cref="XmlReader"/> pass (DTD prohibited, resolver null) over caller-supplied,
    /// already length-bounded text collects only the root element name, the first "SW.Blocks.*" element
    /// name, and the ProgrammingLanguage text found inside that block, if any. This never materializes an
    /// XDocument/XmlDocument, so a malicious or huge XML revision cannot be classified via an unsafe,
    /// full-DOM parse.
    /// </summary>
    public static class SimaticMlEvidenceReader
    {
        public static SimaticMlEvidence Probe(string boundedText)
        {
            if (boundedText == null) throw new ArgumentNullException(nameof(boundedText));

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
                CloseInput = true,
            };

            string? rootElementName = null;
            string? blockElementName = null;
            string? programmingLanguageValue = null;
            bool insideBlock = false;
            int blockDepth = -1;

            try
            {
                using StringReader stringReader = new StringReader(boundedText);
                using XmlReader reader = XmlReader.Create(stringReader, settings);

                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element) continue;

                    if (rootElementName == null) rootElementName = reader.LocalName;

                    if (insideBlock && reader.Depth <= blockDepth)
                    {
                        insideBlock = false;
                    }

                    if (blockElementName == null && reader.LocalName.StartsWith("SW.Blocks.", StringComparison.Ordinal))
                    {
                        blockElementName = reader.LocalName;
                        insideBlock = true;
                        blockDepth = reader.Depth;
                    }
                    else if (insideBlock && programmingLanguageValue == null && reader.LocalName == "ProgrammingLanguage")
                    {
                        programmingLanguageValue = reader.IsEmptyElement ? string.Empty : ReadSimpleElementText(reader);
                    }
                }

                return new SimaticMlEvidence(true, rootElementName, blockElementName, programmingLanguageValue);
            }
            catch (XmlException)
            {
                return new SimaticMlEvidence(false, rootElementName, blockElementName, programmingLanguageValue);
            }
        }

        /// <summary>Manually accumulates text-node content for one element without XDocument/DOM materialization.</summary>
        private static string ReadSimpleElementText(XmlReader reader)
        {
            int depth = reader.Depth;
            var text = new StringBuilder();

            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == depth))
            {
                if (reader.NodeType == XmlNodeType.Text || reader.NodeType == XmlNodeType.CDATA)
                {
                    text.Append(reader.Value);
                }
            }

            return text.ToString();
        }
    }

    /// <summary>Immutable evidence collected by <see cref="SimaticMlEvidenceReader.Probe"/>.</summary>
    public sealed class SimaticMlEvidence
    {
        public SimaticMlEvidence(bool isWellFormed, string? rootElementName, string? blockElementName, string? programmingLanguageValue)
        {
            IsWellFormed = isWellFormed;
            RootElementName = rootElementName;
            BlockElementName = blockElementName;
            ProgrammingLanguageValue = programmingLanguageValue;
        }

        public bool IsWellFormed { get; }
        public string? RootElementName { get; }
        public string? BlockElementName { get; }
        public string? ProgrammingLanguageValue { get; }
    }
}
