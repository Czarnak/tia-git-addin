# PLC Diff and VCI Git Workflow Design

**Date:** 2026-07-15

**Status:** Approved

**Target:** TIA Portal V21 Add-In, .NET Framework 4.8 WPF shell, .NET Standard 2.0 core

## Scope

This design covers the implementation of:

1. explicit fallback for unsupported or partially supported PLC artifacts;
2. deep interface comparison;
3. FBD visual comparison;
4. structured SCL comparison;
5. a CI-safe real-Git workflow integration test beginning with the resulting VCI
   workspace file change;
6. a separate live-TIA V21 acceptance lane.

The V21 comparison API investigation is complete. Its result is recorded in
[TIA Portal V21 Compare API Investigation](../tia-v21-compare-api-investigation.md).

## Approved Scope Decisions

During the 2026-07-15 design review, the user explicitly approved splitting the
original end-to-end criterion:

- the automated lane begins with the resulting VCI workspace file change and tests
  the real Git workflow;
- the named live-TIA end-to-end acceptance procedure performs the actual VCI
  project-to-workspace synchronization before exercising the packaged Add-In.

Neither lane is represented as proving the other's runtime boundary.

## Decided API Boundary

TIA Portal V21 public comparison APIs operate on live TIA engineering objects and
return comparison data. They do not accept Git revisions or serialized VCI
workspace artifacts and do not expose Siemens' graphical LAD/FBD comparison
editor.

Git revision review therefore continues to use the project-owned SimaticML
comparison pipeline and custom renderers. Text or structured comparison remains
the explicit fallback. Production code must not reference or invoke internal
Siemens.Automation.CommonServices.Compare types.

## Goals

- Select a comparison mode from artifact semantics rather than path labels alone.
- Give every artifact an explicit support level, mode, limitation, and diagnostics.
- Preserve independent left and right values throughout parsing, comparison, and UI.
- Compare FBD graph semantics without relying on unstable SimaticML UId values.
- Compare SCL structure while retaining raw text and comments.
- Produce deterministic results under harmless XML or source reordering.
- Verify the Git workflow with real local Git and no external services or credentials.
- Keep actual VCI export/synchronization validation in an isolated live-TIA lane.
- Meet the repository's 80% line-coverage requirement for testable production code.

## Non-goals

- Reproducing Siemens' private graphical comparison editor.
- Loading Git revisions into a second TIA project to use PlcSoftware.CompareTo.
- Referencing internal or reflection-discovered Siemens comparison APIs.
- Implementing a complete Siemens SCL compiler.
- Treating the CI-safe Git E2E test as proof that VCI export itself occurred.
- Automating live TIA Portal inside ordinary CI.

## Project Boundaries

### TiaGitAddIn.Core

The core remains independent of Siemens assemblies and owns:

- bounded working-tree and committed-revision loading;
- byte-oriented Git blob access, encoding detection, and temporary-file lifetime;
- artifact and pair classification;
- immutable comparison contracts;
- comparison strategy selection and fallback policy;
- SimaticML parsing and semantic comparison;
- neutral logic-network models;
- tolerant SCL lexing, parsing, and comparison;
- text comparison;
- Git workflow operations.

### TiaGitAddIn

The .NET Framework WPF Add-In owns:

- TIA Portal and VCI integration;
- requesting and disposing revision leases from the core provider;
- selection cancellation and latest-result coordination;
- mapping domain results to ViewModels;
- visual, structured, and text views;
- user-facing limitations and diagnostics.

### TiaGitAddIn.IntegrationTests

A new .NET 8 test project owns the CI-safe real-Git workflow integration test. It
references the core project but not the WPF Add-In or Siemens assemblies.

### Live-TIA Acceptance

An opt-in runbook validates actual ExportObject or project-to-workspace
Synchronize behavior with the packaged Add-In in TIA Portal V21.

## Immutable Comparison Contract

The shared domain contract contains the following concepts:

| Concept | Responsibility |
|---|---|
| PlcArtifactKind | LAD, FBD, SCL, STL, SFC, generic XML, text, binary, or unknown |
| PlcComparisonMode | Visual, structured, text, or unsupported |
| PlcSupportLevel | Full, partial, fallback, or unsupported |
| PlcRevision | Side, source revision, original path/suffix, bounded bytes, encoding metadata, decoded text when safe, and missing/binary state |
| PlcRevisionLease | Owns immutable loaded revisions and deterministic temporary-resource cleanup |
| PlcArtifactDescriptor | Classification evidence and preferred capability |
| PlcComparisonDiagnostic | Stable code, severity, safe message, and optional source location |
| ComparisonPresentation | Abstract immutable presentation with sealed typed variants |
| PlcComparisonResult | Artifact kind, requested/actual mode, support, limitation, diagnostics, and presentation |
| IPlcArtifactClassifier | Classifies individual revisions and resolves a comparison pair |
| IPlcRevisionProvider | Loads working-tree or committed sides without losing original path, suffix, bytes, or missing-side meaning |
| IGitBlobReader | Obtains bounded committed blob bytes without shell decoding or a forced .xml suffix |
| IPlcComparisonStrategy | Declares supported kinds and returns a typed result |
| PlcComparisonCoordinator | Selects a strategy and enforces fallback and error policy |

Collections are copied at construction and exposed read-only. Comparers create new
result objects; they do not mutate parser models or previous results.

Typed presentation variants cover interface, logic network, SCL structure, text,
unsupported, and hard-error states. This avoids routing all PLC content through a
LAD-specific ViewModel.

## Comparison Flow

1. A user selects a changed file.
2. The core revision provider loads the requested left and right sides, retaining
   original paths, suffixes, bounded bytes, encoding evidence, and missing-side
   reasons for committed revisions.
3. Each available side is classified using suffix, normalized VCI path, and
   bounded content inspection.
4. Pair resolution chooses the common artifact kind and requested mode.
5. The coordinator invokes the registered semantic strategy.
6. A successful strategy returns an immutable typed result.
7. A recoverable parse or capability failure is converted to a partial or text
   result with an explicit limitation.
8. The WPF mapper selects the appropriate view and exposes the raw text diff as an
   alternative where text exists.

Classification rules:

- SCL source is recognized by source suffix and lexical content.
- XML is treated as PLC content only when namespace, block, programming-language,
  or known SimaticML structure provides evidence.
- A generic XML document receives text comparison rather than LAD rendering.
- An added or deleted file is classified from the available side.
- Conflicting side classifications use text fallback and explain the conflict.
- Binary content is reported as unsupported rather than decoded as text.

The provider replaces the current string-only, forced-.xml extraction behavior.
Text decoding accepts BOM-declared UTF-8/UTF-16 and strict UTF-8; decoding failure
marks the side binary or unsupported instead of replacing bytes. The production
Git blob implementation must preserve raw bytes, while tests exercise working-tree
and committed retrieval through the same provider contract.

Initial capability routing is explicit:

| Artifact | Preferred mode | Fallback |
|---|---|---|
| LAD | Visual | Structured SimaticML summary, then text |
| FBD | Visual | Structured graph summary, then text |
| SCL | Structured | Text |
| STL or SFC without a semantic parser | Text fallback | None |
| Generic XML or other recognized text | Text fallback | None |
| Malformed text-based PLC export | Text fallback | None |
| Binary content | Unsupported | None |

## Concurrency and UI State

The WPF selection coordinator owns a CancellationTokenSource and monotonically
increasing selection generation. Selecting another file cancels the prior load and
comparison. A result is applied only when its generation remains current.

Async void is limited to genuine WPF event boundaries. All comparison and loading
methods return Task and accept cancellation. Cancellation does not produce a user
error. UI-bound properties are updated on the dispatcher only after the immutable
domain result is complete.

## Fallback and Error Policy

| Condition | Outcome |
|---|---|
| Fully parsed supported artifact | Visual or structured result |
| Recoverable unknown elements | Partial result plus limitation |
| Unparseable text-based artifact | Text fallback plus parser diagnostic |
| Generic or unsupported text | Text fallback plus capability limitation |
| Conflicting side types | Text fallback plus classification diagnostic |
| Binary artifact | Unsupported result plus limitation |
| Revision cannot be loaded | Hard-error result |
| Operation cancelled | No result applied |

