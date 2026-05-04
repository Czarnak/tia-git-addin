# Architecture: TIA Portal V21 Git Add-In

## Overview

A C#/.NET Framework 4.8 TIA Portal V21 Add-In that embeds Git workflows directly inside TIA Portal. The add-in uses the TIA VCI workspace as the canonical Git working tree and invokes local `git.exe` via `System.Diagnostics.Process` for all Git operations. It exposes context menu entries in TIA Portal's project tree and opens WPF-based modal dialogs for staging, commit, history, branch management, and diff viewing. No external windows, no credential management, no libgit2sharp.

## Technology Stack

| Technology | Justification |
|---|---|
| C# / .NET Framework 4.8 | TIA Portal V21 Add-In SDK targets net48; all Siemens.Engineering assemblies require it. |
| WPF (System.Windows) | TIA Portal hosts WPF controls natively in Add-In dialogs; WinForms is not supported for embedded panels. |
| Siemens.Engineering.AddIn.dll | Required assembly for Add-In lifecycle, context menus, and TIA Portal integration. |
| Siemens.Engineering.dll | Openness API for accessing project structure, VCI workspace paths, and project metadata. |
| Siemens.Collaboration.Net.TiaPortal.AddIn.Build (NuGet) | Build package that auto-detects TIA installation, manages references, generates .addin file via post-build. |
| System.Diagnostics.Process | Invokes local git.exe; no native Git library dependencies. |
| System.Text.Json | Configuration serialization; ships with .NET; no external dependency. |
| xUnit + Moq | Unit testing framework; industry standard for .NET, good async support. |
| DiffPlex (NuGet) | In-process unified/side-by-side diff computation for text files; avoids shelling out to diff tools. |

## Directory Structure

