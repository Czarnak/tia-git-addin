# tia-git-addin

TIA Portal V21 Add-In for working with Git from inside TIA Portal Version Control workflows.

The add-in targets `.NET Framework 4.8` and packages as a native TIA Portal V21 `.addin` file. Git operations use the locally configured `git.exe`; the add-in does not store credentials or implement Git protocol behavior.

## Status

Implemented:

- TIA Portal V21 project packaging that emits `TiaGitAddIn.addin`.
- Minimal project-tree Add-In entry point with an `Open Git Panel...` menu item.
- Core Git models, path validation, configuration persistence, Git output parsing, process runner, repository discovery, and operation serialization.
- WPF UI architecture (MVVM) with ViewModels and Views for Status, History, Commit, Diff, and Settings.
- Unit coverage for both foundation and UI logic (34 tests passing).

In Progress:

- Full integration of the WPF Git panel with live TIA Portal project context.
- End-to-end VCI workspace detection and resolution.
- Hardening of fetch, pull, push, and branch management workflows.

## Prerequisites

- TIA Portal V21 installed at `C:\Program Files\Siemens\Automation\Portal V21`.
- .NET SDK compatible with the repo `global.json`.
- Local Git installation available as `git` or `git.exe`.
- SIMATIC Automation Compare Tool (SACT) for advanced visual diff features.

## Build

Restore and build:

```powershell
dotnet restore TiaGitAddIn.sln
dotnet build TiaGitAddIn.sln --no-restore
```

Expected add-in package:

```text
src/TiaGitAddIn/bin/Debug/net48/TiaGitAddIn.addin
```

The main project calls the V21 publisher directly:

```text
C:\Program Files\Siemens\Automation\Portal V21\PublicAPI\V21\Siemens.Engineering.AddIn.Publisher.exe
```

## Test

```powershell
dotnet test TiaGitAddIn.sln --no-build
```

## Project Layout

```text
src/
  TiaGitAddIn/
    Entry/           TIA Portal Add-In menu entry points
    Models/          Git and diff data models
    Configuration/   Path validation and project config persistence
    Services/        Git process execution, parsing, repository discovery
    UI/              WPF MVVM components (ViewModels, Views, Converters)
  TiaGitAddIn.Tests/ Unit and UI logic tests
```

## Roadmap

1. **Add-In Shell & TIA Integration**
   - Connect the WPF main dialog to live TIA Portal project events and lifecycle.
   - Resolve active TIA project and VCI workspace paths reliably across different project types.
   - Implement user-facing error handling and guidance for missing VCI setups.

2. **Repository Management**
   - Automate detection of existing Git repositories from the VCI workspace path.
   - Add UI support for initializing a new Git repository for an active workspace.

3. **History & Diff Enhancements**
   - Enhance text diff viewing with better syntax highlighting or TIA-specific block metadata.
   - Research and implement "Compare with TIA" for binary or graphical artifacts using internal TIA APIs.

4. **LAD/FBD Visual Diff Viewer**
   - Integrate with SIMATIC Automation Compare Tool (SACT) CLI to produce structured JSON diffs.
   - Implement graphical ladder-logic rendering using WPF Canvas and a custom layout engine.
   - Support color-coded highlights for semantic network differences (added/removed/changed).

5. **Hardening & Verification**
   - Exercise the add-in inside TIA Portal V21 with real-world VCI workspaces.
   - Add integration and E2E coverage for the end-to-end VCI-to-Git lifecycle.
   - Review permissions before distribution and optimize the `.addin` package size.
