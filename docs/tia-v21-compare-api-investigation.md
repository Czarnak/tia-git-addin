# TIA Portal V21 Compare API Investigation

**Status:** Decided  
**Date:** 2026-07-15  
**Target:** TIA Portal V21 Public API build `2100.0.121.1`

The probe machine used `C:\Program Files\Siemens\Automation\Portal V21` as the installation root. The portable placeholder `<TIA_V21_ROOT>` is used below.

## Decision

Retain the native SimaticML comparer and custom LAD renderer for Git revision review. Keep the text/structured view as the fallback for unsupported or unparseable artifacts.

TIA Portal V21 does expose supported, in-process comparison APIs, but they are data APIs for TIA engineering objects. They return hierarchical result objects and do not expose the Siemens graphical LAD/FBD comparison editor. They also do not accept VCI workspace files, SimaticML files, Git commit identifiers, paths, streams, or raw revision content.

Do not reference or invoke the internal `Siemens.Automation.CommonServices.Compare.*` UI/command assemblies. Their graphical compare implementation is not part of the documented Public API or Add-In contract.

## Investigation method

The investigation used the locally installed V21 API as the source of truth:

- Parsed the public XML documentation under `<TIA_V21_ROOT>\PublicAPI\V21\net48`.
- Read the installed Openness help in `<TIA_V21_ROOT>\Help\en-US\TIAPortalOpennessenUS.zip`, including the PLC software comparison (`67988419211.htm`), hardware comparison (`115151758091.htm`), and detailed library comparison (`169143209483.htm`) topics.
- Inspected public assembly metadata with `System.Reflection.Metadata` without starting TIA Portal, instantiating Siemens types, or running static initializers.
- Scanned every installed `Siemens.Engineering.AddIn*.xml` member name for `Compare` and `Diff`.
- Inspected the internal compare assembly metadata only to establish where the graphical editor boundary lies. Internal metadata was not treated as a supported API.

The metadata probe scanned 16 `Siemens.Engineering*.dll` Public API assemblies and six targeted internal comparison/UI assemblies with zero metadata failures. The internal scan was intentionally targeted rather than an exhaustive scan of the entire Portal installation.

The principal assemblies were:

| Assembly | File version | SHA-256 |
| --- | --- | --- |
| `Siemens.Engineering.Base.dll` | `2100.0.121.1` | `8C12B0FA70C298F1CD1221105880752AB204ED63C9F6D9DCB68833D977BCB5D0` |
| `Siemens.Engineering.Step7.dll` | `2100.0.121.1` | `DFBBE3863005FA2FBC8CB553A3E7E4612ACBACA19124939EB8D29DE24C814EBA` |
| `Siemens.Engineering.AddIn.Base.dll` | `2100.0.121.1` | `8412A3428CB2C41C7129DFD9A173C7F1454667621B53BC3BDDD73629F050CBA0` |
| `Siemens.Engineering.AddIn.Step7.dll` | `2100.0.121.1` | `10C2A0F280AEB5347B91A49D81AF304B69521087205EB3319BF7A6ED538A7B28` |

No live comparison call was made because a useful call requires valid project or online engineering objects. The public signatures, documented result shape, and Add-In surface are sufficient to decide whether the API can compare Git revisions or host the native graphical editor.

## Supported public comparison options

| Public API | Assembly | Result | Relevant limitation |
| --- | --- | --- | --- |
| `PlcSoftware.CompareTo(ISoftwareCompareTarget)` | `Siemens.Engineering.Step7.dll` | `CompareResult` | Compares TIA software targets, not serialized files or Git revisions. |
| `PlcSoftware.CompareToOnline()` | `Siemens.Engineering.Step7.dll` | `CompareResult` | Requires an accessible online target; this is offline/online diagnostics, not commit review. |
| `HardwareObject.CompareTo(IHardwareCompareTarget)` | `Siemens.Engineering.Base.dll` | `CompareResult` | Compares hardware engineering objects, not hardware export files. |
| Project/global library and library object `CompareTo*` methods | `Siemens.Engineering.Base.dll` | `LibraryCompareResult` or `DetailedCompareResult` | Limited to TIA library objects and versions. |
| `MappedObject.GetStatus()` and `MappedObject.Status` | `Siemens.Engineering.Base.dll` | `IndividualObjectCompareResult` | Reports coarse VCI equality/divergence; it does not return a content diff. |

The installed Openness help documents the supported PLC software scenarios as configured PLC to configured PLC, project library, global library, or PLC master copy, plus an offline configured PLC to its connected online PLC. It does not document a comparison target representing an arbitrary file or historical revision.

The documented generic result tree exposes:

- `CompareResult.RootElement`;
- child `CompareResultElement.Elements`;
- `ComparisonResult` states such as identical, different, missing, or irrelevant;
- `DetailedInformation`, `LeftName`, and `RightName`.

The VCI result exposes `Equal`, `Unequal`, `WorkspaceFileMissing`, or `Unknown`, plus flags indicating whether the project object or workspace file changed. Neither result model exposes a diagram, editor control, rendering model, or serialized LAD/FBD network.