```
tia-git-addin/
|-- TiaGitAddIn.sln                          # Visual Studio solution
|-- README.md
|-- LICENSE
|-- PRD.md
|-- ARCHITECTURE.md
|-- .gitignore
|
|-- src/
|   |-- TiaGitAddIn/                         # Main Add-In project (class library, net48)
|   |   |-- TiaGitAddIn.csproj
|   |   |-- AddInPublisherConfiguration.xml  # Siemens publisher config for .addin generation
|   |   |
|   |   |-- Entry/                           # TIA Add-In entry point classes
|   |   |   |-- GitAddIn.cs                  # Implements AddInBase — Add-In lifecycle
|   |   |   |-- GitAddInProvider.cs          # Implements AddInProvider — registers context menus
|   |   |   |-- ProjectTreeMenu.cs           # ContextMenuAddIn for project tree right-click
|   |   |
|   |   |-- Services/                        # Core service layer
|   |   |   |-- IGitService.cs               # Interface: all git operations
|   |   |   |-- GitService.cs                # Implementation: shells out to git.exe
|   |   |   |-- GitProcessRunner.cs          # Low-level Process wrapper with timeout/cancellation
|   |   |   |-- IGitProcessRunner.cs         # Interface for process runner (testable)
|   |   |   |-- GitOutputParser.cs           # Parses git status/log/diff porcelain output
|   |   |   |-- IRepositoryDiscovery.cs      # Interface: find/validate repo for TIA project
|   |   |   |-- RepositoryDiscovery.cs       # Locates .git dir from VCI workspace path
|   |   |   |-- IVciWorkspaceLocator.cs      # Interface: resolve VCI workspace path from TIA project
|   |   |   |-- VciWorkspaceLocator.cs        # Uses Openness API to get VCI workspace directory
|   |   |   |-- OperationSerializer.cs       # Prevents concurrent git operations
|   |   |
|   |   |-- Models/                          # Data models (POCOs, no behavior)
|   |   |   |-- GitStatus.cs                 # Staged/unstaged/untracked/conflicted file lists
|   |   |   |-- FileStatusEntry.cs           # Single file: path, index status, worktree status
|   |   |   |-- CommitInfo.cs                # Hash, author, date, subject, parent hash
|   |   |   |-- BranchInfo.cs                # Name, is-current, tracking remote, ahead/behind
|   |   |   |-- DiffResult.cs                # Collection of DiffEntry for a commit or working tree
|   |   |   |-- DiffEntry.cs                 # File path, change type, hunks
|   |   |   |-- DiffHunk.cs                  # Header, old/new line ranges, lines
|   |   |   |-- DiffLine.cs                  # Line content, type (context/add/delete)
|   |   |   |-- RemoteInfo.cs                # Remote name, URL
|   |   |   |-- OperationResult.cs           # Success/failure, message, detail
|   |   |   |-- GitConfiguration.cs          # Persisted per-project settings
|   |   |
|   |   |-- Configuration/                  # Settings persistence
|   |   |   |-- IConfigurationService.cs     # Interface: load/save project-level config
|   |   |   |-- ConfigurationService.cs      # JSON file in VCI workspace root (.tia-git-addin.json)
|   |   |   |-- PathValidator.cs             # Static: validates paths, rejects traversal/injection
|   |   |
|   |   |-- UI/                              # WPF views and view models
|   |   |   |-- Views/
|   |   |   |   |-- MainDialog.xaml           # Primary dialog: tabs for status/history/settings
|   |   |   |   |-- MainDialog.xaml.cs
|   |   |   |   |-- StatusView.xaml           # File status list, stage/unstage buttons
|   |   |   |   |-- StatusView.xaml.cs
|   |   |   |   |-- CommitView.xaml           # Commit message box, commit button
|   |   |   |   |-- CommitView.xaml.cs
|   |   |   |   |-- HistoryView.xaml          # Commit log list, commit detail panel
|   |   |   |   |-- HistoryView.xaml.cs
|   |   |   |   |-- DiffView.xaml             # Inline text diff viewer with syntax coloring
|   |   |   |   |-- DiffView.xaml.cs
|   |   |   |   |-- BranchView.xaml           # Branch list, create/switch, remote status
|   |   |   |   |-- BranchView.xaml.cs
|   |   |   |   |-- SettingsView.xaml         # Git path, repo path, workspace path config
|   |   |   |   |-- SettingsView.xaml.cs
|   |   |   |   |-- ProgressOverlay.xaml      # Overlay for long-running ops with cancel
|   |   |   |   |-- ProgressOverlay.xaml.cs
|   |   |   |
|   |   |   |-- ViewModels/
|   |   |   |   |-- MainViewModel.cs          # Top-level VM, tab navigation, operation state
|   |   |   |   |-- StatusViewModel.cs        # Drives StatusView: file list, stage/unstage commands
|   |   |   |   |-- CommitViewModel.cs        # Commit message, validation, commit command
|   |   |   |   |-- HistoryViewModel.cs       # Log loading, commit selection, changed files
|   |   |   |   |-- DiffViewModel.cs          # Diff computation, line-level display model
|   |   |   |   |-- BranchViewModel.cs        # Branch ops, remote tracking display
|   |   |   |   |-- SettingsViewModel.cs      # Config editing, path validation feedback
|   |   |   |   |-- ViewModelBase.cs          # INotifyPropertyChanged base
|   |   |   |   |-- RelayCommand.cs           # ICommand implementation
|   |   |   |   |-- AsyncRelayCommand.cs      # Async ICommand with busy tracking
|   |   |   |
|   |   |   |-- Converters/
|   |   |   |   |-- FileStatusToColorConverter.cs
|   |   |   |   |-- BoolToVisibilityConverter.cs
|   |   |   |   |-- DiffLineTypeToColorConverter.cs
|   |   |
|   |   |-- Logging/
|   |   |   |-- IAddInLogger.cs              # Interface: structured logging
|   |   |   |-- FileLogger.cs               # Writes to %APPDATA%/TiaGitAddIn/logs/
|   |
|   |-- TiaGitAddIn.Tests/                   # Unit test project (net48, xUnit)
|       |-- TiaGitAddIn.Tests.csproj
|       |-- Services/
|       |   |-- GitOutputParserTests.cs      # Parse status/log/diff porcelain output
|       |   |-- GitServiceTests.cs           # Mock IGitProcessRunner, verify command assembly
|       |   |-- RepositoryDiscoveryTests.cs  # Path traversal, .git detection
|       |   |-- OperationSerializerTests.cs  # Concurrent operation prevention
|       |
|       |-- Configuration/
|       |   |-- ConfigurationServiceTests.cs # Load/save/defaults
|       |   |-- PathValidatorTests.cs        # Injection, traversal, whitespace, length
|       |
|       |-- Models/
|       |   |-- GitStatusTests.cs            # Model construction and classification
|       |
|       |-- ViewModels/
|           |-- StatusViewModelTests.cs      # Stage/unstage command logic
|           |-- CommitViewModelTests.cs      # Validation rules
|
|-- docs/                                    # Optional dev notes (not shipped)
|   |-- tia-addin-api-notes.md               # Discovered API surface from Siemens docs
```