Fallback is never silent. The result header derives two distinct badges from the
contract, for example Visual · Full, Structured · Partial, Text · Fallback, or
Unsupported · Unsupported. Partial, fallback, and unsupported results show a short
limitation panel. Expandable details may include diagnostic codes and safe source
locations, but never stack traces, credential-bearing Git URLs, or temporary paths.

The obsolete SACT installation message and ISactService terminology are removed or
renamed to reflect the project-owned SimaticML implementation.

## Deep Interface Comparison

The parser already captures more metadata than the current comparer and UI expose.
The new comparison preserves a left and right snapshot for every section and
member.

Each member snapshot contains:

- section and nested semantic path;
- name and datatype;
- retain/remanence state;
- default or start value;
- multilingual comment map;
- accessibility, informative, and version values;
- additional normalized SimaticML attributes;
- recursively nested children.

The equality contract is explicit:

| UI field | SimaticML source | Normalization and equality |
|---|---|---|
| Section | Interface section element | Canonical TIA section identity; empty-section presence is significant |
| Name/path | Member names plus parent chain | Unicode Form C and exact ordinal comparison; used for identity |
| Datatype | Datatype attribute | Trim outer whitespace; otherwise exact text |
| Retain | Remanence/Retain attribute | Three-state true, false, or unspecified; exact state comparison |
| Default/start value | StartValue/default element | Normalize line endings and outer whitespace; preserve internal text |
| Comment | Multilingual comment elements | Map by language key; normalize line endings and trailing line whitespace; compare each language independently |
| Accessibility | Accessibility attribute | Normalize to the known enum value; preserve unknown text |
| Informative | Informative attribute | Three-state Boolean comparison |
| Version | Version attribute | Trim outer whitespace; exact comparison |
| Additional semantics | Explicit whitelisted attributes | Normalize by declared type; compare key/value pairs |

Volatile UId/composition identifiers, timestamps, export ordering metadata, and
document/export version fields are excluded from member equality. Any additional
semantic attribute must be added to the whitelist with a focused test.

Matching uses section plus normalized nested path, independent of XML ordinal.
Rendering preserves canonical TIA section order, then the right-side declaration
order, followed by left-only members in their left-side order. A pure reorder does
not become an addition or removal, and repeated comparisons remain deterministic.

The interface comparer reports field-level changes and section/member
additions/removals. The ViewModel retains both sides instead of selecting the
right-side datatype or default. A focused interface control is extracted from the
large LAD XAML view and shows hierarchical members with side-specific columns.
Unchanged interfaces produce no changed rows or section status while retaining the
complete side snapshots.

## FBD Semantic and Visual Comparison

FlgNet content is adapted to a neutral LogicNetworkGraph:

- graphs contain blocks and networks;
- nodes represent accesses, parts, calls, power rails, open branches, and generic
  unknown parts;
- pins retain connector names and directions where known;
- edges connect semantic node/pin endpoints;
- source identifiers are retained only as trace data, not matching identity.

In this scope the neutral matcher is enabled for FBD only. Existing LAD comparison
and layout behavior remains unchanged apart from shared routing, fallback, and
immutable result contracts. Replacing LAD matching with the neutral graph requires
a separate migration increment and full existing LAD parity tests.

Blocks and networks are matched by stable semantic keys and deterministic
fingerprints. Nodes are matched conservatively:

1. unique exact semantic signatures;
2. unique signatures augmented by connector and graph-neighbourhood context;
3. otherwise explicit added and removed nodes.

Ambiguous nodes are not guessed as modifications. Edge comparison operates on
mapped endpoints and connector names, allowing rewiring to be reported explicitly.
Changes containing only UId regeneration are ignored.

Unknown but structurally valid parts render as labelled generic nodes and make the
result partial. A network that cannot produce trustworthy structure falls back to
a structured raw summary or text.

