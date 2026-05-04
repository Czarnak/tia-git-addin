# tia-git-addin

TIA Portal V21 Add-In for working with Git from inside TIA Portal Version Control workflows.

The add-in targets `.NET Framework 4.8` and packages as a native TIA Portal V21 `.addin` file. Git operations use the locally configured `git.exe`; the add-in does not store credentials or implement Git protocol behavior.

## Status

Implemented:

- TIA Portal V21 project packaging that emits `TiaGitAddIn.addin`.
- Minimal project-tree Add-In entry point with an `Open Git Panel...` menu item.
- Core Git models, path validation, configuration persistence, Git output parsing, process runner, repository discovery, and operation serialization.
- Unit coverage for the current non-UI foundation.

Not implemented yet:

- Full WPF Git panel.
- End-to-end VCI workspace detection in a live TIA Portal project.
- Fetch, pull, push, branch creation, commit history UI, and diff UI.

## Prerequisites

- TIA Portal V21 installed at `C:\Program Files\Siemens\Automation\Portal V21`.
- .NET SDK compatible with the repo `global.json`.
- Local Git installation available as `git` or `git.exe`.

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

Current verified result: `37/37` tests passing.

## Project Layout

```text
src/
  TiaGitAddIn/
    Entry/           TIA Portal Add-In menu entry points
    Models/          Git and diff data models
    Configuration/   Path validation and project config persistence
    Services/        Git process execution, parsing, repository discovery
  TiaGitAddIn.Tests/ Unit tests
```

## Roadmap

1. Add-In shell
   - Replace the placeholder menu action with the WPF main dialog.
   - Resolve the active TIA project and VCI workspace path reliably.
   - Add user-facing error handling for unsupported or missing VCI setup.

2. Repository setup
   - Detect existing Git repositories from the VCI workspace.
   - Support initializing a repository for a workspace.
   - Persist `.tia-git-addin.json` project settings safely.

3. Status and commit workflow
   - Display changed, staged, untracked, deleted, and conflicted files.
   - Stage and unstage individual files or groups.
   - Validate commit messages and commit staged changes.

4. History and diff workflow
   - Show commit history with hash, author, date, subject, and changed files.
   - Compare a selected commit against its parent.
   - Add text diff viewing for exported source/XML files.

5. Remote and branch workflow
   - Show branch and remote tracking state.
   - Support fetch, pull, push, branch creation, and branch switching.
   - Surface merge conflicts and Git errors without hiding details.

6. TIA Portal integration hardening
   - Exercise the add-in inside TIA Portal V21 with real VCI workspaces.
   - Add integration and E2E coverage for the critical commit/history/diff/push flow.
   - Review permissions before distribution and keep the `.addin` package minimal.