## Component Map

| Component | Responsibility | Depends On | Exposes |
|---|---|---|---|
| `GitAddIn` | Add-In lifecycle: initialization, shutdown, TIA Portal reference capture | Siemens.Engineering.AddIn | `Start()`, `Stop()` |
| `GitVciEditorProvider` | Registers VCI workspace-view provider with TIA Portal | Siemens.Engineering.AddIn.VersionControl | `GetVciWorkspaceViewAddInProvider()` |
| `GitVciWorkspaceMenu` | Builds VCI workspace-view right-click menu item ("Open Git Panel...") for `WorkspaceFile` and `WorkspaceFolder` | `GitVciWorkspaceViewProvider`, UI layer | Context menu item |
| `VciWorkspaceLocator` | Resolves VCI workspace filesystem path from VCI `WorkspaceFile.FileInfo` or `WorkspaceFolder.DirectoryInfo`, plus reflected path/directory/file fallbacks | Siemens.Engineering.AddIn.VersionControl through object/reflection boundary | `TryGetWorkspacePath(object): string?` |
| `RepositoryDiscovery` | Finds `.git` directory, validates repo state, detects init-needed | `PathValidator` | `FindRepository(path): RepoInfo?`, `InitRepository(path)` |
| `GitProcessRunner` | Executes git.exe with Siemens Add-In `Utilities.Process`, captures stdout/stderr, enforces timeout | `PathValidator` | `RunAsync(args, workDir, cancel): ProcessResult` |
| `GitOutputParser` | Parses porcelain output of `git status`, `git log`, `git diff` into models | None (pure functions) | Static parse methods |
| `GitService` | Orchestrates Git operations: status, stage, unstage, commit, fetch, pull, push, branch, log, diff | `IGitProcessRunner`, `GitOutputParser` | `IGitService` interface |
| `OperationSerializer` | Ensures only one Git operation runs at a time; queues or rejects concurrent requests | None (SemaphoreSlim) | `AcquireAsync()`, `Release()` |
| `ConfigurationService` | Loads/saves `.tia-git-addin.json` from VCI workspace root | `PathValidator`, System.Text.Json | `IConfigurationService` |
| `PathValidator` | Validates filesystem paths; rejects traversal attacks, invalid chars, excessive length | None (static) | `Validate(path): ValidationResult` |
| `GitPanelWindow` | Minimal WPF dialog window; hosts Status and Commit tabs | `MainViewModel` | `ShowDialog()` on a separate STA thread |
| `MainViewModel` | Coordinates minimal status/commit UI state and repository path | `IGitService`, child VMs | Properties, refresh |
| `StatusViewModel` | Displays file status, stage/unstage selection, refresh | `IGitService` via `MainViewModel` | Observable collections, commands |
| `CommitViewModel` | Commit message entry, validation, execute commit | `IGitService` via `MainViewModel` | `CommitCommand` |
| `HistoryViewModel` | Loads commit log, selects commit, shows changed files | `IGitService` via `MainViewModel` | `SelectedCommit`, file list |
| `DiffViewModel` | Computes and presents text diff (unified or side-by-side) | DiffPlex, `IGitService` | Diff line collections |
| `BranchViewModel` | Lists branches, create/switch, shows remote tracking | `IGitService` via `MainViewModel` | Branch commands |
| `SettingsViewModel` | Edits git.exe path, repo path; validates on change | `IConfigurationService`, `PathValidator` | Settings properties |
| `FileLogger` | Writes timestamped log entries to disk | None | `IAddInLogger` |

## Implementation Task List

### Done