FBD receives a dedicated FbdDiffViewModel and FbdDiffView. Layout is deterministic
and topology-based, with stable tie-breaking. Left and right views synchronize
navigation. Added, removed, modified, and rewired states use labels or icons as
well as colour. The raw-text view remains available.

Implementation proceeds in two test-driven increments: graph/parser/comparer, then
layout/renderer.

## Structured SCL Comparison

SCL uses a tolerant lexer and shallow structural parser. It recognizes:

- organization blocks, function blocks, functions, data blocks, and type blocks;
- declaration sections and individual declarations;
- REGION and END_REGION grouping;
- executable statements and block terminators;
- line comments, block comments, strings, and quoted identifiers;
- keywords, identifiers, literals, and operators for highlighting.

The lexer retains source spans and comments. The parser recovers at semicolons,
region boundaries, declaration terminators, and block terminators. Unparsed spans
remain attached to a partial result.

Comparison rules:

- blocks match by kind and identifier;
- declarations match by section and identifier;
- a rename is reported only for a unique removed/added pair whose remaining
  declaration fingerprint is equal;
- ambiguous rename candidates remain explicit removal and addition;
- statements compare normalized token sequences, ignoring formatting-only
  whitespace;
- comments are compared separately;
- formatting remains visible in the raw-text view.

A reliable partial tree produces a partial structured result. If block structure
cannot be established, the coordinator returns text fallback with diagnostics.
Syntax highlighting uses the same lexer tokens as comparison so displayed and
compared semantics cannot diverge.

The initial rename acceptance criterion applies to declaration identifiers.
Top-level block-name or region-label changes remain removal/addition unless a later
design adds a separately tested rule. SclPresentation is hierarchical: file,
block, region, declaration section or statement group, then individual change.
The WPF view must display block and region headers in source order and retain an
explicit ungrouped bucket for recoverable spans outside a region.

## WPF Presentation

The main diff ViewModel exposes one current comparison presentation. WPF
DataTemplates select dedicated views for interface, logic network, SCL, text,
unsupported, and error results.

The visual contract includes:

- a persistent comparison-mode badge;
- an inline limitation panel for partial/fallback/unsupported states;
- expandable safe diagnostics;
- side-specific values and field-level status;
- a raw-text alternative for text-based artifacts;
- synchronized navigation where side-by-side views are used.

Existing large XAML is split into focused controls so files remain below the
repository's 800-line limit. Layout logic remains outside code-behind where it can
be tested deterministically.

## Automated Real-Git Workflow Integration Design

The CI-safe test is a process-level Core integration test. It begins with the
filesystem result that an export/synchronization would have produced; it does not
claim to execute TIA VCI. The user-approved live lane supplies that acceptance
boundary.

The test uses a unique temporary working repository and local bare remote under a
deliberately short test root so Windows path validation is not affected by the
runner's profile length. It is marked Trait("Category", "GitE2E") and remains
included in the unfiltered release gate.

Setup:

- create the repositories under a unique test directory;
- use a test-only System.Diagnostics.Process implementation of IGitProcessRunner;
- set per-process GIT_CONFIG_NOSYSTEM=1, GIT_CONFIG_GLOBAL to a temporary empty
  file, GIT_TERMINAL_PROMPT=0, and GCM_INTERACTIVE=Never;
- set repository-local user name and email;
- set commit.gpgsign=false, core.hooksPath to an isolated empty directory, and
  core.autocrlf=false;
- create a fixed main branch and run git push --set-upstream origin main during
  baseline fixture setup;
- pass environment data per process without mutating process-global variables, so
  parallel tests cannot inherit one another's Git configuration;
- never contact an external remote.

Scenario:

1. Copy a baseline sanitized VCI artifact into the workspace.
2. Create and push the baseline commit with main configured as the upstream branch.
3. Replace the file with a second fixture representing the result of a completed
   VCI export/synchronization.
4. Use GetStatusAsync and assert exactly one expected unstaged artifact.
5. Call StageAsync and assert that artifact is staged and no unexpected path is
   present.
