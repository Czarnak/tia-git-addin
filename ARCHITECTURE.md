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
| `GitAddInProvider` | Registers context menu providers with TIA Portal | `GitAddIn`, Siemens.Engineering.AddIn | `GetContextMenuAddIns()` |
| `ProjectTreeMenu` | Builds right-click menu items ("Open Git Panel", "Git Status", etc.) | `GitAddInProvider`, UI layer | Context menu items |
| `VciWorkspaceLocator` | Resolves VCI workspace filesystem path from TIA Portal project object | Siemens.Engineering (Openness) | `GetWorkspacePath(Project): string` |
| `RepositoryDiscovery` | Finds `.git` directory, validates repo state, detects init-needed | `PathValidator` | `FindRepository(path): RepoInfo?`, `InitRepository(path)` |
| `GitProcessRunner` | Executes git.exe with arguments, captures stdout/stderr, enforces timeout | `PathValidator` | `RunAsync(args, workDir, cancel): ProcessResult` |
| `GitOutputParser` | Parses porcelain output of `git status`, `git log`, `git diff` into models | None (pure functions) | Static parse methods |
| `GitService` | Orchestrates Git operations: status, stage, unstage, commit, fetch, pull, push, branch, log, diff | `IGitProcessRunner`, `GitOutputParser` | `IGitService` interface |
| `OperationSerializer` | Ensures only one Git operation runs at a time; queues or rejects concurrent requests | None (SemaphoreSlim) | `AcquireAsync()`, `Release()` |
| `ConfigurationService` | Loads/saves `.tia-git-addin.json` from VCI workspace root | `PathValidator`, System.Text.Json | `IConfigurationService` |
| `PathValidator` | Validates filesystem paths; rejects traversal attacks, invalid chars, excessive length | None (static) | `Validate(path): ValidationResult` |
| `MainDialog` | Primary WPF dialog window; hosts tabbed views | All ViewModels | `ShowDialog()` |
| `MainViewModel` | Coordinates tabs, holds shared state (repo path, branch), dispatches refresh | `IGitService`, `IConfigurationService`, child VMs | Properties, commands |
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
- [x] Add minimal project-tree Add-In entry point.
- [x] Add `Open Git Panel...` project-tree context menu action.
- [x] Verify TIA Portal V21 loads the generated `.addin` package.
- [x] Add Git data models: `GitStatus`, `FileStatusEntry`, `CommitInfo`, `BranchInfo`, `RemoteInfo`, `OperationResult`, and diff model classes.
- [x] Add configuration model and `.tia-git-addin.json` persistence service.
- [x] Add path validation for repository paths and `git.exe`.
- [x] Add low-level Git process runner with timeout, cancellation, stdout/stderr capture, and Windows-safe argument escaping.
- [x] Add Git output parser for status, log, branches, and remotes.
- [x] Add repository discovery from a workspace path.
- [x] Add VCI workspace locator baseline for reflected path/directory/file properties.
- [x] Add operation serializer to prevent concurrent Git operations.
- [x] Add unit tests for current model, configuration, parser, path validation, argument escaping, operation serializer, and workspace locator behavior.
- [x] Update `README.md` with build/test instructions and roadmap.
- [x] Update `graphify-out` after code/documentation changes.

### Partial

- [ ] `IGitService` and `GitService` core operations.
  - Done: status, stage one file, unstage one file, commit, branch listing, checkout, log, remote listing.
  - Pending: stage multiple files, stage all, fetch, pull, push, create branch, working tree diff, commit diff, commit file list, repository init.
- [ ] Add-In entry classes.
  - Done: V21 project-tree provider/menu shell and placeholder dialog.
  - Pending: wire menu action to repository discovery, configuration loading, and WPF main dialog.
- [ ] VCI workspace discovery.
  - Done: baseline reflected path handling.
  - Pending: live TIA Portal project/VCI object traversal and user guidance when VCI setup is missing.
- [ ] Configuration handling.
  - Done: load/save/recover malformed config.
  - Pending: settings UI, startup validation, and migration/versioning strategy if config shape changes.

### Not Started

- [ ] Main WPF dialog shell.
- [ ] `MainViewModel` and tab coordination.
- [ ] Status view and `StatusViewModel`.
- [ ] Commit view and `CommitViewModel`.
- [ ] History view and `HistoryViewModel`.
- [ ] Diff view and `DiffViewModel`.
- [ ] Branch view and `BranchViewModel`.
- [ ] Settings view and `SettingsViewModel`.
- [ ] Shared WPF commands: `RelayCommand` and `AsyncRelayCommand`.
- [ ] WPF converters for file status, visibility, and diff line styling.
- [ ] Progress/cancel overlay for long-running Git operations.
- [ ] File logger under `%APPDATA%/TiaGitAddIn/logs`.
- [ ] Full diff parsing/rendering.
- [ ] Remote operations: fetch, pull, and push.
- [ ] Branch creation workflow.
- [ ] Merge conflict UI and conflict-specific guidance.
- [ ] End-to-end TIA Portal V21 workflow test with a real VCI workspace.
- [ ] Integration tests for Git command assembly through `GitService`.
- [ ] Packaging/distribution review for final permissions and deployment process.

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
10. **VciWorkspaceLocator** -- Siemens Openness API integration. Depends on Siemens.Engineering.
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
19. **MainDialog + MainViewModel** -- Tab host, wires all child VMs, manages shared state.
20. **TIA Add-In entry point** -- GitAddIn, GitAddInProvider, ProjectTreeMenu. Wires everything, registers with TIA Portal.