- [x] Create Visual Studio solution and `net48` Add-In project.
- [x] Add TIA Portal V21 publisher configuration.
- [x] Configure build to emit `TiaGitAddIn.addin` through the V21 Add-In publisher.
- [x] Reference TIA Portal V21 Add-In assemblies from `PublicAPI/V21/net48`.
- [x] Add VCI workspace-view Add-In entry point.
- [x] Add `Open Git Panel...` VCI workspace context menu action for `WorkspaceFolder` and `WorkspaceFile`.
- [x] Verify TIA Portal V21 loads the generated `.addin` package.
- [x] Add Git data models: `GitStatus`, `FileStatusEntry`, `CommitInfo`, `BranchInfo`, `RemoteInfo`, `OperationResult`, and diff model classes.
- [x] Add configuration model and `.tia-git-addin.json` persistence service.
- [x] Add path validation for repository paths and `git.exe`.
- [x] Add low-level Git process runner with timeout, cancellation, stdout/stderr capture, and Windows-safe argument escaping.
- [x] Switch Git process execution to Siemens Add-In `Utilities.Process` to satisfy TIA sandbox `ProcessStartPermission`.
- [x] Add Git output parser for status, log, branches, and remotes.
- [x] Add repository discovery from a workspace path.
- [x] Add VCI workspace locator for `WorkspaceFolder.DirectoryInfo`, `WorkspaceFile.FileInfo`, and reflected path/directory/file properties.
- [x] Add operation serializer to prevent concurrent Git operations.
- [x] Add minimal WPF Git panel with status refresh, stage, unstage, and commit.
- [x] Add minimal `MainViewModel`, `StatusViewModel`, `CommitViewModel`, `AsyncCommand`, and file-status item view model.
- [x] Add file logger under `%APPDATA%/TiaGitAddIn/logs`.
- [x] Add Add-In publisher permissions required by the panel and Git execution: `UIPermission`, `SecurityPermission.UnmanagedCode`, `FileIOPermission`, and `ProcessStartPermission`.
- [x] Add unit tests for current model, configuration, parser, path validation, argument escaping, operation serializer, and workspace locator behavior.
- [x] Add UI/launch regression tests for status refresh, staging, commit validation, VCI launch, callback reflection, and publisher permissions.
- [x] Smoke-test in TIA Portal V21 VCI workspace view: panel opens and refreshes Git status.
- [x] Update `README.md` with build/test instructions and roadmap.
- [x] Update `graphify-out` after code/documentation changes.

### Partial

- [ ] `IGitService` and `GitService` core operations.
  - Done: status, stage one file, unstage one file, commit, branch listing, checkout, log, remote listing.
  - Pending: stage multiple files, stage all, fetch, pull, push, create branch, working tree diff, commit diff, commit file list, repository init.
- [ ] Add-In entry classes.
  - Done: VCI editor provider, VCI workspace-view provider/menu, repository discovery/config loading, panel launch, callback exception logging.
  - Pending: optional VCI import/export workflow hooks if repository-specific import/export automation is needed.
- [ ] VCI workspace discovery.
  - Done: VCI workspace file/folder object path handling and reflected path fallback.
  - Pending: user guidance when selected VCI item is outside a Git repository or VCI setup is missing.
- [ ] Configuration handling.
  - Done: load/save/recover malformed config.
  - Pending: settings UI, startup validation, and migration/versioning strategy if config shape changes.
- [ ] Main WPF panel shell.
  - Done: minimal code-built `GitPanelWindow` with Status and Commit tabs.
  - Pending: full tabbed shell, status bar, settings/history/diff/branch tabs, and progress overlay.

### Not Started

- [ ] Full `MainViewModel` tab coordination.
- [ ] Full status view polish: grouped staged/unstaged/untracked lists, multi-select, stage all.
- [ ] Full commit view polish: staged-file awareness, character count, richer validation.
- [ ] History view and `HistoryViewModel`.
- [ ] Diff view and `DiffViewModel`.
- [ ] Branch view and `BranchViewModel`.
- [ ] Settings view and `SettingsViewModel`.
- [ ] Shared WPF commands: sync `RelayCommand` and extended `AsyncCommand` error/cancel handling.
- [ ] WPF converters for file status, visibility, and diff line styling.
- [ ] Progress/cancel overlay for long-running Git operations.
- [ ] Full diff parsing/rendering.
- [ ] Remote operations: fetch, pull, and push.
- [ ] Branch creation workflow.
- [ ] Merge conflict UI and conflict-specific guidance.
- [ ] End-to-end TIA Portal V21 workflow test with a real VCI workspace.
- [ ] Integration tests for Git command assembly through `GitService`.
- [ ] Packaging/distribution review for final deployment process.

## Data Model

### GitConfiguration (persisted as `.tia-git-addin.json`)

| Field | Type | Description |
|---|---|---|
| `GitExecutablePath` | `string?` | Override path to git.exe; null = use PATH |
| `RepositoryPath` | `string` | Absolute path to the Git working tree (VCI workspace) |
| `DefaultRemote` | `string` | Default remote name, typically "origin" |
| `MaxLogEntries` | `int` | Number of commits to load in history (default 200) |
| `Version` | `int` | Config schema version for forward compat |

### FileStatusEntry