6. Call CommitAsync with a fixed subject and assert the working tree is clean.
7. Call GetCommitLogAsync and assert the head subject and hash.
8. Call GetCommitFilesAsync(newHash) and assert the expected artifact is present.
9. Call GetCommitDiffAsync(newHash) and assert the committed patch contains the
   expected old and new content.
10. Call the parameterless PushAsync used by the UI.
11. Assert the bare remote's refs/heads/main equals the new hash and its committed
    artifact content matches the workspace.

Raw Git is used only for fixture setup and independent remote verification; the
workflow under test uses GitService. Failures report the workflow phase, Git
version, redacted command and argument transcript, working directory, exit code,
bounded stdout/stderr, elapsed time or timeout, and temp-root path. Fixture disposal
always runs cleanup and reports cleanup failures. Cleanup retries transient Windows
file locks. TIA_GIT_E2E_KEEP_TEMP=1 may preserve the directory locally, but CI
ignores that option and always cleans.

The planned focused developer command is:

    dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~VciGitWorkflowTests"

The full integration-project command used by CI is:

    dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release

Both commands are documented in README.md with the completed test so developers do
not need a TIA installation to exercise this automated lane.

The boundary coverage is explicit:

| Behavior | Automated Core integration | Live-TIA lane |
|---|---|---|
| GitService status/stage/commit/history/diff/push | Yes | Smoke confirmation |
| Real local git.exe and bare remote | Yes | Yes |
| Production Siemens GitProcessRunner | No | Yes |
| ProcessStartPermission and TIA sandbox | No | Yes |
| VCI workspace selection/path resolution | No | Yes |
| Actual VCI project-to-workspace synchronization | No | Yes |
| Packaged Add-In and WPF interaction | No | Yes |

## Live-TIA V21 Acceptance

The live lane is a repeatable manual TIA procedure, not a new production export
service or headless CI harness. It standardizes on an existing mapped object and
the built-in VCI Project to workspace synchronization command.

Prerequisites:

- a sanitized disposable V21 project named TiaGitAcceptance;
- one mapped PLC block named Program with workspace output Program.xml;
- a VCI workspace and bare remote beneath a short per-run root;
- a known baseline marker GitAcceptanceV1 and changed marker GitAcceptanceV2;
- a locally built package with a unique version/identity for the run.

Procedure:

1. Record the package SHA-256 and install the unique V21 .addin package.
2. Launch a fresh isolated TIA V21 instance and confirm that the loaded package
   identity matches the recorded artifact.
3. Confirm approval of TIA.ReadWrite and ProcessStartPermission.
4. Open a fresh copy of TiaGitAcceptance and its VCI workspace.
5. In the built-in VCI workspace UI, verify the Program to Program.xml mapping and
   run Project to workspace synchronization to establish the V1 baseline.
6. Initialize the workspace repository, create main, commit V1, and set upstream to
   the local bare remote.
7. Change the known marker to GitAcceptanceV2 inside the TIA project.
8. Run the same built-in Project to workspace synchronization and assert that
   Program.xml changed and contains the V2 marker.
9. Use the packaged Add-In to verify status, stage, commit, clean status, history,
   and revision diff.
10. Use the Add-In's parameterless push and verify remote refs/heads/main and
    committed Program.xml content.
11. Save evidence under TestResults/LiveTiaV21/<run-id>: summary.md with pass/fail,
    source commit, candidate run/artifact ID, package identity/SHA-256, TIA/Public
    API versions, reviewer, permission confirmation, redacted Git command
    transcript, remote hash, and a sanitized copy of the Add-In log. Validate the
    evidence schema and attach the sanitized bundle to the draft release or named
    release-evidence artifact.
12. Save and close the project, close the isolated TIA instance, await process
    exit, then retry-delete the project copy, workspace, and bare remote. Preserve
    them only through an explicit local debugging choice.

An initial smoke run occurs as soon as the automated Core integration lane and
packaged build are available. The complete procedure runs again as a release gate.
It is not an ordinary CI dependency.

## Security and Reliability

- XML readers prohibit DTD processing and external entity resolution.
- File size, token count, and parser nesting are bounded with clear diagnostics.
- Git paths and revisions are validated at boundaries and passed without a shell;
  end-of-options separators are used where supported.