## Add-In boundary

The five installed V21 Add-In XML documentation files contain no public member whose name includes `Compare` or `Diff`:

- `Siemens.Engineering.AddIn.Base.xml`;
- `Siemens.Engineering.AddIn.Permissions.xml`;
- `Siemens.Engineering.AddIn.Safety.xml`;
- `Siemens.Engineering.AddIn.Step7.xml`;
- `Siemens.Engineering.AddIn.Utilities.xml`.

An Add-In can call the public `PlcSoftware` methods if it adds a non-copy-local reference to `Siemens.Engineering.Step7.dll` and obtains suitable engineering objects. `Siemens.Engineering.AddIn.Step7.dll` is a different assembly and does not provide `PlcSoftware`.

That does not help the current Git workflow:

- the current VCI menu receives `WorkspaceFile` or `WorkspaceFolder` selections;
- a Git comparison supplies two file revisions, often through temporary XML files;
- the public API requires TIA engineering object instances on both sides;
- the result is headless comparison data, not the built-in graphical editor.

Materializing a historical Git revision as a second TIA project or imported object would add project lifecycle, compatibility, performance, and mutation risks while still not exposing the graphical editor. It is not a suitable comparison path for this Add-In.

## Graphical editor boundary

The installed Portal `Bin` directory contains:

- `Siemens.Automation.CommonServices.Compare.Core.dll`;
- `Siemens.Automation.CommonServices.Compare.Openness.dll`;
- `Siemens.Automation.CommonServices.Compare.UI.dll`.

Metadata inspection shows that the graphical side-by-side editor is implemented behind internal types and command routing, including an internal `CompareEditorStarter` and internal command identifiers for side-by-side and online comparison. That code depends on TIA frame-application view services and is not documented in `PublicAPI\V21\net48`.

CLR visibility alone would not make an implementation type a supported Siemens Add-In API. Direct references are unavailable for internal types, reflection would be version-coupled, and additional permissions would not turn the internal UI into a supported contract. This path is rejected.

## Permissions

No compare-specific Add-In permission is documented in `Siemens.Engineering.AddIn.Permissions.xml`.

- `Siemens.Engineering.AddIn.Publisher.xsd` requires an Add-In to declare either `TIA.ReadOnly` or `TIA.ReadWrite`; no third compare-specific TIA permission exists.
- Public comparison methods execute in-process and do not require `ProcessStartPermission` merely to perform the comparison.
- They still require normal TIA Add-In/Openness access, valid engineering objects, and—for `CompareToOnline()`—a reachable online target and the applicable TIA user rights.
- The current package already requests `TIA.ReadWrite` for its broader VCI workflow and `ProcessStartPermission` for local `git.exe`; those declarations do not expose the internal graphical compare UI.
- Launching an external Siemens comparison application would require process-start permission and would violate the PRD constraint that normal workflows stay inside TIA Portal.

## Option evaluation

| Option | Supported contract | Accepts Git revisions | Graphical LAD/FBD view | Decision |
| --- | --- | --- | --- | --- |
| Public `Siemens.Engineering.*` compare methods | Yes | No | No; result tree only | Keep as a possible future diagnostic for live TIA objects, not the Git diff engine. |
| Internal compare UI/command services | No | No direct file-revision contract found | Yes, internally | Reject reflection/internal assembly coupling. |
| External Siemens comparison process | External application | Not evaluated; no in-process file-revision contract | External window | Reject for the normal in-TIA workflow. |
| Native SimaticML comparer and custom LAD renderer | Project-owned implementation | Yes | Yes, for supported LAD content | Selected. |
| Existing text/structured diff | Project-owned implementation | Yes | No | Retain as fallback. |

## Consequences and known limitations

The selected path matches the canonical repository boundary: Git compares VCI workspace files, and the Add-In renders those exact revisions without loading a second TIA project.

The custom renderer does not claim Siemens-native comparison fidelity. Current limitations include:

- LAD is implemented; FBD remains planned;
- only the first block in a SimaticML file is compared;
- compile units are paired by index;
- the renderer is a simplified geometric representation of Siemens LAD;
- non-TIA XML classification and parse failures fall back to the text view.

The relevant implementation is in [SimaticMlComparer.cs](../src/TiaGitAddIn.Core/Services/SimaticMl/SimaticMlComparer.cs), [LadLayoutEngine.cs](../src/TiaGitAddIn.Core/Services/LadLayoutEngine.cs), and [DiffView.xaml](../src/TiaGitAddIn/UI/Views/DiffView.xaml).

No production code or Siemens assembly reference is added by this decision.

## Revisit criteria

Revisit the decision if a future Siemens Public API release documents at least one of the following:

- an Add-In API that hosts or opens the native compare editor through a supported contract;
- a comparison API accepting VCI workspace files, SimaticML files, streams, or other revision content;
- a documented graphical comparison model or control that an Add-In may embed.

Also reconsider the public result APIs if product scope expands to open-project, project-to-project, library, hardware, or offline/online diagnostics. Those are valid use cases, but they are separate from Git revision review.
