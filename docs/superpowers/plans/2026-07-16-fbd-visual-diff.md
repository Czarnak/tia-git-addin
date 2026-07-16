# FBD Visual Diff Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (<code>- [ ]</code>) syntax for tracking.

**Goal:** Add an FBD-only, deterministic semantic and visual diff that extracts a neutral graph from bounded SimaticML, matches nodes without using UIds as identity, renders an accessible synchronized WPF comparison, and safely degrades to the shared structured or raw-text fallback.

**Architecture:** The <code>netstandard2.0</code> Core adapts the foundation parser's <code>SimaticMlFile</code> into immutable <code>LogicNetworkGraph</code> objects, compares those graphs in two conservative stages, and lays out a union topology deterministically. The <code>net48</code> WPF layer maps the concrete <code>FbdPresentation : LogicNetworkPresentation</code> into focused ViewModels and an implicit DataTemplate; the foundation-owned result envelope, coordinator, raw-text fallback, diagnostics, selection generation, and STA helper remain the only shared routing seams. Existing LAD parser/comparer/layout/ViewModel behavior is not migrated to this graph.

**Tech Stack:** .NET SDK 8.0.420; C# 12 under the repository's current <code>LangVersion=latest</code>; <code>netstandard2.0</code> Core; <code>net48</code> WPF; xUnit 2.9.0 on VSTest; LINQ to XML through the foundation's hardened <code>XmlReader</code>; no new runtime or test package.

## Global Constraints

- This plan starts only after <code>docs/superpowers/plans/2026-07-16-comparison-foundation-interface.md</code> is implemented and green. Its contract names below are dependencies, not types to recreate.
- Core stays <code>netstandard2.0</code>, Siemens-free, WPF-free, and package-neutral. WPF and Siemens references remain confined to <code>src/TiaGitAddIn</code>.
- FBD alone uses <code>LogicNetworkGraph</code>. LAD output, matching, component/wire states, layout coordinates, and ViewModel data remain unchanged except for the foundation's shared envelope, routing, mode/support metadata, and fallback.
- Use the foundation enums in <code>Models/Comparison/PlcComparisonEnums.cs</code>. Do not introduce competing artifact, mode, support, presentation-kind, diagnostic-severity, or revision-side enums.
- Parse text only through <code>SimaticMlParser.ParseText</code> with <code>SimaticMlParserLimits.Default</code>: 16,777,216 characters, 250,000 elements, maximum depth 128, <code>DtdProcessing.Prohibit</code>, <code>XmlResolver = null</code>, and cancellation checks.
- Apply <code>FbdGraphLimits.Default</code> exactly: 256 blocks, 2,048 networks per block, 2,048 nodes per network, 4,096 edges per network, 65,536 total nodes, and 131,072 total edges. Stop at the first exceeded boundary and emit stable diagnostic <code>FBD003</code>.
- Source UIds may resolve endpoints inside one parsed side and may appear in <code>FbdSourceTrace</code>. They never enter block keys, network keys, exact signatures, neighbourhood signatures, diff keys, ordering, or layout tie-breakers.
- Normalize semantic names with Unicode NFC, trim outer whitespace only where the field contract allows it, and compare/order with <code>StringComparer.Ordinal</code>. Do not case-fold PLC identifiers.
- No GUID, clock, random value, dictionary enumeration order, XML list index, or process-specific hash code may affect graph identity, matching, diff ordering, or layout.
- Every production constructor defensively copies input collections. Every public collection is read-only. Builders, comparers, layout, and ViewModel mappers return new objects and never mutate parsed models, prior results, or caller collections.
- Unknown structurally valid FBD parts become labelled generic nodes and yield <code>Visual · Partial</code> with diagnostic <code>FBD001</code>. Trustworthy metadata without trustworthy connectivity yields <code>Structured · Partial</code> with <code>FBD002</code>. Unparseable FBD yields the foundation's <code>Text · Fallback</code> with parser diagnostics.
- Raw XML is never interpreted as XAML or HTML. Bound labels are plain WPF text, capped at 256 characters, and diagnostics expose only stable code, severity, safe message, and safe source location.
- Added, removed, modified, and rewired states always have visible text/glyph/stroke cues in addition to colour. Keyboard focus, selection, zoom, pan, and change navigation are required.
- New focused C# and XAML files stay at or below 800 lines; methods stay below 50 lines; nesting stays at four levels or less.
- A sanitized real TIA Portal V21 FBD export and provenance manifest are mandatory for AC-044/AC-100/AC-102. Synthetic XML isolates edge cases but cannot replace the real fixture.
- Testable production code added by this plan must have at least 80% total line coverage, and the repository-wide merged coverage gate owned by `docs/superpowers/plans/2026-07-16-vci-git-workflow.md` must remain at or above 80%.
- No credential, customer/project/device identity, author identity, network address, absolute machine path, internal Siemens comparison reference, or private TIA assembly enters production code or committed fixtures.
- After code changes, run <code>graphify update .</code>. Documentation-only execution of this plan does not require a graph update.

---

## Repository Evidence and Dependency Gate

- <code>graphify-out/GRAPH_REPORT.md</code> reports <code>SimaticMlParser</code>, <code>LadLayoutEngine</code>, and <code>SimaticMlComparer</code> as high-connectivity nodes. Keep the new graph builder/comparer/layout in focused files rather than extending those god nodes.
- The report was built from commit <code>6d8f2e62</code>. Its declared update command is part of final verification.
- <code>graphify-out/wiki/index.md</code> and the entire <code>graphify-out/wiki</code> directory are absent in the inspected tree; navigation therefore used the report and exact source files.
- Current relevant files are <code>Services/SimaticMl/SimaticMlModels.cs</code> (228 lines), <code>SimaticMlParser.cs</code> (599), <code>LadVisualGraphBuilder.cs</code> (469), <code>LadLayoutEngine.cs</code> (599), <code>UI/ViewModels/LadDiffViewModel.cs</code> (229), and <code>UI/Views/LadDiffView.xaml</code> (779). None is a destination for FBD rendering code.

- The additive integration order is VCI Task 1 → foundation Tasks 1–10 → this complete FBD plan → the complete SCL plan rebased over FBD for <code>GitPanelLaunchService.cs</code> and <code>ComparisonTemplates.xaml</code> → VCI Tasks 2–4 → foundation Task 11 → VCI Tasks 5–8. FBD and SCL feature branches may develop independently after foundation handoffs, but their shared-file commits do not merge in parallel: FBD lands first; SCL rebases and proves both registrations/templates remain; VCI later preserves both while adding only its named adapter logging.

Before Task 1, run:

~~~powershell
$required = @(
  'src/TiaGitAddIn.Core/Models/Comparison/PlcComparisonEnums.cs',
  'src/TiaGitAddIn.Core/Models/Comparison/ComparisonPresentation.cs',
  'src/TiaGitAddIn.Core/Models/Comparison/PlcComparisonResult.cs',
  'src/TiaGitAddIn.Core/Services/Comparison/IPlcComparisonStrategy.cs',
  'src/TiaGitAddIn.Core/Services/Comparison/PlcComparisonResultFactory.cs',
  'src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlParserLimits.cs',
  'src/TiaGitAddIn/UI/Mapping/IComparisonPresentationMapper.cs',
  'src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonPresentationViewModel.cs',
  'src/TiaGitAddIn/UI/Views/Comparison/ComparisonPresentationHost.xaml',
  'src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml',
  'src/TiaGitAddIn.Tests/UI/WpfTestHost.cs'
)
$missing = $required | Where-Object { -not (Test-Path -LiteralPath $_) }
if ($missing.Count -ne 0) { throw "Foundation prerequisite missing: $($missing -join ', ')" }
~~~

Expected: exit 0 and no output. Any missing path blocks this plan; implement the named foundation plan first.

## Foundation-Owned Contracts Consumed Verbatim

~~~csharp
public sealed class PlcComparisonRequest
{
    public PlcComparisonRequest(
        PlcRevision left,
        PlcRevision right,
        PlcArtifactPairDescriptor pair);
}

public sealed class PlcComparisonContext
{
    public PlcComparisonContext(
        PlcComparisonRequest request,
        ComparisonRawText? rawText);
}

public interface IPlcComparisonStrategy
{
    IReadOnlyCollection<PlcArtifactKind> SupportedKinds { get; }

    Task<PlcComparisonResult> CompareAsync(
        PlcComparisonContext context,
        CancellationToken cancellationToken);
}

public abstract class ComparisonPresentation
{
    protected ComparisonPresentation(ComparisonPresentationKind kind);
}

public abstract class LogicNetworkPresentation : ComparisonPresentation
{
    protected LogicNetworkPresentation();
}

public sealed class PlcComparisonResultFactory
{
    public PlcComparisonResult CreateSemantic(
        PlcComparisonContext context,
        PlcComparisonMode actualMode,
        PlcSupportLevel supportLevel,
        string limitation,
        IEnumerable<PlcComparisonDiagnostic> diagnostics,
        ComparisonPresentation presentation);

    public PlcComparisonResult CreateTextFallback(
        PlcComparisonContext context,
        string limitation,
        IEnumerable<PlcComparisonDiagnostic> diagnostics);
}

public sealed class ComparisonViewModelMetadata
{
    public static ComparisonViewModelMetadata From(PlcComparisonResult result);

    public string ModeLabel { get; }
    public string SupportLabel { get; }
    public string Header { get; }
    public string Limitation { get; }
    public IReadOnlyList<ComparisonDiagnosticViewModel> Diagnostics { get; }
    public ComparisonRawTextViewModel? RawText { get; }
    public bool HasLimitation { get; }
    public bool HasRawText { get; }
}

public abstract class ComparisonPresentationViewModel
{
    protected ComparisonPresentationViewModel(
        ComparisonPresentationKind kind,
        ComparisonViewModelMetadata metadata);
}

public sealed class ComparisonDiagnosticViewModel
{
    public ComparisonDiagnosticViewModel(PlcComparisonDiagnostic diagnostic);
}

public sealed class ComparisonRawTextViewModel
{
    public ComparisonRawTextViewModel(ComparisonRawText rawText);
}

public interface IComparisonPresentationViewModelFactory
{
    bool CanMap(ComparisonPresentation presentation);

    ComparisonPresentationViewModel Map(
        PlcComparisonResult result,
        ComparisonViewModelMetadata metadata);
}

public sealed class ComparisonPresentationMapper
{
    public ComparisonPresentationMapper(
        IEnumerable<IComparisonPresentationViewModelFactory> factories);
}

internal static class ComparisonTestData
{
    public static PlcRevision TextRevision(
        PlcRevisionSide side,
        string text,
        string path = "Program.xml");

    public static PlcRevision MissingRevision(
        PlcRevisionSide side,
        string path = "Program.xml");

    public static PlcArtifactPairDescriptor Pair(
        PlcArtifactKind kind,
        PlcComparisonMode requestedMode,
        PlcPairChangeKind changeKind = PlcPairChangeKind.Modified);

    public static PlcComparisonContext Context(
        PlcArtifactKind kind,
        PlcComparisonMode requestedMode,
        string leftText = "left",
        string rightText = "right",
        string path = "Program.xml");
}
~~~

The foundation also owns <code>ComparisonRawText</code>, <code>TextPresentation</code>, <code>PlcComparisonDiagnostic</code>, <code>PlcSourceLocation</code>, <code>ComparisonPresentationMapper</code>, <code>ComparisonPresentationViewModel</code>, <code>ComparisonPresentationHost</code>, and <code>WpfTestHost</code>. FBD code consumes them and adds only <code>FbdPresentation</code>, its specialized mapper, and its implicit DataTemplate.

## File Map