### Wave 9: Polish and packaging
21. **ProgressOverlay + long-running operation UX** -- Cancel support, busy indicators.
22. **End-to-end manual test protocol + packaging verification** -- Build .addin, verify load in TIA Portal.

## Open Questions

1. **TIA Portal V21 Add-In UI surfaces**: Does V21 support dockable panes (`DockablePane` or `NavigationPaneAddIn`) in addition to context menus and modal dialogs? The architecture assumes modal dialog launched from context menu. If dockable panes are available, the MainDialog should become a dockable pane instead.

2. **TIA Portal V21 compare API**: Does `Siemens.Engineering` V21 expose any in-process compare/diff API (e.g., `ICompareService`, `CompareBlocks`) that can render LAD/FBD differences inside the Add-In without opening an external window? This determines whether DiffView can offer graphical block comparison or must fall back to structured XML summary.

3. **VCI workspace path discovery**: What is the exact Openness API call chain to get the VCI workspace filesystem path from a `Project` object? Candidates: `project.Path`, VCI-related properties, or a combination of project path heuristics. Must be verified against V21 Openness API docs or by inspecting the assembly.

4. **AddIn.Build NuGet package V21 version**: The latest known `Siemens.Collaboration.Net.TiaPortal.AddIn.Build` on NuGet is V17. Is there a V21-specific build package, or does V17 work with V21? If neither, the post-build publisher invocation must be manually configured.

5. **WPF hosting constraints**: Are there thread apartment or dispatcher constraints when TIA Portal hosts WPF content from an Add-In? Does the Add-In run on the TIA Portal UI thread or a background thread?

6. **Merge conflict UX**: When `git status` reports conflicted files after a pull, should the add-in offer a "mark resolved" action (via `git add`) or only display the conflict and expect the user to resolve externally?

7. **.NET Framework version confirmation**: Search results indicate V21 Openness targets net48. Must confirm this is also the correct target for the AddIn assembly itself (not net6.0 or net8.0).

---

## Task Breakdown (YAML)