| Field | Type | Description |
|---|---|---|
| `FilePath` | `string` | Relative path from repo root |
| `IndexStatus` | `FileStatus` enum | Status in staging area (Added, Modified, Deleted, Renamed, Untracked, Ignored, Conflicted) |
| `WorkTreeStatus` | `FileStatus` enum | Status in working tree |
| `OldFilePath` | `string?` | Previous path if renamed |

### GitStatus

| Field | Type | Description |
|---|---|---|
| `Branch` | `string` | Current branch name |
| `TrackingBranch` | `string?` | Upstream tracking branch |
| `Ahead` | `int` | Commits ahead of upstream |
| `Behind` | `int` | Commits behind upstream |
| `Entries` | `List<FileStatusEntry>` | All file status entries |
| `HasConflicts` | `bool` | Computed: any entry with Conflicted status |

### CommitInfo

| Field | Type | Description |
|---|---|---|
| `Hash` | `string` | Full SHA-1 hash |
| `ShortHash` | `string` | First 8 chars |
| `Author` | `string` | Author name |
| `AuthorEmail` | `string` | Author email |
| `Date` | `DateTimeOffset` | Author date |
| `Subject` | `string` | First line of commit message |
| `Body` | `string?` | Remaining commit message |
| `ParentHashes` | `List<string>` | Parent commit hashes |
| `ChangedFiles` | `List<string>` | File paths changed (populated on demand) |

### BranchInfo

| Field | Type | Description |
|---|---|---|
| `Name` | `string` | Branch name |
| `IsCurrent` | `bool` | Whether this is HEAD |
| `IsRemote` | `bool` | Whether this is a remote-tracking branch |
| `TrackingBranch` | `string?` | Upstream tracking ref |
| `Ahead` | `int` | Commits ahead |
| `Behind` | `int` | Commits behind |

### DiffResult / DiffEntry / DiffHunk / DiffLine

| Entity | Fields |
|---|---|
| `DiffResult` | `List<DiffEntry> Entries`, `string? CommitHash`, `bool IsWorkingTreeDiff` |
| `DiffEntry` | `string FilePath`, `string? OldFilePath`, `ChangeType Type` (Add/Modify/Delete/Rename), `List<DiffHunk> Hunks`, `bool IsBinary` |
| `DiffHunk` | `string Header`, `int OldStart`, `int OldCount`, `int NewStart`, `int NewCount`, `List<DiffLine> Lines` |
| `DiffLine` | `string Content`, `DiffLineType Type` (Context/Add/Delete), `int? OldLineNumber`, `int? NewLineNumber` |

### OperationResult

| Field | Type | Description |
|---|---|---|
| `Success` | `bool` | Whether operation succeeded |
| `Message` | `string` | User-facing message |
| `Detail` | `string?` | Full git stderr/stdout for diagnostics |
| `ExitCode` | `int` | Git process exit code |

### RemoteInfo

| Field | Type | Description |
|---|---|---|
| `Name` | `string` | Remote name (e.g., "origin") |
| `FetchUrl` | `string` | Fetch URL |
| `PushUrl` | `string` | Push URL |

## External Integrations

| Name | Purpose | Auth Method |
|---|---|---|
| TIA Portal V21 (Siemens.Engineering.AddIn.dll) | Add-In lifecycle, context menu registration, UI hosting | N/A (in-process COM/.NET) |
| TIA Portal V21 Openness (Siemens.Engineering.dll) | Access project structure, VCI workspace path | N/A (in-process COM/.NET) |
| Local git.exe | All Git operations (status, commit, push, etc.) | User's pre-configured Git credential helper (not managed by add-in) |

## Key Interfaces

### IGitService

```csharp
namespace TiaGitAddIn.Services
{
    public interface IGitService
    {
        Task<GitStatus> GetStatusAsync(CancellationToken ct = default);
        Task<OperationResult> StageAsync(IReadOnlyList<string> paths, CancellationToken ct = default);
        Task<OperationResult> UnstageAsync(IReadOnlyList<string> paths, CancellationToken ct = default);
        Task<OperationResult> StageAllAsync(CancellationToken ct = default);
        Task<OperationResult> CommitAsync(string message, CancellationToken ct = default);
        Task<OperationResult> FetchAsync(string? remote = null, CancellationToken ct = default);
        Task<OperationResult> PullAsync(string? remote = null, string? branch = null, CancellationToken ct = default);
        Task<OperationResult> PushAsync(string? remote = null, string? branch = null, CancellationToken ct = default);
        Task<IReadOnlyList<BranchInfo>> GetBranchesAsync(CancellationToken ct = default);
        Task<OperationResult> CreateBranchAsync(string name, CancellationToken ct = default);
        Task<OperationResult> SwitchBranchAsync(string name, CancellationToken ct = default);
        Task<IReadOnlyList<CommitInfo>> GetLogAsync(int maxCount = 200, CancellationToken ct = default);
        Task<DiffResult> GetWorkingTreeDiffAsync(CancellationToken ct = default);
        Task<DiffResult> GetCommitDiffAsync(string commitHash, CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetCommitFilesAsync(string commitHash, CancellationToken ct = default);
        Task<OperationResult> InitAsync(string path, CancellationToken ct = default);
        Task<IReadOnlyList<RemoteInfo>> GetRemotesAsync(CancellationToken ct = default);
    }
}
```