- Git diagnostics redact URL user information and possible credential material.
- Temporary paths are unique, access-scoped, and cleaned in finally blocks.
- Cancellation and cleanup are safe under rapid UI selection.
- No credentials, internal Siemens assemblies, or machine-specific installation
  paths enter production code or fixtures.
- Detailed diagnostic context is logged only through the existing safe logging
  boundary; the UI receives sanitized messages.

## Test Strategy

### Unit and Component Tests

- classifier matrix for suffix, path, content, pair conflicts, additions, and
  deletions;
- coordinator strategy selection, partial result, fallback, hard error, and
  cancellation;
- interface fields, nesting, sections, multilingual comments, and reorder
  stability;
- FBD additions, removals, modifications, rewiring, duplicate nodes, UId-only
  changes, unknown elements, malformed XML, and deterministic layout;
- SCL blocks, declarations, statements, comments, regions, unique renames,
  ambiguous renames, whitespace-only changes, recovery, and fallback;
- ViewModel mapping, mode/limitation visibility, template selection, rapid
  selection, and latest-result-wins behavior;
- explicit generic XML, malformed SimaticML, unsupported STL, unsupported SFC,
  partially understood FlgNet, working-tree/commit parity, and selectable raw-text
  fallback cases;
- STA WPF smoke tests that instantiate interface, FBD, SCL, text-fallback, and
  unsupported views with representative ViewModels and resolve their
  DataTemplates/resources at runtime;
- XML entity rejection, parser limits, and diagnostic redaction.

### Fixtures

Sanitized real TIA Portal V21 FBD and SCL exports anchor compatibility tests.
Focused synthetic fixtures isolate individual comparison rules. Synthetic data
alone is insufficient for final FBD or SCL acceptance.

Fixtures live under:

- src/TiaGitAddIn.Tests/TestData/SimaticMl/Fbd/V21;
- src/TiaGitAddIn.Tests/TestData/Scl/V21;
- src/TiaGitAddIn.IntegrationTests/TestData/VciGitWorkflow.

Each fixture directory contains a manifest recording V21 Public API build, artifact
kind, original encoding/BOM, sanitization actions, SHA-256, and expected parser
support level. Fixtures must not contain customer/project/device names, author
identities, network addresses, credentials, or machine-specific paths. A fixture
sanitization test scans these policies before compatibility tests run.

### Integration and E2E Tests

- working-tree and committed-revision comparison use the same coordinator;
- committed temporary files retain original suffixes;
- the real-Git E2E scenario covers status, stage, commit, history, diff, push,
  remote verification, diagnostics, and cleanup;
- tests can run repeatedly without shared repositories, credentials, ports, or
  global configuration changes.

## Coverage and CI

Both test projects pin coverlet.msbuild 6.0.4 with PrivateAssets=all. The net48
xUnit run disables the VSTest AppDomain during coverage collection. The unit run
first emits Coverlet JSON; the net8 integration run merges that JSON, emits merged
JSON and Cobertura, and enforces Threshold=80, ThresholdType=line, and
ThresholdStat=total.

The merged scope includes TiaGitAddIn.Core and testable TiaGitAddIn production
classes. Exclusions are limited to compiler/XAML generated files such as obj
g.cs/g.i.cs output and GeneratedCode/CompilerGenerated attributes. A
Siemens-runtime bootstrap exclusion requires an individually named pattern and
written justification; directories are not broadly excluded to manufacture the
threshold.

The final report is written to
TestResults/Coverage/coverage.cobertura.xml and uploaded as a workflow artifact.
The implementation plan will pin the PowerShell-safe MSBuild property syntax and
prove the merged command in the existing self-hosted Windows environment before
feature work.

A reusable self-hosted Windows test/coverage workflow is invoked for pull requests,
main-branch pushes, and release candidates. It performs checkout, .NET 8 setup,
restore, Release build, net48 unit/component tests, net8 GitE2E tests, coverage
merge, threshold enforcement, and coverage upload.

Release publication is split into candidate and approval stages:

1. after the reusable test gate passes, the tag workflow builds the V21 .addin
   candidate once, records SHA-256, and uploads the package and hash as immutable
   candidate artifacts;
2. the operator downloads that exact candidate for live-TIA acceptance;
3. summary.md records pass/fail, source commit, candidate run/artifact ID, package
   SHA-256, TIA/Public API versions, and reviewer;
4. sanitized live evidence is attached to the draft release or another durable
   release-evidence artifact;
5. a protected release environment/manual publication workflow validates the
   accepted evidence ID and package hash, downloads the same candidate, and
   publishes it without rebuilding.

Uploading evidence or publishing remains an explicit externally visible action and
requires user approval when executed. The focused and full integration commands in
the preceding section are documented in README.md.

## Delivery Order and Dependencies

1. Review and land the completed V21 API investigation documentation.
2. Scaffold the integration-test project, pin coverage tooling in both test
   projects, emit net48 JSON, merge/enforce it in the net8 run, and prove the
   commands on the self-hosted runner.
3. Implement classification, immutable contracts, coordinator, fallback,
   diagnostics, revision loading, and cancellation-safe UI routing.
4. After step 3, run three comparison branches independently:
   - deep interface comparison and focused interface view;
   - FBD graph comparison followed by its visual renderer;
   - structured SCL lexer/parser/comparer followed by its focused view.
5. In parallel with steps 3 and 4, implement the automated real-Git workflow test;
   it depends on the integration-test project and existing GitService, not on PLC
   comparison features.
6. As soon as step 5 and a fresh package build are available, run the initial
   live-TIA Git/VCI smoke and resolve environment or permission failures.
7. Integrate the branches, run full regression/coverage, build and hash the release
   candidate once, execute complete live-TIA acceptance against that artifact, and
   publish the identical candidate only after protected approval.

Each implementation phase follows RED, GREEN, and refactor and remains
independently reviewable.

## Implementation-Plan Follow-up

This document is the shared architecture design, not the executable coding plan.
After document approval, the writing-plans phase produces four linked,
independently executable plans:

1. comparison foundation, revision loading, fallback, routing, and deep interface;
2. FBD graph comparison and visual rendering;
3. SCL lexer/parser/comparison and structured view;
4. real-Git integration, CI/coverage, live-TIA evidence, and release gating.

Each plan names exact files and signatures, test methods, RED/GREEN/refactor steps,
focused verification commands, dependency points, and intended commit boundaries.
No implementation begins from this design alone.

## Acceptance-Criteria Traceability