| Path | Action | Single responsibility |
|---|---|---|
| <code>src/TiaGitAddIn.Core/Models/Comparison/LogicNetwork/LogicNetworkEnums.cs</code> | Create | Neutral node/pin/diff/match enums only |
| <code>src/TiaGitAddIn.Core/Models/Comparison/LogicNetwork/LogicNetworkGraph.cs</code> | Create | Immutable blocks, networks, nodes, pins, endpoints, edges, source trace |
| <code>src/TiaGitAddIn.Core/Models/Comparison/LogicNetwork/LogicNetworkDiff.cs</code> | Create | Immutable block/network/node/edge diff records as classes |
| <code>src/TiaGitAddIn.Core/Models/Comparison/Fbd/FbdPresentation.cs</code> | Create | Concrete visual/structured logic-network presentation and summaries |
| <code>src/TiaGitAddIn.Core/Models/Comparison/Fbd/FbdLayoutModels.cs</code> | Create | Immutable coordinates, ports, routes, canvas, and paired layouts |
| <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphLimits.cs</code> | Create | Exact graph caps and validation |
| <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphBuilder.cs</code> | Create | SimaticML-to-neutral-graph adaptation |
| <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdSemanticKeyBuilder.cs</code> | Create | NFC/ordinal keys, exact signatures, neighbourhood signatures, fingerprints |
| <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphComparer.cs</code> | Create | Conservative node/edge matching and deterministic statuses |
| <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdLayoutEngine.cs</code> | Create | Deterministic union-topology layout |
| <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdComparisonStrategy.cs</code> | Create | Parse/build/compare/fallback orchestration through foundation contracts |
| <code>src/TiaGitAddIn/UI/Mapping/FbdPresentationViewModelFactory.cs</code> | Create | Domain presentation to typed FBD ViewModel through the specialized factory seam |
| <code>src/TiaGitAddIn/UI/ViewModels/Comparison/FbdDiffViewModel.cs</code> | Create | Network/change selection and shared viewport state |
| <code>src/TiaGitAddIn/UI/ViewModels/Comparison/FbdNetworkViewModels.cs</code> | Create | Immutable paired canvas/node/edge/pin ViewModels |
| <code>src/TiaGitAddIn/UI/Behaviors/FbdScrollSyncBehavior.cs</code> | Create | WPF scroll-offset synchronization and wheel-to-zoom event routing |
| <code>src/TiaGitAddIn/UI/Views/Comparison/FbdDiffView.xaml</code> | Create | Focused FBD comparison view |
| <code>src/TiaGitAddIn/UI/Views/Comparison/FbdDiffView.xaml.cs</code> | Create | Constructor-only view code-behind |
| <code>src/TiaGitAddIn/UI/Views/Comparison/FbdNodeTemplates.xaml</code> | Create | Reusable accessible node/pin/edge/status resources |
| <code>src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml</code> | Modify | Add one implicit FBD ViewModel-to-view DataTemplate |
| <code>src/TiaGitAddIn/UI/GitPanelLaunchService.cs</code> | Modify | Register FBD strategy and specialized presentation mapper in foundation composition |
| <code>src/TiaGitAddIn.Tests/Services/FbdGraphModelTests.cs</code> | Create | Immutability and neutral model invariants |
| <code>src/TiaGitAddIn.Tests/Services/FbdFixtureManifestTests.cs</code> | Create | Provenance, hashes, sanitization, real-fixture requirement |
| <code>src/TiaGitAddIn.Tests/Services/FbdGraphBuilderTests.cs</code> | Create | Nodes, pins, edges, unknown parts, summaries, limits |
| <code>src/TiaGitAddIn.Tests/Services/FbdGraphComparerTests.cs</code> | Create | Exact/neighbourhood matching, ambiguity, statuses, rewiring, UId |
| <code>src/TiaGitAddIn.Tests/Services/FbdComparisonStrategyTests.cs</code> | Create | Full/partial/structured/text outcomes and cancellation |
| <code>src/TiaGitAddIn.Tests/Services/FbdLayoutEngineTests.cs</code> | Create | Stable coordinates, connector order, routes, bounds |
| <code>src/TiaGitAddIn.Tests/UI/FbdDiffViewModelTests.cs</code> | Create | Mapping, selection, zoom/pan, change navigation |
| <code>src/TiaGitAddIn.Tests/UI/FbdDiffViewSmokeTests.cs</code> | Create | STA resources, DataTemplate, bindings, accessibility, raw-text host |
| <code>src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/manifest.json</code> | Create | Fixture provenance and expected support/count contract |
| <code>src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/real-v21-fbd-sanitized.xml</code> | Create from supplied artifact | Sanitized real V21 compatibility anchor |
| <code>src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/synthetic-fbd-edge-cases.xml</code> | Create | Deterministic compact edge-case fixture |

No other production or test file is in scope. In particular, do not modify <code>LadVisualGraphBuilder.cs</code>, <code>LadLayoutEngine.cs</code>, <code>LadDiffViewModel.cs</code>, or <code>LadDiffView.xaml</code>.

## Locked FBD Public Surface

The tasks below must keep these names and signatures consistent.

~~~csharp
namespace TiaGitAddIn.Models.Comparison.LogicNetwork
{
    public enum LogicNodeKind { Access, Part, Call, PowerRail, OpenBranch, GenericPart }
    public enum LogicPinDirection { Input, Output, Bidirectional, Unknown }
    public enum LogicDiffStatus { Unchanged, Added, Removed, Modified, Rewired }
    public enum LogicMatchStage { None, ExactSignature, Neighbourhood }

    public sealed class FbdSourceTrace
    {
        public FbdSourceTrace(
            PlcRevisionSide side,
            string? sourceUId,
            PlcSourceLocation? location);
        public PlcRevisionSide Side { get; }
        public string? SourceUId { get; }
        public PlcSourceLocation? Location { get; }
    }

    public sealed class LogicPin
    {
        public LogicPin(
            string key,
            string name,
            LogicPinDirection direction,
            int ordinal,
            bool isConnected);
        public string Key { get; }
        public string Name { get; }
        public LogicPinDirection Direction { get; }
        public int Ordinal { get; }
        public bool IsConnected { get; }
    }

    public sealed class LogicNode
    {
        public LogicNode(
            string key,
            LogicNodeKind kind,
            string operation,
            string operand,
            IEnumerable<KeyValuePair<string, string>> attributes,
            IEnumerable<LogicPin> pins,
            FbdSourceTrace trace);
        public string Key { get; }
        public LogicNodeKind Kind { get; }
        public string Operation { get; }
        public string Operand { get; }
        public IReadOnlyDictionary<string, string> Attributes { get; }
        public IReadOnlyList<LogicPin> Pins { get; }
        public FbdSourceTrace Trace { get; }
    }

    public sealed class LogicEndpoint
    {
        public LogicEndpoint(string nodeKey, string pinKey);
        public string NodeKey { get; }
        public string PinKey { get; }
    }

    public sealed class LogicEdge
    {
        public LogicEdge(
            string key,
            LogicEndpoint source,
            LogicEndpoint target,
            FbdSourceTrace trace);
        public string Key { get; }
        public LogicEndpoint Source { get; }
        public LogicEndpoint Target { get; }
        public FbdSourceTrace Trace { get; }
    }

    public sealed class LogicNetwork
    {
        public LogicNetwork(
            string key,
            string title,
            IEnumerable<LogicNode> nodes,
            IEnumerable<LogicEdge> edges);
        public string Key { get; }
        public string Title { get; }
        public IReadOnlyList<LogicNode> Nodes { get; }
        public IReadOnlyList<LogicEdge> Edges { get; }
    }

    public sealed class LogicBlock
    {
        public LogicBlock(
            string key,
            string name,
            string blockKind,
            int? number,
            IEnumerable<LogicNetwork> networks);
        public string Key { get; }
        public string Name { get; }
        public string BlockKind { get; }
        public int? Number { get; }
        public IReadOnlyList<LogicNetwork> Networks { get; }
    }

    public sealed class LogicNetworkGraph
    {
        public LogicNetworkGraph(
            PlcRevisionSide side,
            IEnumerable<LogicBlock> blocks);
        public PlcRevisionSide Side { get; }
        public IReadOnlyList<LogicBlock> Blocks { get; }
        public static LogicNetworkGraph Empty(PlcRevisionSide side);
    }
}
~~~

~~~csharp
namespace TiaGitAddIn.Models.Comparison.Fbd
{
    public sealed class FbdNetworkSummary
    {
        public FbdNetworkSummary(
            string blockKey,
            string networkKey,
            int nodeCount,
            int pinCount,
            int edgeCount,
            string reason);
    }

    public sealed class FbdGraphBuildResult
    {
        public FbdGraphBuildResult(
            LogicNetworkGraph graph,
            bool hasTrustworthyConnectivity,
            bool hasUnknownParts,
            IEnumerable<FbdNetworkSummary> summaries,
            IEnumerable<PlcComparisonDiagnostic> diagnostics);
    }

    public sealed class FbdPresentation : LogicNetworkPresentation
    {
        private FbdPresentation(
            bool hasVisualGraph,
            IEnumerable<LogicBlockDiff> blocks,
            IEnumerable<FbdNetworkSummary> summaries);

        public bool HasVisualGraph { get; }
        public IReadOnlyList<LogicBlockDiff> Blocks { get; }
        public IReadOnlyList<FbdNetworkSummary> Summaries { get; }

        public static FbdPresentation Visual(
            IEnumerable<LogicBlockDiff> blocks,
            IEnumerable<FbdNetworkSummary> summaries);

        public static FbdPresentation Structured(
            IEnumerable<FbdNetworkSummary> summaries);
    }
}
~~~

~~~csharp
public sealed class FbdGraphBuilder
{
    public FbdGraphBuildResult Build(
        SimaticMlFile model,
        PlcRevisionSide side,
        FbdGraphLimits limits,
        CancellationToken cancellationToken);
}

public static class FbdSemanticKeyBuilder
{
    public static string BuildBlockKey(BlockDefinition block);
    public static string BuildNetworkKey(
        string blockKey,
        CompileUnitDefinition compileUnit,
        IEnumerable<LogicNode> nodes);
    public static string BuildExactSignature(LogicNode node);
    public static string BuildNeighbourhoodSignature(
        LogicNode node,
        LogicNetwork network);
    public static string BuildEdgeSignature(LogicEdge edge);
}

public sealed class FbdGraphComparer
{
    public FbdPresentation Compare(
        FbdGraphBuildResult left,
        FbdGraphBuildResult right,
        CancellationToken cancellationToken);
}

public sealed class FbdLayoutEngine
{
    public FbdLayoutDocument Layout(
        FbdPresentation presentation,
        CancellationToken cancellationToken);
}
~~~

## Acceptance Traceability

| Acceptance criteria | Implemented and proved by |
|---|---|
| AC-007, AC-047 | Tasks 1, 3, and 5: defensive copies; UId trace-only invariants |
| AC-008, AC-014, AC-017, AC-018, AC-019, AC-117 | Task 6: FBD-only strategy through foundation routing/result factory |
| AC-022, AC-023, AC-028, AC-029, AC-030, AC-032 | Tasks 8 and 9: typed mapper, host, templates, limitations, safe diagnostics, raw text, STA |
| AC-031, AC-033 | Task 9: focused file sizes and non-colour cues |
| AC-044, AC-045, AC-046 | Tasks 2 through 4: real fixture, typed graph, connector semantics, stable block/network keys |
| AC-048, AC-049, AC-050, AC-116 | Task 5: add/remove/modify/rewire; ambiguity; exact then neighbourhood |
| AC-051, AC-052, AC-113 | Tasks 4 and 6: generic node partial, structured partial, final text fallback |
| AC-053, AC-054 | Tasks 7 through 9: deterministic layout and dedicated synchronized view |
| AC-095, AC-096 | Tasks 4 and 6: consume hardened parser and enforce graph caps |
| AC-100, AC-101, AC-102 | Task 2: provenance, hash, sanitization, real V21 anchor |
| AC-103 | Final verification: focused/full tests, 80% coverage, build, scans, graph update |
| AC-105 | Tasks 6 and 9 plus final LAD regression command |

### Task 1: Immutable Neutral Graph, Diff, Presentation, and Limit Contracts

**Acceptance criteria:** AC-007, AC-044, AC-045, AC-047, AC-096.

**Files:**
- Create: <code>src/TiaGitAddIn.Core/Models/Comparison/LogicNetwork/LogicNetworkEnums.cs</code>
- Create: <code>src/TiaGitAddIn.Core/Models/Comparison/LogicNetwork/LogicNetworkGraph.cs</code>
- Create: <code>src/TiaGitAddIn.Core/Models/Comparison/LogicNetwork/LogicNetworkDiff.cs</code>
- Create: <code>src/TiaGitAddIn.Core/Models/Comparison/Fbd/FbdPresentation.cs</code>
- Create: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphLimits.cs</code>
- Test: <code>src/TiaGitAddIn.Tests/Services/FbdGraphModelTests.cs</code>

**Interfaces:**
- Consumes: foundation <code>PlcRevisionSide</code>, <code>PlcSourceLocation</code>, <code>PlcComparisonDiagnostic</code>, and <code>LogicNetworkPresentation</code>.
- Produces: the exact graph and presentation surface in “Locked FBD Public Surface”; Tasks 3 through 9 compile against it.

- [ ] **Step 1 [2–5 min]: Write the defensive-copy RED test**

~~~csharp
[Fact]
public void GraphConstructorsDefensivelyCopyCallerCollections()
{
    var pins = new List<LogicPin>
    {
        new LogicPin("in:0", "IN", LogicPinDirection.Input, 0, true)
    };
    var attributes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Version"] = "1.0"
    };
    var node = new LogicNode(
        "node:1",
        LogicNodeKind.Part,
        "And",
        string.Empty,
        attributes,
        pins,
        new FbdSourceTrace(PlcRevisionSide.Left, "17", null));
    var nodes = new List<LogicNode> { node };
    var network = new LogicNetwork("network:main", "Main", nodes, Array.Empty<LogicEdge>());

    pins.Clear();
    attributes.Clear();
    nodes.Clear();

    Assert.Single(node.Pins);
    Assert.Equal("1.0", node.Attributes["Version"]);
    Assert.Single(network.Nodes);
    Assert.Throws<NotSupportedException>(
        () => ((IDictionary<string, string>)node.Attributes).Add("Changed", "true"));
}
~~~

- [ ] **Step 2 [2–5 min]: Run the focused test and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.Services.FbdGraphModelTests.GraphConstructorsDefensivelyCopyCallerCollections"
~~~

Expected: FAIL to compile with CS0246 for <code>LogicPin</code> or <code>LogicNode</code>.

- [ ] **Step 3 [2–5 min]: Add the exact enums and immutable-copy helper**

~~~csharp
namespace TiaGitAddIn.Models.Comparison.LogicNetwork
{
    public enum LogicNodeKind
    {
        Access,
        Part,
        Call,
        PowerRail,
        OpenBranch,
        GenericPart
    }

    public enum LogicPinDirection
    {
        Input,
        Output,
        Bidirectional,
        Unknown
    }

    public enum LogicDiffStatus
    {
        Unchanged,
        Added,
        Removed,
        Modified,
        Rewired
    }

    public enum LogicMatchStage
    {
        None,
        ExactSignature,
        Neighbourhood
    }
}
~~~

Add this internal helper at the bottom of <code>LogicNetworkGraph.cs</code>:

~~~csharp
internal static class ImmutableModelCopy
{
    public static IReadOnlyList<T> List<T>(IEnumerable<T> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        return new ReadOnlyCollection<T>(values.ToList());
    }