### IGitProcessRunner

```csharp
namespace TiaGitAddIn.Services
{
    public interface IGitProcessRunner
    {
        Task<ProcessResult> RunAsync(
            string arguments,
            string workingDirectory,
            CancellationToken ct = default,
            int timeoutMs = 30000);
    }

    public record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}
```

### IConfigurationService

```csharp
namespace TiaGitAddIn.Configuration
{
    public interface IConfigurationService
    {
        GitConfiguration Load(string workspacePath);
        void Save(string workspacePath, GitConfiguration config);
        string GetConfigFilePath(string workspacePath);
    }
}
```

### IVciWorkspaceLocator

```csharp
namespace TiaGitAddIn.Services
{
    public interface IVciWorkspaceLocator
    {
        string? GetWorkspacePath(object tiaProject);
        bool IsVciWorkspaceValid(string path);
    }
}
```

### IRepositoryDiscovery

```csharp
namespace TiaGitAddIn.Services
{
    public interface IRepositoryDiscovery
    {
        string? FindGitRoot(string startPath);
        bool IsGitRepository(string path);
        Task<OperationResult> InitializeRepository(string path, IGitService gitService);
    }
}
```

## Git Operations Implementation Detail

All operations go through `GitProcessRunner` which:
1. Validates `gitExePath` via `PathValidator` (must end in `git.exe` or `git`, no path traversal).
2. Creates `ProcessStartInfo` with `RedirectStandardOutput = true`, `RedirectStandardError = true`, `UseShellExecute = false`, `CreateNoWindow = true`.
3. Applies working directory.
4. Reads stdout/stderr asynchronously to avoid deadlocks.
5. Enforces timeout via `CancellationToken` + `Process.Kill()`.
6. Returns `ProcessResult` record.

### Command construction (in GitService)

| Operation | Git Command |
|---|---|
| Status | `git status --porcelain=v2 --branch` |
| Stage files | `git add -- <paths>` |
| Unstage files | `git restore --staged -- <paths>` |
| Stage all | `git add -A` |
| Commit | `git commit -m "<message>"` (message escaped, no shell) |
| Fetch | `git fetch <remote>` |
| Pull | `git pull <remote> <branch>` |
| Push | `git push <remote> <branch>` |
| Branches | `git branch -a --format="%(refname:short) %(upstream:short) %(upstream:track)"` |
| Create branch | `git branch <name>` |
| Switch branch | `git switch <name>` |
| Log | `git log --format="%H%x00%an%x00%ae%x00%aI%x00%s%x00%b%x00%P" -n <count>` |
| Working tree diff | `git diff HEAD` |
| Commit diff | `git diff <hash>^..<hash>` |
| Commit files | `git diff-tree --no-commit-id --name-status -r <hash>` |
| Init | `git init` |
| Remotes | `git remote -v` |

**Security:** Arguments are passed via `ProcessStartInfo.Arguments`, never via shell. Paths are validated. Commit messages are passed via `-m` with proper escaping (or via `--file` with temp file for multi-line). No user input is interpolated into a shell command string.

## Diff Viewer Strategy

### Text files
- Use **DiffPlex** NuGet package to compute inline diffs from `git diff` output or from raw file content.
- Display in a WPF `ItemsControl` with line-by-line coloring (green = added, red = deleted, gray = context).
- Support unified view (default) and side-by-side view (toggle).

### TIA-aware artifacts (LAD/FBD blocks)
- VCI workspace exports LAD/FBD blocks as XML files.
- Parse the XML to extract structured metadata: block name, network count, instruction count, comment changes.
- Show a **structured comparison summary** (table: what changed per network/instruction) rather than raw XML diff.
- If TIA Portal V21 exposes `ICompareService` or similar in-process API (to be investigated at implementation time), hook into it. Otherwise, fall back to the structured XML summary.
- Clearly label the fallback: "Graphical LAD/FBD comparison not available. Showing structured change summary."

