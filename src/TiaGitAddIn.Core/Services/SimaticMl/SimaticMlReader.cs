using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using TiaGitAddIn.Models.Comparison;

namespace TiaGitAddIn.Services.SimaticMl
{
    /// <summary>
    /// The safe-loading half of <see cref="SimaticMlParser"/>: bounded reading, DTD/external-entity
    /// rejection, and diagnostic mapping. Split into its own file (both halves are the same
    /// <c>partial class</c>) purely to keep each file under the project's size guideline; the public
    /// surface is still <c>SimaticMlParser.ParseText</c>.
    /// </summary>
    public static partial class SimaticMlParser
    {
        private const string DtdDiagnosticCode = "CMP-XML-DTD";
        private const string SyntaxDiagnosticCode = "CMP-XML-SYNTAX";
        private const string CharactersLimitDiagnosticCode = "CMP-XML-LIMIT-CHARACTERS";
        private const string ElementsLimitDiagnosticCode = "CMP-XML-LIMIT-ELEMENTS";
        private const string DepthLimitDiagnosticCode = "CMP-XML-LIMIT-DEPTH";

        /// <summary>Cooperative cancellation is observed at least this often while scanning the document.</summary>
        private const int CancellationCheckInterval = 64;

        /// <summary>
        /// Safely parses SimaticML XML text into an immutable <see cref="SimaticMlFile"/>. DTDs and external
        /// entities are never resolved, the document is bounded by <paramref name="limits"/> before any DOM is
        /// built, and every failure is reported as a diagnostic in the returned result rather than thrown.
        /// </summary>
        public static SimaticMlParseResult ParseText(
            string xml,
            SimaticMlParserLimits limits,
            PlcRevisionSide side,
            CancellationToken cancellationToken)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            if (limits == null) throw new ArgumentNullException(nameof(limits));

            cancellationToken.ThrowIfCancellationRequested();

            if (xml.Length > limits.MaximumCharactersInDocument)
            {
                return Failure(CreateLimitDiagnostic(CharactersLimitDiagnosticCode, side,
                    "The SimaticML document exceeds the maximum allowed character count."));
            }

            PlcComparisonDiagnostic? boundaryFailure = ScanBoundaries(xml, limits, side, cancellationToken);
            if (boundaryFailure != null)
            {
                return Failure(boundaryFailure);
            }

            XDocument document;
            try
            {
                using var stringReader = new StringReader(xml);
                using XmlReader reader = XmlReader.Create(stringReader, CreateReaderSettings(limits));
                document = XDocument.Load(reader, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
            catch (XmlException ex)
            {
                return Failure(MapXmlException(ex, side));
            }

            XElement? root = document.Root;
            if (root == null)
            {
                return Failure(new PlcComparisonDiagnostic(SyntaxDiagnosticCode, PlcDiagnosticSeverity.Error,
                    "The XML document has no root element.", new PlcSourceLocation(side)));
            }

            var blocks = new List<BlockDefinition>();
            foreach (XElement blockElement in root.Elements()
                .Where(e => e.Name.LocalName.StartsWith("SW.Blocks.", StringComparison.Ordinal)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                blocks.Add(ParseBlock(blockElement));
            }

            var model = new SimaticMlFile
            {
                EngineeringVersion = Child(root, "Engineering")?.Attribute("version")?.Value,
                Blocks = blocks.ToArray(),
            };

            return new SimaticMlParseResult(model, isPartial: false, Array.Empty<PlcComparisonDiagnostic>());
        }

        /// <summary>
        /// First pass over the document: counts elements and tracks nesting depth without ever building a
        /// DOM, so an oversized or maliciously deep document is rejected before <see cref="XDocument"/>
        /// allocates anything. Also the first point at which a prohibited DTD throws.
        /// </summary>
        private static PlcComparisonDiagnostic? ScanBoundaries(
            string xml, SimaticMlParserLimits limits, PlcRevisionSide side, CancellationToken cancellationToken)
        {
            int elementCount = 0;
            int readCount = 0;

            try
            {
                using var stringReader = new StringReader(xml);
                using XmlReader reader = XmlReader.Create(stringReader, CreateReaderSettings(limits));

                while (reader.Read())
                {
                    readCount++;
                    if (readCount % CancellationCheckInterval == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        elementCount++;
                        if (elementCount > limits.MaximumElementCount)
                        {
                            return CreateLimitDiagnostic(ElementsLimitDiagnosticCode, side,
                                "The SimaticML document exceeds the maximum allowed element count.");
                        }
                    }

                    if (reader.Depth > limits.MaximumDepth)
                    {
                        return CreateLimitDiagnostic(DepthLimitDiagnosticCode, side,
                            "The SimaticML document exceeds the maximum allowed nesting depth.");
                    }
                }
            }
            catch (XmlException ex)
            {
                return MapXmlException(ex, side);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }

        private static XmlReaderSettings CreateReaderSettings(SimaticMlParserLimits limits) => new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = false,
            IgnoreWhitespace = false,
            MaxCharactersInDocument = limits.MaximumCharactersInDocument,
            CloseInput = true,
        };

        /// <summary>
        /// Maps a caught <see cref="XmlException"/> to a stable diagnostic code with only safe side/line/column
        /// data. The message text is always our own fixed wording - never <c>ex.Message</c> - so a future .NET
        /// runtime change to exception wording can never leak entity URIs, file paths, or raw document content.
        /// </summary>
        private static PlcComparisonDiagnostic MapXmlException(XmlException ex, PlcRevisionSide side)
        {
            int? line = ex.LineNumber > 0 ? ex.LineNumber : null;
            int? column = ex.LinePosition > 0 ? ex.LinePosition : null;
            var location = new PlcSourceLocation(side, line, column);

            bool isDtdRelated = ex.Message.IndexOf("DTD", StringComparison.OrdinalIgnoreCase) >= 0;

            return isDtdRelated
                ? new PlcComparisonDiagnostic(DtdDiagnosticCode, PlcDiagnosticSeverity.Error,
                    "Document Type Definitions are not permitted in SimaticML documents.", location)
                : new PlcComparisonDiagnostic(SyntaxDiagnosticCode, PlcDiagnosticSeverity.Error,
                    "The XML document is not well-formed.", location);
        }

        private static PlcComparisonDiagnostic CreateLimitDiagnostic(string code, PlcRevisionSide side, string message)
            => new PlcComparisonDiagnostic(code, PlcDiagnosticSeverity.Error, message, new PlcSourceLocation(side));

        private static SimaticMlParseResult Failure(PlcComparisonDiagnostic diagnostic)
            => new SimaticMlParseResult(null, isPartial: false, new[] { diagnostic });
    }
}