```yaml
tasks:
  - id: task_001
    title: "Solution scaffold and project structure"
    wave: 1
    description: |
      Create the Visual Studio solution and project files:
      - TiaGitAddIn.sln at repo root
      - src/TiaGitAddIn/TiaGitAddIn.csproj targeting net48, referencing Siemens.Collaboration.Net.TiaPortal.AddIn.Build NuGet, DiffPlex NuGet, System.Text.Json NuGet
      - src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj targeting net48, referencing xUnit, Moq, and the main project
      - AddInPublisherConfiguration.xml with metadata from Architecture doc
      - Create all empty directories: Entry/, Services/, Models/, Configuration/, UI/Views/, UI/ViewModels/, UI/Converters/, Logging/
      - .editorconfig with C# conventions
    inputs:
      - ARCHITECTURE.md (this file)
    outputs:
      - TiaGitAddIn.sln
      - src/TiaGitAddIn/TiaGitAddIn.csproj
      - src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj
      - src/TiaGitAddIn/AddInPublisherConfiguration.xml
      - .editorconfig
    acceptance_criteria:
      - "Solution opens in Visual Studio 2022 without errors"
      - "dotnet restore succeeds (NuGet packages resolve)"
      - "dotnet build succeeds (empty project compiles)"
      - "Test project references main project"
    depends_on: []

  - id: task_002
    title: "Data models (all POCOs)"
    wave: 1
    description: |
      Create all model classes in src/TiaGitAddIn/Models/:
      - FileStatus enum: Unmodified, Modified, Added, Deleted, Renamed, Copied, Untracked, Ignored, Conflicted
      - ChangeType enum: Add, Modify, Delete, Rename, Copy
      - DiffLineType enum: Context, Add, Delete, Header
      - FileStatusEntry.cs with FilePath, IndexStatus, WorkTreeStatus, OldFilePath
      - GitStatus.cs with Branch, TrackingBranch, Ahead, Behind, Entries list, computed HasConflicts
      - CommitInfo.cs with Hash, ShortHash, Author, AuthorEmail, Date, Subject, Body, ParentHashes, ChangedFiles
      - BranchInfo.cs with Name, IsCurrent, IsRemote, TrackingBranch, Ahead, Behind
      - DiffLine.cs with Content, Type, OldLineNumber, NewLineNumber
      - DiffHunk.cs with Header, OldStart, OldCount, NewStart, NewCount, Lines
      - DiffEntry.cs with FilePath, OldFilePath, Type, Hunks, IsBinary
      - DiffResult.cs with Entries, CommitHash, IsWorkingTreeDiff
      - RemoteInfo.cs with Name, FetchUrl, PushUrl
      - OperationResult.cs with Success, Message, Detail, ExitCode
      - GitConfiguration.cs with GitExecutablePath, RepositoryPath, DefaultRemote, MaxLogEntries, Version
      All classes in namespace TiaGitAddIn.Models. No logic, only properties.
    inputs:
      - ARCHITECTURE.md data model section
    outputs:
      - All .cs files in src/TiaGitAddIn/Models/
    acceptance_criteria:
      - "All model classes compile"
      - "All enums defined with correct values"
      - "Namespace is TiaGitAddIn.Models"
      - "No dependencies on external packages or other project files"
    depends_on: []

  - id: task_003
    title: "PathValidator with unit tests"
    wave: 1
    description: |
      Create src/TiaGitAddIn/Configuration/PathValidator.cs:
      - Static class with method Validate(string path) returning (bool IsValid, string? ErrorMessage)
      - Reject null/empty/whitespace
      - Reject paths containing ".." segments (directory traversal)
      - Reject paths containing NUL or other control characters
      - Reject paths longer than 260 chars (MAX_PATH)
      - Reject paths containing invalid filename chars (except path separators)
      - Accept both / and \ as separators
      - Method ValidateGitExecutable(string path): additionally verify ends with "git.exe" or "git"
      
      Create src/TiaGitAddIn.Tests/Configuration/PathValidatorTests.cs:
      - Test valid absolute path
      - Test valid relative path
      - Test null/empty/whitespace rejection
      - Test ".." traversal rejection
      - Test control character rejection
      - Test overlong path rejection
      - Test git executable path validation
      - At least 12 test cases
    inputs:
      - ARCHITECTURE.md PathValidator description
    outputs:
      - src/TiaGitAddIn/Configuration/PathValidator.cs
      - src/TiaGitAddIn.Tests/Configuration/PathValidatorTests.cs
    acceptance_criteria:
      - "All tests pass"
      - "Traversal paths like 'C:\\repo\\..\\..\\windows\\system32' are rejected"
      - "Valid paths like 'C:\\Users\\Dev\\vci-workspace' are accepted"
      - "NUL-containing paths are rejected"
      - "git.exe validation works for both 'git' and 'git.exe'"
    depends_on: []

  - id: task_004
    title: "GitProcessRunner implementation and interface"
    wave: 2
    description: |
      Create src/TiaGitAddIn/Services/IGitProcessRunner.cs:
      - Interface with RunAsync(string arguments, string workingDirectory, CancellationToken ct, int timeoutMs) returning Task<ProcessResult>
      - ProcessResult record: int ExitCode, string StandardOutput, string StandardError
      
      Create src/TiaGitAddIn/Services/GitProcessRunner.cs:
      - Constructor takes gitExePath (string), validates with PathValidator
      - RunAsync creates ProcessStartInfo: FileName=gitExePath, Arguments=arguments, WorkingDirectory=workingDirectory, RedirectStandardOutput=true, RedirectStandardError=true, UseShellExecute=false, CreateNoWindow=true
      - Reads stdout and stderr asynchronously using Task.WhenAll to prevent deadlock
      - Enforces timeout: registers CancellationToken callback to kill process
      - Returns ProcessResult with exit code, stdout, stderr
      - Throws ArgumentException if path validation fails
      - Default timeoutMs = 30000
    inputs:
      - task_003 (PathValidator)
      - ARCHITECTURE.md GitProcessRunner section
    outputs:
      - src/TiaGitAddIn/Services/IGitProcessRunner.cs
      - src/TiaGitAddIn/Services/GitProcessRunner.cs
    acceptance_criteria:
      - "Compiles without errors"
      - "ProcessStartInfo has CreateNoWindow=true and UseShellExecute=false"
      - "stdout and stderr read concurrently (not sequentially)"
      - "CancellationToken kills the process"
      - "Path validation called in constructor"
    depends_on: [task_001, task_003]

  - id: task_005
    title: "GitOutputParser with unit tests"
    wave: 2
    description: |
      Create src/TiaGitAddIn/Services/GitOutputParser.cs:
      - Static class, namespace TiaGitAddIn.Services
      - ParseStatus(string porcelainV2Output) -> GitStatus: parses `git status --porcelain=v2 --branch` output including branch header lines (# branch.oid, # branch.head, # branch.upstream, # branch.ab) and file entry lines (1/2/u/? prefixes)
      - ParseLog(string logOutput) -> List<CommitInfo>: parses `git log --format="%H%x00%an%x00%ae%x00%aI%x00%s%x00%b%x00%P"` output using NUL separator
      - ParseDiff(string diffOutput) -> DiffResult: parses unified diff output including diff headers, @@ hunk headers, +/- lines
      - ParseBranches(string branchOutput) -> List<BranchInfo>: parses `git branch -a --format` output
      - ParseRemotes(string remoteOutput) -> List<RemoteInfo>: parses `git remote -v` output
      - ParseDiffTree(string diffTreeOutput) -> List<(string Status, string FilePath)>: parses `git diff-tree --name-status` output
      
      Create src/TiaGitAddIn.Tests/Services/GitOutputParserTests.cs:
      - Test ParseStatus with: clean repo, modified files, staged files, untracked files, conflicts, rename, branch header with ahead/behind
      - Test ParseLog with: single commit, multiple commits, merge commit (two parents)
      - Test ParseDiff with: added file, modified file, deleted file, binary file, multiple hunks
      - Test ParseBranches with: local branches, remote branches, current branch marker
      - Test ParseRemotes with: single remote, multiple remotes
      - At least 20 test cases total
    inputs:
      - task_002 (Models)
      - ARCHITECTURE.md command table and data models
    outputs:
      - src/TiaGitAddIn/Services/GitOutputParser.cs
      - src/TiaGitAddIn.Tests/Services/GitOutputParserTests.cs
    acceptance_criteria:
      - "All tests pass"
      - "Porcelain v2 status parsing handles all status codes (M, A, D, R, C, ?, !, u)"
      - "Branch header parsing extracts ahead/behind counts"
      - "Log parsing handles NUL-separated fields correctly"
      - "Diff parsing handles multi-hunk files"
      - "Empty input returns empty collections, not null"
    depends_on: [task_001, task_002]

  - id: task_006
    title: "ConfigurationService with unit tests"
    wave: 2
    description: |
      Create src/TiaGitAddIn/Configuration/IConfigurationService.cs:
      - Load(string workspacePath) -> GitConfiguration
      - Save(string workspacePath, GitConfiguration config) -> void
      - GetConfigFilePath(string workspacePath) -> string
      
      Create src/TiaGitAddIn/Configuration/ConfigurationService.cs:
      - Config file name: ".tia-git-addin.json" in workspace root
      - Load: read file, deserialize with System.Text.Json, return defaults if file missing or corrupt
      - Save: serialize with System.Text.Json (indented), write to file
      - GetConfigFilePath: combines workspace path + config file name, validates via PathValidator
      - Default config: GitExecutablePath=null (use PATH), DefaultRemote="origin", MaxLogEntries=200, Version=1
      
      Create src/TiaGitAddIn.Tests/Configuration/ConfigurationServiceTests.cs:
      - Test load from nonexistent file returns defaults
      - Test save then load roundtrip preserves all fields
      - Test corrupt JSON file returns defaults without throwing
      - Test GetConfigFilePath returns correct path
      - Test path validation on workspace path
    inputs:
      - task_002 (GitConfiguration model)
      - task_003 (PathValidator)
    outputs:
      - src/TiaGitAddIn/Configuration/IConfigurationService.cs
      - src/TiaGitAddIn/Configuration/ConfigurationService.cs
      - src/TiaGitAddIn.Tests/Configuration/ConfigurationServiceTests.cs
    acceptance_criteria:
      - "All tests pass"
      - "Missing config file returns valid default GitConfiguration"
      - "Corrupt JSON does not throw; returns defaults"
      - "Roundtrip preserves all properties"
      - "Config file is written as indented JSON"
    depends_on: [task_001, task_002, task_003]

  - id: task_007
    title: "GitService implementation with unit tests"
    wave: 3
    description: |
      Create src/TiaGitAddIn/Services/IGitService.cs with all methods from ARCHITECTURE.md interface definition.
      
      Create src/TiaGitAddIn/Services/GitService.cs:
      - Constructor takes IGitProcessRunner and string workingDirectory
      - Each method constructs the appropriate git command string per the command table in ARCHITECTURE.md
      - Each method calls _runner.RunAsync() then parses output with GitOutputParser
      - Each method wraps result in appropriate model (GitStatus, OperationResult, etc.)
      - CommitAsync: validates message not empty/whitespace; uses -m flag; for multi-line messages writes to temp file and uses --file flag then deletes temp file
      - StageAsync/UnstageAsync: validates paths list not empty; passes paths after -- separator
      - PushAsync/PullAsync/FetchAsync: use default remote "origin" if null
      - Non-zero exit code for query operations (status, log, diff) -> throw GitOperationException
      - Non-zero exit code for mutation operations (commit, push) -> return OperationResult with Success=false
      
      Create src/TiaGitAddIn.Tests/Services/GitServiceTests.cs:
      - Mock IGitProcessRunner to return canned stdout/stderr
      - Test GetStatusAsync constructs correct command and parses result
      - Test StageAsync with single file and multiple files
      - Test CommitAsync with valid message
      - Test CommitAsync rejects empty message
      - Test PushAsync uses default remote
      - Test non-zero exit code produces failure OperationResult
      - At least 15 test cases
    inputs:
      - task_004 (IGitProcessRunner)
      - task_005 (GitOutputParser)
      - task_002 (Models)
    outputs:
      - src/TiaGitAddIn/Services/IGitService.cs
      - src/TiaGitAddIn/Services/GitService.cs
      - src/TiaGitAddIn.Tests/Services/GitServiceTests.cs
    acceptance_criteria:
      - "All tests pass"
      - "All IGitService methods implemented"
      - "Command strings match the command table in ARCHITECTURE.md"
      - "Empty commit message throws or returns failure"
      - "Paths passed after -- separator"
      - "Default remote 'origin' used when null"
    depends_on: [task_004, task_005]

  - id: task_008
    title: "RepositoryDiscovery and OperationSerializer with tests"
    wave: 3
    description: |
      Create src/TiaGitAddIn/Services/IRepositoryDiscovery.cs and RepositoryDiscovery.cs:
      - FindGitRoot(string startPath): walk up directory tree looking for .git folder; validate with PathValidator; return null if not found or path invalid
      - IsGitRepository(string path): check if path contains .git directory or file
      - InitializeRepository: delegates to IGitService.InitAsync
      
      Create src/TiaGitAddIn/Services/OperationSerializer.cs:
      - Internal SemaphoreSlim(1, 1)
      - AcquireAsync(CancellationToken ct): tries to acquire semaphore; returns IDisposable that releases on Dispose
      - If semaphore already held, throws GitOperationInProgressException (do not queue)
      - Use TryAcquireAsync with 0 timeout to detect contention
      
      Create GitOperationInProgressException.cs in Services/
      
      Create tests:
      - src/TiaGitAddIn.Tests/Services/RepositoryDiscoveryTests.cs: test find .git, test not found, test invalid path
      - src/TiaGitAddIn.Tests/Services/OperationSerializerTests.cs: test single acquire succeeds, test double acquire throws, test release allows re-acquire
    inputs:
      - task_003 (PathValidator)
      - task_007 (IGitService for InitializeRepository)
    outputs:
      - src/TiaGitAddIn/Services/IRepositoryDiscovery.cs
      - src/TiaGitAddIn/Services/RepositoryDiscovery.cs
      - src/TiaGitAddIn/Services/OperationSerializer.cs
      - src/TiaGitAddIn/Services/GitOperationInProgressException.cs
      - src/TiaGitAddIn.Tests/Services/RepositoryDiscoveryTests.cs
      - src/TiaGitAddIn.Tests/Services/OperationSerializerTests.cs
    acceptance_criteria:
      - "All tests pass"
      - "FindGitRoot walks up and finds .git"
      - "FindGitRoot returns null for path with no .git ancestor"
      - "OperationSerializer blocks concurrent acquire"
      - "Disposed handle releases the semaphore"
    depends_on: [task_003, task_007]

  - id: task_009
    title: "Logging infrastructure"
    wave: 4
    description: |
      Create src/TiaGitAddIn/Logging/IAddInLogger.cs:
      - void Info(string message)
      - void Warn(string message)
      - void Error(string message, Exception? ex = null)
      - void Debug(string message)
      
      Create src/TiaGitAddIn/Logging/FileLogger.cs:
      - Writes to %APPDATA%/TiaGitAddIn/logs/tia-git-addin-{date}.log
      - Thread-safe (lock or ConcurrentQueue)
      - Format: [YYYY-MM-DD HH:mm:ss.fff] [LEVEL] message
      - Auto-creates directory if missing
      - Implements IDisposable to flush
    inputs:
      - ARCHITECTURE.md logging section
    outputs:
      - src/TiaGitAddIn/Logging/IAddInLogger.cs
      - src/TiaGitAddIn/Logging/FileLogger.cs
    acceptance_criteria:
      - "Compiles without errors"
      - "Creates log directory if missing"
      - "Thread-safe write operations"
      - "Log format includes timestamp, level, message"
    depends_on: [task_001]

  - id: task_010
    title: "VciWorkspaceLocator (TIA Openness integration)"
    wave: 4
    description: |
      Create src/TiaGitAddIn/Services/IVciWorkspaceLocator.cs and VciWorkspaceLocator.cs:
      - GetWorkspacePath(object tiaProject): accepts TiaPortal Project object (typed as object to keep interface testable), casts to Siemens.Engineering.Project, accesses project.Path property and VCI workspace configuration
      - Strategy: The VCI workspace is typically a sibling directory or subdirectory of the TIA project. Use project.Path to derive workspace location. Provide a fallback that reads the workspace path from GitConfiguration if Openness API does not directly expose it.
      - IsVciWorkspaceValid(string path): checks directory exists and looks reasonable (contains expected VCI folder structure markers)
      - All Siemens.Engineering API usage isolated in this one class
      - Wrap Siemens API calls in try/catch; log and return null on failure
    inputs:
      - task_002 (Models)
      - task_009 (IAddInLogger)
      - Siemens.Engineering.dll reference (from NuGet/TIA installation)
    outputs:
      - src/TiaGitAddIn/Services/IVciWorkspaceLocator.cs
      - src/TiaGitAddIn/Services/VciWorkspaceLocator.cs
    acceptance_criteria:
      - "Compiles against Siemens.Engineering assembly"
      - "All Siemens API access in try/catch"
      - "Returns null on failure, does not throw"
      - "Interface does not reference Siemens types (uses object)"
    depends_on: [task_001, task_002, task_009]

  - id: task_011
    title: "MVVM infrastructure (ViewModelBase, commands, converters)"
    wave: 5
    description: |
      Create src/TiaGitAddIn/UI/ViewModels/ViewModelBase.cs:
      - Implements INotifyPropertyChanged
      - Protected SetProperty<T>(ref T field, T value, [CallerMemberName] string prop)
      - Protected OnPropertyChanged(string propertyName)
      
      Create src/TiaGitAddIn/UI/ViewModels/RelayCommand.cs:
      - Implements ICommand
      - Constructor takes Action<object?> execute, Func<object?, bool>? canExecute
      - RaiseCanExecuteChanged() method
      
      Create src/TiaGitAddIn/UI/ViewModels/AsyncRelayCommand.cs:
      - Implements ICommand
      - Constructor takes Func<object?, Task> execute, Func<object?, bool>? canExecute
      - IsBusy property (disables while executing)
      - Exception property for error capture
      - Automatically calls RaiseCanExecuteChanged when busy state changes
      
      Create converters in src/TiaGitAddIn/UI/Converters/:
      - BoolToVisibilityConverter.cs (IValueConverter)
      - FileStatusToColorConverter.cs (maps FileStatus enum to SolidColorBrush)
      - DiffLineTypeToColorConverter.cs (maps DiffLineType to background color: green/red/white/blue)
    inputs:
      - task_002 (Models for enums)
    outputs:
      - src/TiaGitAddIn/UI/ViewModels/ViewModelBase.cs
      - src/TiaGitAddIn/UI/ViewModels/RelayCommand.cs
      - src/TiaGitAddIn/UI/ViewModels/AsyncRelayCommand.cs
      - src/TiaGitAddIn/UI/Converters/BoolToVisibilityConverter.cs
      - src/TiaGitAddIn/UI/Converters/FileStatusToColorConverter.cs
      - src/TiaGitAddIn/UI/Converters/DiffLineTypeToColorConverter.cs
    acceptance_criteria:
      - "All classes compile"
      - "ViewModelBase raises PropertyChanged on SetProperty"
      - "AsyncRelayCommand sets IsBusy during execution"
      - "All converters implement IValueConverter with Convert and ConvertBack"
    depends_on: [task_002]

  - id: task_012
    title: "SettingsViewModel and SettingsView"
    wave: 5
    description: |
      Create src/TiaGitAddIn/UI/ViewModels/SettingsViewModel.cs:
      - Properties: GitExePath, RepositoryPath, DefaultRemote, MaxLogEntries, ValidationMessage
      - BrowseGitExeCommand: opens file dialog for git.exe selection
      - BrowseRepoPathCommand: opens folder dialog for repo path
      - SaveCommand: validates paths with PathValidator, saves via IConfigurationService
      - Real-time validation on property change (PathValidator)
      
      Create src/TiaGitAddIn/UI/Views/SettingsView.xaml + .xaml.cs:
      - TextBox for git.exe path with Browse button
      - TextBox for repository path with Browse button
      - TextBox for default remote
      - NumericUpDown (or TextBox with validation) for max log entries
      - Save button
      - Validation error display
    inputs:
      - task_006 (IConfigurationService)
      - task_011 (MVVM infrastructure)
      - task_003 (PathValidator)
    outputs:
      - src/TiaGitAddIn/UI/ViewModels/SettingsViewModel.cs
      - src/TiaGitAddIn/UI/Views/SettingsView.xaml
      - src/TiaGitAddIn/UI/Views/SettingsView.xaml.cs
    acceptance_criteria:
      - "Compiles without errors"
      - "Path validation shows error message on invalid input"
      - "Save persists configuration via IConfigurationService"
      - "Browse buttons open appropriate dialogs"
    depends_on: [task_006, task_011]

  - id: task_013
    title: "StatusViewModel + StatusView and CommitViewModel + CommitView"
    wave: 6
    description: |
      Create src/TiaGitAddIn/UI/ViewModels/StatusViewModel.cs:
      - ObservableCollection<FileStatusEntry> for staged, unstaged, untracked files
      - SelectedFiles property (multi-select)
      - RefreshCommand: calls IGitService.GetStatusAsync, updates collections
      - StageCommand: calls IGitService.StageAsync with selected paths
      - UnstageCommand: calls IGitService.UnstageAsync with selected paths
      - StageAllCommand: calls IGitService.StageAllAsync
      - Uses OperationSerializer for all commands
      - CurrentBranch, Ahead, Behind, TrackingBranch properties from GitStatus
      
      Create src/TiaGitAddIn/UI/Views/StatusView.xaml + .xaml.cs:
      - Three-section layout: Staged files, Unstaged changes, Untracked files
      - Each section is a ListView with checkboxes for multi-select
      - File status icons/colors via FileStatusToColorConverter
      - Stage/Unstage/Stage All buttons
      - Branch and tracking info header bar
      
      Create src/TiaGitAddIn/UI/ViewModels/CommitViewModel.cs:
      - CommitMessage property (string, bound to TextBox)
      - CommitCommand: validates message not empty/whitespace, calls IGitService.CommitAsync
      - IsCommitEnabled: computed from CommitMessage validity and staged files count > 0
      - CharacterCount property for commit message
      - LastCommitResult property (OperationResult)
      
      Create src/TiaGitAddIn/UI/Views/CommitView.xaml + .xaml.cs:
      - Multi-line TextBox for commit message
      - Character count label
      - Commit button (disabled when invalid)
      - Last commit result display (success/error)
      
      Create tests:
      - src/TiaGitAddIn.Tests/ViewModels/StatusViewModelTests.cs
      - src/TiaGitAddIn.Tests/ViewModels/CommitViewModelTests.cs
    inputs:
      - task_007 (IGitService)
      - task_008 (OperationSerializer)
      - task_011 (MVVM infrastructure)
    outputs:
      - src/TiaGitAddIn/UI/ViewModels/StatusViewModel.cs
      - src/TiaGitAddIn/UI/Views/StatusView.xaml
      - src/TiaGitAddIn/UI/Views/StatusView.xaml.cs
      - src/TiaGitAddIn/UI/ViewModels/CommitViewModel.cs
      - src/TiaGitAddIn/UI/Views/CommitView.xaml
      - src/TiaGitAddIn/UI/Views/CommitView.xaml.cs
      - src/TiaGitAddIn.Tests/ViewModels/StatusViewModelTests.cs
      - src/TiaGitAddIn.Tests/ViewModels/CommitViewModelTests.cs
    acceptance_criteria:
      - "All tests pass"
      - "StatusViewModel refreshes file lists from IGitService"
      - "Stage/Unstage commands call correct IGitService methods"
      - "CommitViewModel rejects empty/whitespace messages"
      - "Commit button disabled when no staged files or empty message"
      - "XAML compiles and binds to ViewModels"
    depends_on: [task_007, task_008, task_011]

  - id: task_014
    title: "BranchViewModel + BranchView"
    wave: 6
    description: |
      Create src/TiaGitAddIn/UI/ViewModels/BranchViewModel.cs:
      - ObservableCollection<BranchInfo> Branches
      - SelectedBranch property
      - NewBranchName property
      - RefreshCommand: calls IGitService.GetBranchesAsync
      - CreateBranchCommand: validates name, calls IGitService.CreateBranchAsync
      - SwitchBranchCommand: calls IGitService.SwitchBranchAsync
      - RemoteInfo display (fetch/push URLs)
      - FetchCommand, PullCommand, PushCommand
      - Uses OperationSerializer
      - Shows OperationResult after each remote operation
      
      Create src/TiaGitAddIn/UI/Views/BranchView.xaml + .xaml.cs:
      - ListView of branches (local and remote, grouped)
      - Current branch highlighted
      - Create branch: TextBox + Create button
      - Switch branch button
      - Fetch/Pull/Push buttons with result display
      - Ahead/Behind indicators
    inputs:
      - task_007 (IGitService)
      - task_008 (OperationSerializer)
      - task_011 (MVVM infrastructure)
    outputs:
      - src/TiaGitAddIn/UI/ViewModels/BranchViewModel.cs
      - src/TiaGitAddIn/UI/Views/BranchView.xaml
      - src/TiaGitAddIn/UI/Views/BranchView.xaml.cs
    acceptance_criteria:
      - "Compiles without errors"
      - "Branch list loads from IGitService"
      - "Create branch validates non-empty name"
      - "Switch branch updates UI state"
      - "Remote operations show success/failure result"
    depends_on: [task_007, task_008, task_011]

  - id: task_015
    title: "HistoryViewModel + HistoryView"
    wave: 7
    description: |
      Create src/TiaGitAddIn/UI/ViewModels/HistoryViewModel.cs:
      - ObservableCollection<CommitInfo> Commits
      - SelectedCommit property
      - CommitFiles property: List<string> populated when a commit is selected
      - RefreshCommand: calls IGitService.GetLogAsync with configured maxCount
      - On SelectedCommit change: calls IGitService.GetCommitFilesAsync
      - ViewDiffCommand: triggers DiffViewModel to show diff for selected commit
      
      Create src/TiaGitAddIn/UI/Views/HistoryView.xaml + .xaml.cs:
      - DataGrid or ListView for commit log: columns for ShortHash, Subject, Author, Date
      - Detail panel below: full hash, author email, full message body
      - Changed files list for selected commit
      - "View Diff" button for selected commit
    inputs:
      - task_007 (IGitService)
      - task_011 (MVVM infrastructure)
    outputs:
      - src/TiaGitAddIn/UI/ViewModels/HistoryViewModel.cs
      - src/TiaGitAddIn/UI/Views/HistoryView.xaml
      - src/TiaGitAddIn/UI/Views/HistoryView.xaml.cs
    acceptance_criteria:
      - "Compiles without errors"
      - "Commit log populates from IGitService"
      - "Selecting a commit loads its changed files"
      - "View Diff button enabled only when commit selected"
    depends_on: [task_007, task_011]

  - id: task_016
    title: "DiffViewModel + DiffView"
    wave: 7
    description: |
      Create src/TiaGitAddIn/UI/ViewModels/DiffViewModel.cs:
      - DiffResult property
      - SelectedDiffEntry property (which file's diff to show)
      - DiffLines: ObservableCollection<DiffLine> for current file
      - ShowWorkingTreeDiffCommand: calls IGitService.GetWorkingTreeDiffAsync
      - ShowCommitDiffCommand(string hash): calls IGitService.GetCommitDiffAsync
      - IsTiaArtifact(string filePath): heuristic check for LAD/FBD XML files
      - For TIA artifacts: parse XML, show structured summary (block name, network changes, instruction changes) instead of raw diff
      - FallbackMessage property: "Graphical LAD/FBD comparison not available. Showing structured change summary."
      
      Create src/TiaGitAddIn/UI/Views/DiffView.xaml + .xaml.cs:
      - File selector (ComboBox or ListView of changed files)
      - Diff display: ItemsControl with DiffLine template
        - Each line: line number gutter (old | new), content text
        - Background color from DiffLineTypeToColorConverter
        - Monospace font
      - Toggle: unified vs side-by-side (stretch goal; unified is default/required)
      - TIA artifact fallback panel: structured change table when IsTiaArtifact=true
      - Scrollable with virtualization for large diffs
    inputs:
      - task_007 (IGitService)
      - task_011 (MVVM infrastructure, converters)
      - DiffPlex NuGet (for optional in-memory diff computation)
    outputs:
      - src/TiaGitAddIn/UI/ViewModels/DiffViewModel.cs
      - src/TiaGitAddIn/UI/Views/DiffView.xaml
      - src/TiaGitAddIn/UI/Views/DiffView.xaml.cs
    acceptance_criteria:
      - "Compiles without errors"
      - "Text diff renders with line-by-line coloring"
      - "TIA artifact detection returns true for .xml files in expected VCI paths"
      - "Fallback message displayed for TIA artifacts"
      - "Large diffs scroll without freezing (VirtualizingStackPanel)"
    depends_on: [task_007, task_011]

  - id: task_017
    title: "MainDialog + MainViewModel (integration shell)"
    wave: 8
    description: |
      Create src/TiaGitAddIn/UI/ViewModels/MainViewModel.cs:
      - Holds shared state: RepositoryPath, CurrentBranch, IsInitialized, IsBusy
      - Creates and owns child VMs: StatusViewModel, CommitViewModel, HistoryViewModel, DiffViewModel, BranchViewModel, SettingsViewModel
      - Injects IGitService, IConfigurationService, OperationSerializer, IAddInLogger into children
      - InitializeCommand: discover repo, load config, refresh status
      - SelectedTab property for tab navigation
      - RefreshAllCommand: refreshes status + branch info
      - Global error handler: catches unhandled VM exceptions, shows error, logs
      
      Create src/TiaGitAddIn/UI/Views/MainDialog.xaml + .xaml.cs:
      - Window with TabControl
      - Tabs: Status & Commit (combined), Branches & Remotes, History, Diff, Settings
      - Status bar at bottom: current branch, ahead/behind, last operation result
      - Uses MainViewModel as DataContext
      - Window sizing: 900x650 default, resizable
      
      Create src/TiaGitAddIn/UI/Views/ProgressOverlay.xaml + .xaml.cs:
      - Semi-transparent overlay with spinner/progress text
      - Cancel button
      - Bound to MainViewModel.IsBusy and MainViewModel.BusyMessage
    inputs:
      - task_012 (SettingsViewModel/View)
      - task_013 (StatusViewModel/View, CommitViewModel/View)
      - task_014 (BranchViewModel/View)
      - task_015 (HistoryViewModel/View)
      - task_016 (DiffViewModel/View)
    outputs:
      - src/TiaGitAddIn/UI/ViewModels/MainViewModel.cs
      - src/TiaGitAddIn/UI/Views/MainDialog.xaml
      - src/TiaGitAddIn/UI/Views/MainDialog.xaml.cs
      - src/TiaGitAddIn/UI/Views/ProgressOverlay.xaml
      - src/TiaGitAddIn/UI/Views/ProgressOverlay.xaml.cs
    acceptance_criteria:
      - "Compiles without errors"
      - "All tabs present and wired to child views"
      - "Tab switching works"
      - "Status bar shows branch info"
      - "Progress overlay shows during IsBusy"
      - "Global error handler catches and displays exceptions"
    depends_on: [task_012, task_013, task_014, task_015, task_016]

  - id: task_018
    title: "TIA Add-In entry point (GitAddIn, GitAddInProvider, ProjectTreeMenu)"
    wave: 8
    description: |
      Create src/TiaGitAddIn/Entry/GitAddIn.cs:
      - Class inherits from AddInBase (Siemens.Engineering.AddIn)
      - Start() method: store TiaPortal reference, initialize logger, resolve VCI workspace
      - Stop() method: dispose logger, clean up
      - Stores TiaPortal instance for child access
      
      Create src/TiaGitAddIn/Entry/GitAddInProvider.cs:
      - Class inherits from AddInProvider
      - Constructor receives TiaPortal instance
      - GetContextMenuAddIns(): returns list containing ProjectTreeMenu instance
      
      Create src/TiaGitAddIn/Entry/ProjectTreeMenu.cs:
      - Class inherits from ContextMenuAddIn
      - Constructor: receives TiaPortal reference, display name "Git"
      - BuildContextMenuItems(ContextMenuAddInRoot root):
        - "Open Git Panel" -> creates and wires MainDialog, shows it
        - "Git Status" -> quick status popup
        - "Git Commit" -> opens MainDialog on commit tab
      - Click delegates: create service instances (GitProcessRunner, GitService, ConfigurationService, VciWorkspaceLocator), create MainViewModel, create MainDialog, show dialog
      - Error handling: wrap all in try/catch, show MessageBox on fatal errors, log everything
    inputs:
      - task_017 (MainDialog, MainViewModel)
      - task_010 (VciWorkspaceLocator)
      - task_007 (GitService)
      - task_006 (ConfigurationService)
      - task_009 (IAddInLogger)
      - Siemens.Engineering.AddIn.dll
    outputs:
      - src/TiaGitAddIn/Entry/GitAddIn.cs
      - src/TiaGitAddIn/Entry/GitAddInProvider.cs
      - src/TiaGitAddIn/Entry/ProjectTreeMenu.cs
    acceptance_criteria:
      - "Compiles against Siemens.Engineering.AddIn assembly"
      - "GitAddIn inherits AddInBase"
      - "GitAddInProvider returns ProjectTreeMenu"
      - "ProjectTreeMenu creates menu items with correct labels"
      - "Click delegate creates full service graph and opens MainDialog"
      - "All creation wrapped in try/catch with logging"
    depends_on: [task_006, task_007, task_009, task_010, task_017]

  - id: task_019
    title: "Progress overlay UX and long-running operation polish"
    wave: 9
    description: |
      Enhance all AsyncRelayCommand usages across ViewModels:
      - Ensure IsBusy propagates to MainViewModel.IsBusy
      - Ensure BusyMessage is set before each operation ("Fetching...", "Pushing...", "Loading history...")
      - Wire CancellationTokenSource to ProgressOverlay cancel button
      - Add timeout display (elapsed seconds counter)
      - Ensure OperationSerializer rejection shows user-friendly message "Another Git operation is in progress. Please wait."
      - Add auto-refresh after mutation operations (commit, stage, unstage, pull, push, branch switch)
      - Disable all action buttons during IsBusy via CanExecute
    inputs:
      - task_017 (MainDialog with ProgressOverlay)
      - All ViewModels
    outputs:
      - Modified UI/ViewModels/*.cs files (enhanced busy/cancel logic)
      - Modified UI/Views/ProgressOverlay.xaml (timer display)
    acceptance_criteria:
      - "Progress overlay visible during all async operations"
      - "Cancel button cancels the running operation"
      - "All buttons disabled during IsBusy"
      - "Auto-refresh after mutations"
      - "Concurrent operation rejection shows friendly message"
    depends_on: [task_017, task_018]

  - id: task_020
    title: "Build verification and packaging"
    wave: 9
    description: |
      Verify full build pipeline:
      - Run dotnet build on the solution in Release configuration
      - Verify all unit tests pass with dotnet test
      - Verify AddInPublisherConfiguration.xml references correct assemblies
      - Document the post-build step that invokes Siemens.Engineering.AddIn.Publisher.exe
      - Verify the output .addin file is generated (requires TIA Portal installed; document manual steps if not available in CI)
      - Create a BUILDING.md with:
        - Prerequisites (VS2022, TIA Portal V21, .NET Framework 4.8 targeting pack)
        - Build steps
        - How to deploy .addin file
        - How to run tests
      - Tag considerations for GitHub release
    inputs:
      - All previous tasks completed
      - TIA Portal V21 installed (for publisher tool)
    outputs:
      - BUILDING.md
      - Verified clean build
      - Verified test pass
    acceptance_criteria:
      - "dotnet build -c Release succeeds without warnings"
      - "dotnet test passes all tests"
      - "BUILDING.md documents all steps"
      - "AddInPublisherConfiguration.xml is valid"
    depends_on: [task_018, task_019]
```

## Status

COMPLETE