## Error Handling and Operation Serialization

### OperationSerializer
- Uses `SemaphoreSlim(1, 1)` to serialize all Git operations.
- `AcquireAsync(CancellationToken)` attempts to acquire; if already held, returns false or throws `GitOperationInProgressException`.
- UI disables action buttons while an operation is in progress.
- Each ViewModel command wraps calls in `using (await _serializer.AcquireAsync(ct))`.

### Error categories
| Category | Handling |
|---|---|
| git.exe not found | Show settings dialog to configure path; log warning |
| Non-zero exit code | Parse stderr; show user message; store detail in OperationResult |
| Merge conflicts | Parse status for conflict markers; show conflict file list with instructions |
| Network errors (fetch/pull/push) | Show stderr message; suggest checking credentials/remote config |
| Timeout | Kill process; show timeout message with configurable duration |
| Path validation failure | Block operation; show which path failed and why |
| Concurrent operation | Reject with "Operation already in progress" message |

### Logging
- All git commands logged with arguments (sanitized), exit code, elapsed time.
- Errors logged with full stderr.
- Log files in `%APPDATA%/TiaGitAddIn/logs/` with daily rotation.

## Testing Strategy

### What to test (unit tests, xUnit + Moq)

| Test Area | What to Mock | What to Assert |
|---|---|---|
| `GitOutputParser` | Nothing (pure functions) | Correct parsing of `--porcelain=v2` status, `--format` log, unified diff output |
| `GitService` | `IGitProcessRunner` | Correct command construction, correct model mapping from ProcessResult |
| `RepositoryDiscovery` | Filesystem (via interface or temp dirs) | `.git` directory detection, traversal boundary |
| `PathValidator` | Nothing (static) | Rejects `..`, NUL, overlong, invalid chars; accepts valid absolute/relative paths |
| `ConfigurationService` | Filesystem (temp directory) | Roundtrip save/load, default values, corrupt file handling |
| `OperationSerializer` | Nothing | Concurrent acquire fails; release allows next acquire |
| `StatusViewModel` | `IGitService` | Stage/unstage commands enable/disable correctly, list updates on refresh |
| `CommitViewModel` | `IGitService` | Empty message blocked, whitespace-only blocked, commit executes |

### What NOT to test
- Siemens.Engineering API calls (requires running TIA Portal).
- Actual git.exe invocation (integration test, not unit test).
- WPF rendering.

### Test project structure
- `TiaGitAddIn.Tests` project references `TiaGitAddIn` project.
- No reference to Siemens assemblies in test project.
- All Siemens-dependent code is behind interfaces (`IVciWorkspaceLocator`).

## Build and Packaging

### Build process
1. Developer opens `TiaGitAddIn.sln` in Visual Studio 2022.
2. NuGet restore pulls `Siemens.Collaboration.Net.TiaPortal.AddIn.Build`, `DiffPlex`, `xUnit`, `Moq`.
3. Build compiles `TiaGitAddIn.dll` targeting `net48`.
4. Post-build event (configured by AddIn.Build package) invokes `Siemens.Engineering.AddIn.Publisher.exe` with `AddInPublisherConfiguration.xml` to produce `TiaGitAddIn.addin`.
5. Output: `src/TiaGitAddIn/bin/Release/TiaGitAddIn.addin`.

### AddInPublisherConfiguration.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<AddInPublisherConfiguration>
  <FeatureAssembly>TiaGitAddIn.dll</FeatureAssembly>
  <AdditionalAssemblies>
    <Assembly>DiffPlex.dll</Assembly>
  </AdditionalAssemblies>
  <AddInInformation>
    <Name>TIA Git Add-In</Name>
    <Description>Git version control integration for TIA Portal V21</Description>
    <Version>1.0.0</Version>
    <Author>SciTeeX</Author>
    <RequiredTIAPortalVersion>21.0</RequiredTIAPortalVersion>
  </AddInInformation>