    public static IReadOnlyDictionary<string, string> OrdinalMap(
        IEnumerable<KeyValuePair<string, string>> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var copy = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> value in values)
        {
            copy.Add(value.Key, value.Value);
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}
~~~

- [ ] **Step 4 [2–5 min]: Add graph constructors with validated get-only state**

Implement the public surface exactly as locked above. Every string constructor argument uses:

~~~csharp
private static string Required(string value, string parameterName)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new ArgumentException("Value must not be empty.", parameterName);
    }

    return value;
}
~~~

Every aggregate uses <code>ImmutableModelCopy.List</code>; <code>LogicNode.Attributes</code> uses <code>ImmutableModelCopy.OrdinalMap</code>; <code>LogicNetworkGraph.Empty</code> is:

~~~csharp
public static LogicNetworkGraph Empty(PlcRevisionSide side) =>
    new LogicNetworkGraph(side, Array.Empty<LogicBlock>());
~~~

- [ ] **Step 5 [2–5 min]: Add immutable diff pairs**

Use these exact constructors and properties in <code>LogicNetworkDiff.cs</code>:

~~~csharp
public sealed class LogicNodeDiff
{
    public LogicNodeDiff(
        string key,
        LogicDiffStatus status,
        LogicMatchStage matchStage,
        LogicNode? left,
        LogicNode? right)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Status = status;
        MatchStage = matchStage;
        Left = left;
        Right = right;
    }

    public string Key { get; }
    public LogicDiffStatus Status { get; }
    public LogicMatchStage MatchStage { get; }
    public LogicNode? Left { get; }
    public LogicNode? Right { get; }
}

public sealed class LogicEdgeDiff
{
    public LogicEdgeDiff(
        string key,
        LogicDiffStatus status,
        LogicEdge? left,
        LogicEdge? right)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Status = status;
        Left = left;
        Right = right;
    }

    public string Key { get; }
    public LogicDiffStatus Status { get; }
    public LogicEdge? Left { get; }
    public LogicEdge? Right { get; }
}

public sealed class LogicNetworkDiff
{
    public LogicNetworkDiff(
        string key,
        LogicDiffStatus status,
        LogicNetwork? left,
        LogicNetwork? right,
        IEnumerable<LogicNodeDiff> nodes,
        IEnumerable<LogicEdgeDiff> edges)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Status = status;
        Left = left;
        Right = right;
        Nodes = ImmutableModelCopy.List(nodes);
        Edges = ImmutableModelCopy.List(edges);
    }

    public string Key { get; }
    public LogicDiffStatus Status { get; }
    public LogicNetwork? Left { get; }
    public LogicNetwork? Right { get; }
    public IReadOnlyList<LogicNodeDiff> Nodes { get; }
    public IReadOnlyList<LogicEdgeDiff> Edges { get; }
}

public sealed class LogicBlockDiff
{
    public LogicBlockDiff(
        string key,
        LogicDiffStatus status,
        LogicBlock? left,
        LogicBlock? right,
        IEnumerable<LogicNetworkDiff> networks)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Status = status;
        Left = left;
        Right = right;
        Networks = ImmutableModelCopy.List(networks);
    }

    public string Key { get; }
    public LogicDiffStatus Status { get; }
    public LogicBlock? Left { get; }
    public LogicBlock? Right { get; }
    public IReadOnlyList<LogicNetworkDiff> Networks { get; }
}
~~~

- [ ] **Step 6 [2–5 min]: Add the concrete FBD presentation factories**

~~~csharp
public sealed class FbdPresentation : LogicNetworkPresentation
{
    private FbdPresentation(
        bool hasVisualGraph,
        IEnumerable<LogicBlockDiff> blocks,
        IEnumerable<FbdNetworkSummary> summaries)
    {
        HasVisualGraph = hasVisualGraph;
        Blocks = ImmutableModelCopy.List(blocks);
        Summaries = ImmutableModelCopy.List(summaries);
    }

    public bool HasVisualGraph { get; }
    public IReadOnlyList<LogicBlockDiff> Blocks { get; }
    public IReadOnlyList<FbdNetworkSummary> Summaries { get; }

    public static FbdPresentation Visual(
        IEnumerable<LogicBlockDiff> blocks,
        IEnumerable<FbdNetworkSummary> summaries) =>
        new FbdPresentation(true, blocks, summaries);

    public static FbdPresentation Structured(
        IEnumerable<FbdNetworkSummary> summaries) =>
        new FbdPresentation(
            false,
            Array.Empty<LogicBlockDiff>(),
            summaries);
}
~~~

- [ ] **Step 7 [2–5 min]: Add exact graph limits**

~~~csharp
public sealed class FbdGraphLimits
{
    public static FbdGraphLimits Default { get; } = new FbdGraphLimits(
        maximumBlocks: 256,
        maximumNetworksPerBlock: 2_048,
        maximumNodesPerNetwork: 2_048,
        maximumEdgesPerNetwork: 4_096,
        maximumTotalNodes: 65_536,
        maximumTotalEdges: 131_072);

    public FbdGraphLimits(
        int maximumBlocks,
        int maximumNetworksPerBlock,
        int maximumNodesPerNetwork,
        int maximumEdgesPerNetwork,
        int maximumTotalNodes,
        int maximumTotalEdges)
    {
        MaximumBlocks = Positive(maximumBlocks, nameof(maximumBlocks));
        MaximumNetworksPerBlock = Positive(maximumNetworksPerBlock, nameof(maximumNetworksPerBlock));
        MaximumNodesPerNetwork = Positive(maximumNodesPerNetwork, nameof(maximumNodesPerNetwork));
        MaximumEdgesPerNetwork = Positive(maximumEdgesPerNetwork, nameof(maximumEdgesPerNetwork));
        MaximumTotalNodes = Positive(maximumTotalNodes, nameof(maximumTotalNodes));
        MaximumTotalEdges = Positive(maximumTotalEdges, nameof(maximumTotalEdges));
    }

    public int MaximumBlocks { get; }
    public int MaximumNetworksPerBlock { get; }
    public int MaximumNodesPerNetwork { get; }
    public int MaximumEdgesPerNetwork { get; }
    public int MaximumTotalNodes { get; }
    public int MaximumTotalEdges { get; }

    private static int Positive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }

        return value;
    }
}
~~~

- [ ] **Step 8 [2–5 min]: Run model tests and confirm GREEN**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.Services.FbdGraphModelTests"
~~~

Expected: PASS; no mutable collection can change an existing graph/diff/presentation.

- [ ] **Step 9 [2–5 min]: Refactor and verify file limits**

Run:

~~~powershell
$files = Get-ChildItem src/TiaGitAddIn.Core/Models/Comparison/LogicNetwork,src/TiaGitAddIn.Core/Models/Comparison/Fbd -Filter *.cs
$oversize = $files | Where-Object { (Get-Content -LiteralPath $_.FullName).Count -gt 800 }
if ($oversize) { throw "Oversize FBD model file: $($oversize.FullName -join ', ')" }
~~~

Expected: exit 0 and no output.

- [ ] **Step 10 [2–5 min]: Commit the contract increment**

~~~powershell
git add src/TiaGitAddIn.Core/Models/Comparison src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphLimits.cs src/TiaGitAddIn.Tests/Services/FbdGraphModelTests.cs
git commit -m "feat: add immutable FBD graph contracts"
~~~

### Task 2: Sanitized V21 Fixture and Provenance Gate

**Acceptance criteria:** AC-044, AC-100, AC-101, AC-102.

**External input:** A real FBD block exported by TIA Portal V21 must be supplied outside the repository before this task. The plan intentionally does not invent those bytes because a fabricated file cannot satisfy AC-102.

**Files:**
- Create: <code>src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/real-v21-fbd-sanitized.xml</code>
- Create: <code>src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/synthetic-fbd-edge-cases.xml</code>
- Create: <code>src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/manifest.json</code>
- Create: <code>src/TiaGitAddIn.Tests/Services/FbdFixtureManifestTests.cs</code>

**Interfaces:**
- Consumes: file-system fixture bytes and <code>System.Security.Cryptography.SHA256</code>.
- Produces: manifest entries with <code>file</code>, <code>provenance</code>, <code>tiaPublicApiBuild</code>, <code>encoding</code>, <code>bom</code>, <code>sanitizationActions</code>, lowercase <code>sha256</code>, <code>expectedSupportLevel</code>, and expected block/network/node/pin/edge counts.

- [ ] **Step 1 [2–5 min]: Write the manifest RED test**

~~~csharp
[Fact]
public void EveryFbdFixtureHasValidProvenanceHashAndSanitization()
{
    FbdFixtureManifest manifest = FbdFixtureManifest.Load(TestDataPath("manifest.json"));

    Assert.Contains(
        manifest.Fixtures,
        fixture => fixture.Provenance == "sanitized-real-v21");
    Assert.All(manifest.Fixtures, fixture =>
    {
        string path = TestDataPath(fixture.File);
        Assert.True(File.Exists(path), path);
        Assert.Equal(fixture.Sha256, ComputeSha256(path));
        Assert.False(string.IsNullOrWhiteSpace(fixture.TiaPublicApiBuild));
        Assert.NotEmpty(fixture.SanitizationActions);
        Assert.DoesNotContain(File.ReadAllText(path), ForbiddenFixturePattern);
    });
}
~~~

Implement <code>ForbiddenFixturePattern</code> as a case-insensitive compiled regex covering URL user information, <code>password</code>, <code>token</code>, IPv4/IPv6 addresses, drive-rooted paths, UNC paths, and the supplied source's customer/project/device/author tokens.

- [ ] **Step 2 [2–5 min]: Run the manifest test and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~FbdFixtureManifestTests.EveryFbdFixtureHasValidProvenanceHashAndSanitization"
~~~

Expected: FAIL because <code>manifest.json</code> or the real V21 file does not exist.

- [ ] **Step 3 [2–5 min]: Copy the supplied export to the exact fixture path**

Use <code>Copy-Item -LiteralPath</code> from the approved supplied artifact to <code>src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/real-v21-fbd-sanitized.xml</code>. Do not use any XML from <code>priv/TIA_internal</code>; those are schema references, not sanitized real FBD exports.

- [ ] **Step 4 [2–5 min]: Sanitize one identity category at a time**

Replace project, PLC/device, block, author, comment identity, address, credential-like, and absolute-path values with stable neutral values <code>FixtureProject</code>, <code>FixturePLC</code>, <code>FixtureFbdBlock</code>, <code>FixtureAuthor</code>, <code>FixtureComment</code>, <code>192.0.2.1</code> only if an address field must structurally remain, and <code>C:\Fixture\Path</code> only if a path field must structurally remain. Then remove the address/path entirely because AC-101 forbids them in the committed fixture. Preserve namespaces, element kinds, connector names, graph topology, and programming language <code>FBD</code>.

- [ ] **Step 5 [2–5 min]: Add the exact synthetic edge-case XML**

Create a compact <code>Document</code> containing one FBD block and one <code>FlgNet</code> with an <code>Access</code>, a known <code>Part Name="And"</code>, an unknown <code>Part Name="FixtureUnknownPart"</code>, one <code>Call</code>, a power rail, an open connector, named <code>IN1</code>/<code>IN2</code>/<code>OUT</code> connectors, and three wires. Use only fixture identities and set all UIds to small positive integers. The parser tests already document the repository's accepted SimaticML namespace and element shape; copy that exact namespace/shape and change <code>ProgrammingLanguage</code> to <code>FBD</code>.

- [ ] **Step 6 [2–5 min]: Create manifest values from the sanitized bytes**

Record the installed V21 Public API build, original encoding/BOM, every sanitization action, expected support <code>Full</code> for the real file and <code>Partial</code> for the synthetic unknown-part file, and independently reviewed graph counts. Compute lowercase SHA-256 with:

~~~powershell
(Get-FileHash -Algorithm SHA256 -LiteralPath 'src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/real-v21-fbd-sanitized.xml').Hash.ToLowerInvariant()
(Get-FileHash -Algorithm SHA256 -LiteralPath 'src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21/synthetic-fbd-edge-cases.xml').Hash.ToLowerInvariant()
~~~

Paste the two returned digests as the final JSON values; do not use an all-zero or generated-at-test-time hash.

- [ ] **Step 7 [2–5 min]: Run the sanitization gate and confirm GREEN**

Run the focused test from Step 2.

Expected: PASS; at least one entry has provenance <code>sanitized-real-v21</code>, both hashes match, and no forbidden value is found.

- [ ] **Step 8 [2–5 min]: Commit the fixture increment**

~~~powershell
git add src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21 src/TiaGitAddIn.Tests/Services/FbdFixtureManifestTests.cs
git commit -m "test: add sanitized V21 FBD fixtures"
~~~

### Task 3: Neutral FBD Blocks, Networks, Nodes, Pins, and Stable Keys

**Acceptance criteria:** AC-044, AC-045, AC-046, AC-047.

**Files:**
- Create: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdSemanticKeyBuilder.cs</code>
- Create: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphBuilder.cs</code>
- Create: <code>src/TiaGitAddIn.Tests/Services/FbdGraphBuilderTests.cs</code>

**Interfaces:**
- Consumes: <code>SimaticMlFile</code>, <code>BlockDefinition</code>, <code>CompileUnitDefinition</code>, <code>NetworkSourceDefinition</code>, <code>FbdGraphLimits</code>.
- Produces: <code>FbdGraphBuilder.Build(SimaticMlFile, PlcRevisionSide, FbdGraphLimits, CancellationToken)</code>, plus stable key/signature methods locked above.

- [ ] **Step 1 [2–5 min]: Write the real-fixture graph RED test**

