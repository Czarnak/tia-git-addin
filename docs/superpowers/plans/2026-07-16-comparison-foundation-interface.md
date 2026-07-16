# Comparison Foundation and Interface Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Siemens-independent comparison foundation that loads Git/working-tree revisions without byte loss, classifies and routes PLC artifacts through one immutable result contract, safely parses SimaticML, deeply compares block interfaces, and presents the result through cancellation-safe WPF composition.

**Architecture:** `TiaGitAddIn.Core` owns immutable revision, classification, comparison, diagnostic, parser, and interface-comparison contracts; it remains `netstandard2.0` and has no Siemens or WPF references. `TiaGitAddIn` owns the `net48` Siemens process-stream adapter and WPF mapping/controls, while `TiaGitAddIn.IntegrationTests` remains `net8.0`, Core-only, and is supplied by the prerequisite VCI Git workflow plan. A single coordinator routes a content-classified pair to exactly one strategy, always returns one typed presentation for non-cancelled work, and lets selection cancellation escape without manufacturing an error result.

**Tech Stack:** C# with `LangVersion=latest`; .NET Standard 2.0 Core; .NET Framework 4.8 WPF Add-In; .NET 8 integration tests; xUnit.net v2/VSTest; Newtonsoft.Json 13.0.4 where already used; `System.Xml.XmlReader`; Siemens TIA Portal V21 Add-In API only in the Add-In project; Coverlet MSBuild 6.0.4 through the VCI-owned test gate.

## Global Constraints

- Target frameworks stay exact: `TiaGitAddIn.Core=netstandard2.0`, `TiaGitAddIn=net48`, `TiaGitAddIn.Tests=net48`, and `TiaGitAddIn.IntegrationTests=net8.0`.
- `TiaGitAddIn.Core` and `TiaGitAddIn.IntegrationTests` must contain zero Siemens and zero WPF references; only `TiaGitAddIn` may reference Siemens Add-In/TIA assemblies and WPF.
- Public V21 evidence is authoritative: Git revision comparison must not call `PlcSoftware.CompareTo`, `CompareToOnline`, `CompareEditorStarter`, `Siemens.Automation.CommonServices.Compare`, any internal equivalent reached by reflection, or a second `TiaPortal`/project instance.
- Use only documented `TIA.ReadWrite` and `ProcessStartPermission` entries. Do not invent a comparison permission or state that public in-process comparison needs `ProcessStartPermission`.
- All new domain objects are immutable: validate constructor input, defensively copy enumerable/byte input, expose `IReadOnlyList<T>`/`IReadOnlyDictionary<TKey,TValue>`, and never mutate caller-owned or previously returned objects.
- Every non-cancelled coordinator outcome has exactly one typed presentation. `Full` requires an empty limitation; `Partial`, `Fallback`, and `Unsupported` require a non-blank limitation. Hard failures use `ErrorPresentation`, `Unsupported` mode/support, and no raw-text presentation.
- Cancellation is not an error: propagate `OperationCanceledException`, apply no result/banner, and dispose every revision lease exactly once in `finally`/`using` paths.
- Decode only strict UTF-8, UTF-8 BOM, UTF-16 LE BOM, or UTF-16 BE BOM. Invalid strict UTF-8 remains undecoded/binary; never use replacement-character decoding.
- Enforce a default revision limit of `16,777,216` bytes before parsing and text-comparison limits of `20,000` lines per side, `32,768` characters per line, and `4,000,000` matrix cells.
- XML parsing uses `XmlReaderSettings.DtdProcessing=Prohibit`, `XmlResolver=null`, deterministic character/element/depth limits, and cooperative cancellation; diagnostics expose stable code/severity and side/line/column only.
- Path inputs are normalized repository-relative paths, reject rooted paths/traversal/NUL, and are passed as discrete process arguments after literal `--`; revision input is `HEAD` or 7–64 hexadecimal characters and is never concatenated into a shell command.
- User-visible and logged diagnostics must redact credential-bearing URLs, token/password-shaped values, stack traces, and private temporary paths. Internal detail goes through `IAddInLogger` only after the same credential redaction.
- New focused source and XAML files stay at or below 800 lines; prefer 200–400 lines and functions below 50 lines.
- Follow strict RED → GREEN → IMPROVE for every behavior. Each RED command must fail for the named missing behavior before production code is written.
- The merged production line-coverage threshold is exactly 80 percent. Consume `pwsh -NoProfile -File scripts/Invoke-TestGate.ps1`; do not create a second coverage workflow or broaden exclusions.
- Before every commit: inspect the staged diff, scan for secrets/internal compare references, and use Conventional Commits (`test:`, `feat:`, `fix:`, `refactor:`, `docs:`).
- After code modifications, run `graphify update .`; final verification must show `graphify-out/GRAPH_REPORT.md` names the current source revision.

---

## Evidence Boundary and Current Repository Facts

The implementer must preserve these inspected facts rather than rediscovering an incompatible architecture:

| Boundary | Current source of truth | Decision used by this plan |
| --- | --- | --- |
| SDK | `global.json` | SDK `8.0.420`, `rollForward=latestFeature`. |
| Core | `src/TiaGitAddIn.Core/TiaGitAddIn.Core.csproj` | `netstandard2.0`; no Siemens/WPF; Newtonsoft.Json `13.0.4`. |
| Add-In | `src/TiaGitAddIn/TiaGitAddIn.csproj` | `net48`, WPF, Siemens `Siemens.Engineering.AddIn` and `Siemens.Engineering.Base` references, Core project reference. |
| Unit tests | `src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj` | `net48`, xUnit `2.9.0`, runner `2.8.2`, Microsoft.NET.Test.Sdk `17.10.0`. |
| Integration tests | Produced by `docs/superpowers/plans/2026-07-16-vci-git-workflow.md` | `net8.0`, Core-only, Coverlet `6.0.4`; this plan adds tests but does not create/edit its project file or solution entry. |
| V21 comparison evidence | `docs/tia-v21-compare-api-investigation.md` | Installed public API compares live project objects and returns data-only status; it does not accept serialized VCI/Git blobs or open the engineering compare editor. Project-owned comparison is required. |
| Product decision | `docs/plans/2026-07-15-plc-diff-and-vci-workflow-design.md`, `docs/PRD.md`, `README.md` | Project-owned SimaticML/SCL comparison with explicit structured/text/unsupported fallbacks. |
| Existing extraction | `IGitFileExtractor`, `GitFileExtractor` | Returns UTF-8 temp-file text and loses raw-byte identity; retire it from active comparison composition after the revision provider is wired. |
| Existing Siemens adapter | `src/TiaGitAddIn/Services/GitProcessRunner.cs` | Siemens process exposes `StandardOutput`/`StandardError` as `StreamReader`; raw stdout is read through `StandardOutput.BaseStream` in the Add-In only. |
| Existing parser/comparer | `Services/SimaticMl/SimaticMlParser.cs`, `SimaticMlComparer.cs` | Parser uses unsafe `XDocument.Load`; comparer collapses interface fields. Replace active parsing/interface comparison behind safe immutable contracts while retaining LAD semantics. |
| Existing UI | `DiffViewModel`, `LadDiffViewModel`, `DiffView.xaml`, `LadDiffView.xaml` | Path heuristic and `async void` loading are replaced by coordinator selection and a presentation host; LAD remains a strategy, not a universal route. |

## Cross-Plan Contract Ownership

| Producer | Produced contract/artifact | Consumers and prohibition |
| --- | --- | --- |
| `docs/superpowers/plans/2026-07-16-vci-git-workflow.md` | `src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj`, its solution entry, Coverlet `6.0.4` in both test projects, `scripts/Invoke-TestGate.ps1`, and all CI/coverage files | This plan and the FBD/SCL plans only add tests and call the gate. They must not edit those project/coverage/workflow files. |
| This plan | All types in `TiaGitAddIn.Models.Comparison`, `IPlcComparisonStrategy`, `PlcComparisonCoordinator`, `PlcComparisonResultFactory`, revision provider/lease, classifier, safe `SimaticMlParser.ParseText`, interface presentation, mapper aggregate, WPF host, `WpfTestHost`, and `ComparisonTestData` | FBD and SCL plans consume these exact names. They must not fork result envelopes, mode/support enums, diagnostics, raw-text types, mapper metadata, coordinator, parser limits, or test helpers. |
| FBD plan | `FbdPresentation : LogicNetworkPresentation`, FBD strategy/parser/graph/layout, FBD WPF factory/view/templates | Registers through `IPlcComparisonStrategy` and `IComparisonPresentationViewModelFactory`; this plan never embeds an FBD graph in LAD models. |
| SCL plan | `SclPresentation : ComparisonPresentation`, SCL strategy/lexer/parser/comparer, SCL WPF factory/view/templates | Registers through the same interfaces; this plan never tokenizes/compiles SCL. |

The additive integration order is fixed: VCI Task 1 → this plan's Tasks 1–10 → the complete FBD plan → the complete SCL plan rebased over FBD for `GitPanelLaunchService.cs` and `ComparisonTemplates.xaml` → VCI Tasks 2–4 → this plan's Task 11 → VCI Tasks 5–8. Feature work may branch in parallel after its prerequisite contracts exist, but commits touching either shared composition file serialize in that order and preserve every earlier registration/template. The VCI plan owns the integration scaffold and final gate; this plan never creates a competing project, coverage script, or workflow.

## Locked Shared Interfaces

Use namespace `TiaGitAddIn.Models.Comparison` for domain/result types and `TiaGitAddIn.Services.Comparison` for services. These signatures are cross-plan API and may change only by updating all three comparison plans together:

```csharp
public enum PlcArtifactKind { Unknown, Lad, Fbd, Scl, Stl, Sfc, GenericXml, Text, Binary }
public enum PlcComparisonMode { Visual, Structured, Text, Unsupported }
public enum PlcSupportLevel { Full, Partial, Fallback, Unsupported }
public enum ComparisonPresentationKind { Interface, LogicNetwork, Scl, Text, Unsupported, Error }
public enum PlcDiagnosticSeverity { Info, Warning, Error }
public enum PlcRevisionSide { Left, Right }
public enum PlcPairChangeKind { Modified, Added, Removed }

public interface IPlcComparisonStrategy
{
    IReadOnlyCollection<PlcArtifactKind> SupportedKinds { get; }
    Task<PlcComparisonResult> CompareAsync(
        PlcComparisonContext context,
        CancellationToken cancellationToken);
}

public abstract class ComparisonPresentation
{
    protected ComparisonPresentation(ComparisonPresentationKind kind) { Kind = kind; }
    public ComparisonPresentationKind Kind { get; }
}

public abstract class LogicNetworkPresentation : ComparisonPresentation
{
    protected LogicNetworkPresentation() : base(ComparisonPresentationKind.LogicNetwork) { }
}
```

`PlcComparisonResult` has this single public constructor and performs the invariant checks centrally:

```csharp
public PlcComparisonResult(
    PlcArtifactKind artifactKind,
    PlcComparisonMode requestedMode,
    PlcComparisonMode actualMode,
    PlcSupportLevel supportLevel,
    string limitation,
    IEnumerable<PlcComparisonDiagnostic> diagnostics,
    ComparisonPresentation presentation,
    ComparisonRawText? rawText)
```

It exposes only these get-only properties: `PlcArtifactKind ArtifactKind`, `PlcComparisonMode RequestedMode`, `PlcComparisonMode ActualMode`, `PlcSupportLevel SupportLevel`, `string Limitation`, `IReadOnlyList<PlcComparisonDiagnostic> Diagnostics`, `ComparisonPresentation Presentation`, and `ComparisonRawText? RawText`.

The shared text seam is exact:

```csharp
public interface ITextComparer
{
    TextPresentation Compare(ComparisonRawText rawText);
}
```

The shared WPF seam is exact:

```csharp
public interface IComparisonPresentationMapper
{
    ComparisonPresentationViewModel Map(PlcComparisonResult result);
}

public interface IComparisonPresentationViewModelFactory
{
    bool CanMap(ComparisonPresentation presentation);
    ComparisonPresentationViewModel Map(
        PlcComparisonResult result,
        ComparisonViewModelMetadata metadata);
}
```

## File Responsibility Map

### New Core files

- `src/TiaGitAddIn.Core/Models/Comparison/PlcComparisonEnums.cs` — all shared enums listed above plus revision source/encoding/missing kinds and text diff line kind.
- `src/TiaGitAddIn.Core/Models/Comparison/PlcRevision.cs` — immutable present/missing revision and immutable `PlcRevisionSource`/`PlcTextEncoding` value objects.
- `src/TiaGitAddIn.Core/Models/Comparison/PlcArtifactDescriptor.cs` — single-side classifier evidence and invariant-valid pair descriptor.
- `src/TiaGitAddIn.Core/Models/Comparison/PlcComparisonRequest.cs` — immutable request/context/raw-text types.
- `src/TiaGitAddIn.Core/Models/Comparison/PlcComparisonDiagnostic.cs` — stable diagnostic and safe source location.
- `src/TiaGitAddIn.Core/Models/Comparison/ComparisonPresentations.cs` — presentation base classes and text/unsupported/error presentations.
- `src/TiaGitAddIn.Core/Models/Comparison/PlcComparisonResult.cs` — the complete result invariant.
- `src/TiaGitAddIn.Core/Models/Comparison/InterfaceSnapshot.cs` — immutable independent section/member/comment/attribute snapshots.
- `src/TiaGitAddIn.Core/Models/Comparison/InterfacePresentation.cs` — hierarchical section/member/field comparisons.
- `src/TiaGitAddIn.Core/Services/Comparison/IPlcComparisonStrategy.cs` — strategy seam.
- `src/TiaGitAddIn.Core/Services/Comparison/ITextComparer.cs` and `LineTextComparer.cs` — bounded generic text diff.
- `src/TiaGitAddIn.Core/Services/Comparison/PlcComparisonResultFactory.cs` — semantic/fallback/unsupported/hard-error creation.
- `src/TiaGitAddIn.Core/Services/Comparison/PlcArtifactClassifier.cs` — suffix/path/content matrix and pair conflict resolver.
- `src/TiaGitAddIn.Core/Services/Comparison/PlcComparisonCoordinator.cs` — sole strategy selection and result-validation point.
- `src/TiaGitAddIn.Core/Services/Comparison/ComparisonDiagnosticSanitizer.cs` — safe UI/log messages.
- `src/TiaGitAddIn.Core/Services/Comparison/TextFallbackStrategy.cs` — generic XML/text/STL/SFC fallback.
- `src/TiaGitAddIn.Core/Services/Comparison/InterfaceSnapshotBuilder.cs` and `InterfaceComparer.cs` — normalization, canonical matching, deterministic merge.
- `src/TiaGitAddIn.Core/Services/Revision/IGitBinaryProcessRunner.cs` — raw-byte process seam.
- `src/TiaGitAddIn.Core/Services/Revision/IGitBlobReader.cs` and `GitBlobReader.cs` — validated `git cat-file` byte loading.
- `src/TiaGitAddIn.Core/Services/Revision/IPlcRevisionProvider.cs` and `PlcRevisionProvider.cs` — working-tree/commit provider, strict decoding, size gate.
- `src/TiaGitAddIn.Core/Services/Revision/PlcRevisionLease.cs` — unique scoped temp directory and deterministic cleanup.
- `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlParserLimits.cs` and `SimaticMlParseResult.cs` — safe parser contract consumed by LAD/FBD.

### Modified Core files

- `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlModels.cs` — make parsed collections and values immutable; retain existing semantic names used by LAD/FBD.
- `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlParser.cs` — add bounded `ParseText`; keep `Parse(path)` only as a compatibility wrapper during migration.
- `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlComparer.cs` — delegate interface comparison to `InterfaceComparer`; do not change existing LAD network semantics.
- `src/TiaGitAddIn.Core/Services/IGitFileExtractor.cs` and `GitFileExtractor.cs` — mark obsolete after active callers migrate; delete only when repository search proves zero production callers.

### New/modified Add-In files