</AddInPublisherConfiguration>
```

### GitHub release
- `.addin` file is the sole release artifact.
- Users copy it to `%ProgramFiles%\Siemens\Automation\Portal V21\AddIns\` or user-level AddIns folder.
- No installer.

## Implementation Order

Tasks are ordered by dependency. Each wave can be implemented in parallel within the wave.

### Wave 1: Foundation (no dependencies)
1. **Solution scaffold** -- Create .sln, .csproj files, directory structure, NuGet references, AddInPublisherConfiguration.xml.
2. **Data models** -- All POCOs in Models/ folder. No dependencies on anything.
3. **PathValidator + tests** -- Static validation, independent of everything.

### Wave 2: Core services (depends on Wave 1)
4. **GitProcessRunner + interface** -- Process execution wrapper. Depends on PathValidator.
5. **GitOutputParser + tests** -- Pure parsing functions. Depends on Models.
6. **ConfigurationService + tests** -- JSON persistence. Depends on Models, PathValidator.

### Wave 3: Git service (depends on Wave 2)
7. **GitService + tests** -- Full IGitService implementation. Depends on GitProcessRunner, GitOutputParser, Models.
8. **RepositoryDiscovery + tests** -- Repo detection. Depends on PathValidator, IGitService.
9. **OperationSerializer + tests** -- Concurrency guard. No code dependency but logically pairs with GitService.

### Wave 4: TIA integration (depends on Wave 1 models, Wave 3 services)
10. **VciWorkspaceLocator** -- VCI workspace-view object path integration. Depends on VCI workspace objects or reflected equivalents.
11. **Logging** -- IAddInLogger + FileLogger. Independent but used by all layers from here on.

### Wave 5: UI infrastructure (depends on Wave 3)
12. **ViewModelBase, RelayCommand, AsyncRelayCommand, Converters** -- MVVM plumbing. No service deps.
13. **SettingsViewModel + SettingsView** -- First complete view; tests config flow end-to-end.

### Wave 6: Main UI views (depends on Wave 5)
14. **StatusViewModel + StatusView** -- File status display, stage/unstage.
15. **CommitViewModel + CommitView** -- Commit message + execute.
16. **BranchViewModel + BranchView** -- Branch list, create, switch.

### Wave 7: History and diff (depends on Wave 6)
17. **HistoryViewModel + HistoryView** -- Commit log, commit selection.
18. **DiffViewModel + DiffView** -- Text diff rendering, TIA-aware fallback summary.

### Wave 8: Integration shell (depends on all above)
19. **GitPanelWindow + MainViewModel** -- Tab host, wires current child VMs, manages shared state.
20. **TIA VCI Add-In entry point** -- VciEditor provider, VCI workspace-view provider, WorkspaceFile/WorkspaceFolder menu. Wires everything, registers with TIA Portal's VCI workspace view.

### Wave 9: Polish and packaging
21. **ProgressOverlay + long-running operation UX** -- Cancel support, busy indicators.
22. **End-to-end manual test protocol + packaging verification** -- Build .addin, verify load in TIA Portal.

## Open Questions

1. **TIA Portal V21 Add-In UI surfaces**: Does V21 support dockable panes (`DockablePane` or `NavigationPaneAddIn`) in addition to context menus and modal dialogs? The architecture assumes modal dialog launched from context menu. If dockable panes are available, the MainDialog should become a dockable pane instead.

2. **TIA Portal V21 compare API**: Does `Siemens.Engineering` V21 expose any in-process compare/diff API (e.g., `ICompareService`, `CompareBlocks`) that can render LAD/FBD differences inside the Add-In without opening an external window? This determines whether DiffView can offer graphical block comparison or must fall back to structured XML summary.

3. **VCI workspace path discovery**: Resolved for current implementation. Use VCI workspace-view objects instead of project-tree objects: `WorkspaceFolder.DirectoryInfo` and `WorkspaceFile.FileInfo` expose the filesystem context needed to discover the Git repository. Project-tree objects are not the correct integration point for the Git panel.

4. **AddIn.Build NuGet package V21 version**: The latest known `Siemens.Collaboration.Net.TiaPortal.AddIn.Build` on NuGet is V17. Is there a V21-specific build package, or does V17 work with V21? If neither, the post-build publisher invocation must be manually configured.

5. **WPF hosting constraints**: Resolved for current implementation. TIA Add-In callbacks must return promptly; the panel is shown on a separate STA thread. WPF `Window` construction requires `System.Security.Permissions.SecurityPermission.UnmanagedCode` in the Add-In package.

6. **Merge conflict UX**: When `git status` reports conflicted files after a pull, should the add-in offer a "mark resolved" action (via `git add`) or only display the conflict and expect the user to resolve externally?

7. **.NET Framework version confirmation**: Search results indicate V21 Openness targets net48. Must confirm this is also the correct target for the AddIn assembly itself (not net6.0 or net8.0).

---

## Status

COMPLETE