~~~csharp
[Fact]
public void BuildsSemanticGraphFromSanitizedV21Export()
{
    FbdFixture fixture = FbdFixtureManifest.LoadRealFbd();
    SimaticMlParseResult parsed = SimaticMlParser.ParseText(
        File.ReadAllText(fixture.Path),
        SimaticMlParserLimits.Default,
        PlcRevisionSide.Left,
        CancellationToken.None);

    FbdGraphBuildResult result = new FbdGraphBuilder().Build(
        Assert.IsType<SimaticMlFile>(parsed.Model),
        PlcRevisionSide.Left,
        FbdGraphLimits.Default,
        CancellationToken.None);

    Assert.Equal(fixture.ExpectedBlocks, result.Graph.Blocks.Count);
    Assert.Equal(fixture.ExpectedNetworks, result.Graph.Blocks.Sum(block => block.Networks.Count));
    Assert.Equal(fixture.ExpectedNodes, result.Graph.Blocks.SelectMany(block => block.Networks).Sum(network => network.Nodes.Count));
    Assert.Contains(
        result.Graph.Blocks.SelectMany(block => block.Networks).SelectMany(network => network.Nodes),
        node => node.Kind == LogicNodeKind.Access);
    Assert.Contains(
        result.Graph.Blocks.SelectMany(block => block.Networks).SelectMany(network => network.Nodes),
        node => node.Kind == LogicNodeKind.Call);
}
~~~

- [ ] **Step 2 [2–5 min]: Run the builder test and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~FbdGraphBuilderTests.BuildsSemanticGraphFromSanitizedV21Export"
~~~

Expected: FAIL to compile with CS0246 for <code>FbdGraphBuilder</code>.

- [ ] **Step 3 [2–5 min]: Implement NFC/ordinal block keys**

~~~csharp
public static string BuildBlockKey(BlockDefinition block)
{
    if (block == null)
    {
        throw new ArgumentNullException(nameof(block));
    }

    return string.Join(
        "|",
        Normalize(block.BlockKind),
        Normalize(block.Name),
        block.Number?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
}

private static string Normalize(string? value) =>
    (value ?? string.Empty).Trim().Normalize(NormalizationForm.FormC);
~~~

Do not use <code>BlockDefinition.Id</code>.

- [ ] **Step 4 [2–5 min]: Implement deterministic network keys**

Use the normalized title when it is non-empty. Otherwise hash the sorted exact node signatures with SHA-256 and lowercase hex; do not use <code>CompileUnitDefinition.Id</code>, <code>CompositionName</code> when it is merely a collection label, or list index.

~~~csharp
public static string BuildNetworkKey(
    string blockKey,
    CompileUnitDefinition compileUnit,
    IEnumerable<LogicNode> nodes)
{
    string title = ExtractNetworkTitle(compileUnit);
    string identity = title.Length != 0
        ? "title:" + title
        : "fingerprint:" + Sha256(string.Join(
            "\n",
            nodes.Select(BuildExactSignature).OrderBy(value => value, StringComparer.Ordinal)));
    return blockKey + "/network/" + identity;
}
~~~

When one block has duplicate unnamed fingerprints, append a deterministic duplicate discriminator derived from the sorted edge signatures, never the source order. If both fingerprints remain identical, retain a duplicate group for conservative add/remove matching rather than inventing an index identity.

- [ ] **Step 5 [2–5 min]: Build typed Access, Part, Call, rail, branch, and generic nodes**

Implement one private method per input kind. Node operation/operand rules are exact:

| Input | Kind | Operation | Operand |
|---|---|---|---|
| <code>AccessDefinition</code> | Access | normalized <code>Scope</code> or <code>Access</code> | <code>SymbolPath</code>, else typed constant <code>ConstantType:ConstantValue</code> |
| known <code>PartDefinition</code> | Part | normalized <code>Name</code> | normalized <code>Equation</code> |
| unknown <code>PartDefinition</code> | GenericPart | normalized <code>Name</code>, capped at 256 chars | normalized <code>Equation</code> |
| <code>CallDefinition</code> | Call | normalized <code>CallInfo.BlockType:CallInfo.Name</code> | normalized instance name/scope |
| power rail | PowerRail | <code>PowerRail</code> | empty |
| open branch | OpenBranch | <code>OpenBranch</code> | empty |

Copy semantic attributes in ordinal key order. Include part version, <code>DisabledENO</code>, template values, automatic typing, negation, invisibility, call parameters, and instance scope. Exclude every UId and raw XML string.

- [ ] **Step 6 [2–5 min]: Derive pins and directions**

Create pins from call parameters and every <code>NameCon</code> observed for a node. Direction rules:

~~~csharp
private static LogicPinDirection DirectionForSection(string? section) =>
    Normalize(section) switch
    {
        "Input" => LogicPinDirection.Input,
        "Output" => LogicPinDirection.Output,
        "InOut" => LogicPinDirection.Bidirectional,
        _ => LogicPinDirection.Unknown
    };
~~~

For known part connectors use immutable ordinal sets:

~~~csharp
private static readonly ISet<string> OutputConnectorNames =
    new HashSet<string>(new[] { "OUT", "ENO", "Q", "Ret_Val" }, StringComparer.Ordinal);
~~~

All other named connectors are <code>Input</code> unless both roles are observed, in which case use <code>Bidirectional</code>. Sort pins by direction, NFC name, then duplicate ordinal. Set <code>IsConnected=false</code> when a named connector has no second resolvable endpoint.

- [ ] **Step 7 [2–5 min]: Preserve UId only in trace**

Create each node trace with:

~~~csharp
new FbdSourceTrace(
    side,
    sourceUId?.ToString(CultureInfo.InvariantCulture),
    location: null)
~~~

Use a side-local <code>Dictionary&lt;int, string&gt;</code> only while resolving parsed UId references. Dispose of that map with the builder call; do not expose it in the graph.

- [ ] **Step 8 [2–5 min]: Add reorder/UId key tests**

~~~csharp
[Fact]
public void StableKeysIgnoreSerializationOrderAndSourceUIds()
{
    SimaticMlFile left = FbdTestGraphs.Parse("reordered-left");
    SimaticMlFile right = FbdTestGraphs.Parse("reordered-right-with-new-uids");
    var builder = new FbdGraphBuilder();

    LogicNetworkGraph leftGraph = builder.Build(left, PlcRevisionSide.Left, FbdGraphLimits.Default, CancellationToken.None).Graph;
    LogicNetworkGraph rightGraph = builder.Build(right, PlcRevisionSide.Right, FbdGraphLimits.Default, CancellationToken.None).Graph;

    Assert.Equal(
        leftGraph.Blocks.Select(block => block.Key).OrderBy(key => key, StringComparer.Ordinal),
        rightGraph.Blocks.Select(block => block.Key).OrderBy(key => key, StringComparer.Ordinal));
    Assert.Equal(
        leftGraph.Blocks.SelectMany(block => block.Networks).Select(network => network.Key).OrderBy(key => key, StringComparer.Ordinal),
        rightGraph.Blocks.SelectMany(block => block.Networks).Select(network => network.Key).OrderBy(key => key, StringComparer.Ordinal));
}
~~~

- [ ] **Step 9 [2–5 min]: Run all builder tests and confirm GREEN**

Run the Task 3 class filter.

Expected: PASS with manifest-declared counts, typed nodes, stable reorder keys, and no UId in any semantic key/signature.

- [ ] **Step 10 [2–5 min]: Commit the node extraction increment**

~~~powershell
git add src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdSemanticKeyBuilder.cs src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphBuilder.cs src/TiaGitAddIn.Tests/Services/FbdGraphBuilderTests.cs
git commit -m "feat: extract neutral FBD graph nodes"
~~~

### Task 4: Semantic Edges, Open Connectors, Unknown Parts, and Graph Limits

**Acceptance criteria:** AC-019, AC-045, AC-051, AC-052, AC-095, AC-096.

**Files:**
- Modify: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphBuilder.cs</code>
- Modify: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdSemanticKeyBuilder.cs</code>
- Modify: <code>src/TiaGitAddIn.Tests/Services/FbdGraphBuilderTests.cs</code>

**Interfaces:**
- Consumes: Task 3 node lookup and foundation diagnostics.
- Produces: complete <code>LogicEdge</code> lists, summaries, <code>HasTrustworthyConnectivity</code>, <code>HasUnknownParts</code>, and stable diagnostics <code>FBD001</code>/<code>FBD002</code>/<code>FBD003</code>.

- [ ] **Step 1 [2–5 min]: Write connector/open-edge RED tests**

~~~csharp
[Fact]
public void PreservesConnectorSemanticsAndLeavesOpenConnectorUnconnected()
{
    FbdGraphBuildResult result = BuildSyntheticEdgeCases();
    LogicNetwork network = Assert.Single(Assert.Single(result.Graph.Blocks).Networks);
    LogicNode andNode = Assert.Single(network.Nodes.Where(node => node.Operation == "And"));

    Assert.Contains(andNode.Pins, pin =>
        pin.Name == "IN1" &&
        pin.Direction == LogicPinDirection.Input &&
        pin.IsConnected);
    Assert.Contains(andNode.Pins, pin =>
        pin.Name == "IN2" &&
        pin.Direction == LogicPinDirection.Input &&
        !pin.IsConnected);
    Assert.All(network.Edges, edge =>
    {
        Assert.Contains(network.Nodes, node => node.Key == edge.Source.NodeKey);
        Assert.Contains(network.Nodes, node => node.Key == edge.Target.NodeKey);
    });
}
~~~

- [ ] **Step 2 [2–5 min]: Write unknown/summary/limit RED tests**

~~~csharp
[Fact]
public void UnknownPartIsPartialAndConnectivityFailureRetainsSummary()
{
    FbdGraphBuildResult result = BuildSyntheticEdgeCases();

    Assert.True(result.HasUnknownParts);
    Assert.Contains(
        result.Graph.Blocks.SelectMany(block => block.Networks).SelectMany(network => network.Nodes),
        node => node.Kind == LogicNodeKind.GenericPart &&
                node.Operation == "FixtureUnknownPart");
    Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "FBD001");
    Assert.NotEmpty(result.Summaries);
}

[Fact]
public void StopsAtConfiguredNodeLimit()
{
    FbdGraphLimits limits = new FbdGraphLimits(1, 1, 1, 4, 1, 4);
    FbdGraphBuildResult result = new FbdGraphBuilder().Build(
        FbdTestGraphs.TwoNodeNetwork(),
        PlcRevisionSide.Left,
        limits,
        CancellationToken.None);

    Assert.False(result.HasTrustworthyConnectivity);
    Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "FBD003");
}
~~~

- [ ] **Step 3 [2–5 min]: Run the two tests and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~FbdGraphBuilderTests.PreservesConnectorSemanticsAndLeavesOpenConnectorUnconnected|FullyQualifiedName~FbdGraphBuilderTests.UnknownPartIsPartialAndConnectivityFailureRetainsSummary|FullyQualifiedName~FbdGraphBuilderTests.StopsAtConfiguredNodeLimit"
~~~

Expected: FAIL because edges, diagnostics, summaries, or limit stopping are absent.

- [ ] **Step 4 [2–5 min]: Resolve wire endpoints without identity leakage**

For each wire, resolve connections through the side-local UId map, create/update named pins, and sort resolvable endpoints by node key then pin key. Emit star edges from the first endpoint to every later endpoint. A wire with fewer than two resolvable endpoints emits no edge and leaves its pin disconnected.

~~~csharp
private static IEnumerable<LogicEdge> BuildEdges(
    WireDefinition wire,
    IReadOnlyDictionary<int, string> nodeKeyBySourceUId,
    IReadOnlyDictionary<string, LogicNode> nodeByKey,
    PlcRevisionSide side)
{
    List<LogicEndpoint> endpoints = ResolveEndpoints(wire, nodeKeyBySourceUId, nodeByKey)
        .Distinct(LogicEndpointComparer.Instance)
        .OrderBy(endpoint => endpoint.NodeKey, StringComparer.Ordinal)
        .ThenBy(endpoint => endpoint.PinKey, StringComparer.Ordinal)
        .ToList();

    if (endpoints.Count < 2)
    {
        return Array.Empty<LogicEdge>();
    }

    LogicEndpoint anchor = endpoints[0];
    return endpoints
        .Skip(1)
        .Select(target => CreateEdge(anchor, target, wire, side))
        .OrderBy(edge => edge.Key, StringComparer.Ordinal)
        .ToArray();
}
~~~

<code>CreateEdge</code> derives its key from canonical endpoint node/pin keys and uses the wire UId only in <code>FbdSourceTrace</code>.

- [ ] **Step 5 [2–5 min]: Add deterministic summaries and support diagnostics**

For every parsed network add <code>FbdNetworkSummary</code> with stable block/network keys and actual node/pin/edge counts. Set:

- <code>FBD001</code>, Warning, “Unknown FBD part rendered as a generic function box.” for any generic part;
- <code>FBD002</code>, Warning, “FBD connectivity is incomplete; showing the structured graph summary.” when metadata exists but endpoint resolution cannot establish trustworthy connectivity;
- <code>FBD003</code>, Error, “FBD graph limit exceeded; semantic graph construction stopped.” at the exact configured boundary.

Do not include raw XML, source paths, stack traces, or UIds in those messages.

- [ ] **Step 6 [2–5 min]: Enforce limits before allocation**

Check block/network count before entering each collection, node count before creating a node, and edge count before creating an edge. Check <code>cancellationToken.ThrowIfCancellationRequested()</code> before each network and every 128 nodes/edges. On the first limit failure, stop that side and return the summaries collected to that boundary.

- [ ] **Step 7 [2–5 min]: Run builder tests and confirm GREEN**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.Services.FbdGraphBuilderTests"
~~~

Expected: PASS; open pins remain visible and unconnected, unknown nodes are generic/partial, limit input stops deterministically, and every edge references valid semantic endpoints.