- `src/TiaGitAddIn/Services/GitProcessRunner.cs` — implement raw-byte process seam from Siemens `StreamReader.BaseStream`, with size/cancellation bounds.
- `src/TiaGitAddIn/UI/GitPanelLaunchService.cs` — production composition for provider, classifier, strategies, coordinator, mapper, and Siemens adapter.
- `src/TiaGitAddIn/AddInPublisherConfiguration.xml` — correct stale SACT file-dialog comment while retaining only documented permissions.
- `src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonPresentationViewModel.cs` — base, metadata, diagnostic, and raw-text VMs.
- `src/TiaGitAddIn/UI/ViewModels/Comparison/InterfaceComparisonViewModel.cs` — independent left/right interface tree rows.
- `src/TiaGitAddIn/UI/ViewModels/Comparison/TextComparisonViewModel.cs` — bounded line presentation.
- `src/TiaGitAddIn/UI/ViewModels/Comparison/UnsupportedComparisonViewModel.cs` and `ErrorComparisonViewModel.cs` — explicit terminal views.
- `src/TiaGitAddIn/UI/Mapping/IComparisonPresentationMapper.cs`, `IComparisonPresentationViewModelFactory.cs`, and `ComparisonPresentationMapper.cs` — one aggregate mapping point.
- `src/TiaGitAddIn/UI/Mapping/InterfacePresentationViewModelFactory.cs`, `TextPresentationViewModelFactory.cs`, `UnsupportedPresentationViewModelFactory.cs`, and `ErrorPresentationViewModelFactory.cs` — foundation factories; FBD/SCL add their own later.
- `src/TiaGitAddIn/UI/Views/Comparison/ComparisonPresentationHost.xaml` and `.xaml.cs` — shared header/diagnostics/raw toggle/content host.
- `src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml` — interface/text/unsupported/error templates; later plans merge FBD/SCL templates.
- `src/TiaGitAddIn/UI/Views/Comparison/InterfaceDiffView.xaml` and `.xaml.cs` — extracted deep interface view.
- `src/TiaGitAddIn/UI/Views/Comparison/TextDiffView.xaml` and `.xaml.cs` — generic text view.
- `src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonSelectionCoordinator.cs` — latest-selection generation/cancellation/dispatcher application.
- `src/TiaGitAddIn/UI/ViewModels/DiffViewModel.cs`, `MainViewModel.cs`, and `src/TiaGitAddIn/UI/Views/DiffView.xaml` — replace heuristic/`async void` comparison path with the host and Task-returning load.
- `src/TiaGitAddIn/UI/ViewModels/LadDiffViewModel.cs`, `LadInterfaceRowViewModel.cs`, and `src/TiaGitAddIn/UI/Views/LadDiffView.xaml` — remove interface value collapse and obsolete SACT failure copy; preserve LAD logic behavior.

### New/modified tests

- `src/TiaGitAddIn.Tests/Architecture/ComparisonBoundaryTests.cs`
- `src/TiaGitAddIn.Tests/Comparison/ComparisonTestData.cs`
- `src/TiaGitAddIn.Tests/Comparison/PlcComparisonContractTests.cs`
- `src/TiaGitAddIn.Tests/Comparison/PlcArtifactClassifierTests.cs`
- `src/TiaGitAddIn.Tests/Comparison/PlcComparisonCoordinatorTests.cs`
- `src/TiaGitAddIn.Tests/Comparison/LineTextComparerTests.cs`
- `src/TiaGitAddIn.Tests/Revision/PlcRevisionProviderTests.cs`
- `src/TiaGitAddIn.Tests/Revision/PlcRevisionLeaseTests.cs`
- `src/TiaGitAddIn.Tests/Services/SimaticMl/SimaticMlParserSecurityTests.cs`
- `src/TiaGitAddIn.Tests/Services/SimaticMl/InterfaceComparerTests.cs`
- `src/TiaGitAddIn.Tests/UI/ComparisonPresentationMapperTests.cs`
- `src/TiaGitAddIn.Tests/UI/ComparisonSelectionCoordinatorTests.cs`
- `src/TiaGitAddIn.Tests/UI/ComparisonViewSmokeTests.cs`
- `src/TiaGitAddIn.Tests/UI/WpfTestHost.cs`
- `src/TiaGitAddIn.IntegrationTests/Architecture/ComparisonProjectBoundaryTests.cs` — add only after the VCI prerequisite creates the project.

---

### Task 1: Freeze the V21 API and Project Boundary in Executable Tests

**Acceptance criteria:** AC-001, AC-002, AC-003, AC-004, AC-005, AC-006, AC-106.

**Files:**
- Create: `src/TiaGitAddIn.Tests/Architecture/ComparisonBoundaryTests.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/Architecture/ComparisonProjectBoundaryTests.cs`
- Modify: `src/TiaGitAddIn/AddInPublisherConfiguration.xml`
- Verify only: `docs/tia-v21-compare-api-investigation.md`, `docs/plans/2026-07-15-plc-diff-and-vci-workflow-design.md`, `docs/PRD.md`, `README.md`

**Interfaces:**
- Consumes: VCI-owned `TiaGitAddIn.IntegrationTests.csproj` and solution entry.
- Produces: executable guardrails that all subsequent comparison tasks must keep green; no production API.

- [ ] **Step 1: Write the RED production-boundary theory (2–5 min)**

Create `ComparisonBoundaryTests.cs` with a repository-root finder and this exact scan. Limit reflection detection to compare/internal-editor tokens so existing legitimate Add-In menu reflection is not falsely banned.

```csharp
[Fact]
public void ProductionComparisonPathContainsNoInternalOrLiveObjectCompareApi()
{
    string root = RepositoryRoot.Find();
    string[] forbidden =
    {
        "Siemens.Automation.CommonServices.Compare",
        "CompareEditorStarter",
        "PlcSoftware.CompareTo(",
        "CompareToOnline(",
        "typeof(CompareEditorStarter)",
        "GetType(\"Siemens.Automation.CommonServices.Compare"
    };

    string[] files = Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}TiaGitAddIn.Tests{Path.DirectorySeparatorChar}"))
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}TiaGitAddIn.IntegrationTests{Path.DirectorySeparatorChar}"))
        .ToArray();

    foreach (string file in files)
    {
        string text = File.ReadAllText(file);
        foreach (string token in forbidden)
        {
            Assert.DoesNotContain(token, text, StringComparison.OrdinalIgnoreCase);
        }
    }
}

[Fact]
public void EvidenceDocumentsSelectProjectOwnedComparisonWithoutConflict()
{
    string root = RepositoryRoot.Find();
    string investigation = File.ReadAllText(Path.Combine(root, "docs", "tia-v21-compare-api-investigation.md"));
    string design = File.ReadAllText(Path.Combine(root, "docs", "plans", "2026-07-15-plc-diff-and-vci-workflow-design.md"));
    string prd = File.ReadAllText(Path.Combine(root, "docs", "PRD.md"));
    string readme = File.ReadAllText(Path.Combine(root, "README.md"));

    Assert.Contains("PlcSoftware.CompareTo", investigation, StringComparison.Ordinal);
    Assert.Contains("CompareToOnline", investigation, StringComparison.Ordinal);
    Assert.Contains("project-owned", investigation, StringComparison.OrdinalIgnoreCase);
    Assert.All(new[] { design, prd, readme }, text =>
        Assert.Contains("project-owned", text, StringComparison.OrdinalIgnoreCase));
    Assert.DoesNotContain("Git blobs are accepted by PlcSoftware.CompareTo", string.Join("\n", investigation, design, prd, readme), StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run the focused RED test (2–5 min)**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ComparisonBoundaryTests"
```

Expected: FAIL because `RepositoryRoot` and/or the executable boundary tests do not exist. Do not weaken token matching to make unrelated code pass.

- [ ] **Step 3: Add the exact test-only root helper and project assertions (2–5 min)**

Put this helper in the same test file, then add assertions for exact TFMs/references and permissions:

```csharp
internal static class RepositoryRoot
{
    public static string Find()
    {
        DirectoryInfo? cursor = new DirectoryInfo(AppContext.BaseDirectory);
        while (cursor != null && !File.Exists(Path.Combine(cursor.FullName, "TiaGitAddIn.sln")))
        {
            cursor = cursor.Parent;
        }

        return cursor?.FullName ?? throw new DirectoryNotFoundException("TiaGitAddIn.sln was not found.");
    }
}

[Fact]
public void CoreAndAddInKeepTheirFrameworkAndReferenceBoundary()
{
    string root = RepositoryRoot.Find();
    XDocument core = XDocument.Load(Path.Combine(root, "src", "TiaGitAddIn.Core", "TiaGitAddIn.Core.csproj"));
    XDocument addIn = XDocument.Load(Path.Combine(root, "src", "TiaGitAddIn", "TiaGitAddIn.csproj"));

    Assert.Equal("netstandard2.0", core.Descendants("TargetFramework").Single().Value);
    Assert.DoesNotContain(core.Descendants("Reference"), x =>
        ((string?)x.Attribute("Include"))?.StartsWith("Siemens.", StringComparison.OrdinalIgnoreCase) == true);
    Assert.DoesNotContain(core.Descendants("UseWPF"), x => x.Value.Equals("true", StringComparison.OrdinalIgnoreCase));
    Assert.Equal("net48", addIn.Descendants("TargetFramework").Single().Value);
    Assert.Contains(addIn.Descendants("Reference"), x =>
        ((string?)x.Attribute("Include"))?.StartsWith("Siemens.", StringComparison.OrdinalIgnoreCase) == true);
}

[Fact]
public void PublisherDeclaresOnlyDocumentedComparisonAndGitPermissions()
{
    string xml = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "src", "TiaGitAddIn", "AddInPublisherConfiguration.xml"));
    Assert.Contains("TIA.ReadWrite", xml, StringComparison.Ordinal);
    Assert.Contains("ProcessStartPermission", xml, StringComparison.Ordinal);
    Assert.DoesNotContain("ComparePermission", xml, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("SACT", xml, StringComparison.OrdinalIgnoreCase);
}
```

In `ComparisonProjectBoundaryTests.cs`, use `XDocument` to assert `TargetFramework=net8.0`, exactly one `ProjectReference` ending `TiaGitAddIn.Core.csproj`, and no `UseWPF`, `Siemens.*`, or `TiaGitAddIn.csproj` reference.

- [ ] **Step 4: Make the stale publisher copy pass without altering permissions (2–5 min)**

Change only the `FileDialogPermission` comment text from the obsolete Node/SACT wording to:

```xml
<!-- Allows the user to select a Git executable path through the Add-In settings dialog. -->
```

Do not add/remove permission elements.