| Task criterion | Planned proof | Lane |
|---|---|---|
| V21 public compare options, limits, permissions, and authoritative evidence | docs/tia-v21-compare-api-investigation.md supported-options, permissions, and evidence sections | Documentation review |
| Explicit supported-API decision | Investigation decision plus matching PRD.md and README.md statements | Documentation review |
| Every artifact declares actual mode, support, and limitation | PlcComparisonCoordinatorTests.EveryResultDeclaresModeSupportAndLimitation | CI unit |
| Structured comparison is preferred when trustworthy; otherwise text | PlcComparisonCoordinatorTests.FallsBackFromPartialStructureToTextOnlyWhenRequired | CI unit |
| Working-tree and committed revisions route consistently | RevisionComparisonIntegrationTests.RoutesWorkingTreeAndCommitIdenticallyAndPreservesSuffix | CI integration |
| Generic XML receives text fallback | PlcArtifactClassifierTests.ClassifiesGenericXmlAsTextFallback | CI unit |
| Malformed SimaticML receives text fallback with diagnostics | PlcComparisonCoordinatorTests.MalformedSimaticMlReturnsDiagnosticTextFallback | CI unit |
| Unsupported STL/SFC and partial FlgNet are explicit | PlcComparisonCoordinatorTests.UnsupportedAndPartialArtifactsExposeLimitations | CI unit |
| Interface datatype, Retain, comments, defaults, and attributes compare deeply | SimaticMlComparerTests.ComparesEveryInterfaceSemanticFieldOnBothSides | CI unit |
| Interface sections and nested members preserve both sides | SimaticMlComparerTests.ComparesEmptySectionsAndNestedMemberTrees | CI unit |
| Interface reordering remains deterministic without false add/remove | SimaticMlComparerTests.ReorderUsesRightDeclarationOrderWithoutFalseChanges | CI unit |
| FBD is parsed into a semantic node/pin/edge graph | FbdGraphBuilderTests.BuildsSemanticGraphFromSanitizedV21Export | CI unit/fixture |
| FBD reports node add/remove/change and rewiring | FbdGraphComparerTests.ReportsAddRemoveModifyAndRewire | CI unit |
| FBD has a dedicated deterministic visual view | FbdLayoutEngineTests.IsDeterministic plus FbdDiffViewSmokeTests.ResolvesBindingsAndTemplates | CI unit/STA |
| Unknown FBD parts are partial and malformed FBD falls back | FbdComparisonTests.UnknownPartIsPartialAndMalformedExportFallsBack | CI unit |
| SCL parser recognizes blocks, declarations, statements, comments, and regions | SclParserTests.ParsesSanitizedV21StructuresAndComments | CI unit/fixture |
| SCL presentation groups by block and region with syntax tokens | SclDiffViewModelTests.GroupsByBlockAndRegion plus SclDiffViewSmokeTests.ResolvesHighlightedTokens | CI unit/STA |
| SCL reports additions, removals, and unique declaration renames | SclComparerTests.ReportsAddRemoveAndUniqueDeclarationRename | CI unit |
| Malformed SCL retains diagnostics and text fallback | SclComparisonTests.MalformedSourceReturnsDiagnosticTextFallback | CI unit |
| Actual VCI project-to-workspace update/export occurs | Live-TIA procedure steps 4 through 8, including V1/V2 marker assertion | Release-only live acceptance |
| Status and stage operate on the fixture workspace change | VciGitWorkflowTests.FixtureWorkspaceChangeCanBeCommittedAndPushed status/stage assertions | CI Core integration |
| Commit, clean status, history, files, and diff are correct | Same test's commit/log/GetCommitFilesAsync/GetCommitDiffAsync assertions | CI Core integration |
| Push uses no real credentials and reaches the expected remote ref | Same test's parameterless PushAsync and local bare refs/heads/main assertion | CI Core integration |
| Failure diagnostics and cleanup are actionable and repeatable | VciGitRepositoryFixtureTests.RedactsDiagnosticsAndAlwaysCleans plus repeat execution | CI integration |
| Developer and CI commands are documented and enforced | README.md commands plus release.yml test job and publish needs: test | Documentation/CI |
| Production runner, permissions, package, VCI mapping, and UI work in V21 | Live-TIA steps 1 through 12 and TestResults/LiveTiaV21 evidence | Release-only live acceptance |

## Principal Risks and Mitigations

| Risk | Mitigation |
|---|---|
| V21 export variants exceed synthetic assumptions | Require sanitized real V21 fixtures and retain raw fallback |
| SimaticML UId regeneration creates false changes | Match semantic signatures and graph neighbourhoods |
| Duplicate FBD nodes make pairing ambiguous | Use conservative unique matching; report add/remove otherwise |
| Shallow SCL parser overstates rename or structure | Conservative rename rule, partial support, retained raw spans |
| Rapid WPF selection applies stale results | Cancellation plus generation check and dispatcher boundary |
| Existing net48 coverage tooling is incompatible | Prove and pin the coverage command before feature phases |
| Internal Siemens APIs appear tempting during implementation | CI/reference review and explicit non-goal |
| E2E test accidentally depends on user Git state | Isolated config, local identity, prompt suppression, bare remote |

## Definition of Done

- All original acceptance criteria map to an automated test or named live-TIA step.
- Existing and new tests pass.
- Testable production assemblies meet at least 80% line coverage.
- Fallback and limitations are visible for every unsupported or partial artifact.
- Working-tree and committed revisions produce consistent routing.
- No stale async result can replace the active selection.
- The V21 Add-In package builds successfully.
- No secret, internal Siemens compare reference, or unsafe parser setting is added.
- Graphify is updated after code changes.
- Live-TIA V21 acceptance passes before release.