- [ ] **Step 8 [2–5 min]: Commit the connectivity increment**

~~~powershell
git add src/TiaGitAddIn.Core/Services/Comparison/Fbd src/TiaGitAddIn.Tests/Services/FbdGraphBuilderTests.cs
git commit -m "feat: extract FBD connector topology"
~~~

### Task 5: Exact-Signature, Neighbourhood, Ambiguity, and Rewire Comparison

**Acceptance criteria:** AC-046, AC-047, AC-048, AC-049, AC-050, AC-116.

**Files:**
- Create: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphComparer.cs</code>
- Modify: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdSemanticKeyBuilder.cs</code>
- Create: <code>src/TiaGitAddIn.Tests/Services/FbdGraphComparerTests.cs</code>

**Interfaces:**
- Consumes: two <code>FbdGraphBuildResult</code> values.
- Produces: <code>FbdGraphComparer.Compare(left, right, CancellationToken)</code> returning a visual <code>FbdPresentation</code> with immutable, deterministically ordered <code>LogicBlockDiff</code>, <code>LogicNetworkDiff</code>, <code>LogicNodeDiff</code>, and <code>LogicEdgeDiff</code> collections.

- [ ] **Step 1 [2–5 min]: Write UId-only and exact-stage RED tests**

~~~csharp
[Fact]
public void UIdRegenerationIsTraceOnlyAndExactSignatureMatchesFirst()
{
    FbdGraphBuildResult left = BuildGraph("same-semantics-uids-1");
    FbdGraphBuildResult right = BuildGraph("same-semantics-uids-900");

    FbdPresentation result = new FbdGraphComparer().Compare(
        left,
        right,
        CancellationToken.None);

    LogicNodeDiff node = Assert.Single(
        Assert.Single(Assert.Single(result.Blocks).Networks).Nodes);
    Assert.Equal(LogicDiffStatus.Unchanged, node.Status);
    Assert.Equal(LogicMatchStage.ExactSignature, node.MatchStage);
    Assert.NotEqual(node.Left!.Trace.SourceUId, node.Right!.Trace.SourceUId);
}
~~~

- [ ] **Step 2 [2–5 min]: Write neighbourhood/ambiguity RED tests**

~~~csharp
[Fact]
public void DuplicateExactSignaturesUseUniqueNeighbourhoodAndAmbiguityIsNotGuessed()
{
    FbdPresentation unique = Compare("duplicate-left", "duplicate-right-unique-neighbourhood");
    Assert.Equal(
        2,
        unique.Blocks.SelectMany(block => block.Networks)
            .SelectMany(network => network.Nodes)
            .Count(node => node.MatchStage == LogicMatchStage.Neighbourhood));

    FbdPresentation ambiguous = Compare("ambiguous-left", "ambiguous-right");
    LogicNodeDiff[] ambiguousNodes = ambiguous.Blocks
        .SelectMany(block => block.Networks)
        .SelectMany(network => network.Nodes)
        .Where(node => node.Status != LogicDiffStatus.Unchanged)
        .ToArray();

    Assert.All(ambiguousNodes, node =>
        Assert.Contains(node.Status, new[] { LogicDiffStatus.Added, LogicDiffStatus.Removed }));
    Assert.DoesNotContain(ambiguousNodes, node => node.Status == LogicDiffStatus.Modified);
}
~~~

- [ ] **Step 3 [2–5 min]: Write add/remove/modify/rewire RED test**

~~~csharp
[Fact]
public void ReportsAddRemoveModifyAndRewire()
{
    FbdPresentation result = Compare("change-left", "change-right");
    LogicNetworkDiff network = Assert.Single(Assert.Single(result.Blocks).Networks);

    Assert.Single(network.Nodes.Where(node => node.Status == LogicDiffStatus.Added));
    Assert.Single(network.Nodes.Where(node => node.Status == LogicDiffStatus.Removed));
    Assert.Single(network.Nodes.Where(node => node.Status == LogicDiffStatus.Modified));
    LogicEdgeDiff rewired = Assert.Single(
        network.Edges.Where(edge => edge.Status == LogicDiffStatus.Rewired));
    Assert.NotNull(rewired.Left);
    Assert.NotNull(rewired.Right);
}
~~~

- [ ] **Step 4 [2–5 min]: Run comparer tests and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.Services.FbdGraphComparerTests"
~~~

Expected: FAIL to compile with CS0246 for <code>FbdGraphComparer</code>.

- [ ] **Step 5 [2–5 min]: Implement the exact semantic signature**

~~~csharp
public static string BuildExactSignature(LogicNode node)
{
    string attributes = string.Join(
        ";",
        node.Attributes.Select(pair => pair.Key + "=" + pair.Value));
    string pins = string.Join(
        ";",
        node.Pins
            .OrderBy(pin => pin.Direction)
            .ThenBy(pin => pin.Name, StringComparer.Ordinal)
            .ThenBy(pin => pin.Ordinal)
            .Select(pin => pin.Direction + ":" + Normalize(pin.Name)));

    return string.Join(
        "|",
        node.Kind.ToString(),
        Normalize(node.Operation),
        Normalize(node.Operand),
        attributes,
        pins);
}
~~~

This method deliberately reads no <code>FbdSourceTrace</code>.

- [ ] **Step 6 [2–5 min]: Implement the neighbourhood signature**

The second-stage signature retains node kind and connector shape, but excludes the node's own operation/operand so a unique structural slot can pair a changed operation/operand. It includes exact signatures of adjacent nodes and both connector names:

~~~csharp
public static string BuildNeighbourhoodSignature(
    LogicNode node,
    LogicNetwork network)
{
    IReadOnlyDictionary<string, LogicNode> nodes = network.Nodes.ToDictionary(
        item => item.Key,
        item => item,
        StringComparer.Ordinal);
    string pinShape = string.Join(
        ";",
        node.Pins
            .OrderBy(pin => pin.Direction)
            .ThenBy(pin => pin.Name, StringComparer.Ordinal)
            .ThenBy(pin => pin.Ordinal)
            .Select(pin => pin.Direction + ":" + Normalize(pin.Name)));
    string neighbours = string.Join(
        ";",
        IncidentDescriptors(node.Key, network, nodes)
            .OrderBy(value => value, StringComparer.Ordinal));

    return node.Kind + "|" + pinShape + "|" + neighbours;
}
~~~

<code>IncidentDescriptors</code> returns <code>direction|localPin|neighbourExactSignature|neighbourPin</code>. It canonicalizes incoming/outgoing direction and never uses edge/node UIds.

- [ ] **Step 7 [2–5 min]: Pair only unique groups in two stages**