- [ ] **Step 5: Run both focused suites GREEN (2–5 min)**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ComparisonBoundaryTests"
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~ComparisonProjectBoundaryTests"
```

Expected: PASS; Core is Siemens-free, integration is Core-only, the evidence choice is consistent, and publisher permissions remain documented.

- [ ] **Step 6: Commit the executable boundary (2–5 min)**

```powershell
git diff --check
git diff -- src/TiaGitAddIn/AddInPublisherConfiguration.xml src/TiaGitAddIn.Tests/Architecture/ComparisonBoundaryTests.cs src/TiaGitAddIn.IntegrationTests/Architecture/ComparisonProjectBoundaryTests.cs
git add src/TiaGitAddIn/AddInPublisherConfiguration.xml src/TiaGitAddIn.Tests/Architecture/ComparisonBoundaryTests.cs src/TiaGitAddIn.IntegrationTests/Architecture/ComparisonProjectBoundaryTests.cs
git commit -m "test: lock comparison API boundaries"
```

Expected: one conventional commit; no project/solution/coverage file is staged.

---

### Task 2: Add Immutable Comparison Contracts and the Complete Result Invariant

**Acceptance criteria:** AC-007, AC-008, AC-022, AC-023, AC-030, AC-117.

**Files:**
- Create: all `src/TiaGitAddIn.Core/Models/Comparison/*.cs` files listed in the file map except interface-specific files (Task 8)
- Create: `src/TiaGitAddIn.Core/Services/Comparison/IPlcComparisonStrategy.cs`
- Create: `src/TiaGitAddIn.Core/Services/Comparison/ITextComparer.cs`
- Create: `src/TiaGitAddIn.Core/Services/Comparison/PlcComparisonResultFactory.cs`
- Create: `src/TiaGitAddIn.Tests/Comparison/ComparisonTestData.cs`
- Create: `src/TiaGitAddIn.Tests/Comparison/PlcComparisonContractTests.cs`

**Interfaces:**
- Consumes: no comparison production contract; starts the shared API.
- Produces: every locked domain/result signature used by Tasks 3–10 and the FBD/SCL plans.

- [ ] **Step 1: Write RED invariant and defensive-copy tests (2–5 min)**

Create `PlcComparisonContractTests.cs` with exact checks for immutable byte/diagnostic copies and invalid result combinations:

```csharp
[Fact]
public void PresentRevisionDefensivelyCopiesBytes()
{
    byte[] input = Encoding.UTF8.GetBytes("left");
    PlcRevision revision = PlcRevision.Present(
        PlcRevisionSide.Left,
        PlcRevisionSource.WorkingTree,
        "Program.xml",
        input,
        PlcTextEncoding.Utf8WithoutBom,
        "left",
        false,
        string.Empty);

    input[0] = (byte)'X';

    Assert.Equal((byte)'l', revision.Bytes[0]);
    Assert.IsAssignableFrom<IReadOnlyList<byte>>(revision.Bytes);
}

[Theory]
[InlineData(PlcSupportLevel.Full, "unexpected limitation")]
[InlineData(PlcSupportLevel.Partial, "")]
[InlineData(PlcSupportLevel.Fallback, " ")]
[InlineData(PlcSupportLevel.Unsupported, "")]
public void ResultRejectsInvalidLimitationInvariant(PlcSupportLevel support, string limitation)
{
    Assert.Throws<ArgumentException>(() => new PlcComparisonResult(
        PlcArtifactKind.Text,
        PlcComparisonMode.Text,
        PlcComparisonMode.Text,
        support,
        limitation,
        Array.Empty<PlcComparisonDiagnostic>(),
        new TextPresentation(Array.Empty<TextDiffLine>()),
        new ComparisonRawText("left", "right", false, false)));
}

[Fact]
public void ResultRejectsPresentationModeMismatch()
{
    Assert.Throws<ArgumentException>(() => new PlcComparisonResult(
        PlcArtifactKind.Fbd,
        PlcComparisonMode.Visual,
        PlcComparisonMode.Visual,
        PlcSupportLevel.Full,
        string.Empty,
        Array.Empty<PlcComparisonDiagnostic>(),
        new TextPresentation(Array.Empty<TextDiffLine>()),
        new ComparisonRawText("left", "right", false, false)));
}
```

- [ ] **Step 2: Run the contract test RED (2–5 min)**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~PlcComparisonContractTests"
```

Expected: FAIL with missing `TiaGitAddIn.Models.Comparison` types.

- [ ] **Step 3: Add exact revision/request/diagnostic contracts (2–5 min)**

Implement the enums from **Locked Shared Interfaces**, then implement these public surfaces. Every `IEnumerable<T>` constructor calls `ToArray()` once after null validation; every dictionary copy uses `StringComparer.Ordinal`.

```csharp
public enum PlcRevisionSourceKind { WorkingTree, Head, Commit, ParentOfCommit }
public enum PlcRevisionMissingReason { None, Added, Deleted, NotPresentInRevision }
public enum PlcTextEncodingKind { None, Utf8, Utf16LittleEndian, Utf16BigEndian }

public sealed class PlcRevisionSource
{
    private PlcRevisionSource(PlcRevisionSourceKind kind, string? commitHash)
    {
        Kind = kind;
        CommitHash = commitHash;
    }

    public PlcRevisionSourceKind Kind { get; }
    public string? CommitHash { get; }
    public static PlcRevisionSource WorkingTree { get; } = new PlcRevisionSource(PlcRevisionSourceKind.WorkingTree, null);
    public static PlcRevisionSource Head { get; } = new PlcRevisionSource(PlcRevisionSourceKind.Head, "HEAD");
    public static PlcRevisionSource Commit(string hash) => new PlcRevisionSource(PlcRevisionSourceKind.Commit, hash);
    public static PlcRevisionSource ParentOfCommit(string hash) => new PlcRevisionSource(PlcRevisionSourceKind.ParentOfCommit, hash);
}

public sealed class PlcTextEncoding
{
    private PlcTextEncoding(PlcTextEncodingKind kind, bool hasBom) { Kind = kind; HasBom = hasBom; }
    public PlcTextEncodingKind Kind { get; }
    public bool HasBom { get; }
    public static PlcTextEncoding None { get; } = new PlcTextEncoding(PlcTextEncodingKind.None, false);
    public static PlcTextEncoding Utf8WithoutBom { get; } = new PlcTextEncoding(PlcTextEncodingKind.Utf8, false);
    public static PlcTextEncoding Utf8WithBom { get; } = new PlcTextEncoding(PlcTextEncodingKind.Utf8, true);
    public static PlcTextEncoding Utf16LittleEndianWithBom { get; } = new PlcTextEncoding(PlcTextEncodingKind.Utf16LittleEndian, true);
    public static PlcTextEncoding Utf16BigEndianWithBom { get; } = new PlcTextEncoding(PlcTextEncodingKind.Utf16BigEndian, true);
}

public sealed class PlcRevision
{
    private PlcRevision(PlcRevisionSide side, PlcRevisionSource source, string originalPath,
        IReadOnlyList<byte> bytes, PlcTextEncoding encoding, string? text, bool isMissing,
        PlcRevisionMissingReason missingReason, bool isBinary, string encodingLimitation)
    {
        Side = side; Source = source; OriginalPath = originalPath;
        OriginalSuffix = Path.GetExtension(originalPath); Bytes = bytes;
        Encoding = encoding; Text = text; IsMissing = isMissing;
        MissingReason = missingReason; IsBinary = isBinary; EncodingLimitation = encodingLimitation;
    }

    public PlcRevisionSide Side { get; }
    public PlcRevisionSource Source { get; }
    public string OriginalPath { get; }
    public string OriginalSuffix { get; }
    public IReadOnlyList<byte> Bytes { get; }
    public PlcTextEncoding Encoding { get; }
    public string? Text { get; }
    public bool IsMissing { get; }
    public PlcRevisionMissingReason MissingReason { get; }
    public bool IsBinary { get; }
    public string EncodingLimitation { get; }

    public static PlcRevision Present(PlcRevisionSide side, PlcRevisionSource source, string originalPath,
        IEnumerable<byte> bytes, PlcTextEncoding encoding, string? text, bool isBinary, string encodingLimitation)
        => new PlcRevision(side, source ?? throw new ArgumentNullException(nameof(source)), RequirePath(originalPath),
            (bytes ?? throw new ArgumentNullException(nameof(bytes))).ToArray(), encoding ?? throw new ArgumentNullException(nameof(encoding)),
            text, false, PlcRevisionMissingReason.None, isBinary, encodingLimitation ?? string.Empty);

    public static PlcRevision Missing(PlcRevisionSide side, PlcRevisionSource source, string originalPath,
        PlcRevisionMissingReason reason)
        => new PlcRevision(side, source ?? throw new ArgumentNullException(nameof(source)), RequirePath(originalPath),
            Array.Empty<byte>(), PlcTextEncoding.None, null, true, reason, false, string.Empty);

    private static string RequirePath(string path) => string.IsNullOrWhiteSpace(path)
        ? throw new ArgumentException("Original path is required.", nameof(path)) : path;
}

public sealed class PlcSourceLocation
{
    public PlcSourceLocation(PlcRevisionSide side, int? line = null, int? column = null, int? startOffset = null, int? length = null)
    { Side = side; Line = line; Column = column; StartOffset = startOffset; Length = length; }
    public PlcRevisionSide Side { get; }
    public int? Line { get; }
    public int? Column { get; }
    public int? StartOffset { get; }
    public int? Length { get; }
}

public sealed class PlcComparisonDiagnostic
{
    public PlcComparisonDiagnostic(string code, PlcDiagnosticSeverity severity, string message, PlcSourceLocation? location = null)
    { Code = Require(code, nameof(code)); Severity = severity; Message = Require(message, nameof(message)); Location = location; }
    public string Code { get; }
    public PlcDiagnosticSeverity Severity { get; }
    public string Message { get; }
    public PlcSourceLocation? Location { get; }
    private static string Require(string value, string name) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("A non-blank value is required.", name) : value;
}
```

- [ ] **Step 4: Add descriptor/request/raw-text contracts and test data (2–5 min)**

Implement these exact constructors/properties, including Added/Removed/Modified validation:

```csharp
public sealed class PlcArtifactDescriptor
{
    public PlcArtifactDescriptor(PlcArtifactKind artifactKind, PlcComparisonMode preferredMode, IEnumerable<string> evidence)
    { ArtifactKind = artifactKind; PreferredMode = preferredMode; Evidence = (evidence ?? throw new ArgumentNullException(nameof(evidence))).ToArray(); }
    public PlcArtifactKind ArtifactKind { get; }
    public PlcComparisonMode PreferredMode { get; }
    public IReadOnlyList<string> Evidence { get; }
}

public sealed class PlcArtifactPairDescriptor
{
    public PlcArtifactPairDescriptor(PlcArtifactDescriptor? left, PlcArtifactDescriptor? right,
        PlcArtifactKind artifactKind, PlcComparisonMode requestedMode, PlcPairChangeKind changeKind,
        string limitation, IEnumerable<PlcComparisonDiagnostic>? diagnostics = null)
    {
        bool valid = changeKind == PlcPairChangeKind.Modified ? left != null && right != null
            : changeKind == PlcPairChangeKind.Added ? left == null && right != null
            : left != null && right == null;
        if (!valid) throw new ArgumentException("Pair sides do not match the declared change kind.", nameof(changeKind));
        Left = left; Right = right; ArtifactKind = artifactKind; RequestedMode = requestedMode;
        ChangeKind = changeKind; Limitation = limitation ?? string.Empty;
        Diagnostics = (diagnostics ?? Array.Empty<PlcComparisonDiagnostic>()).ToArray();
    }
    public PlcArtifactDescriptor? Left { get; }
    public PlcArtifactDescriptor? Right { get; }
    public PlcArtifactKind ArtifactKind { get; }
    public PlcComparisonMode RequestedMode { get; }
    public PlcPairChangeKind ChangeKind { get; }
    public string Limitation { get; }
    public IReadOnlyList<PlcComparisonDiagnostic> Diagnostics { get; }
}

public sealed class PlcComparisonRequest
{
    public PlcComparisonRequest(PlcRevision left, PlcRevision right, PlcArtifactPairDescriptor pair)
    { Left = left ?? throw new ArgumentNullException(nameof(left)); Right = right ?? throw new ArgumentNullException(nameof(right)); Pair = pair ?? throw new ArgumentNullException(nameof(pair)); }
    public PlcRevision Left { get; }
    public PlcRevision Right { get; }
    public PlcArtifactPairDescriptor Pair { get; }
}

public sealed class ComparisonRawText
{
    public ComparisonRawText(string? leftText, string? rightText, bool isLeftMissing, bool isRightMissing)
    { LeftText = leftText; RightText = rightText; IsLeftMissing = isLeftMissing; IsRightMissing = isRightMissing; }
    public string? LeftText { get; }
    public string? RightText { get; }
    public bool IsLeftMissing { get; }
    public bool IsRightMissing { get; }
}

public sealed class PlcComparisonContext
{
    public PlcComparisonContext(PlcComparisonRequest request, ComparisonRawText? rawText)
    { Request = request ?? throw new ArgumentNullException(nameof(request)); RawText = rawText; }
    public PlcComparisonRequest Request { get; }
    public ComparisonRawText? RawText { get; }
}
```

Create `ComparisonTestData` with the exact shared helper surface:

```csharp
internal static class ComparisonTestData
{
    public static PlcRevision TextRevision(PlcRevisionSide side, string text, string path = "Program.xml")
        => PlcRevision.Present(side, PlcRevisionSource.WorkingTree, path, Encoding.UTF8.GetBytes(text),
            PlcTextEncoding.Utf8WithoutBom, text, false, string.Empty);

    public static PlcRevision MissingRevision(PlcRevisionSide side, string path = "Program.xml")
        => PlcRevision.Missing(side, PlcRevisionSource.WorkingTree, path,
            side == PlcRevisionSide.Left ? PlcRevisionMissingReason.Added : PlcRevisionMissingReason.Deleted);

    public static PlcArtifactPairDescriptor Pair(PlcArtifactKind kind, PlcComparisonMode requestedMode,
        PlcPairChangeKind changeKind = PlcPairChangeKind.Modified)
    {
        var descriptor = new PlcArtifactDescriptor(kind, requestedMode, new[] { "test" });
        return new PlcArtifactPairDescriptor(changeKind == PlcPairChangeKind.Added ? null : descriptor,
            changeKind == PlcPairChangeKind.Removed ? null : descriptor, kind, requestedMode,
            changeKind, string.Empty);
    }

    public static PlcComparisonContext Context(PlcArtifactKind kind, PlcComparisonMode requestedMode,
        string leftText = "left", string rightText = "right", string path = "Program.xml")
        => new PlcComparisonContext(new PlcComparisonRequest(TextRevision(PlcRevisionSide.Left, leftText, path),
            TextRevision(PlcRevisionSide.Right, rightText, path), Pair(kind, requestedMode)),
            new ComparisonRawText(leftText, rightText, false, false));
}
```

- [ ] **Step 5: Add presentations/result/factory and enforce compatibility (2–5 min)**

Implement text lines/presentation, `UnsupportedPresentation`, and `ErrorPresentation`. In `PlcComparisonResult`, reject null diagnostics/presentation, copy diagnostics, enforce limitation, and map modes to kinds exactly: `Visual` accepts `LogicNetwork` or `Interface`; `Structured` accepts `Interface` or `Scl`; `Text` accepts `Text`; `Unsupported` accepts `Unsupported` or `Error`. Add factory methods with these signatures:

```csharp
public PlcComparisonResult CreateSemantic(PlcComparisonContext context, PlcComparisonMode actualMode,
    PlcSupportLevel supportLevel, string limitation, IEnumerable<PlcComparisonDiagnostic> diagnostics,
    ComparisonPresentation presentation);
public PlcComparisonResult CreateTextFallback(PlcComparisonContext context, string limitation,
    IEnumerable<PlcComparisonDiagnostic> diagnostics);
public PlcComparisonResult CreateUnsupported(PlcComparisonContext context, string limitation,
    IEnumerable<PlcComparisonDiagnostic> diagnostics);
public PlcComparisonResult CreateHardError(PlcArtifactKind artifactKind, PlcComparisonMode requestedMode,
    string limitation, IEnumerable<PlcComparisonDiagnostic> diagnostics);
```

`CreateSemantic` retains `context.RawText`; `CreateTextFallback` requires it and invokes `ITextComparer`; unsupported/hard error set raw text to null. Add a parameterized test covering Full, Partial, Fallback, Unsupported, and Error presentation compatibility.

- [ ] **Step 6: Run contracts GREEN and refactor copy helpers (2–5 min)**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~PlcComparisonContractTests"
```

Expected: PASS. Then extract one internal `ImmutableCopy.Of<T>(IEnumerable<T>, string)` helper only if three or more model files duplicate the same null/copy check; rerun the same command and expect PASS.

- [ ] **Step 7: Commit the shared contracts (2–5 min)**

```powershell
git diff --check
git diff --stat
git add src/TiaGitAddIn.Core/Models/Comparison src/TiaGitAddIn.Core/Services/Comparison/IPlcComparisonStrategy.cs src/TiaGitAddIn.Core/Services/Comparison/ITextComparer.cs src/TiaGitAddIn.Core/Services/Comparison/PlcComparisonResultFactory.cs src/TiaGitAddIn.Tests/Comparison/ComparisonTestData.cs src/TiaGitAddIn.Tests/Comparison/PlcComparisonContractTests.cs
git commit -m "feat: add immutable comparison contracts"
```

Expected: shared contracts are committed before FBD/SCL implementation begins.

---

### Task 3: Preserve Raw Revision Bytes, Encoding, Identity, and Scoped Lifetime

**Acceptance criteria:** AC-009, AC-010, AC-011, AC-012, AC-013, AC-021, AC-097, AC-099, AC-118.

**Files:**
- Create: `src/TiaGitAddIn.Core/Services/Revision/IGitBinaryProcessRunner.cs`
- Create: `src/TiaGitAddIn.Core/Services/Revision/IGitBlobReader.cs`
- Create: `src/TiaGitAddIn.Core/Services/Revision/GitBlobReader.cs`
- Create: `src/TiaGitAddIn.Core/Services/Revision/IPlcRevisionProvider.cs`
- Create: `src/TiaGitAddIn.Core/Services/Revision/PlcRevisionProvider.cs`
- Create: `src/TiaGitAddIn.Core/Services/Revision/PlcRevisionLease.cs`
- Modify: `src/TiaGitAddIn/Services/GitProcessRunner.cs`
- Create: `src/TiaGitAddIn.Tests/Revision/PlcRevisionProviderTests.cs`
- Create: `src/TiaGitAddIn.Tests/Revision/PlcRevisionLeaseTests.cs`
- Modify: `src/TiaGitAddIn.Tests/Services/GitProcessRunnerTests.cs`

**Interfaces:**
- Consumes: `PlcRevision`, `PlcRevisionSource`, encoding/missing enums from Task 2; existing `IGitProcessRunner` remains the text-command seam for non-blob Git work.
- Produces: `IPlcRevisionProvider.LoadAsync(...)`, raw-byte `IGitBinaryProcessRunner`, and `PlcRevisionLease`, consumed by Tasks 4, 6, and 10.

- [ ] **Step 1: Write RED encoding and byte-identity tests (2–5 min)**

Create a fake `IGitBlobReader` returning caller-supplied bytes and write this exact theory:

```csharp
[Theory]
[InlineData("utf8", "żółć", PlcTextEncodingKind.Utf8, false)]
[InlineData("utf8-bom", "żółć", PlcTextEncodingKind.Utf8, true)]
[InlineData("utf16-le", "żółć", PlcTextEncodingKind.Utf16LittleEndian, true)]
[InlineData("utf16-be", "żółć", PlcTextEncodingKind.Utf16BigEndian, true)]
public async Task LoadAsyncDecodesOnlySupportedStrictEncodings(
    string fixture, string expected, PlcTextEncodingKind kind, bool hasBom)
{
    byte[] bytes = EncodingFixture.Create(fixture, expected);
    var provider = CreateProvider(bytes, maximumBytes: bytes.Length);

    using PlcRevisionLease lease = await provider.LoadAsync(
        PlcRevisionSide.Left,
        PlcRevisionSource.Commit("0123456789abcdef"),
        "Neutral/Program.bin",
        CancellationToken.None);

    Assert.Equal(bytes, lease.Revision.Bytes);
    Assert.Equal(expected, lease.Revision.Text);
    Assert.Equal(kind, lease.Revision.Encoding.Kind);
    Assert.Equal(hasBom, lease.Revision.Encoding.HasBom);
    Assert.Equal(".bin", lease.Revision.OriginalSuffix);
    Assert.DoesNotContain('\uFFFD', lease.Revision.Text!);
}

[Fact]
public async Task InvalidUtf8IsUndecodedBinary()
{
    var provider = CreateProvider(new byte[] { 0xC3, 0x28 }, maximumBytes: 2);
    using PlcRevisionLease lease = await provider.LoadAsync(
        PlcRevisionSide.Right, PlcRevisionSource.Head, "Program.xml", CancellationToken.None);

    Assert.True(lease.Revision.IsBinary);
    Assert.Null(lease.Revision.Text);
    Assert.Equal(PlcTextEncodingKind.None, lease.Revision.Encoding.Kind);
    Assert.False(string.IsNullOrWhiteSpace(lease.Revision.EncodingLimitation));
}

[Fact]
public async Task NPlusOneBytesFailBeforeBlobRead()
{
    var blob = new FakeGitBlobReader(size: 5, bytes: new byte[5]);
    var provider = CreateProvider(blob, maximumBytes: 4);

    await Assert.ThrowsAsync<RevisionSizeLimitException>(() => provider.LoadAsync(
        PlcRevisionSide.Left, PlcRevisionSource.Head, "Program.xml", CancellationToken.None));

    Assert.Equal(0, blob.ReadCount);
}
```

- [ ] **Step 2: Run provider tests RED (2–5 min)**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~PlcRevisionProviderTests"
```

Expected: FAIL with missing revision-provider/blob-reader types.

- [ ] **Step 3: Define the raw process/blob/provider seams exactly (2–5 min)**

```csharp
public sealed class GitBinaryProcessResult
{
    public GitBinaryProcessResult(int exitCode, IEnumerable<byte> standardOutput, string standardError, bool timedOut)
    { ExitCode = exitCode; StandardOutput = (standardOutput ?? throw new ArgumentNullException(nameof(standardOutput))).ToArray(); StandardError = standardError ?? string.Empty; TimedOut = timedOut; }
    public int ExitCode { get; }
    public IReadOnlyList<byte> StandardOutput { get; }
    public string StandardError { get; }
    public bool TimedOut { get; }
    public bool IsSuccess => ExitCode == 0 && !TimedOut;
}

public interface IGitBinaryProcessRunner
{
    Task<GitBinaryProcessResult> RunBinaryAsync(string gitExecutablePath, string workingDirectory,
        IReadOnlyList<string> arguments, int maximumStandardOutputBytes, CancellationToken cancellationToken);
}

public interface IGitBlobReader
{
    Task<long> GetSizeAsync(PlcRevisionSource source, string repositoryRelativePath, CancellationToken cancellationToken);
    Task<IReadOnlyList<byte>> ReadAsync(PlcRevisionSource source, string repositoryRelativePath,
        int maximumBytes, CancellationToken cancellationToken);
}

public interface IPlcRevisionProvider
{
    Task<PlcRevisionLease> LoadAsync(PlcRevisionSide side, PlcRevisionSource source,
        string repositoryRelativePath, CancellationToken cancellationToken);
    PlcRevisionLease Missing(PlcRevisionSide side, PlcRevisionSource source,
        string repositoryRelativePath, PlcRevisionMissingReason reason);
}

public sealed class PlcRevisionProviderOptions
{
    public PlcRevisionProviderOptions(int maximumBytes, string temporaryRoot)
    {
        if (maximumBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        MaximumBytes = maximumBytes;
        TemporaryRoot = string.IsNullOrWhiteSpace(temporaryRoot) ? throw new ArgumentException("Temporary root is required.", nameof(temporaryRoot)) : temporaryRoot;
    }
    public int MaximumBytes { get; }
    public string TemporaryRoot { get; }
    public static PlcRevisionProviderOptions Default { get; } = new PlcRevisionProviderOptions(
        16_777_216, Path.Combine(Path.GetTempPath(), "TiaGitAddIn", "comparison"));
}
```

`GitBlobReader` constructor is `(IGitProcessRunner textRunner, IGitBinaryProcessRunner binaryRunner, string gitExecutablePath, string repositoryRoot)`. Validate the repository-relative path before process invocation: reject empty, rooted, drive-qualified, NUL, `.`/`..` segments, and normalized paths escaping `repositoryRoot`. Validate sources: `HEAD` is literal; commit and parent sources carry only 7–64 hex characters. Form the object expression only after validation; parent appends `^` internally. Invoke `cat-file -s <object-expression>` through the text runner, then `cat-file blob <object-expression>` through the binary runner. These are discrete arguments and `UseShellExecute=false`; never emit a command line to a shell.

- [ ] **Step 4: Implement strict decode and pre-parse size gating (2–5 min)**

`PlcRevisionProvider.LoadAsync` must execute this order: validate path/source → get size → reject `>MaximumBytes` → read with the same maximum → verify returned count equals reported size → decode → construct immutable revision → construct lease. Use this decoding matrix:

```csharp
private static DecodedRevision Decode(byte[] bytes)
{
    if (StartsWith(bytes, 0xEF, 0xBB, 0xBF))
        return Text(new UTF8Encoding(false, true), bytes, 3, PlcTextEncoding.Utf8WithBom);
    if (StartsWith(bytes, 0xFF, 0xFE))
        return Text(new UnicodeEncoding(false, true, true), bytes, 2, PlcTextEncoding.Utf16LittleEndianWithBom);
    if (StartsWith(bytes, 0xFE, 0xFF))
        return Text(new UnicodeEncoding(true, true, true), bytes, 2, PlcTextEncoding.Utf16BigEndianWithBom);
    if (bytes.Any(value => value == 0))
        return DecodedRevision.Binary("NUL bytes were found without a supported Unicode BOM.");

    try
    {
        string text = new UTF8Encoding(false, true).GetString(bytes);
        return DecodedRevision.Text(text, PlcTextEncoding.Utf8WithoutBom);
    }
    catch (DecoderFallbackException)
    {
        return DecodedRevision.Binary("Content is not strict UTF-8 and has no supported Unicode BOM.");
    }
}
```

Catch no I/O/process exception here. Let a typed `RevisionLoadException`/`RevisionSizeLimitException` reach the coordinator load boundary so it becomes a hard error rather than fallback.

- [ ] **Step 5: Write the RED lease isolation/cancellation tests (2–5 min)**

```csharp
[Fact]
public void ConcurrentLeasesAreUniquePreserveSuffixAndDeleteTheirOwnScope()
{
    string root = CreateTestRoot();
    PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, "text", "Blocks/Program.scl");
    PlcRevisionLease first = PlcRevisionLease.Create(revision, root);
    PlcRevisionLease second = PlcRevisionLease.Create(revision, root);

    Assert.NotEqual(first.WorkingFilePath, second.WorkingFilePath);
    Assert.Equal(".scl", Path.GetExtension(first.WorkingFilePath));
    Assert.True(File.Exists(first.WorkingFilePath));
    Assert.True(File.Exists(second.WorkingFilePath));

    string firstDirectory = first.LeaseDirectory!;
    string secondDirectory = second.LeaseDirectory!;
    first.Dispose();
    Assert.False(Directory.Exists(firstDirectory));
    Assert.True(Directory.Exists(secondDirectory));
    second.Dispose();
    Assert.False(Directory.Exists(secondDirectory));
}

[Fact]
public void MissingLeaseCreatesNoTemporaryFileAndDisposeIsIdempotent()
{
    PlcRevision revision = ComparisonTestData.MissingRevision(PlcRevisionSide.Left);
    PlcRevisionLease lease = PlcRevisionLease.Create(revision, CreateTestRoot());
    Assert.Null(lease.WorkingFilePath);
    lease.Dispose();
    lease.Dispose();
    Assert.Equal(1, lease.DisposeCountForTests);
}
```

- [ ] **Step 6: Run lease tests RED, then implement scoped cleanup GREEN (2–5 min)**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~PlcRevisionLeaseTests"
```

Expected RED: missing `PlcRevisionLease.Create`/properties. Implement it so a present revision creates `<temporaryRoot>/<Guid:N>/revision<originalSuffix>` with `FileMode.CreateNew`, writes exact bytes, and exposes `Revision`, `LeaseDirectory`, and `WorkingFilePath`. Missing revisions create no directory. `Dispose` uses `Interlocked.Exchange` for one cleanup attempt; retry transient `IOException`/`UnauthorizedAccessException` three times with delays `20, 50, 100` ms, then throw `RevisionCleanupException` containing a redacted lease identifier, never the full temporary path. Rerun and expect PASS.

- [ ] **Step 7: Add raw stdout reading to the Siemens adapter (2–5 min)**

Make `GitProcessRunner` implement both `IGitProcessRunner` and `IGitBinaryProcessRunner`. Reuse process-start validation, quoting, timeout, cancellation, and kill logic. In the binary branch, read `process.StandardOutput.BaseStream` into an 81920-byte buffer; after each read, throw `RevisionSizeLimitException` if `total > maximumStandardOutputBytes`, cancel/kill the process, and clear the partially accumulated buffer. Read stderr concurrently as text. Do not convert stdout through `StreamReader`, and do not log bytes or the temporary path.

Add a focused adapter test using the repository's existing fake/process boundary to assert arguments remain separate and `maximumStandardOutputBytes` is honored. If the Siemens process type cannot be constructed in a unit test, extract an internal `ReadBoundedAsync(Stream,int,CancellationToken)` method and expose it to the test assembly through the repository's existing `InternalsVisibleTo` mechanism.

- [ ] **Step 8: Run revision and process tests GREEN (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~PlcRevisionProviderTests|FullyQualifiedName~PlcRevisionLeaseTests|FullyQualifiedName~GitProcessRunnerTests"
```

Expected: PASS for exact bytes, all four encodings, invalid UTF-8, limit `N/N+1`, malicious path/revision rejection, concurrent leases, cancellation, and bounded raw stream reading.

- [ ] **Step 9: Commit the revision infrastructure (2–5 min)**

Extract shared Add-In process setup/argument escaping only after both text and binary paths pass; do not change the existing `IGitProcessRunner` contract used by VCI Git. Then run:

```powershell
git diff --check
git add src/TiaGitAddIn.Core/Services/Revision src/TiaGitAddIn/Services/GitProcessRunner.cs src/TiaGitAddIn.Tests/Revision src/TiaGitAddIn.Tests/Services/GitProcessRunnerTests.cs
git commit -m "feat: load immutable comparison revisions"
```

Expected: a focused commit with no project or coverage changes.

---

### Task 4: Classify Artifact Pairs from Suffix, Path, and Content Evidence

**Acceptance criteria:** AC-012, AC-014, AC-015, AC-016, AC-017, AC-020, AC-024, AC-115.

**Files:**
- Create: `src/TiaGitAddIn.Core/Services/Comparison/PlcArtifactClassifier.cs`
- Create: `src/TiaGitAddIn.Tests/Comparison/PlcArtifactClassifierTests.cs`

**Interfaces:**
- Consumes: `PlcRevision`, `PlcArtifactDescriptor`, and `PlcArtifactPairDescriptor` from Task 2.
- Produces: `IPlcArtifactClassifier.Classify(PlcRevision)` and `Resolve(PlcRevision,PlcRevision)`, consumed by Task 6.

- [ ] **Step 1: Write the RED evidence-matrix theory (2–5 min)**

```csharp
[Theory]
[InlineData("Neutral/Program.xml", "<SW.Blocks.FB><AttributeList><ProgrammingLanguage>LAD</ProgrammingLanguage></AttributeList></SW.Blocks.FB>", PlcArtifactKind.Lad, PlcComparisonMode.Visual)]
[InlineData("Neutral/Program.xml", "<SW.Blocks.FB><AttributeList><ProgrammingLanguage>FBD</ProgrammingLanguage></AttributeList></SW.Blocks.FB>", PlcArtifactKind.Fbd, PlcComparisonMode.Visual)]
[InlineData("Neutral/Program.scl", "FUNCTION_BLOCK Motor\nBEGIN\nEND_FUNCTION_BLOCK", PlcArtifactKind.Scl, PlcComparisonMode.Structured)]
[InlineData("SCL/Program.xml", "<root><value>plain xml</value></root>", PlcArtifactKind.GenericXml, PlcComparisonMode.Text)]
[InlineData("Neutral/Program.stl", "A I 0.0", PlcArtifactKind.Stl, PlcComparisonMode.Text)]
[InlineData("Neutral/Program.sfc", "SFC Test", PlcArtifactKind.Sfc, PlcComparisonMode.Text)]
public void ClassifyUsesTheEvidenceMatrix(string path, string text, PlcArtifactKind kind, PlcComparisonMode mode)
{
    PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, text, path);
    PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);
    Assert.Equal(kind, result.ArtifactKind);
    Assert.Equal(mode, result.PreferredMode);
    Assert.NotEmpty(result.Evidence);
}

[Fact]
public void ValidSclSuffixWithoutLexicalEvidenceFallsBackToTextWithDiagnostic()
{
    PlcRevision revision = ComparisonTestData.TextRevision(PlcRevisionSide.Left, "ordinary notes", "Program.scl");
    PlcArtifactDescriptor result = new PlcArtifactClassifier().Classify(revision);
    Assert.Equal(PlcArtifactKind.Text, result.ArtifactKind);
    Assert.Equal(PlcComparisonMode.Text, result.PreferredMode);
    Assert.Contains(result.Evidence, value => value.Contains("invalid-scl-evidence", StringComparison.Ordinal));
}
```

- [ ] **Step 2: Run classifier tests RED (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~PlcArtifactClassifierTests"
```

Expected: FAIL because `PlcArtifactClassifier` is missing.

- [ ] **Step 3: Implement the single-side classifier in precedence order (2–5 min)**

Define:

```csharp
public interface IPlcArtifactClassifier
{
    PlcArtifactDescriptor Classify(PlcRevision revision);
    PlcArtifactPairDescriptor Resolve(PlcRevision left, PlcRevision right);
}
```

The precedence is exact:

1. missing side is not classified;
2. `IsBinary` or undecoded present bytes → `Binary/Unsupported`;
3. well-formed SimaticML root/block plus its `ProgrammingLanguage` value → `Lad/Visual` or `Fbd/Visual`;
4. SCL suffix plus lexical top-level block opener/terminator evidence outside strings/comments → `Scl/Structured`;
5. STL/SFC suffix or bounded leading content marker → its artifact kind with `Text` preferred mode;
6. well-formed non-Simatic XML → `GenericXml/Text`;
7. all other decoded input → `Text/Text`.

Normalize path separators and suffix case, but never classify from a directory word alone. Limit classification inspection to the first 1,048,576 characters. Use `XmlReader` with DTD prohibited/resolver null for XML evidence; classification must not build a full DOM.

- [ ] **Step 4: Write RED pair-resolution tests (2–5 min)**

```csharp
[Fact]
public void ConflictingFbdAndSclSidesResolveToTextFallback()
{
    PlcRevision left = ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml, "Program.xml");
    PlcRevision right = ComparisonTestData.TextRevision(PlcRevisionSide.Right, ValidScl, "Program.scl");
    PlcArtifactPairDescriptor pair = new PlcArtifactClassifier().Resolve(left, right);
    Assert.Equal(PlcArtifactKind.Text, pair.ArtifactKind);
    Assert.Equal(PlcComparisonMode.Text, pair.RequestedMode);
    Assert.Contains(pair.Diagnostics, d => d.Code == "CMP-CLASS-CONFLICT");
    Assert.False(string.IsNullOrWhiteSpace(pair.Limitation));
}

[Theory]
[InlineData(true, PlcPairChangeKind.Added)]
[InlineData(false, PlcPairChangeKind.Removed)]
public void MissingSideClassifiesFromAvailableSide(bool leftMissing, PlcPairChangeKind expected)
{
    PlcRevision left = leftMissing ? ComparisonTestData.MissingRevision(PlcRevisionSide.Left)
        : ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml);
    PlcRevision right = leftMissing ? ComparisonTestData.TextRevision(PlcRevisionSide.Right, FbdXml)
        : PlcRevision.Missing(PlcRevisionSide.Right, PlcRevisionSource.WorkingTree, "Program.xml", PlcRevisionMissingReason.Deleted);
    PlcArtifactPairDescriptor pair = new PlcArtifactClassifier().Resolve(left, right);
    Assert.Equal(expected, pair.ChangeKind);
    Assert.Equal(PlcArtifactKind.Fbd, pair.ArtifactKind);
}
```

- [ ] **Step 5: Implement deterministic pair resolution and run GREEN (2–5 min)**

When both kinds/modes match, retain them. When one side is missing, use the present side and explicit Added/Removed. When either is binary, return `Binary/Unsupported`. When present sides conflict, return `Text/Text`, limitation `Artifact kinds differ; semantic comparison is unavailable.`, and diagnostic code `CMP-CLASS-CONFLICT`; keep each side descriptor as evidence. Working-tree versus commit source kind must never affect the result.

Run the focused command from Step 2. Expected: PASS for the full matrix, conflicts, missing sides, path-only traps, generic XML, and source-kind equivalence.

- [ ] **Step 6: Commit the evidence classifier (2–5 min)**

Keep the classifier under 400 lines by extracting only the bounded `SimaticMlEvidenceReader` and `SclLexicalProbe` if needed; both remain in `Services/Comparison` and have focused unit tests. Then:

```powershell
git diff --check
git add src/TiaGitAddIn.Core/Services/Comparison src/TiaGitAddIn.Tests/Comparison/PlcArtifactClassifierTests.cs
git commit -m "feat: classify PLC comparison artifacts"
```

Expected: one content-aware classifier commit; no UI path heuristic is added.

---

### Task 5: Add Bounded Generic Text Comparison and Safe Diagnostics

**Acceptance criteria:** AC-015, AC-016, AC-018, AC-020, AC-023, AC-030, AC-098, AC-117.

**Files:**
- Create: `src/TiaGitAddIn.Core/Services/Comparison/LineTextComparer.cs`
- Create: `src/TiaGitAddIn.Core/Services/Comparison/ComparisonDiagnosticSanitizer.cs`
- Create: `src/TiaGitAddIn.Core/Services/Comparison/TextFallbackStrategy.cs`
- Create: `src/TiaGitAddIn.Tests/Comparison/LineTextComparerTests.cs`
- Create: `src/TiaGitAddIn.Tests/Comparison/ComparisonDiagnosticSanitizerTests.cs`

**Interfaces:**
- Consumes: `ITextComparer`, result factory, text presentation/line types, and strategy seam from Task 2.
- Produces: bounded text fallback and safe diagnostic creation consumed by Task 6 and all semantic strategies.

- [ ] **Step 1: Write RED text-diff behavior and limit tests (2–5 min)**

```csharp
[Fact]
public void CompareRetainsIndependentLineNumbersAndChangeKinds()
{
    var comparer = new LineTextComparer(TextComparisonLimits.Default);
    TextPresentation result = comparer.Compare(new ComparisonRawText(
        "same\nremoved\nold", "same\nadded\nnew", false, false));

    Assert.Collection(result.Lines,
        line => Assert.Equal(TextDiffLineKind.Unchanged, line.Kind),
        line => Assert.Equal(TextDiffLineKind.Removed, line.Kind),
        line => Assert.Equal(TextDiffLineKind.Added, line.Kind),
        line => Assert.Equal(TextDiffLineKind.Removed, line.Kind),
        line => Assert.Equal(TextDiffLineKind.Added, line.Kind));
    Assert.Equal(2, result.Lines[1].LeftLineNumber);
    Assert.Null(result.Lines[1].RightLineNumber);
    Assert.Null(result.Lines[2].LeftLineNumber);
    Assert.Equal(2, result.Lines[2].RightLineNumber);
}

[Fact]
public void CompareSwitchesToBoundedLinearDiffAboveMatrixLimit()
{
    var limits = new TextComparisonLimits(100, 100, maximumMatrixCells: 4);
    var comparer = new LineTextComparer(limits);
    TextPresentation result = comparer.Compare(new ComparisonRawText("a\nb\nc", "a\nx\nc", false, false));
    Assert.True(result.UsedLinearFallback);
    Assert.Contains(result.Lines, line => line.Kind == TextDiffLineKind.Removed && line.Text == "b");
    Assert.Contains(result.Lines, line => line.Kind == TextDiffLineKind.Added && line.Text == "x");
}

[Fact]
public void CompareTruncatesDisplayAtConfiguredLineAndLengthLimits()
{
    var comparer = new LineTextComparer(new TextComparisonLimits(2, 3, 100));
    TextPresentation result = comparer.Compare(new ComparisonRawText("abcdef\nline2\nline3", "abcdef\nline2\nline3", false, false));
    Assert.True(result.IsTruncated);
    Assert.Contains(result.Lines, line => line.Kind == TextDiffLineKind.Omitted);
    Assert.All(result.Lines.Where(line => line.Kind != TextDiffLineKind.Omitted), line => Assert.True(line.Text.Length <= 3));
}
```

- [ ] **Step 2: Run text tests RED (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~LineTextComparerTests"
```

Expected: FAIL because the bounded implementation and its properties do not exist.

- [ ] **Step 3: Implement the exact text contracts and deterministic algorithms (2–5 min)**

```csharp
public sealed class TextComparisonLimits
{
    public TextComparisonLimits(int maximumLinesPerSide, int maximumLineLength, long maximumMatrixCells)
    {
        if (maximumLinesPerSide <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLinesPerSide));
        if (maximumLineLength <= 0) throw new ArgumentOutOfRangeException(nameof(maximumLineLength));
        if (maximumMatrixCells <= 0) throw new ArgumentOutOfRangeException(nameof(maximumMatrixCells));
        MaximumLinesPerSide = maximumLinesPerSide; MaximumLineLength = maximumLineLength; MaximumMatrixCells = maximumMatrixCells;
    }
    public int MaximumLinesPerSide { get; }
    public int MaximumLineLength { get; }
    public long MaximumMatrixCells { get; }
    public static TextComparisonLimits Default { get; } = new TextComparisonLimits(20_000, 32_768, 4_000_000);
}

public enum TextDiffLineKind { Unchanged, Added, Removed, Omitted }

public sealed class TextDiffLine
{
    public TextDiffLine(TextDiffLineKind kind, int? leftLineNumber, int? rightLineNumber, string text)
    { Kind = kind; LeftLineNumber = leftLineNumber; RightLineNumber = rightLineNumber; Text = text ?? throw new ArgumentNullException(nameof(text)); }
    public TextDiffLineKind Kind { get; }
    public int? LeftLineNumber { get; }
    public int? RightLineNumber { get; }
    public string Text { get; }
}

public sealed class TextPresentation : ComparisonPresentation
{
    public TextPresentation(IEnumerable<TextDiffLine> lines, bool isTruncated = false, bool usedLinearFallback = false)
        : base(ComparisonPresentationKind.Text)
    { Lines = (lines ?? throw new ArgumentNullException(nameof(lines))).ToArray(); IsTruncated = isTruncated; UsedLinearFallback = usedLinearFallback; }
    public IReadOnlyList<TextDiffLine> Lines { get; }
    public bool IsTruncated { get; }
    public bool UsedLinearFallback { get; }
}
```

Normalize CRLF/CR to LF for display only. When `leftCount * rightCount <= MaximumMatrixCells`, use an LCS table and emit ties deterministically as removal then addition. Above it, compare positions in O(n+m), emitting an unequal position as removal then addition. Cap displayed lines/characters and append one `Omitted` row; never mutate or truncate `ComparisonRawText` itself. Rerun Step 2 and expect PASS.

- [ ] **Step 4: Write RED diagnostic-redaction tests (2–5 min)**

```csharp
[Fact]
public void SanitizeRemovesSecretsPathsUserInfoAndStackTrace()
{
    string unsafeMessage = "failed https://alice:secret@example.test/repo token=abc123 " +
        @"C:\Users\alice\AppData\Local\Temp\TiaGitAddIn\comparison\lease\Program.xml" +
        "\r\n   at Namespace.Type.Method()";

    PlcComparisonDiagnostic result = new ComparisonDiagnosticSanitizer().ForUser(
        "CMP-PARSE-001", PlcDiagnosticSeverity.Error, unsafeMessage,
        new PlcSourceLocation(PlcRevisionSide.Right, 12, 4));

    Assert.Equal("CMP-PARSE-001", result.Code);
    Assert.Equal(12, result.Location!.Line);
    Assert.DoesNotContain("alice", result.Message, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("secret", result.Message, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("abc123", result.Message, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("Temp", result.Message, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain(" at ", result.Message, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 5: Implement safe diagnostics and fallback strategy GREEN (2–5 min)**

`ComparisonDiagnosticSanitizer.ForUser` accepts only a stable caller-owned code, enum severity, unsafe source string, and already-safe location. Apply compiled bounded regexes for URL user information, `(token|password|secret|apikey)=...`, Windows/Unix temporary paths, and stack-trace lines; cap the result at 1,024 characters; if blank after redaction use `Comparison failed; see the Add-In log with reference <code>.` Do not put a path in `PlcSourceLocation`.

Implement `TextFallbackStrategy` with supported kinds `{ Text, GenericXml, Stl, Sfc }`; it calls `PlcComparisonResultFactory.CreateTextFallback`, using pair limitation when present or `<kind> semantic comparison is unavailable.` It never catches cancellation.

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~LineTextComparerTests|FullyQualifiedName~ComparisonDiagnosticSanitizerTests"
```

Expected: PASS with no secret/path/stack data and deterministic bounded text output.

- [ ] **Step 6: Commit text fallback and sanitizer (2–5 min)**

```powershell
git diff --check
git add src/TiaGitAddIn.Core/Models/Comparison/ComparisonPresentations.cs src/TiaGitAddIn.Core/Services/Comparison/LineTextComparer.cs src/TiaGitAddIn.Core/Services/Comparison/ComparisonDiagnosticSanitizer.cs src/TiaGitAddIn.Core/Services/Comparison/TextFallbackStrategy.cs src/TiaGitAddIn.Tests/Comparison/LineTextComparerTests.cs src/TiaGitAddIn.Tests/Comparison/ComparisonDiagnosticSanitizerTests.cs
git commit -m "feat: add bounded text comparison fallback"
```

Expected: a focused commit; raw text remains available only through the existing immutable result field.

---

### Task 6: Centralize Strategy Routing and Every Non-Cancelled Outcome

**Acceptance criteria:** AC-008, AC-013, AC-015, AC-016, AC-017, AC-018, AC-019, AC-020, AC-021, AC-024, AC-117.

**Files:**
- Create: `src/TiaGitAddIn.Core/Services/Comparison/PlcComparisonCoordinator.cs`
- Create: `src/TiaGitAddIn.Tests/Comparison/PlcComparisonCoordinatorTests.cs`

**Interfaces:**
- Consumes: classifier (Task 4), strategy collection/result factory/sanitizer (Tasks 2 and 5), loaded revisions (Task 3).
- Produces: `IPlcComparisonCoordinator.CompareAsync` consumed by selection orchestration in Task 10; the sole semantic-strategy router used by FBD/SCL.

- [ ] **Step 1: Write RED strategy-selection and invariant tests (2–5 min)**

```csharp
[Fact]
public async Task SelectsExactlyOneCompatibleStrategy()
{
    var fbd = new RecordingStrategy(PlcArtifactKind.Fbd, SemanticFbdResult());
    var scl = new RecordingStrategy(PlcArtifactKind.Scl, SemanticSclResult());
    IPlcComparisonCoordinator coordinator = CreateCoordinator(fbd, scl, new TextFallbackStrategy(ResultFactory));

    PlcComparisonResult result = await coordinator.CompareAsync(
        ComparisonTestData.TextRevision(PlcRevisionSide.Left, FbdXml),
        ComparisonTestData.TextRevision(PlcRevisionSide.Right, FbdXml),
        CancellationToken.None);

    Assert.Equal(1, fbd.CallCount);
    Assert.Equal(0, scl.CallCount);
    Assert.Equal(ComparisonPresentationKind.LogicNetwork, result.Presentation.Kind);
}

[Fact]
public async Task DuplicateSemanticRegistrationReturnsHardError()
{
    IPlcComparisonCoordinator coordinator = CreateCoordinator(
        new RecordingStrategy(PlcArtifactKind.Fbd, SemanticFbdResult()),
        new RecordingStrategy(PlcArtifactKind.Fbd, SemanticFbdResult()));
    PlcComparisonResult result = await coordinator.CompareAsync(LeftFbd, RightFbd, CancellationToken.None);
    Assert.Equal(ComparisonPresentationKind.Error, result.Presentation.Kind);
    Assert.Equal(PlcSupportLevel.Unsupported, result.SupportLevel);
    Assert.Null(result.RawText);
    Assert.Contains(result.Diagnostics, d => d.Code == "CMP-ROUTE-DUPLICATE");
}

[Fact]
public async Task CancellationEscapesWithoutAnErrorResult()
{
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
        CreateCoordinator(new CancellingStrategy()).CompareAsync(LeftFbd, RightFbd, cts.Token));
}
```

- [ ] **Step 2: Run coordinator tests RED (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~PlcComparisonCoordinatorTests"
```

Expected: FAIL because coordinator interfaces/classes are missing.

- [ ] **Step 3: Implement the coordinator public API and route table (2–5 min)**

```csharp
public interface IPlcComparisonCoordinator
{
    Task<PlcComparisonResult> CompareAsync(
        PlcRevision left,
        PlcRevision right,
        CancellationToken cancellationToken);

    PlcComparisonResult CreateRevisionLoadError(
        PlcArtifactKind bestKnownKind,
        PlcComparisonMode requestedMode,
        Exception exception,
        PlcRevisionSide side);
}
```

Constructor:

```csharp
public PlcComparisonCoordinator(
    IPlcArtifactClassifier classifier,
    IEnumerable<IPlcComparisonStrategy> strategies,
    PlcComparisonResultFactory resultFactory,
    ComparisonDiagnosticSanitizer sanitizer)
```

Copy registrations in the constructor. In `CompareAsync`, check cancellation, resolve the pair, build `ComparisonRawText` only when every present side decoded, and build `PlcComparisonContext`. Route binary directly to unsupported. Find strategies by exact `SupportedKinds.Contains(pair.ArtifactKind)`: zero registrations with raw text → factory text fallback and `CMP-ROUTE-FALLBACK`; zero without raw text → unsupported; one → invoke it; more than one → hard error `CMP-ROUTE-DUPLICATE`. Never inspect filename/path again.

- [ ] **Step 4: Add recoverable/hard-error boundaries without catching cancellation (2–5 min)**

Introduce a Core-only `RecoverableComparisonException` carrying one safe diagnostic and a non-blank limitation. Catch it only when raw text exists and create text fallback. Catch parser format/limit exceptions as recoverable when raw text exists. Catch all other `Exception` after an explicit `catch (OperationCanceledException) { throw; }`, sanitize it, and return hard error. `CreateRevisionLoadError` always returns hard error with `CMP-REVISION-LOAD` (or `CMP-REVISION-LIMIT` for `RevisionSizeLimitException`) and never raw text.

After a strategy returns, assert result `ArtifactKind == pair.ArtifactKind` and `RequestedMode == pair.RequestedMode`; a violation becomes `CMP-RESULT-INVARIANT` hard error. Do not rewrap a valid result or discard its typed presentation.

- [ ] **Step 5: Add RED fallback/error matrix theory (2–5 min)**

```csharp
[Theory]
[InlineData(PlcArtifactKind.GenericXml, PlcComparisonMode.Text, PlcSupportLevel.Fallback, ComparisonPresentationKind.Text)]
[InlineData(PlcArtifactKind.Stl, PlcComparisonMode.Text, PlcSupportLevel.Fallback, ComparisonPresentationKind.Text)]
[InlineData(PlcArtifactKind.Sfc, PlcComparisonMode.Text, PlcSupportLevel.Fallback, ComparisonPresentationKind.Text)]
[InlineData(PlcArtifactKind.Binary, PlcComparisonMode.Unsupported, PlcSupportLevel.Unsupported, ComparisonPresentationKind.Unsupported)]
public async Task NonSemanticKindsReturnExplicitOutcome(PlcArtifactKind kind, PlcComparisonMode mode,
    PlcSupportLevel support, ComparisonPresentationKind presentation)
{
    PlcComparisonResult result = await CreateCoordinator(TextStrategy)
        .CompareAsync(RevisionFor(kind, PlcRevisionSide.Left), RevisionFor(kind, PlcRevisionSide.Right), CancellationToken.None);
    Assert.Equal(kind, result.ArtifactKind);
    Assert.Equal(mode, result.ActualMode);
    Assert.Equal(support, result.SupportLevel);
    Assert.Equal(presentation, result.Presentation.Kind);
    Assert.False(string.IsNullOrWhiteSpace(result.Limitation));
}
```

Add separate tests for pair conflict, malformed semantic input → text fallback with raw sides, recoverable unknown structure → semantic Partial, strategy exception → hard error/no raw, and working-tree/commit identity. Run Step 2; expected RED for any branch not yet handled.

- [ ] **Step 6: Complete routing GREEN and refactor table construction (2–5 min)**

Make every matrix case pass. Build an immutable dictionary from kind to registrations once in the constructor; retain duplicate entries so duplicate detection still works. Rerun Step 2. Expected: PASS and every non-cancelled result satisfies AC-117.

- [ ] **Step 7: Commit centralized routing (2–5 min)**

```powershell
git diff --check
git add src/TiaGitAddIn.Core/Services/Comparison/PlcComparisonCoordinator.cs src/TiaGitAddIn.Tests/Comparison/PlcComparisonCoordinatorTests.cs
git commit -m "feat: centralize comparison strategy routing"
```

Expected: one coordinator commit; no ViewModel contains routing logic yet.

---

### Task 7: Replace Unsafe SimaticML Loading with an Immutable, Bounded Parser Seam

**Acceptance criteria:** AC-007, AC-018, AC-095, AC-096, AC-113, AC-114.

**Files:**
- Create: `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlParserLimits.cs`
- Create: `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlParseResult.cs`
- Modify: `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlModels.cs`
- Modify: `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlParser.cs`
- Modify: parser call sites under `src/TiaGitAddIn.Core/Services/SimaticMl/`
- Create: `src/TiaGitAddIn.Tests/Services/SimaticMl/SimaticMlParserSecurityTests.cs`
- Modify: existing parser tests under `src/TiaGitAddIn.Tests/Services/SimaticMl/`

**Interfaces:**
- Consumes: safe diagnostics/source side from Task 2; decoded text from Task 3.
- Produces: `SimaticMlParser.ParseText(...)`, immutable `SimaticMlFile`, and parser limits/result consumed by Task 8 and the FBD plan.

- [ ] **Step 1: Write RED DTD, external-entity, and boundary tests (2–5 min)**

```csharp
[Fact]
public void ParseTextRejectsDtdWithoutResolvingExternalContent()
{
    string xml = "<!DOCTYPE x [<!ENTITY leak SYSTEM 'file:///C:/Windows/win.ini'>]>" +
                 "<Document><SW.Blocks.FB><AttributeList><Name>&leak;</Name></AttributeList></SW.Blocks.FB></Document>";
    SimaticMlParseResult result = SimaticMlParser.ParseText(xml, SimaticMlParserLimits.Default,
        PlcRevisionSide.Left, CancellationToken.None);
    Assert.False(result.IsSuccess);
    Assert.Null(result.Model);
    Assert.Contains(result.Diagnostics, d => d.Code == "CMP-XML-DTD");
    Assert.DoesNotContain("Windows", string.Join(" ", result.Diagnostics.Select(d => d.Message)), StringComparison.OrdinalIgnoreCase);
}

[Theory]
[InlineData("characters")]
[InlineData("elements")]
[InlineData("depth")]
public void ParseTextAcceptsNAndRejectsNPlusOne(string boundary)
{
    ParserBoundaryCase fixture = ParserBoundaryCase.Create(boundary);
    Assert.True(SimaticMlParser.ParseText(fixture.AtLimitXml, fixture.Limits,
        PlcRevisionSide.Left, CancellationToken.None).IsSuccess);
    SimaticMlParseResult over = SimaticMlParser.ParseText(fixture.OverLimitXml, fixture.Limits,
        PlcRevisionSide.Left, CancellationToken.None);
    Assert.False(over.IsSuccess);
    Assert.Contains(over.Diagnostics, d => d.Code == fixture.ExpectedDiagnosticCode);
}

[Fact]
public void ParseTextObservesPreCancelledToken()
{
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    Assert.ThrowsAny<OperationCanceledException>(() => SimaticMlParser.ParseText(
        MinimalFbXml, SimaticMlParserLimits.Default, PlcRevisionSide.Right, cts.Token));
}
```

- [ ] **Step 2: Run parser security tests RED (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SimaticMlParserSecurityTests"
```

Expected: FAIL because `ParseText`, limits, and parse result do not exist; the current `XDocument.Load(path)` path is not acceptable GREEN behavior.

- [ ] **Step 3: Add the exact safe parser contract (2–5 min)**

```csharp
public sealed class SimaticMlParserLimits
{
    public SimaticMlParserLimits(int maximumCharactersInDocument, int maximumElementCount, int maximumDepth)
    {
        if (maximumCharactersInDocument <= 0) throw new ArgumentOutOfRangeException(nameof(maximumCharactersInDocument));
        if (maximumElementCount <= 0) throw new ArgumentOutOfRangeException(nameof(maximumElementCount));
        if (maximumDepth <= 0) throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        MaximumCharactersInDocument = maximumCharactersInDocument;
        MaximumElementCount = maximumElementCount;
        MaximumDepth = maximumDepth;
    }
    public int MaximumCharactersInDocument { get; }
    public int MaximumElementCount { get; }
    public int MaximumDepth { get; }
    public static SimaticMlParserLimits Default { get; } = new SimaticMlParserLimits(16_777_216, 250_000, 128);
}

public sealed class SimaticMlParseResult
{
    public SimaticMlParseResult(SimaticMlFile? model, bool isPartial, IEnumerable<PlcComparisonDiagnostic> diagnostics)
    { Model = model; IsPartial = isPartial; Diagnostics = (diagnostics ?? throw new ArgumentNullException(nameof(diagnostics))).ToArray(); }
    public SimaticMlFile? Model { get; }
    public bool IsPartial { get; }
    public IReadOnlyList<PlcComparisonDiagnostic> Diagnostics { get; }
    public bool IsSuccess => Model != null && !Diagnostics.Any(d => d.Severity == PlcDiagnosticSeverity.Error);
}
```

Add this exact overload to the existing static parser:

```csharp
public static SimaticMlParseResult ParseText(string xml, SimaticMlParserLimits limits,
    PlcRevisionSide side, CancellationToken cancellationToken)
```

Keep `Parse(string xmlPath)` temporarily, but implement it only as a compatibility wrapper that opens a bounded `StreamReader`, checks file length against the default character limit, calls `ParseText`, and throws `InvalidDataException` only for legacy callers when `IsSuccess=false`. New comparison strategies may not call the path overload.

- [ ] **Step 4: Implement secure bounded reading before DOM construction (2–5 min)**

Reject `xml.Length > MaximumCharactersInDocument` before allocating XML structures. Create `XmlReaderSettings` exactly as follows:

```csharp
var settings = new XmlReaderSettings
{
    DtdProcessing = DtdProcessing.Prohibit,
    XmlResolver = null,
    IgnoreComments = false,
    IgnoreWhitespace = false,
    MaxCharactersInDocument = limits.MaximumCharactersInDocument,
    CloseInput = true
};
```

First scan with `XmlReader.Create(new StringReader(xml), settings)`, increment element count on `XmlNodeType.Element`, reject count `>MaximumElementCount`, reject `reader.Depth > MaximumDepth`, and call `cancellationToken.ThrowIfCancellationRequested()` at least every 64 reads. Then create a second reader with the same settings and call `XDocument.Load(reader, PreserveWhitespace|SetLineInfo)`. Catch `XmlException` only to map DTD errors to `CMP-XML-DTD` and other syntax to `CMP-XML-SYNTAX`, preserving safe side/line/column. Do not include entity URI, source path, stack trace, or raw XML in the diagnostic.

- [ ] **Step 5: Make parser models externally immutable (2–5 min)**

In `SimaticMlModels.cs`, change public setters to `internal set`, replace every public `List<T>` with `IReadOnlyList<T>` initialized to `Array.Empty<T>()`, and every public `Dictionary<TKey,TValue>` with `IReadOnlyDictionary<TKey,TValue>` initialized to a read-only empty dictionary using `StringComparer.Ordinal`. Parser helpers build local mutable lists/dictionaries and assign `ToArray()`/new `ReadOnlyDictionary<,>` exactly once when constructing each model. Apply this to `SimaticMlFile`, `BlockDefinition`, `InterfaceSection`, `InterfaceMember`, compile units, networks, accesses/components, parts/template values, calls, open branches, rails, wires, and multilingual text definitions; do not leave a caller-visible mutable collection at any nesting level.

Add a test that retains a parsed model, compares it twice, asserts all collection properties are not assignable to `IList<T>`/`IDictionary<TKey,TValue>`, and serializes it before/after to prove no parser/comparer mutation.

- [ ] **Step 6: Run parser and LAD parser regressions GREEN (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~SimaticMlParserSecurityTests|FullyQualifiedName~SimaticMlParserTests|FullyQualifiedName~SimaticMlComparerTests"
```

Expected: PASS for DTD/entity rejection, exact limits, cancellation, immutable models, and all prior sanitized SimaticML/LAD parsing behavior.

- [ ] **Step 7: Commit the secure parser seam (2–5 min)**

Keep `SimaticMlParser.cs` under 800 lines by moving only limit scanning/diagnostic mapping to `SimaticMlReader.cs` if necessary. Rerun Step 6 after the split. Then:

```powershell
git diff --check
git add src/TiaGitAddIn.Core/Services/SimaticMl src/TiaGitAddIn.Tests/Services/SimaticMl
git commit -m "fix: harden SimaticML parsing boundaries"
```

Expected: secure parser seam committed before any FBD parser consumes it.

---

### Task 8: Compare Complete Interfaces with Independent Immutable Snapshots

**Acceptance criteria:** AC-007, AC-034, AC-035, AC-036, AC-037, AC-038, AC-039, AC-040, AC-041, AC-042, AC-043, AC-105, AC-114.

**Files:**
- Create: `src/TiaGitAddIn.Core/Models/Comparison/InterfaceSnapshot.cs`
- Create: `src/TiaGitAddIn.Core/Models/Comparison/InterfacePresentation.cs`
- Create: `src/TiaGitAddIn.Core/Models/Comparison/LadPresentation.cs`
- Create: `src/TiaGitAddIn.Core/Services/Comparison/InterfaceSnapshotBuilder.cs`
- Create: `src/TiaGitAddIn.Core/Services/Comparison/InterfaceComparer.cs`
- Create: `src/TiaGitAddIn.Core/Services/Comparison/SactCompareResultCloner.cs`
- Modify: `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlParser.cs`
- Modify: `src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlComparer.cs`
- Modify: `src/TiaGitAddIn.Core/Models/Sact/SactInterfaceSections.cs`
- Create: `src/TiaGitAddIn.Tests/Services/SimaticMl/InterfaceComparerTests.cs`
- Modify: `src/TiaGitAddIn.Tests/Services/SimaticMl/SimaticMlComparerTests.cs`

**Interfaces:**
- Consumes: immutable parsed `BlockDefinition` from Task 7 and typed presentation base/result invariant from Task 2.
- Produces: deep `InterfacePresentation`, `LadPresentation`, `InterfaceSnapshotBuilder.Build`, and `InterfaceComparer.Compare`; WPF Task 9 and the LAD strategy in Task 10 consume them. FBD may embed the same immutable `InterfacePresentation` in `FbdPresentation`.

- [ ] **Step 1: Write RED normalization matrix tests (2–5 min)**

Create snapshot test helpers that build one section/member without XML noise. Use this exact theory shape so each field changes independently:

```csharp
[Theory]
[InlineData(InterfaceFieldKind.Datatype, "  Array[1..2] of Int  ", "Array[1..2] of Int", false)]
[InlineData(InterfaceFieldKind.Datatype, "Array [1..2] of Int", "Array[1..2] of Int", true)]
[InlineData(InterfaceFieldKind.StartValue, "  A\r\nB  ", "A\nB", false)]
[InlineData(InterfaceFieldKind.StartValue, "A  B", "A B", true)]
[InlineData(InterfaceFieldKind.Version, " 1.2 ", "1.2", false)]
[InlineData(InterfaceFieldKind.Accessibility, " public ", "Public", false)]
[InlineData(InterfaceFieldKind.Accessibility, "VendorA", "vendora", true)]
public void FieldNormalizationMatchesDeclaredMatrix(
    InterfaceFieldKind field, string left, string right, bool expectedChange)
{
    InterfaceSnapshot leftSnapshot = InterfaceFixture.OneMember(field, left);
    InterfaceSnapshot rightSnapshot = InterfaceFixture.OneMember(field, right);
    InterfaceMemberComparison member = SingleMember(new InterfaceComparer().Compare(leftSnapshot, rightSnapshot));
    Assert.Equal(expectedChange, member.FieldChanges.Any(change => change.Field == field));
    Assert.NotNull(member.Left);
    Assert.NotNull(member.Right);
}

[Theory]
[InlineData(SemanticBoolean.Unspecified, SemanticBoolean.False)]
[InlineData(SemanticBoolean.Unspecified, SemanticBoolean.True)]
[InlineData(SemanticBoolean.False, SemanticBoolean.True)]
public void RetainAndInformativeUseThreeDistinctStates(SemanticBoolean left, SemanticBoolean right)
{
    InterfaceMemberComparison retain = CompareOne(Member(retain: left), Member(retain: right));
    InterfaceMemberComparison informative = CompareOne(Member(informative: left), Member(informative: right));
    Assert.Single(retain.FieldChanges, c => c.Field == InterfaceFieldKind.Retain);
    Assert.Single(informative.FieldChanges, c => c.Field == InterfaceFieldKind.Informative);
}
```

- [ ] **Step 2: Run interface tests RED (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~InterfaceComparerTests"
```

Expected: FAIL because snapshot/presentation/comparer types are missing.

- [ ] **Step 3: Add exact immutable interface types (2–5 min)**

```csharp
public enum SemanticBoolean { Unspecified, False, True }
public enum InterfaceChangeKind { Unchanged, Added, Removed, Modified }
public enum InterfaceFieldKind
{
    Name, Datatype, Retain, DefaultValue, StartValue, Comment,
    Accessibility, Informative, Version, SemanticAttribute
}

public sealed class InterfaceSnapshot
{
    public InterfaceSnapshot(IEnumerable<InterfaceSectionSnapshot> sections)
    { Sections = (sections ?? throw new ArgumentNullException(nameof(sections))).ToArray(); }
    public IReadOnlyList<InterfaceSectionSnapshot> Sections { get; }
}

public sealed class InterfaceSectionSnapshot
{
    public InterfaceSectionSnapshot(string name, bool isPresent, IEnumerable<InterfaceMemberSnapshot> members)
    { Name = Require(name); IsPresent = isPresent; Members = (members ?? throw new ArgumentNullException(nameof(members))).ToArray(); }
    public string Name { get; }
    public bool IsPresent { get; }
    public IReadOnlyList<InterfaceMemberSnapshot> Members { get; }
    private static string Require(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException("Section name is required.", nameof(value)) : value;
}

public sealed class InterfaceMemberSnapshot
{
    public InterfaceMemberSnapshot(string section, string path, string name, string datatype,
        SemanticBoolean retain, string? defaultValue, string? startValue,
        IReadOnlyDictionary<string, string> comments, string? accessibility,
        SemanticBoolean informative, string? version,
        IReadOnlyDictionary<string, string> semanticAttributes,
        IEnumerable<InterfaceMemberSnapshot> children)
    {
        Section = section; Path = path; Name = name; Datatype = datatype; Retain = retain;
        DefaultValue = defaultValue; StartValue = startValue;
        Comments = new ReadOnlyDictionary<string, string>((comments ?? throw new ArgumentNullException(nameof(comments)))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        Accessibility = accessibility; Informative = informative; Version = version;
        SemanticAttributes = new ReadOnlyDictionary<string, string>((semanticAttributes ?? throw new ArgumentNullException(nameof(semanticAttributes)))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        Children = (children ?? throw new ArgumentNullException(nameof(children))).ToArray();
    }
    public string Section { get; }
    public string Path { get; }
    public string Name { get; }
    public string Datatype { get; }
    public SemanticBoolean Retain { get; }
    public string? DefaultValue { get; }
    public string? StartValue { get; }
    public IReadOnlyDictionary<string, string> Comments { get; }
    public string? Accessibility { get; }
    public SemanticBoolean Informative { get; }
    public string? Version { get; }
    public IReadOnlyDictionary<string, string> SemanticAttributes { get; }
    public IReadOnlyList<InterfaceMemberSnapshot> Children { get; }
}
```

Add immutable `InterfaceFieldChange(Field, Key, LeftValue, RightValue)`, `InterfaceMemberComparison(ChangeKind, Left, Right, FieldChanges, Children)`, and `InterfaceSectionComparison(ChangeKind, Left, Right, Members)`. `InterfacePresentation(IEnumerable<InterfaceSectionComparison>)` derives from `ComparisonPresentation(Interface)` and exposes `HasChanges` computed recursively.

- [ ] **Step 4: Implement the normalization contract in one builder (2–5 min)**

`InterfaceSnapshotBuilder.Build(BlockDefinition? block)` returns an empty snapshot for null. The exact rules are:

- canonical sections in order `Input`, `Output`, `InOut`, `Static`, `Temp`, `Constant`, `Return`; known aliases/case normalize to these values; present empty sections remain present;
- member names and each parent segment normalize to Unicode Form C and compare with `StringComparer.Ordinal`; join path segments with `/`; do not include XML ordinal;
- datatype: `Trim()` only;
- default/start: CRLF and CR → LF, then outer `Trim()`, preserve every internal character;
- comments: key by normalized language key using ordinal comparison; values normalize line endings and remove trailing spaces/tabs from each line, not leading/internal whitespace;
- accessibility: trim, case-insensitively canonicalize only `Public`, `Protected`, `Private`, and `ReadOnly`; preserve any other trimmed text exactly;
- retain: blank → Unspecified, case-insensitive `true`/`retain` → True, `false`/`nonretain`/`non-retain` → False;
- informative: nullable Boolean maps directly to the three states;
- version: outer trim only;
- semantic attribute whitelist: `ExternalAccessible`, `ExternalVisible`, `ExternalWritable`, and `SetPoint`; normalize `true`/`1` to `true`, `false`/`0` to `false`, otherwise preserve trimmed text exactly;
- ignore every non-whitelisted attribute, including `UId`, composition IDs/names, timestamps, export order/date/version, document version, order, and ordinal.

Extend parser extraction so `InterfaceMember` retains separate `DefaultValue`, `StartValue`, and language-keyed comments. Do not parse `CommentRawXml` again in the comparer.

- [ ] **Step 5: Write RED hierarchy, identity, and ordering tests (2–5 min)**

```csharp
[Fact]
public void OneSidedParentRetainsSubtreeWithoutDuplicateTopLevelChildren()
{
    InterfaceMemberSnapshot child = Member("Child");
    InterfaceMemberSnapshot parent = Member("Parent", children: new[] { child });
    InterfacePresentation result = new InterfaceComparer().Compare(
        new InterfaceSnapshot(new[] { Section("Input", parent) }), new InterfaceSnapshot(Array.Empty<InterfaceSectionSnapshot>()));
    InterfaceMemberComparison only = Assert.Single(Assert.Single(result.Sections).Members);
    Assert.Equal(InterfaceChangeKind.Removed, only.ChangeKind);
    Assert.Single(only.Children);
    Assert.DoesNotContain(Assert.Single(result.Sections).Members, item => item.Left?.Name == "Child");
}

[Fact]
public void MatchingUsesNfcOrdinalPathAndCaseRemainsDistinct()
{
    InterfacePresentation canonical = CompareMembers(Member("Cafe\u0301"), Member("Café"));
    Assert.Equal(InterfaceChangeKind.Unchanged, SingleMember(canonical).ChangeKind);
    InterfacePresentation casing = CompareMembers(Member("Motor"), Member("motor"));
    Assert.Contains(casing.Sections.SelectMany(s => s.Members), m => m.ChangeKind == InterfaceChangeKind.Added);
    Assert.Contains(casing.Sections.SelectMany(s => s.Members), m => m.ChangeKind == InterfaceChangeKind.Removed);
}

[Fact]
public void MergeOrderUsesRightDeclarationsThenLeftOnly()
{
    InterfacePresentation result = CompareSectionNames(
        left: new[] { "A", "B", "D" }, right: new[] { "C", "A" });
    Assert.Equal(new[] { "C", "A", "B", "D" },
        Assert.Single(result.Sections).Members.Select(m => (m.Right ?? m.Left)!.Name));
}
```

Add exact tests for multilingual add/remove/change, empty-section add/remove, section move as old Removed + new Added, volatile metadata ignored, right/left independent snapshots, whitelisted typed values, pure reorder, and unchanged byte-different interfaces.

- [ ] **Step 6: Implement deterministic recursive comparison GREEN (2–5 min)**

Compare sections independently so a move cannot become a modification. At each sibling level, match by normalized ordinal `Path`; enumerate right-side members in declaration order, then unmatched left members in left order. Recurse only under the matched/one-sided parent and never flatten descendants into section roots. A matched member is Modified when any field change or descendant change exists; otherwise Unchanged. A section is Modified when any member changes, Added/Removed when presence differs, otherwise Unchanged. Preserve both snapshots on matched nodes and exactly one snapshot on added/removed nodes.

Run Step 2. Expected: PASS for AC-034 through AC-043.

- [ ] **Step 7: Integrate interface comparison without mutating LAD semantics (2–5 min)**

Change `SimaticMlComparer` so its legacy network/content comparison code is byte-for-byte behaviorally unchanged, while interface output delegates to the new builder/comparer. Add:

```csharp
public sealed class LadPresentation : LogicNetworkPresentation
{
    private readonly SactCompareResult legacySnapshot;
    public LadPresentation(SactCompareResult legacyResult, InterfacePresentation interfacePresentation)
    {
        legacySnapshot = SactCompareResultCloner.Clone(legacyResult ?? throw new ArgumentNullException(nameof(legacyResult)));
        Interface = interfacePresentation ?? throw new ArgumentNullException(nameof(interfacePresentation));
    }
    public InterfacePresentation Interface { get; }
    public SactCompareResult CreateLegacyResult() => SactCompareResultCloner.Clone(legacySnapshot);
}
```

`SactCompareResultCloner` must recursively copy `SactCompareResult`, `SactInterfaceResult`, every `SactInterfaceMemberComparison`, `SactContentResult`, `SactNetworkResult`, `SactNumberPair`, component/connector/operand/parameter objects, lists, dictionaries, and primitive/string attribute values. Unsupported mutable object values are converted to invariant strings before storage. Add a test that mutates two values returned by `CreateLegacyResult()` and proves the presentation and a third returned copy are unchanged.

Mark `SactInterfaceSections` obsolete and forward its `Order` to the new canonical order including `Static`; active comparison code uses the neutral interface name. Do not remove the legacy type until existing callers migrate.

- [ ] **Step 8: Run interface plus full LAD regression GREEN (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~InterfaceComparerTests|FullyQualifiedName~SimaticMlComparerTests|FullyQualifiedName~LadVisualGraphBuilderTests|FullyQualifiedName~LadLayout"
```

Expected: PASS; existing LAD states/components/wires/layout are unchanged except the approved envelope/interface metadata.

- [ ] **Step 9: Commit deep interface comparison (2–5 min)**

```powershell
git diff --check
git add src/TiaGitAddIn.Core/Models/Comparison src/TiaGitAddIn.Core/Services/Comparison/InterfaceSnapshotBuilder.cs src/TiaGitAddIn.Core/Services/Comparison/InterfaceComparer.cs src/TiaGitAddIn.Core/Services/Comparison/SactCompareResultCloner.cs src/TiaGitAddIn.Core/Services/SimaticMl src/TiaGitAddIn.Core/Models/Sact/SactInterfaceSections.cs src/TiaGitAddIn.Tests/Services/SimaticMl
git commit -m "feat: compare complete PLC interfaces"
```

Expected: immutable interface semantics committed separately from WPF presentation.

---

### Task 9: Map Typed Results to Focused WPF Views

**Acceptance criteria:** AC-022, AC-023, AC-028, AC-029, AC-030, AC-031, AC-032, AC-040, AC-041, AC-042.

**Files:**
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonPresentationViewModel.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonViewModelMetadata.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonDiagnosticViewModel.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonRawTextViewModel.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/InterfaceComparisonViewModel.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/TextComparisonViewModel.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/UnsupportedComparisonViewModel.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/ErrorComparisonViewModel.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/LadComparisonViewModel.cs`
- Create: `src/TiaGitAddIn/UI/Mapping/IComparisonPresentationMapper.cs`
- Create: `src/TiaGitAddIn/UI/Mapping/IComparisonPresentationViewModelFactory.cs`
- Create: `src/TiaGitAddIn/UI/Mapping/ComparisonPresentationMapper.cs`
- Create: foundation factory files listed in the file responsibility map plus `LadPresentationViewModelFactory.cs`
- Create: `src/TiaGitAddIn/UI/Views/Comparison/ComparisonPresentationHost.xaml` and `.xaml.cs`
- Create: `src/TiaGitAddIn/UI/Views/Comparison/ComparisonTemplates.xaml`
- Create: `src/TiaGitAddIn/UI/Views/Comparison/InterfaceDiffView.xaml` and `.xaml.cs`
- Create: `src/TiaGitAddIn/UI/Views/Comparison/TextDiffView.xaml` and `.xaml.cs`
- Modify: `src/TiaGitAddIn/UI/Views/LadDiffView.xaml`
- Modify: `src/TiaGitAddIn/UI/ViewModels/LadDiffViewModel.cs`
- Modify: `src/TiaGitAddIn/UI/ViewModels/LadInterfaceRowViewModel.cs`
- Create: `src/TiaGitAddIn.Tests/UI/WpfTestHost.cs`
- Create: `src/TiaGitAddIn.Tests/UI/ComparisonPresentationMapperTests.cs`
- Create: `src/TiaGitAddIn.Tests/UI/ComparisonViewSmokeTests.cs`

**Interfaces:**
- Consumes: all foundation presentations, diagnostics, and raw text from Tasks 2/8.
- Produces: exact aggregate/specialized mapper seam locked above, reusable STA test host, and presentation host consumed by Task 10 and later FBD/SCL factories/templates.

- [ ] **Step 1: Write the RED aggregate-mapper test (2–5 min)**

```csharp
[Theory]
[MemberData(nameof(FoundationResults))]
public void MapCreatesOneCompatibleViewModelWithSharedMetadata(
    PlcComparisonResult result, Type expectedType, string expectedHeader)
{
    IComparisonPresentationMapper mapper = CreateMapper();
    ComparisonPresentationViewModel viewModel = mapper.Map(result);
    Assert.IsType(expectedType, viewModel);
    Assert.Equal(expectedHeader, viewModel.Header);
    Assert.Equal(result.Limitation, viewModel.Limitation);
    Assert.Equal(result.RawText != null, viewModel.HasRawText);
}

[Fact]
public void MapRejectsZeroOrMultipleFactories()
{
    PlcComparisonResult result = TextFallbackResult();
    Assert.Throws<InvalidOperationException>(() =>
        new ComparisonPresentationMapper(Array.Empty<IComparisonPresentationViewModelFactory>()).Map(result));
    Assert.Throws<InvalidOperationException>(() =>
        new ComparisonPresentationMapper(new IComparisonPresentationViewModelFactory[]
        { new TextPresentationViewModelFactory(), new TextPresentationViewModelFactory() }).Map(result));
}
```

- [ ] **Step 2: Run mapper tests RED (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ComparisonPresentationMapperTests"
```

Expected: FAIL with missing mapping/ViewModel types.

- [ ] **Step 3: Implement shared metadata and factory aggregation (2–5 min)**

```csharp
public sealed class ComparisonViewModelMetadata
{
    private ComparisonViewModelMetadata(PlcComparisonResult result)
    {
        ModeLabel = result.ActualMode.ToString(); SupportLabel = result.SupportLevel.ToString();
        Header = $"{ModeLabel} · {SupportLabel}"; Limitation = result.Limitation;
        Diagnostics = result.Diagnostics.Select(d => new ComparisonDiagnosticViewModel(d)).ToArray();
        RawText = result.RawText == null ? null : new ComparisonRawTextViewModel(result.RawText);
    }
    public string ModeLabel { get; }
    public string SupportLabel { get; }
    public string Header { get; }
    public string Limitation { get; }
    public IReadOnlyList<ComparisonDiagnosticViewModel> Diagnostics { get; }
    public ComparisonRawTextViewModel? RawText { get; }
    public bool HasLimitation => !string.IsNullOrWhiteSpace(Limitation);
    public bool HasRawText => RawText != null;
    public static ComparisonViewModelMetadata From(PlcComparisonResult result) =>
        new ComparisonViewModelMetadata(result ?? throw new ArgumentNullException(nameof(result)));
}

public abstract class ComparisonPresentationViewModel
{
    protected ComparisonPresentationViewModel(ComparisonPresentationKind kind, ComparisonViewModelMetadata metadata)
    { Kind = kind; Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata)); }
    public ComparisonPresentationKind Kind { get; }
    public ComparisonViewModelMetadata Metadata { get; }
    public string Header => Metadata.Header;
    public string Limitation => Metadata.Limitation;
    public bool HasLimitation => Metadata.HasLimitation;
    public bool HasRawText => Metadata.HasRawText;
}
```

`ComparisonDiagnosticViewModel(PlcComparisonDiagnostic)` exposes Code/Severity/Message and formats location only as `Left|Right`, optional `line N`, optional `column N`. `ComparisonRawTextViewModel(ComparisonRawText)` exposes left/right text and missing flags. `ComparisonPresentationMapper` copies factories in its constructor, computes `ComparisonViewModelMetadata.From(result)` exactly once, requires exactly one `CanMap`, and calls it. Specialized factories match concrete presentation types, not artifact paths/kinds.

- [ ] **Step 4: Implement independent interface rows and LAD adapter GREEN (2–5 min)**

`InterfaceComparisonViewModel` recursively maps every `InterfaceSectionComparison`/`InterfaceMemberComparison`. Each row exposes `Left` and `Right` snapshots separately, `StatusLabel` (`Unchanged`, `Added`, `Removed`, `Modified`), field changes, and children; never use `right ?? left` for datatype/default display. `LadPresentationViewModelFactory` calls `LadPresentation.CreateLegacyResult()` once, constructs a result-only `LadDiffViewModel`, and maps its `Interface` through the interface VM. Add this constructor:

```csharp
public LadDiffViewModel(SactCompareResult result, InterfaceComparisonViewModel interfaceComparison,
    IAddInLogger logger, IUiDispatcher? uiDispatcher)
    : base(uiDispatcher)
{
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    Networks = new ObservableCollection<LadNetworkPairViewModel>(
        LadLayoutEngine.LayoutAll(result).Select(layout => new LadNetworkPairViewModel(layout)));
    InterfaceComparison = interfaceComparison ?? throw new ArgumentNullException(nameof(interfaceComparison));
    IsLadDiffLoaded = true;
}
```

Wrap that result-only VM in the typed mapper output:

```csharp
public sealed class LadComparisonViewModel : ComparisonPresentationViewModel
{
    public LadComparisonViewModel(LadDiffViewModel content, ComparisonViewModelMetadata metadata)
        : base(ComparisonPresentationKind.LogicNetwork, metadata)
    { Content = content ?? throw new ArgumentNullException(nameof(content)); }
    public LadDiffViewModel Content { get; }
}
```

The `LadComparisonViewModel` DataTemplate creates `LadDiffView` with `DataContext="{Binding Content}"`.

Remove `ISactService`, `IGitFileExtractor`, `LoadLadDiffAsync`, `IsSactAvailable`, temp-file deletion, and SACT installation copy from the active VM constructor/path. Replace the old right-first interface table in `LadDiffView.xaml` with `<comparison:InterfaceDiffView DataContext="{Binding InterfaceComparison}"/>`.

- [ ] **Step 5: Add the exact STA test host (2–5 min)**

```csharp
internal static class WpfTestHost
{
    public static void Run(Action<Dispatcher> action) =>
        RunAsync(dispatcher => { action(dispatcher); return Task.CompletedTask; }).GetAwaiter().GetResult();

    public static Task RunAsync(Func<Dispatcher, Task> action)
    {
        var completion = new TaskCompletionSource<object?>();
        var thread = new Thread(() =>
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
            dispatcher.BeginInvoke(new Action(async () =>
            {
                try { await action(dispatcher); completion.TrySetResult(null); }
                catch (Exception ex) { completion.TrySetException(ex); }
                finally { dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
            }));
            Dispatcher.Run();
        });
        thread.IsBackground = true;
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return completion.Task;
    }
}
```

- [ ] **Step 6: Write RED template/runtime smoke tests (2–5 min)**

For interface, LAD, text, unsupported, and error foundation VMs, load `ComparisonTemplates.xaml`, find `new DataTemplateKey(viewModel.GetType())`, instantiate the template content, assign DataContext, call `Measure`, `Arrange`, and `UpdateLayout`, and assert the expected dedicated view type. Assert Full hides the limitation panel while Partial/Fallback/Unsupported show it. Capture `PresentationTraceSources.DataBindingSource` errors and assert none. FBD/SCL plans append their concrete VMs/templates to the same theory; final AC-028 runs all six presentation kinds.

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ComparisonViewSmokeTests"
```

Expected RED: templates/views are absent.

- [ ] **Step 7: Build focused views/templates and run GREEN (2–5 min)**

`ComparisonPresentationHost` contains: header text; a limitation panel bound to `HasLimitation`; expandable diagnostics; semantic `ContentControl`; and a selectable raw-text tab bound to `HasRawText`. Merge `ComparisonTemplates.xaml` in the host resources. Add DataTemplates keyed by concrete VM type for `LadDiffView`, `InterfaceDiffView`, `TextDiffView`, unsupported, and error controls. Use a virtualizing `TreeView`/`DataGrid` for interface/text collections. Every added/removed/modified state includes a visible word or icon in addition to color.

Run Steps 2 and 6 commands. Expected: PASS. Then run:

```powershell
$files = Get-ChildItem src/TiaGitAddIn/UI/Views/Comparison,src/TiaGitAddIn/UI/ViewModels/Comparison,src/TiaGitAddIn/UI/Mapping -Recurse -File
$files | Where-Object { (Get-Content -LiteralPath $_.FullName).Count -gt 800 } | Select-Object -ExpandProperty FullName
```

Expected: no output.

- [ ] **Step 8: Commit the WPF presentation seam (2–5 min)**

```powershell
git diff --check
git add src/TiaGitAddIn/UI/ViewModels/Comparison src/TiaGitAddIn/UI/Mapping src/TiaGitAddIn/UI/Views/Comparison src/TiaGitAddIn/UI/ViewModels/LadDiffViewModel.cs src/TiaGitAddIn/UI/ViewModels/LadInterfaceRowViewModel.cs src/TiaGitAddIn/UI/Views/LadDiffView.xaml src/TiaGitAddIn.Tests/UI
git commit -m "feat: present typed PLC comparison results"
```

Expected: one WPF commit with no artifact classifier in a ViewModel.

---

### Task 10: Apply Only the Latest Selection, Wire Production Composition, and Retire Active SACT Extraction

**Acceptance criteria:** AC-006, AC-008, AC-021, AC-024, AC-025, AC-026, AC-027, AC-099, AC-104, AC-105, AC-111, AC-114, AC-118.

**Files:**
- Create: `src/TiaGitAddIn.Core/Services/Comparison/LadComparisonStrategy.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonSelection.cs`
- Create: `src/TiaGitAddIn/UI/ViewModels/Comparison/ComparisonSelectionCoordinator.cs`
- Modify: `src/TiaGitAddIn/UI/ViewModels/DiffViewModel.cs`
- Modify: `src/TiaGitAddIn/UI/Views/DiffView.xaml`
- Modify: `src/TiaGitAddIn/UI/Views/DiffView.xaml.cs`
- Modify: `src/TiaGitAddIn/UI/ViewModels/MainViewModel.cs`
- Modify: `src/TiaGitAddIn/UI/GitPanelLaunchService.cs`
- Delete after zero-caller proof: `src/TiaGitAddIn.Core/Services/IGitFileExtractor.cs`, `src/TiaGitAddIn.Core/Services/GitFileExtractor.cs`, `src/TiaGitAddIn.Core/Services/ISactService.cs`, `src/TiaGitAddIn.Core/Services/SactService.cs`
- Modify/delete their superseded unit tests only after replacement coverage passes
- Create: `src/TiaGitAddIn.Tests/UI/ComparisonSelectionCoordinatorTests.cs`
- Modify: `src/TiaGitAddIn.Tests/UI/DiffViewModelTests.cs`
- Modify: `src/TiaGitAddIn.Tests/Services/GitPanelLaunchServiceTests.cs`

**Interfaces:**
- Consumes: revision provider/leases, coordinator, LAD/interface presentation, WPF mapper, and existing `IUiDispatcher.Invoke(Action)`.
- Produces: latest-wins Task-returning selection flow and production composition; FBD/SCL later add only strategies/factories to these collections.

- [ ] **Step 1: Write RED latest-selection/cancellation tests (2–5 min)**

```csharp
[Fact]
public async Task NewerSelectionIsTheOnlyResultApplied()
{
    var provider = new ControllableRevisionProvider();
    var applied = new List<string>();
    var sut = CreateSelectionCoordinator(provider, vm => applied.Add(vm.Metadata.RawText!.RightText!));
    Task first = sut.SelectAsync(new ComparisonSelection("A.xml", null, PlcPairChangeKind.Modified), CancellationToken.None);
    Task second = sut.SelectAsync(new ComparisonSelection("B.xml", null, PlcPairChangeKind.Modified), CancellationToken.None);
    provider.Complete("B.xml", "B");
    await second;
    provider.CompleteIgnoringCancellation("A.xml", "A");
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
    Assert.Equal(new[] { "B" }, applied);
}

[Fact]
public async Task StandaloneCancellationKeepsCurrentResultAndDisposesBothLeasesOnce()
{
    var provider = new ControllableRevisionProvider();
    var applied = new List<ComparisonPresentationViewModel> { ExistingViewModel };
    var sut = CreateSelectionCoordinator(provider, vm => { applied.Clear(); applied.Add(vm); });
    using var cts = new CancellationTokenSource();
    Task pending = sut.SelectAsync(new ComparisonSelection("C.xml", null, PlcPairChangeKind.Modified), cts.Token);
    provider.ReleaseLeases("C.xml");
    cts.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    Assert.Same(ExistingViewModel, Assert.Single(applied));
    Assert.All(provider.LeasesFor("C.xml"), lease => Assert.Equal(1, lease.DisposeCountForTests));
    Assert.Empty(sut.AppliedErrorsForTests);
}
```

- [ ] **Step 2: Run selection tests RED (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ComparisonSelectionCoordinatorTests"
```

Expected: FAIL because the selection coordinator is missing and the current `DiffViewModel.UpdateLadDiff` is `async void`.

- [ ] **Step 3: Implement exact selection/source mapping (2–5 min)**

```csharp
public sealed class ComparisonSelection
{
    public ComparisonSelection(string repositoryRelativePath, string? commitHash, PlcPairChangeKind changeKind)
    { RepositoryRelativePath = repositoryRelativePath; CommitHash = commitHash; ChangeKind = changeKind; }
    public string RepositoryRelativePath { get; }
    public string? CommitHash { get; }
    public PlcPairChangeKind ChangeKind { get; }
}

public sealed class ComparisonSelectionCoordinator : IDisposable
{
    public Task SelectAsync(ComparisonSelection selection, CancellationToken cancellationToken);
}
```

Constructor dependencies are `IPlcRevisionProvider`, `IPlcComparisonCoordinator`, `IComparisonPresentationMapper`, `IUiDispatcher`, `Action<ComparisonPresentationViewModel> apply`, and `IAddInLogger`. For working tree use left=`HEAD`, right=`WorkingTree`; for a commit use left=`ParentOfCommit(validatedHash)`, right=`Commit(validatedHash)`. Added creates a missing left lease; Removed creates a missing right lease. Increment a `long` generation with `Interlocked.Increment`, cancel/dispose the previous linked CTS, load both sides, compare, map, then inside `dispatcher.Invoke` recheck generation/token before calling `apply`.

- [ ] **Step 4: Implement disposal and error boundaries GREEN (2–5 min)**

Hold both leases in local variables and dispose right then left in `finally` for success, failure, replacement, and standalone cancellation. Explicitly rethrow `OperationCanceledException` and apply no error. Convert revision load/size failures with `IPlcComparisonCoordinator.CreateRevisionLoadError`; map/apply only if the generation is still current. Log cleanup exceptions through `IAddInLogger` using diagnostic code/lease ID only. Rerun Step 2; expected PASS and dispatcher test proves the apply callback runs on the captured dispatcher thread.

- [ ] **Step 5: Add the native LAD strategy with structured fallback (2–5 min)**

`LadComparisonStrategy.SupportedKinds` is exactly `{ PlcArtifactKind.Lad }`. Parse each present side with `SimaticMlParser.ParseText`; malformed XML throws `RecoverableComparisonException` so coordinator returns text fallback. When trustworthy blocks/interfaces parse but LAD network structure is unsupported, return `CreateSemantic(context, Structured, Partial, "LAD network structure is only partially supported; showing trusted block and interface structure.", diagnostics, interfacePresentation)`. Otherwise call the unchanged legacy network comparer and return `CreateSemantic(context, Visual, Full, string.Empty, diagnostics, new LadPresentation(legacyResult, interfacePresentation))`. Always retain raw text through factory behavior and rethrow cancellation.

- [ ] **Step 6: Replace the `async void` ViewModel path with a genuine WPF boundary (2–5 min)**

`DiffViewModel.SelectedEntry` only stores/raises state. Add `Task SelectEntryAsync(DiffEntryViewModel? entry, CancellationToken)` that calls selection coordinator and updates no properties outside its dispatcher callback. Remove `UpdateLadDiff`, `ShowVisualDiff`, and path/language heuristics. Expose `ComparisonPresentationViewModel? CurrentPresentation` and bind `DiffView.xaml` to `ComparisonPresentationHost`.

In `DiffView.xaml.cs`, the only new `async void` is the genuine event handler:

```csharp
private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (DataContext is DiffViewModel viewModel)
    {
        try { await viewModel.SelectEntryAsync(viewModel.SelectedEntry, CancellationToken.None); }
        catch (OperationCanceledException) { }
    }
}
```

Wire it from the file list's `SelectionChanged`. The cancellation catch is at a WPF boundary and intentionally applies no state; all other async comparison methods return `Task` and accept `CancellationToken`.

Add `AsyncVoidIsConfinedToWpfEventBoundaries` to `DiffViewModelTests.cs`. It scans production `.cs` files for `async void`; for every match it reads the containing method declaration and asserts either `(object sender, ...EventArgs e)`/WPF routed-event arguments or a framework override with an event callback signature. It explicitly asserts `DiffViewModel.cs`, `MainViewModel.cs`, selection coordinator, revision provider, comparison coordinator, mapper, and strategies contain no `async void`, and asserts every comparison/loading method declaration contains both `Task` and `CancellationToken`. Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~AsyncVoidIsConfinedToWpfEventBoundaries"
```

Expected: PASS with the single new `DiffView.OnSelectionChanged(object, SelectionChangedEventArgs)` boundary accepted and every non-event comparison method Task-returning.

- [ ] **Step 7: Wire the production composition root (2–5 min)**

In `GitPanelLaunchService`, construct one Siemens `GitProcessRunner`, pass it as both text and binary adapter to `GitBlobReader`, then construct `PlcRevisionProvider`, classifier, `LineTextComparer(Default)`, result factory, sanitizer, `LadComparisonStrategy`, `TextFallbackStrategy`, coordinator, all foundation WPF factories, aggregate mapper, selection coordinator, and ViewModels. Log only `GitProcessRunner/SiemensAddIn` as adapter ID. Do not make a test/SystemGitProcessRunner reachable in production. FBD/SCL integration appends their strategy/factory instances here without changing coordinator/mapper code.

- [ ] **Step 8: Remove obsolete active services only after zero-caller proof (2–5 min)**

Run:

```powershell
rg -n "IGitFileExtractor|GitFileExtractor|ISactService|SactService|SACT not installed|Automation Compare Tool" src/TiaGitAddIn src/TiaGitAddIn.Core
```

Expected before deletion: declarations/tests only, no composition/ViewModel caller. Delete the four obsolete service files and migrate/remove their superseded tests. Run the same scan again; expected no active service/user-copy match. Legacy `Models/Sact` DTO names may remain only behind `LadPresentation.CreateLegacyResult`; they are not an installed runtime dependency.

- [ ] **Step 9: Run selection, composition, and LAD regressions GREEN (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ComparisonSelectionCoordinatorTests|FullyQualifiedName~DiffViewModelTests|FullyQualifiedName~GitPanelLaunchServiceTests|FullyQualifiedName~SimaticMlComparerTests|FullyQualifiedName~Lad"
```

Expected: PASS; latest wins, cancellation applies nothing, leases dispose once, production uses Siemens adapter, obsolete user guidance is absent, and LAD regression output is stable.

- [ ] **Step 10: Commit the end-to-end foundation flow (2–5 min)**

```powershell
git diff --check
git add -A src/TiaGitAddIn.Core/Services src/TiaGitAddIn/UI/GitPanelLaunchService.cs src/TiaGitAddIn/UI src/TiaGitAddIn.Tests
git commit -m "feat: wire cancellation-safe PLC comparison"
```

Expected: active comparison loads raw revisions and routes through one coordinator/mapper.

---

### Task 11: Run the Full Gate, Security Audit, Coverage Threshold, and Graph Refresh

**Acceptance criteria:** AC-002, AC-003, AC-004, AC-006, AC-007, AC-008, AC-025, AC-026, AC-028, AC-031, AC-032, AC-090, AC-091, AC-092, AC-093, AC-094, AC-095, AC-096, AC-097, AC-098, AC-099, AC-103, AC-104, AC-105, AC-106, AC-109, AC-111, AC-117, AC-118.

**Files:**
- Verify: all files changed by Tasks 1–10
- Consume only: `scripts/Invoke-TestGate.ps1`, `.github/workflows/*`, and coverage project settings owned by the VCI plan
- Modify only if evidence text is stale: `docs/tia-v21-compare-api-investigation.md`, `docs/PRD.md`, `README.md`
- Generated refresh: `graphify-out/*` through `graphify update .`

**Interfaces:**
- Consumes: completed foundation plus completed VCI gate; after FBD/SCL integration, their registered strategies/factories and tests.
- Produces: releasable evidence that exact focused/full commands, 80% merged coverage, security boundaries, and graph revision pass.

- [ ] **Step 1: Run a pre-gate RED sensitivity control (2–5 min)**

```powershell
$control = 'Siemens.Automation.CommonServices.Compare'
if ($control -match 'Siemens\.Automation\.CommonServices\.Compare|CompareEditorStarter|PlcSoftware\.CompareTo|CompareToOnline') { throw 'RED: forbidden internal/live comparison reference detected.' }
```

Expected: FAIL with `RED: forbidden internal/live comparison reference detected.` This proves the final static contract rejects a known forbidden control before the real source scan is accepted; do not weaken the expression to make the control pass.

- [ ] **Step 2: Run the exact focused net48 suite (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ComparisonBoundaryTests|FullyQualifiedName~PlcComparisonContractTests|FullyQualifiedName~PlcRevisionProviderTests|FullyQualifiedName~PlcArtifactClassifierTests|FullyQualifiedName~PlcComparisonCoordinatorTests|FullyQualifiedName~SimaticMlParserSecurityTests|FullyQualifiedName~InterfaceComparerTests|FullyQualifiedName~ComparisonPresentationMapperTests|FullyQualifiedName~ComparisonSelectionCoordinatorTests|FullyQualifiedName~ComparisonViewSmokeTests"
```

Expected: PASS with zero failed tests.

- [ ] **Step 3: Run the exact focused net8 boundary suite (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~ComparisonProjectBoundaryTests"
```

Expected: PASS with zero failed tests.

- [ ] **Step 4: Run the full Release build (2–5 min)**

```powershell
dotnet build TiaGitAddIn.sln -c Release -p:EnableTiaAddInPackaging=false
```

Expected: build PASS.

- [ ] **Step 5: Run the full unfiltered net48 test project (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false
```

Expected: PASS with zero failed tests.

- [ ] **Step 6: Run the full unfiltered net8 test project (2–5 min)**

```powershell
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release
```

Expected: PASS with zero failed tests. The command is unfiltered, so Category `GitE2E` remains included.

- [ ] **Step 7: Run the authoritative merged 80% gate (2–5 min)**

```powershell
pwsh -NoProfile -File scripts/Invoke-TestGate.ps1
```

Expected GREEN: exit 0; the VCI-owned script runs net48 with AppDomain denied, merges its JSON into the net8 run, enforces `Threshold=80`, `ThresholdType=line`, `ThresholdStat=total`, and writes `TestResults/Coverage/coverage.json` plus `TestResults/Coverage/coverage.cobertura.xml`. Confirm the report includes `TiaGitAddIn.Core` and designated testable `TiaGitAddIn` production classes and excludes only generated `g.cs/g.i.cs` or generated/compiler attributes. If below 80%, add behavior-focused tests to the lowest-covered foundation classes; do not lower threshold or add broad exclusions.

- [ ] **Step 8: Run security/internal/async/file-size scans (2–5 min)**

```powershell
rg -n -i "Siemens\.Automation\.CommonServices\.Compare|CompareEditorStarter|PlcSoftware\.CompareTo|CompareToOnline|SACT not installed|Automation Compare Tool" src
rg -n "async void" src/TiaGitAddIn --glob "*.cs"
rg -n -i "(api[_-]?key|password|secret|token)\s*[:=]\s*['\"][^'\"]+" src docs --glob "!docs/superpowers/plans/*.md"
$touched = git diff --name-only HEAD~10..HEAD | Where-Object { $_ -match '\.(cs|xaml)$' }
$touched | Where-Object { (Get-Content -LiteralPath $_).Count -gt 800 }
```

Expected: forbidden/secret/file-size scans have no production hit; every `async void` hit is a WPF event override/handler with sender/event args. Review any match manually rather than suppressing the scan.

- [ ] **Step 9: Verify project references and package candidate (2–5 min)**

```powershell
dotnet list src/TiaGitAddIn.Core/TiaGitAddIn.Core.csproj reference
dotnet list src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj reference
dotnet build src/TiaGitAddIn/TiaGitAddIn.csproj -c Release
```

Expected: Core has no Siemens/WPF project dependency, integration references only Core, and the V21 `.addin` package build succeeds with documented permissions.

- [ ] **Step 10: Refresh and verify graph provenance (2–5 min)**

```powershell
graphify update .
git rev-parse HEAD
rg -n "Source revision|Commit|HEAD" graphify-out/GRAPH_REPORT.md
```

Expected: graph update exits 0 and the report identifies the current source revision. Inspect new god nodes/community boundaries for accidental coupling from Core to WPF/Siemens.

- [ ] **Step 11: Commit only necessary verified evidence refresh (2–5 min)**

```powershell
git status --short
git diff --check
git diff --stat
git diff
```

If graph/evidence files changed and accurately describe the verified implementation:

```powershell
git add graphify-out docs/tia-v21-compare-api-investigation.md docs/PRD.md README.md
git commit -m "docs: record comparison foundation verification"
```

Expected: clean worktree after the commit. Do not commit `TestResults`, temp revisions, credentials, live private paths, or an unverified evidence claim.

## Acceptance Traceability

| Requirement group | Implemented by | Primary verification |
| --- | --- | --- |
| V21 public API/project/permission boundary (AC-001–AC-006, AC-106, AC-111) | Tasks 1, 3, 10 | Boundary tests, composition tests, forbidden-reference scan. |
| Immutable result/routing/fallback invariant (AC-007–AC-008, AC-015–AC-024, AC-117) | Tasks 2, 4, 5, 6 | Contract/coordinator matrix tests. |
| Raw revision bytes/encoding/missing/limits/security/lifetime (AC-009–AC-013, AC-021, AC-097, AC-099, AC-118) | Tasks 3, 6, 10 | Provider/lease/selection tests. |
| Latest selection/dispatcher/async WPF boundaries (AC-025–AC-027) | Task 10 | Controllable-task and STA dispatcher tests. |
| WPF mode/support/raw/diagnostics/templates/layout (AC-022–AC-023, AC-028–AC-032) | Task 9 plus FBD/SCL template additions | Mapper and STA smoke suites; line-count scan. |
| Deep interface semantics (AC-034–AC-043) | Tasks 7–9 | Interface normalization/hierarchy/order tests and WPF rows. |
| Secure bounded XML parsing (AC-018, AC-095–AC-096, AC-113–AC-114) | Tasks 6–8, 10 | DTD/entity/limit/malformed/structured-fallback tests. |
| Obsolete terminology and LAD regression (AC-104–AC-105) | Tasks 8–10 | Zero-caller/text scan and complete LAD regression suite. |
| Coverage/release repository state (AC-090–AC-094, AC-103, AC-109) | VCI prerequisite and Task 11 | `scripts/Invoke-TestGate.ps1`, package build, graph refresh, final scans. |

## Dependency Handoff

After Task 2, give FBD/SCL implementers the locked contracts block. After Task 7, give FBD the safe `ParseText` seam. After Task 9, FBD/SCL add only their `IComparisonPresentationViewModelFactory` implementations and DataTemplates. Preparatory branches may advance from those handoffs, but integration still follows VCI Task 1 → foundation Tasks 1–10 → FBD → SCL rebased over FBD → VCI Tasks 2–4 → foundation Task 11 → VCI Tasks 5–8. The SCL rebase must retain the FBD strategy, mapper, and template before it appends SCL entries; VCI shared-file edits then retain both. Task 11's all-kind template smoke and merged gate are the comparison integration acceptance point.