~~~csharp
private static IReadOnlyList<NodeMatch> PairUnique(
    IEnumerable<LogicNode> left,
    IEnumerable<LogicNode> right,
    Func<LogicNode, string> leftSignature,
    Func<LogicNode, string> rightSignature,
    LogicMatchStage stage)
{
    Dictionary<string, LogicNode[]> leftGroups = left
        .GroupBy(leftSignature, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
    Dictionary<string, LogicNode[]> rightGroups = right
        .GroupBy(rightSignature, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

    return leftGroups.Keys
        .Intersect(rightGroups.Keys, StringComparer.Ordinal)
        .Where(key => leftGroups[key].Length == 1 && rightGroups[key].Length == 1)
        .OrderBy(key => key, StringComparer.Ordinal)
        .Select(key => new NodeMatch(leftGroups[key][0], rightGroups[key][0], stage))
        .ToArray();
}
~~~

Remove matched nodes from new unmatched sets after the exact stage, recompute neighbourhood signatures on the original immutable networks, then run the neighbourhood stage. A node appears in at most one <code>NodeMatch</code>.

- [ ] **Step 8 [2–5 min]: Assign deterministic node statuses**

- exact match with equal semantic fingerprint: <code>Unchanged</code>;
- neighbourhood match with equal semantic fingerprint: <code>Unchanged</code>;
- neighbourhood match with unequal semantic fingerprint: <code>Modified</code>;
- unmatched left: <code>Removed</code>;
- unmatched right: <code>Added</code>.

Order output by <code>Key</code> with <code>StringComparer.Ordinal</code>, then by status only to resolve an identical key. Derive diff key from stable network key plus matched node keys; never from source order.

- [ ] **Step 9 [2–5 min]: Compare edges on mapped endpoints**

Map every left endpoint through the node-match table into right-side node keys. First pair exact endpoint-and-pin signatures. Then pair unique unmatched edges whose mapped node pair is equal while a pin differs. Finally pair a unique one-common-endpoint candidate. Both latter cases are <code>Rewired</code>; ambiguous candidates remain separate <code>Removed</code>/<code>Added</code>.

~~~csharp
private static LogicDiffStatus EdgeStatus(
    LogicEdge mappedLeft,
    LogicEdge right)
{
    return FbdSemanticKeyBuilder.BuildEdgeSignature(mappedLeft) ==
           FbdSemanticKeyBuilder.BuildEdgeSignature(right)
        ? LogicDiffStatus.Unchanged
        : LogicDiffStatus.Rewired;
}
~~~

- [ ] **Step 10 [2–5 min]: Match blocks/networks and aggregate status**

Match blocks and networks only when their stable keys are unique in both sides. Identical duplicate-key groups remain deterministic add/remove groups. A missing side is Added/Removed; any changed child makes the parent Modified; otherwise it is Unchanged. Sort blocks and networks by key ordinal.

- [ ] **Step 11 [2–5 min]: Run comparer tests twice and confirm GREEN/determinism**

Run the class-filter command from Step 4 twice.

Expected both runs: PASS with identical test output; exact-stage, neighbourhood-stage, ambiguity, and rewiring assertions all hold.

- [ ] **Step 12 [2–5 min]: Commit the comparer increment**

~~~powershell
git add src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdGraphComparer.cs src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdSemanticKeyBuilder.cs src/TiaGitAddIn.Tests/Services/FbdGraphComparerTests.cs
git commit -m "feat: compare FBD graphs semantically"
~~~

### Task 6: FBD Strategy, Partial/Structured/Text Outcomes, and Foundation Registration

**Acceptance criteria:** AC-008, AC-014, AC-017, AC-018, AC-019, AC-023, AC-051, AC-052, AC-095, AC-096, AC-105, AC-113, AC-117.

**Files:**
- Create: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdComparisonStrategy.cs</code>
- Create: <code>src/TiaGitAddIn.Tests/Services/FbdComparisonStrategyTests.cs</code>
- Modify: <code>src/TiaGitAddIn/UI/GitPanelLaunchService.cs</code> only at the foundation-created strategy composition.

**Interfaces:**
- Consumes: <code>SimaticMlParser.ParseText</code>, <code>SimaticMlParserLimits</code>, <code>FbdGraphBuilder</code>, <code>FbdGraphComparer</code>, <code>PlcComparisonResultFactory</code>, <code>ComparisonTestData</code>.
- Produces: <code>FbdComparisonStrategy : IPlcComparisonStrategy</code>, supporting exactly <code>PlcArtifactKind.Fbd</code>.

- [ ] **Step 1 [2–5 min]: Write full/partial/structured/fallback RED theory**

~~~csharp
[Theory]
[InlineData("full-left", "full-right", PlcComparisonMode.Visual, PlcSupportLevel.Full, true)]
[InlineData("known-left", "unknown-part-right", PlcComparisonMode.Visual, PlcSupportLevel.Partial, true)]
[InlineData("metadata-left", "broken-connectivity-right", PlcComparisonMode.Structured, PlcSupportLevel.Partial, false)]
[InlineData("malformed-left", "full-right", PlcComparisonMode.Text, PlcSupportLevel.Fallback, false)]
public async Task ProducesDeterministicFbdOutcome(
    string leftText,
    string rightText,
    PlcComparisonMode expectedMode,
    PlcSupportLevel expectedSupport,
    bool expectedVisual)
{
    PlcComparisonContext context = ComparisonTestData.Context(
        PlcArtifactKind.Fbd,
        PlcComparisonMode.Visual,
        FbdSources.Get(leftText),
        FbdSources.Get(rightText));

    PlcComparisonResult result = await CreateStrategy().CompareAsync(
        context,
        CancellationToken.None);

    Assert.Equal(PlcArtifactKind.Fbd, result.ArtifactKind);
    Assert.Equal(expectedMode, result.ActualMode);
    Assert.Equal(expectedSupport, result.SupportLevel);
    Assert.Equal(expectedVisual, Assert.IsType<FbdPresentation>(result.Presentation).HasVisualGraph);
    Assert.NotNull(result.RawText);
}
~~~

- [ ] **Step 2 [2–5 min]: Write result-invariant and cancellation RED tests**

~~~csharp
[Fact]
public async Task EveryFbdOutcomeSatisfiesComparisonResultInvariant()
{
    PlcComparisonResult[] results =
    {
        await CompareAsync("full-left", "full-right"),
        await CompareAsync("known-left", "unknown-part-right"),
        await CompareAsync("malformed-left", "full-right")
    };

    Assert.All(results, result =>
    {
        Assert.Equal(PlcArtifactKind.Fbd, result.ArtifactKind);
        Assert.NotNull(result.Presentation);
        Assert.NotNull(result.Diagnostics);
        Assert.Equal(result.SupportLevel == PlcSupportLevel.Full, result.Limitation.Length == 0);
    });
}

[Fact]
public async Task CancellationThrowsWithoutCreatingFallback()
{
    using var cancellation = new CancellationTokenSource();
    cancellation.Cancel();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(
        () => CreateStrategy().CompareAsync(
            ComparisonTestData.Context(PlcArtifactKind.Fbd, PlcComparisonMode.Visual),
            cancellation.Token));
}
~~~

- [ ] **Step 3 [2–5 min]: Run strategy tests and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.Services.FbdComparisonStrategyTests"
~~~

Expected: FAIL to compile with CS0246 for <code>FbdComparisonStrategy</code>.

- [ ] **Step 4 [2–5 min]: Add the exact constructor and supported-kind contract**

~~~csharp
public sealed class FbdComparisonStrategy : IPlcComparisonStrategy
{
    private static readonly IReadOnlyCollection<PlcArtifactKind> FbdKinds =
        Array.AsReadOnly(new[] { PlcArtifactKind.Fbd });

    private readonly FbdGraphBuilder graphBuilder;
    private readonly FbdGraphComparer comparer;
    private readonly PlcComparisonResultFactory resultFactory;
    private readonly SimaticMlParserLimits parserLimits;
    private readonly FbdGraphLimits graphLimits;

    public FbdComparisonStrategy(
        FbdGraphBuilder graphBuilder,
        FbdGraphComparer comparer,
        PlcComparisonResultFactory resultFactory,
        SimaticMlParserLimits parserLimits,
        FbdGraphLimits graphLimits)
    {
        this.graphBuilder = graphBuilder ?? throw new ArgumentNullException(nameof(graphBuilder));
        this.comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        this.resultFactory = resultFactory ?? throw new ArgumentNullException(nameof(resultFactory));
        this.parserLimits = parserLimits ?? throw new ArgumentNullException(nameof(parserLimits));
        this.graphLimits = graphLimits ?? throw new ArgumentNullException(nameof(graphLimits));
    }

    public IReadOnlyCollection<PlcArtifactKind> SupportedKinds => FbdKinds;
}
~~~

- [ ] **Step 5 [2–5 min]: Parse each available side through the hardened seam**

~~~csharp
private FbdSideResult BuildSide(
    PlcRevision revision,
    CancellationToken cancellationToken)
{
    if (revision.IsMissing)
    {
        return FbdSideResult.Empty(revision.Side);
    }

    SimaticMlParseResult parse = SimaticMlParser.ParseText(
        revision.Text ?? string.Empty,
        parserLimits,
        revision.Side,
        cancellationToken);
    if (!parse.IsSuccess)
    {
        return FbdSideResult.ParseFailure(parse.Diagnostics);
    }

    FbdGraphBuildResult graph = graphBuilder.Build(
        parse.Model!,
        revision.Side,
        graphLimits,
        cancellationToken);
    return FbdSideResult.Success(
        graph,
        parse.IsPartial,
        parse.Diagnostics.Concat(graph.Diagnostics));
}
~~~

<code>FbdSideResult</code> is an internal immutable helper in the same file. It copies diagnostics and distinguishes missing, parse failure, and built graph without throwing away safe diagnostics.

- [ ] **Step 6 [2–5 min]: Implement exact outcome precedence**

~~~csharp
public Task<PlcComparisonResult> CompareAsync(
    PlcComparisonContext context,
    CancellationToken cancellationToken)
{
    if (context == null)
    {
        throw new ArgumentNullException(nameof(context));
    }

    cancellationToken.ThrowIfCancellationRequested();
    FbdSideResult left = BuildSide(context.Request.Left, cancellationToken);
    FbdSideResult right = BuildSide(context.Request.Right, cancellationToken);
    PlcComparisonResult result = CreateResult(context, left, right, cancellationToken);
    return Task.FromResult(result);
}
~~~

<code>CreateResult</code> applies this exact precedence:

1. Any non-missing parse failure: <code>CreateTextFallback</code>, limitation “FBD SimaticML could not be parsed; raw text comparison is shown.”, preserving parser diagnostics.
2. Any <code>HasTrustworthyConnectivity=false</code>: <code>CreateSemantic</code> with <code>Structured</code>, <code>Partial</code>, <code>FbdPresentation.Structured</code>, limitation “FBD connectivity is incomplete; a structured graph summary is shown.”.
3. Otherwise compare graphs. Unknown part or partial parse: <code>Visual</code>, <code>Partial</code>, limitation “Some FBD elements are rendered generically; review diagnostics and raw text.”.
4. Otherwise: <code>Visual</code>, <code>Full</code>, empty limitation.

Every branch passes the original <code>PlcComparisonContext</code> to the result factory so decoded raw text remains selectable.

- [ ] **Step 7 [2–5 min]: Register only the FBD strategy**

In the foundation-created immutable strategy array in <code>GitPanelLaunchService.cs</code>, add exactly one final element:

~~~csharp
new FbdComparisonStrategy(
    new FbdGraphBuilder(),
    new FbdGraphComparer(),
    comparisonResultFactory,
    SimaticMlParserLimits.Default,
    FbdGraphLimits.Default)
~~~

Do not change the existing LAD strategy or route LAD through <code>FbdGraphBuilder</code>.

- [ ] **Step 8 [2–5 min]: Run strategy and LAD regression tests**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~FbdComparisonStrategyTests|FullyQualifiedName~LadLayoutEngineTests|FullyQualifiedName~SimaticMlComparerTests|FullyQualifiedName~LadDiffViewModelTests|FullyQualifiedName~LadDiffViewXamlTests"
~~~

Expected: PASS. FBD results satisfy the result invariant; malformed FBD is Text/Fallback; existing LAD regressions remain unchanged.

- [ ] **Step 9 [2–5 min]: Commit the strategy increment**

~~~powershell
git add src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdComparisonStrategy.cs src/TiaGitAddIn.Tests/Services/FbdComparisonStrategyTests.cs src/TiaGitAddIn/UI/GitPanelLaunchService.cs
git commit -m "feat: route FBD comparison safely"
~~~

### Task 7: Deterministic Union-Topology Layout

**Acceptance criteria:** AC-053 and the layout portion of AC-054.

**Files:**
- Create: <code>src/TiaGitAddIn.Core/Models/Comparison/Fbd/FbdLayoutModels.cs</code>
- Create: <code>src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdLayoutEngine.cs</code>
- Create: <code>src/TiaGitAddIn.Tests/Services/FbdLayoutEngineTests.cs</code>

**Interfaces:**
- Consumes: visual <code>FbdPresentation</code>.
- Produces: <code>FbdLayoutEngine.Layout(FbdPresentation, CancellationToken)</code> returning identical paired canvas bounds and aligned coordinates for matched left/right nodes.

- [ ] **Step 1 [2–5 min]: Write deterministic snapshot RED test**

~~~csharp
[Fact]
public void IsDeterministicAcrossRunsAndIrrelevantSourceOrdering()
{
    FbdPresentation firstInput = FbdComparisons.BranchAndRewire(order: "forward");
    FbdPresentation reorderedInput = FbdComparisons.BranchAndRewire(order: "reverse");
    var engine = new FbdLayoutEngine();

    string first = LayoutSnapshot(engine.Layout(firstInput, CancellationToken.None));
    string second = LayoutSnapshot(engine.Layout(firstInput, CancellationToken.None));
    string reordered = LayoutSnapshot(engine.Layout(reorderedInput, CancellationToken.None));

    Assert.Equal(first, second);
    Assert.Equal(first, reordered);
}
~~~

- [ ] **Step 2 [2–5 min]: Write alignment/ports/routes/bounds RED test**

~~~csharp
[Fact]
public void AlignsMatchedNodesAndProducesOrthogonalRoutes()
{
    FbdLayoutDocument document = new FbdLayoutEngine().Layout(
        FbdComparisons.BranchAndRewire(order: "forward"),
        CancellationToken.None);
    FbdNetworkPairLayout pair = Assert.Single(document.Networks);

    Assert.Equal(pair.Left.CanvasWidth, pair.Right.CanvasWidth);
    Assert.Equal(pair.Left.CanvasHeight, pair.Right.CanvasHeight);
    Assert.All(pair.MatchedNodeKeys, key =>
    {
        FbdNodeLayout left = Assert.Single(pair.Left.Nodes.Where(node => node.DiffKey == key));
        FbdNodeLayout right = Assert.Single(pair.Right.Nodes.Where(node => node.DiffKey == key));
        Assert.Equal((left.X, left.Y), (right.X, right.Y));
    });
    Assert.All(
        pair.Left.Edges.Concat(pair.Right.Edges),
        edge => Assert.All(edge.Segments, segment =>
            Assert.True(segment.X1 == segment.X2 || segment.Y1 == segment.Y2)));
}
~~~

- [ ] **Step 3 [2–5 min]: Run layout tests and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.Services.FbdLayoutEngineTests"
~~~

Expected: FAIL to compile with CS0246 for <code>FbdLayoutEngine</code>.

- [ ] **Step 4 [2–5 min]: Add immutable layout values**

Create get-only classes with constructor copies:

~~~csharp
public sealed class FbdLayoutDocument
{
    public FbdLayoutDocument(IEnumerable<FbdNetworkPairLayout> networks);
    public IReadOnlyList<FbdNetworkPairLayout> Networks { get; }
}

public sealed class FbdNetworkPairLayout
{
    public FbdNetworkPairLayout(
        string key,
        string title,
        LogicDiffStatus status,
        FbdCanvasLayout left,
        FbdCanvasLayout right,
        IEnumerable<string> matchedNodeKeys);
}

public sealed class FbdCanvasLayout
{
    public FbdCanvasLayout(
        double canvasWidth,
        double canvasHeight,
        IEnumerable<FbdNodeLayout> nodes,
        IEnumerable<FbdEdgeLayout> edges);
}

public sealed class FbdNodeLayout
{
    public FbdNodeLayout(
        string diffKey,
        LogicNode node,
        LogicDiffStatus status,
        double x,
        double y,
        double width,
        double height,
        IEnumerable<FbdPinLayout> pins);
}

public sealed class FbdEdgeLayout
{
    public FbdEdgeLayout(
        string diffKey,
        LogicDiffStatus status,
        IEnumerable<FbdLineSegment> segments);
}
~~~

Use <code>FbdLineSegment(double x1, double y1, double x2, double y2)</code> and <code>FbdPinLayout(string key, string name, LogicPinDirection direction, double x, double y, bool isConnected)</code>.

- [ ] **Step 5 [2–5 min]: Build one union graph per network pair**

Use each <code>LogicNodeDiff.Key</code> as the union vertex. Project both sides' edges through their node diff keys, de-duplicate canonical union edges, and sort vertices/edges ordinal. Matched, added, and removed nodes therefore occupy one shared coordinate system.

- [ ] **Step 6 [2–5 min]: Assign deterministic topology ranks**

Run Kahn topological ranking with an ordinal priority queue implemented as <code>SortedSet&lt;string&gt;</code>. Rank is the longest predecessor path. After Kahn, assign any cyclic remainder in ordinal key order to the smallest rank compatible with already-ranked predecessors. Never enumerate a dictionary without ordering.

- [ ] **Step 7 [2–5 min]: Assign stable rows and coordinates**

Within a rank, sort by predecessor barycentre, then diff key ordinal. Use exact constants:

~~~csharp
private const double CanvasMargin = 32.0;
private const double NodeWidth = 180.0;
private const double NodeMinimumHeight = 72.0;
private const double PinRowHeight = 22.0;
private const double HorizontalGap = 80.0;
private const double VerticalGap = 40.0;
private const double EdgeChannelGap = 10.0;
~~~

Node height is <code>max(NodeMinimumHeight, 40 + max(inputPinCount, outputPinCount) * PinRowHeight)</code>. Matched sides use the maximum of their two heights.

- [ ] **Step 8 [2–5 min]: Route deterministic orthogonal edges**

Sort edge diff keys ordinal. Route source pin horizontally to a per-edge channel, vertically to target Y, then horizontally to target pin. Channel X is source right edge plus <code>(ordinal + 1) * EdgeChannelGap</code>. Compute common left/right canvas bounds from the union, including route extents.

- [ ] **Step 9 [2–5 min]: Run layout tests and confirm GREEN**

Run the class-filter command from Step 3 twice.

Expected both runs: PASS; snapshot strings, connector ordering, routes, and bounds are byte-identical.

- [ ] **Step 10 [2–5 min]: Commit the layout increment**

~~~powershell
git add src/TiaGitAddIn.Core/Models/Comparison/Fbd/FbdLayoutModels.cs src/TiaGitAddIn.Core/Services/Comparison/Fbd/FbdLayoutEngine.cs src/TiaGitAddIn.Tests/Services/FbdLayoutEngineTests.cs
git commit -m "feat: lay out FBD diffs deterministically"
~~~

### Task 8: Typed FBD Presentation ViewModels, Selection, Zoom, and Pan

**Acceptance criteria:** AC-022, AC-023, AC-025, AC-027, AC-029, AC-033, AC-054.

**Files:**
- Create: <code>src/TiaGitAddIn/UI/Mapping/FbdPresentationViewModelFactory.cs</code>
- Create: <code>src/TiaGitAddIn/UI/ViewModels/Comparison/FbdDiffViewModel.cs</code>
- Create: <code>src/TiaGitAddIn/UI/ViewModels/Comparison/FbdNetworkViewModels.cs</code>
- Create: <code>src/TiaGitAddIn.Tests/UI/FbdDiffViewModelTests.cs</code>

**Interfaces:**
- Consumes: <code>IComparisonPresentationViewModelFactory</code>, <code>ComparisonViewModelMetadata</code>, <code>FbdPresentation</code>, <code>FbdLayoutEngine</code>.
- Produces: <code>FbdPresentationViewModelFactory</code>, <code>FbdDiffViewModel : ComparisonPresentationViewModel</code>, paired canvas/node/edge/pin ViewModels, and shared <code>FbdViewportViewModel</code>.

- [ ] **Step 1 [2–5 min]: Write mapper/metadata RED test**

~~~csharp
[Fact]
public void MapsFbdPresentationAndPreservesFoundationMetadata()
{
    PlcComparisonResult result = FbdResults.PartialVisualWithRawText();
    ComparisonViewModelMetadata metadata = ComparisonViewModelMetadata.From(result);
    var factory = new FbdPresentationViewModelFactory(new FbdLayoutEngine());

    FbdDiffViewModel viewModel = Assert.IsType<FbdDiffViewModel>(
        factory.Map(result, metadata));

    Assert.True(factory.CanMap(result.Presentation));
    Assert.Equal("Visual · Partial", viewModel.Header);
    Assert.True(viewModel.HasLimitation);
    Assert.True(viewModel.HasRawText);
    Assert.NotEmpty(viewModel.Networks);
}
~~~

- [ ] **Step 2 [2–5 min]: Write selection/viewport RED test**

~~~csharp
[Fact]
public void SelectionZoomAndPanStaySynchronized()
{
    FbdDiffViewModel viewModel = CreateViewModel();
    FbdNetworkPairViewModel network = Assert.Single(viewModel.Networks);
    string matchedKey = Assert.Single(network.MatchedNodeKeys);

    viewModel.SelectNodeCommand.Execute(matchedKey);
    viewModel.Viewport.ZoomInCommand.Execute(null);
    viewModel.Viewport.PanRightCommand.Execute(null);
    viewModel.Viewport.PanDownCommand.Execute(null);

    Assert.Equal(matchedKey, network.Left.SelectedNodeKey);
    Assert.Equal(matchedKey, network.Right.SelectedNodeKey);
    Assert.Equal(1.25, viewModel.Viewport.Zoom);
    Assert.Equal(48.0, viewModel.Viewport.HorizontalOffset);
    Assert.Equal(48.0, viewModel.Viewport.VerticalOffset);
}
~~~

- [ ] **Step 3 [2–5 min]: Run ViewModel tests and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.UI.FbdDiffViewModelTests"
~~~

Expected: FAIL to compile with CS0246 for <code>FbdPresentationViewModelFactory</code>.

- [ ] **Step 4 [2–5 min]: Implement the specialized factory**

~~~csharp
public sealed class FbdPresentationViewModelFactory :
    IComparisonPresentationViewModelFactory
{
    private readonly FbdLayoutEngine layoutEngine;

    public FbdPresentationViewModelFactory(FbdLayoutEngine layoutEngine)
    {
        this.layoutEngine = layoutEngine ?? throw new ArgumentNullException(nameof(layoutEngine));
    }

    public bool CanMap(ComparisonPresentation presentation) =>
        presentation is FbdPresentation;

    public ComparisonPresentationViewModel Map(
        PlcComparisonResult result,
        ComparisonViewModelMetadata metadata)
    {
        FbdPresentation presentation = result.Presentation as FbdPresentation
            ?? throw new ArgumentException("Result does not contain an FBD presentation.", nameof(result));
        FbdLayoutDocument layout = layoutEngine.Layout(presentation, CancellationToken.None);
        return new FbdDiffViewModel(presentation, layout, metadata);
    }
}
~~~

- [ ] **Step 5 [2–5 min]: Implement the typed root ViewModel**

~~~csharp
public sealed class FbdDiffViewModel : ComparisonPresentationViewModel
{
    private readonly IReadOnlyList<FbdChangeTarget> changes;
    private int selectedChangeIndex = -1;
    private FbdNetworkPairViewModel? selectedNetwork;

    public FbdDiffViewModel(
        FbdPresentation presentation,
        FbdLayoutDocument layout,
        ComparisonViewModelMetadata metadata)
        : base(ComparisonPresentationKind.LogicNetwork, metadata)
    {
        Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
        Networks = Array.AsReadOnly(layout.Networks.Select(FbdNetworkPairViewModel.FromLayout).ToArray());
        changes = BuildChanges(Networks);
        Viewport = new FbdViewportViewModel();
        SelectNodeCommand = new RelayCommand(SelectNode);
        PreviousChangeCommand = new RelayCommand(_ => MoveChange(-1), _ => changes.Count != 0);
        NextChangeCommand = new RelayCommand(_ => MoveChange(1), _ => changes.Count != 0);
        SelectedNetwork = Networks.FirstOrDefault();
    }

    public FbdPresentation Presentation { get; }
    public IReadOnlyList<FbdNetworkPairViewModel> Networks { get; }
    public FbdViewportViewModel Viewport { get; }
    public RelayCommand SelectNodeCommand { get; }
    public RelayCommand PreviousChangeCommand { get; }
    public RelayCommand NextChangeCommand { get; }

    public FbdNetworkPairViewModel? SelectedNetwork
    {
        get => selectedNetwork;
        set => SetProperty(ref selectedNetwork, value);
    }
}
~~~

- [ ] **Step 6 [2–5 min]: Synchronize selection by diff key**

<code>SelectNode</code> accepts only a string key present in the selected network, assigns the same <code>SelectedNodeKey</code> to left and right canvases, and leaves a missing-side canvas with no selected item. <code>MoveChange</code> wraps through the ordinal list of non-Unchanged nodes then Rewired edges and selects its network/node.

- [ ] **Step 7 [2–5 min]: Implement exact viewport bounds and commands**

~~~csharp
public sealed class FbdViewportViewModel : ViewModelBase
{
    private const double MinimumZoom = 0.25;
    private const double MaximumZoom = 4.0;
    private const double ZoomStep = 0.25;
    private const double PanStep = 48.0;
    private double zoom = 1.0;
    private double horizontalOffset;
    private double verticalOffset;

    public FbdViewportViewModel()
    {
        ZoomInCommand = new RelayCommand(_ => Zoom = Math.Min(MaximumZoom, Zoom + ZoomStep));
        ZoomOutCommand = new RelayCommand(_ => Zoom = Math.Max(MinimumZoom, Zoom - ZoomStep));
        ResetCommand = new RelayCommand(_ => Reset());
        PanLeftCommand = new RelayCommand(_ => HorizontalOffset = Math.Max(0, HorizontalOffset - PanStep));
        PanRightCommand = new RelayCommand(_ => HorizontalOffset += PanStep);
        PanUpCommand = new RelayCommand(_ => VerticalOffset = Math.Max(0, VerticalOffset - PanStep));
        PanDownCommand = new RelayCommand(_ => VerticalOffset += PanStep);
    }

    public double Zoom { get => zoom; set => SetProperty(ref zoom, Math.Max(MinimumZoom, Math.Min(MaximumZoom, value))); }
    public double HorizontalOffset { get => horizontalOffset; set => SetProperty(ref horizontalOffset, Math.Max(0, value)); }
    public double VerticalOffset { get => verticalOffset; set => SetProperty(ref verticalOffset, Math.Max(0, value)); }
    public RelayCommand ZoomInCommand { get; }
    public RelayCommand ZoomOutCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand PanLeftCommand { get; }
    public RelayCommand PanRightCommand { get; }
    public RelayCommand PanUpCommand { get; }
    public RelayCommand PanDownCommand { get; }

    public void UpdateOffsets(double horizontal, double vertical)
    {
        HorizontalOffset = horizontal;
        VerticalOffset = vertical;
    }

    private void Reset()
    {
        Zoom = 1.0;
        HorizontalOffset = 0;
        VerticalOffset = 0;
    }
}
~~~

- [ ] **Step 8 [2–5 min]: Expose non-colour and automation text**

Each node/edge ViewModel exposes <code>StatusLabel</code> exactly <code>Unchanged</code>, <code>Added</code>, <code>Removed</code>, <code>Modified</code>, or <code>Rewired</code>; <code>StatusGlyph</code> exactly <code>＝</code>, <code>＋</code>, <code>−</code>, <code>Δ</code>, or <code>↪</code>; and <code>AutomationName</code> as <code>"{StatusLabel} {Kind}: {Operation} {Operand}"</code> with outer whitespace trimmed. Generic nodes say <code>Generic FBD part</code>.

- [ ] **Step 9 [2–5 min]: Run ViewModel tests and confirm GREEN**

Run the Task 8 class-filter command.

Expected: PASS; foundation header/limitation/raw text survive mapping, selection is paired, zoom clamps to 0.25–4.0, pan never becomes negative, and navigation ordering is stable.

- [ ] **Step 10 [2–5 min]: Commit the ViewModel increment**

~~~powershell
git add src/TiaGitAddIn/UI/Mapping/FbdPresentationViewModelFactory.cs src/TiaGitAddIn/UI/ViewModels/Comparison/FbdDiffViewModel.cs src/TiaGitAddIn/UI/ViewModels/Comparison/FbdNetworkViewModels.cs src/TiaGitAddIn.Tests/UI/FbdDiffViewModelTests.cs
git commit -m "feat: map FBD diffs to typed view models"
~~~

### Task 9: Accessible WPF Templates, Synchronized Viewports, and STA Smoke

**Acceptance criteria:** AC-022, AC-023, AC-028, AC-029, AC-030, AC-031, AC-032, AC-033, AC-054.

**Files:**
- Create: <code>src/TiaGitAddIn/UI/Behaviors/FbdScrollSyncBehavior.cs</code>
- Create: <code>src/TiaGitAddIn/UI/Views/Comparison/FbdNodeTemplates.xaml</code>
- Create: <code>src/TiaGitAddIn/UI/Views/Comparison/FbdDiffView.xaml</code>
- Create: <code>src/TiaGitAddIn/UI/Views/Comparison/FbdDiffView.xaml.cs</code>
- Modify: <code>src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml</code>
- Modify: <code>src/TiaGitAddIn/UI/GitPanelLaunchService.cs</code> only at foundation mapper composition.
- Create: <code>src/TiaGitAddIn.Tests/UI/FbdDiffViewSmokeTests.cs</code>

**Interfaces:**
- Consumes: Task 8 ViewModels, foundation <code>ComparisonPresentationHost</code>/<code>ComparisonTemplates.xaml</code>/<code>WpfTestHost</code>.
- Produces: implicit <code>FbdDiffViewModel</code> DataTemplate, keyboard-selectable paired canvases, synchronized scroll offsets, Ctrl-wheel zoom, visible status cues, and runtime smoke proof.

- [ ] **Step 1 [2–5 min]: Write DataTemplate/STA RED test**

~~~csharp
[Fact]
public void ResolvesBindingsAndTemplates()
{
    WpfTestHost.Run(dispatcher =>
    {
        PlcComparisonResult result = FbdResults.PartialVisualWithRawText();
        var factory = new FbdPresentationViewModelFactory(new FbdLayoutEngine());
        FbdDiffViewModel viewModel = Assert.IsType<FbdDiffViewModel>(
            factory.Map(result, ComparisonViewModelMetadata.From(result)));
        var host = new ComparisonPresentationHost { DataContext = viewModel };

        host.Measure(new Size(1280, 800));
        host.Arrange(new Rect(0, 0, 1280, 800));
        host.UpdateLayout();

        DataTemplate template = Assert.IsType<DataTemplate>(
            host.TryFindResource(typeof(FbdDiffViewModel)));
        Assert.IsType<FbdDiffView>(template.LoadContent());
        Assert.Equal("Visual · Partial", viewModel.Header);
        Assert.True(viewModel.HasRawText);
    });
}
~~~

- [ ] **Step 2 [2–5 min]: Write accessibility/non-colour RED test**

~~~csharp
[Fact]
public void RenderedChangedNodesExposeTextGlyphAndAutomationName()
{
    WpfTestHost.Run(dispatcher =>
    {
        (FbdDiffView view, FbdDiffViewModel viewModel) = FbdViewTestData.CreateAndLayout();
        FbdNodeViewModel changed = Assert.Single(
            viewModel.Networks.SelectMany(network => network.Left.Nodes)
                .Where(node => node.Status == LogicDiffStatus.Modified));

        Assert.Equal("Modified", changed.StatusLabel);
        Assert.Equal("Δ", changed.StatusGlyph);
        Assert.Contains("Modified", changed.AutomationName);
        Assert.NotEmpty(FindVisualChildren<ListBoxItem>(view)
            .Select(AutomationProperties.GetName)
            .Where(name => !string.IsNullOrWhiteSpace(name)));
    });
}
~~~

- [ ] **Step 3 [2–5 min]: Run smoke tests and confirm RED**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~TiaGitAddIn.Tests.UI.FbdDiffViewSmokeTests"
~~~

Expected: FAIL to compile because <code>FbdDiffView</code> does not exist or FAIL because the implicit DataTemplate is absent.

- [ ] **Step 4 [2–5 min]: Add the implicit comparison DataTemplate**

Add namespaces and this single entry to <code>ComparisonTemplates.xaml</code>:

~~~xml
<DataTemplate DataType="{x:Type fbdViewModels:FbdDiffViewModel}">
    <fbdViews:FbdDiffView />
</DataTemplate>
~~~

Do not add an artifact switch or code-behind selector.

- [ ] **Step 5 [2–5 min]: Add status resources and node template**

<code>FbdNodeTemplates.xaml</code> defines frozen brushes and this base template:

~~~xml
<DataTemplate DataType="{x:Type fbdViewModels:FbdNodeViewModel}">
    <Border Padding="8"
            BorderThickness="2"
            Background="{DynamicResource FbdNodeBackgroundBrush}"
            Focusable="True"
            AutomationProperties.Name="{Binding AutomationName}"
            AutomationProperties.HelpText="{Binding ChangeSummary}">
        <Border.Style>
            <Style TargetType="Border">
                <Setter Property="BorderBrush" Value="{DynamicResource FbdUnchangedBrush}" />
                <Style.Triggers>
                    <DataTrigger Binding="{Binding Status}" Value="{x:Static logic:LogicDiffStatus.Added}">
                        <Setter Property="BorderBrush" Value="{DynamicResource FbdAddedBrush}" />
                    </DataTrigger>
                    <DataTrigger Binding="{Binding Status}" Value="{x:Static logic:LogicDiffStatus.Removed}">
                        <Setter Property="BorderBrush" Value="{DynamicResource FbdRemovedBrush}" />
                        <Setter Property="BorderThickness" Value="2,2,2,4" />
                    </DataTrigger>
                    <DataTrigger Binding="{Binding Status}" Value="{x:Static logic:LogicDiffStatus.Modified}">
                        <Setter Property="BorderBrush" Value="{DynamicResource FbdModifiedBrush}" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Style>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding StatusGlyph}" FontWeight="Bold" Margin="0,0,4,0" />
                <TextBlock Text="{Binding StatusLabel}" FontWeight="SemiBold" />
            </StackPanel>
            <TextBlock Grid.Row="1" Text="{Binding DisplayName}" FontWeight="Bold" />
            <ItemsControl Grid.Row="2" ItemsSource="{Binding Pins}" />
        </Grid>
    </Border>
</DataTemplate>
~~~

Add an edge style with <code>StrokeDashArray="5,3"</code> for Rewired and a visible <code>↪ Rewired</code> label at the route midpoint. Colour is never the sole cue.

- [ ] **Step 6 [2–5 min]: Add paired selectable canvases**

Use a <code>ListBox</code>, not a plain <code>ItemsControl</code>, for nodes so Tab/arrow/Space selection works:

~~~xml
<ListBox ItemsSource="{Binding Nodes}"
         SelectedValue="{Binding SelectedNodeKey, Mode=TwoWay}"
         SelectedValuePath="DiffKey"
         Background="Transparent"
         BorderThickness="0"
         KeyboardNavigation.DirectionalNavigation="Contained"
         AutomationProperties.Name="{Binding SideLabel, StringFormat={}{0} FBD nodes}">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <Canvas Width="{Binding DataContext.CanvasWidth, RelativeSource={RelativeSource AncestorType=ListBox}}"
                    Height="{Binding DataContext.CanvasHeight, RelativeSource={RelativeSource AncestorType=ListBox}}" />
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
    <ListBox.ItemContainerStyle>
        <Style TargetType="ListBoxItem">
            <Setter Property="Canvas.Left" Value="{Binding X}" />
            <Setter Property="Canvas.Top" Value="{Binding Y}" />
            <Setter Property="AutomationProperties.Name" Value="{Binding AutomationName}" />
        </Style>
    </ListBox.ItemContainerStyle>
</ListBox>
~~~

Render edge paths in an <code>ItemsControl</code> before the node ListBox in the same Grid.

- [ ] **Step 7 [2–5 min]: Add accessible network/change/viewport controls**

At the top of <code>FbdDiffView.xaml</code>, bind:

~~~xml
<ComboBox ItemsSource="{Binding Networks}"
          SelectedItem="{Binding SelectedNetwork}"
          DisplayMemberPath="Title"
          AutomationProperties.Name="FBD network" />
<Button Content="Previous change" Command="{Binding PreviousChangeCommand}" />
<Button Content="Next change" Command="{Binding NextChangeCommand}" />
<Button Content="Zoom out" Command="{Binding Viewport.ZoomOutCommand}" />
<TextBlock Text="{Binding Viewport.Zoom, StringFormat={}{0:P0}}"
           AutomationProperties.Name="Zoom level" />
<Button Content="Zoom in" Command="{Binding Viewport.ZoomInCommand}" />
<Button Content="Reset view" Command="{Binding Viewport.ResetCommand}" />
<Button Content="Pan left" Command="{Binding Viewport.PanLeftCommand}" />
<Button Content="Pan right" Command="{Binding Viewport.PanRightCommand}" />
<Button Content="Pan up" Command="{Binding Viewport.PanUpCommand}" />
<Button Content="Pan down" Command="{Binding Viewport.PanDownCommand}" />
~~~

The foundation host remains responsible for mode/support header, inline limitation, expandable sanitized diagnostics, and raw-text alternative.

- [ ] **Step 8 [2–5 min]: Implement synchronized scroll behavior**

Expose attached property <code>Viewport</code> of type <code>FbdViewportViewModel</code>. On attach, subscribe to <code>ScrollViewer.ScrollChanged</code>, <code>PreviewMouseWheel</code>, and <code>PropertyChangedEventManager</code>; on detach, remove all three. Scroll updates call <code>UpdateOffsets</code>. ViewModel offset changes call <code>ScrollToHorizontalOffset</code>/<code>ScrollToVerticalOffset</code>. Ctrl-wheel executes zoom in/out and marks the event handled; ordinary wheel remains normal scrolling.

~~~csharp
private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
{
    if (sender is ScrollViewer scrollViewer &&
        GetViewport(scrollViewer) is FbdViewportViewModel viewport)
    {
        viewport.UpdateOffsets(e.HorizontalOffset, e.VerticalOffset);
    }
}

private static void OnViewportPropertyChanged(
    object? sender,
    PropertyChangedEventArgs e)
{
    foreach (ScrollViewer viewer in ViewersFor((FbdViewportViewModel)sender!))
    {
        viewer.ScrollToHorizontalOffset(((FbdViewportViewModel)sender!).HorizontalOffset);
        viewer.ScrollToVerticalOffset(((FbdViewportViewModel)sender!).VerticalOffset);
    }
}
~~~

Store viewers through weak references and prune dead entries on every update.

- [ ] **Step 9 [2–5 min]: Bind both sides to one viewport**

Each side uses:

~~~xml
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Auto"
              behaviors:FbdScrollSyncBehavior.Viewport="{Binding DataContext.Viewport, RelativeSource={RelativeSource AncestorType={x:Type fbdViews:FbdDiffView}}}">
    <Grid>
        <Grid.LayoutTransform>
            <ScaleTransform ScaleX="{Binding DataContext.Viewport.Zoom, RelativeSource={RelativeSource AncestorType={x:Type fbdViews:FbdDiffView}}}"
                            ScaleY="{Binding DataContext.Viewport.Zoom, RelativeSource={RelativeSource AncestorType={x:Type fbdViews:FbdDiffView}}}" />
        </Grid.LayoutTransform>
        <ContentControl Content="{Binding}" />
    </Grid>
</ScrollViewer>
~~~

Bind directly to the numeric <code>Zoom</code>; do not expose a mutable WPF transform from the ViewModel.

- [ ] **Step 10 [2–5 min]: Keep code-behind constructor-only**

~~~csharp
public partial class FbdDiffView : UserControl
{
    public FbdDiffView()
    {
        InitializeComponent();
    }
}
~~~

All interaction state remains in ViewModels/behavior.

- [ ] **Step 11 [2–5 min]: Register the specialized ViewModel factory**

In the foundation-created immutable <code>IComparisonPresentationViewModelFactory</code> array in <code>GitPanelLaunchService.cs</code>, append:

~~~csharp
new FbdPresentationViewModelFactory(new FbdLayoutEngine())
~~~

Construct <code>ComparisonPresentationMapper</code> from the new array. Do not add an FBD condition to <code>DiffView.xaml</code> or <code>ComparisonPresentationHost.xaml</code>.

- [ ] **Step 12 [2–5 min]: Run STA smoke and ViewModel tests**

Run:

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Debug --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~FbdDiffViewSmokeTests|FullyQualifiedName~FbdDiffViewModelTests"
~~~

Expected: PASS on the shared STA dispatcher; template resolves to <code>FbdDiffView</code>, bindings/resources load, changed elements expose non-colour cues and automation names, scroll/zoom state is shared, and raw text remains available through the host.

- [ ] **Step 13 [2–5 min]: Check focused file sizes**

~~~powershell
$files = Get-ChildItem src/TiaGitAddIn/UI/Views/Comparison,src/TiaGitAddIn/UI/ViewModels/Comparison,src/TiaGitAddIn/UI/Mapping,src/TiaGitAddIn/UI/Behaviors -File | Where-Object { $_.Name -like '*Fbd*' }
$oversize = $files | Where-Object { (Get-Content -LiteralPath $_.FullName).Count -gt 800 }
if ($oversize) { throw "Oversize FBD UI file: $($oversize.FullName -join ', ')" }
~~~

Expected: exit 0 and no output.

- [ ] **Step 14 [2–5 min]: Commit the WPF increment**

~~~powershell
git add src/TiaGitAddIn/UI/Behaviors/FbdScrollSyncBehavior.cs src/TiaGitAddIn/UI/Views/Comparison src/TiaGitAddIn/UI/Mapping/FbdPresentationViewModelFactory.cs src/TiaGitAddIn/UI/GitPanelLaunchService.cs src/TiaGitAddIn.Tests/UI/FbdDiffViewSmokeTests.cs
git commit -m "feat: render accessible FBD visual diffs"
~~~

## Final Verification and Release Gate

The merged coverage script is owned by <code>docs/superpowers/plans/2026-07-16-vci-git-workflow.md</code>. That plan may execute in parallel, but its script must exist before the final coverage step.

- [ ] **Verification 1 [2–5 min]: Restore once**

~~~powershell
dotnet restore TiaGitAddIn.sln
~~~

Expected: exit 0.

- [ ] **Verification 2 [2–5 min]: Run every focused FBD test**

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~Fbd"
~~~

Expected: exit 0; all FBD model, fixture, builder, comparer, strategy, layout, ViewModel, and STA smoke tests pass.

- [ ] **Verification 3 [2–5 min]: Run the complete LAD regression corpus**

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release --no-restore -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~LadLayoutEngineTests|FullyQualifiedName~SimaticMlComparerTests|FullyQualifiedName~LadDiffViewModelTests|FullyQualifiedName~LadDiffViewXamlTests"
~~~

Expected: exit 0 with the same LAD assertions and snapshots as before FBD enablement.

- [ ] **Verification 4 [2–5 min]: Run the full unit/component project**

~~~powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release --no-restore -p:EnableTiaAddInPackaging=false
~~~

Expected: exit 0; no failed tests.

- [ ] **Verification 5 [2–5 min]: Build the solution without packaging side effects**

~~~powershell
dotnet build TiaGitAddIn.sln -c Release --no-restore -p:EnableTiaAddInPackaging=false
~~~

Expected: Build succeeded with zero new warnings.

- [ ] **Verification 6 [2–5 min]: Run the merged 80% coverage gate**

~~~powershell
pwsh -NoProfile -File scripts/Invoke-TestGate.ps1
~~~

Expected: exit 0; <code>TestResults/Coverage/coverage.json</code> and <code>TestResults/Coverage/coverage.cobertura.xml</code> exist; total merged line coverage is at least 80%.

- [ ] **Verification 7 [2–5 min]: Scan production and fixtures**

~~~powershell
$forbidden = 'Siemens\.Automation\.CommonServices\.Compare|CompareEditorStarter|password\s*=|token\s*=|https?://[^/\s]+@|[A-Za-z]:\\Users\\'
$hits = rg -n -i $forbidden src/TiaGitAddIn.Core src/TiaGitAddIn src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd
if ($LASTEXITCODE -eq 0) { throw ("Forbidden production/fixture content found: " + $hits) }
if ($LASTEXITCODE -ne 1) { throw "rg scan failed with exit code $LASTEXITCODE" }
~~~

Expected: exit 0 and no matches.

- [ ] **Verification 8 [2–5 min]: Confirm FBD-only routing and no LAD edits**

~~~powershell
$ladChanges = git diff --name-only -- src/TiaGitAddIn.Core/Services/LadLayoutEngine.cs src/TiaGitAddIn.Core/Services/SimaticMl/LadVisualGraphBuilder.cs src/TiaGitAddIn/UI/ViewModels/LadDiffViewModel.cs src/TiaGitAddIn/UI/Views/LadDiffView.xaml
if ($ladChanges) { throw "FBD plan changed LAD implementation files: $($ladChanges -join ', ')" }
~~~

Expected: exit 0 and no output.

- [ ] **Verification 9 [2–5 min]: Refresh the repository graph**

~~~powershell
graphify update .
~~~

Expected: exit 0; <code>GRAPH_REPORT.md</code> identifies the current source revision and includes the new focused FBD nodes.

- [ ] **Verification 10 [2–5 min]: Review final scope**

~~~powershell
git status --short
git diff --check
git diff --stat
~~~

Expected: only file-map paths plus graphify outputs are changed; <code>git diff --check</code> exits 0.

- [ ] **Verification 11 [2–5 min]: Commit graph metadata if changed**

~~~powershell
git add graphify-out
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) { git commit -m "docs: refresh graph after FBD diff" }
~~~

## Plan Self-Review Result

- Spec coverage: AC-007/008/014/017–019/022/023/025/027–033/044–054/095/096/100–103/105/113/116/117 each maps to a named task or final command.
- Type consistency: <code>FbdPresentation</code> is the sole FBD presentation and always derives from foundation <code>LogicNetworkPresentation</code>; <code>FbdPresentationViewModelFactory</code> is the sole specialized mapper; strategy and UI both consume the foundation result/metadata factories.
- Matching order: unique exact signature precedes unique neighbourhood evidence; all unresolved duplicates are deterministic additions/removals.
- UId isolation: only <code>FbdSourceTrace</code> and the builder's side-local endpoint map contain UIds.
- Fallback order: Visual Full/Partial, then Structured Partial when metadata is trustworthy, then shared Text Fallback for parse failure; raw text stays in the shared envelope.
- UI coverage: dedicated DataTemplate/view, synchronized navigation/scroll, numeric zoom, button/scrollbar pan, keyboard selection, safe diagnostics, raw text, STA runtime construction, and non-colour status cues are explicit.
- Scope: no LAD implementation file is modified; final verification fails if one changes.
- Quality: exact limits, cancellation, immutable copies, 800-line cap, focused/full tests, merged 80% gate, security scans, and graph refresh are all executable.
