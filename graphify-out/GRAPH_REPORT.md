# Graph Report - tia-git-addin  (2026-05-04)

## Corpus Check
- 71 files · ~16,913 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 568 nodes · 659 edges · 62 communities detected
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 17 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]
- [[_COMMUNITY_Community 6|Community 6]]
- [[_COMMUNITY_Community 7|Community 7]]
- [[_COMMUNITY_Community 8|Community 8]]
- [[_COMMUNITY_Community 9|Community 9]]
- [[_COMMUNITY_Community 10|Community 10]]
- [[_COMMUNITY_Community 11|Community 11]]
- [[_COMMUNITY_Community 12|Community 12]]
- [[_COMMUNITY_Community 13|Community 13]]
- [[_COMMUNITY_Community 14|Community 14]]
- [[_COMMUNITY_Community 15|Community 15]]
- [[_COMMUNITY_Community 16|Community 16]]
- [[_COMMUNITY_Community 17|Community 17]]
- [[_COMMUNITY_Community 18|Community 18]]
- [[_COMMUNITY_Community 19|Community 19]]
- [[_COMMUNITY_Community 20|Community 20]]
- [[_COMMUNITY_Community 21|Community 21]]
- [[_COMMUNITY_Community 22|Community 22]]
- [[_COMMUNITY_Community 23|Community 23]]
- [[_COMMUNITY_Community 24|Community 24]]
- [[_COMMUNITY_Community 25|Community 25]]
- [[_COMMUNITY_Community 26|Community 26]]
- [[_COMMUNITY_Community 27|Community 27]]
- [[_COMMUNITY_Community 28|Community 28]]
- [[_COMMUNITY_Community 29|Community 29]]
- [[_COMMUNITY_Community 30|Community 30]]
- [[_COMMUNITY_Community 31|Community 31]]
- [[_COMMUNITY_Community 32|Community 32]]
- [[_COMMUNITY_Community 33|Community 33]]
- [[_COMMUNITY_Community 34|Community 34]]
- [[_COMMUNITY_Community 35|Community 35]]
- [[_COMMUNITY_Community 36|Community 36]]
- [[_COMMUNITY_Community 37|Community 37]]
- [[_COMMUNITY_Community 38|Community 38]]
- [[_COMMUNITY_Community 39|Community 39]]
- [[_COMMUNITY_Community 40|Community 40]]
- [[_COMMUNITY_Community 41|Community 41]]
- [[_COMMUNITY_Community 42|Community 42]]
- [[_COMMUNITY_Community 43|Community 43]]
- [[_COMMUNITY_Community 44|Community 44]]
- [[_COMMUNITY_Community 45|Community 45]]
- [[_COMMUNITY_Community 46|Community 46]]
- [[_COMMUNITY_Community 47|Community 47]]
- [[_COMMUNITY_Community 48|Community 48]]
- [[_COMMUNITY_Community 49|Community 49]]
- [[_COMMUNITY_Community 50|Community 50]]
- [[_COMMUNITY_Community 51|Community 51]]
- [[_COMMUNITY_Community 52|Community 52]]
- [[_COMMUNITY_Community 53|Community 53]]
- [[_COMMUNITY_Community 54|Community 54]]
- [[_COMMUNITY_Community 55|Community 55]]
- [[_COMMUNITY_Community 56|Community 56]]
- [[_COMMUNITY_Community 57|Community 57]]
- [[_COMMUNITY_Community 58|Community 58]]
- [[_COMMUNITY_Community 59|Community 59]]
- [[_COMMUNITY_Community 60|Community 60]]
- [[_COMMUNITY_Community 61|Community 61]]

## God Nodes (most connected - your core abstractions)
1. `GitService` - 24 edges
2. `FakeGitService` - 20 edges
3. `FakeGitService` - 20 edges
4. `FakeGitService` - 20 edges
5. `IGitService` - 19 edges
6. `GitOutputParser` - 15 edges
7. `BranchViewModel` - 10 edges
8. `GitPanelWindow` - 10 edges
9. `GitPanelWindow` - 10 edges
10. `StatusViewModel` - 9 edges

## Surprising Connections (you probably didn't know these)
- `FileLogger` --inherits--> `IAddInLogger`  [EXTRACTED]
  TiaGitAddIn\Logging\FileLogger.cs →   _Bridges community 25 → community 1_
- `GitService` --inherits--> `IGitService`  [EXTRACTED]
  TiaGitAddIn\Services\GitService.cs →   _Bridges community 4 → community 3_
- `IGitService` --inherits--> `FakeGitService`  [EXTRACTED]
   → TiaGitAddIn.Tests\UI\GitPanelLaunchServiceTests.cs  _Bridges community 3 → community 1_
- `IGitService` --inherits--> `FakeGitService`  [EXTRACTED]
   → TiaGitAddIn.Tests\UI\StatusViewModelTests.cs  _Bridges community 3 → community 2_
- `VciWorkspaceLocator` --inherits--> `IVciWorkspaceLocator`  [EXTRACTED]
  TiaGitAddIn\Services\VciWorkspaceLocator.cs →   _Bridges community 21 → community 1_

## Communities

### Community 0 - "Community 0"
Cohesion: 0.07
Nodes (15): MainViewModel, TiaGitAddIn.UI, ViewModelBase, BranchViewModel, TiaGitAddIn.UI.ViewModels, CommitViewModel, TiaGitAddIn.UI.ViewModels, MainViewModel (+7 more)

### Community 1 - "Community 1"
Cohesion: 0.05
Nodes (10): IAddInLogger, IVciWorkspaceLocator, FakeGitService, FakeLogger, FakeWorkspaceLocator, GitPanelLaunchServiceTests, NullLogger, ThrowingWorkspaceLocator (+2 more)

### Community 2 - "Community 2"
Cohesion: 0.07
Nodes (3): FakeGitService, StatusViewModelTests, TiaGitAddIn.Tests.UI

### Community 3 - "Community 3"
Cohesion: 0.09
Nodes (4): IGitService, CommitViewModelTests, FakeGitService, TiaGitAddIn.Tests.UI

### Community 4 - "Community 4"
Cohesion: 0.2
Nodes (2): GitService, TiaGitAddIn.Services

### Community 5 - "Community 5"
Cohesion: 0.14
Nodes (5): GitPanelWindow, TiaGitAddIn.UI, GitPanelWindow, TiaGitAddIn.UI.Views, Window

### Community 6 - "Community 6"
Cohesion: 0.1
Nodes (2): IGitService, TiaGitAddIn.Services

### Community 7 - "Community 7"
Cohesion: 0.13
Nodes (7): ContextMenuAddIn, GitProjectTreeMenu, TiaGitAddIn.Entry, GitVciWorkspaceMenu, TiaGitAddIn.Entry, GitPanelLaunchService, TiaGitAddIn.UI

### Community 8 - "Community 8"
Cohesion: 0.21
Nodes (2): GitOutputParser, TiaGitAddIn.Services

### Community 9 - "Community 9"
Cohesion: 0.12
Nodes (7): BoolToVisibilityConverter, TiaGitAddIn.UI.Converters, DiffLineTypeToColorConverter, TiaGitAddIn.UI.Converters, FileStatusToColorConverter, TiaGitAddIn.UI.Converters, IValueConverter

### Community 10 - "Community 10"
Cohesion: 0.13
Nodes (6): IUiDispatcher, ImmediateUiDispatcher, TiaGitAddIn.UI, RecordingUiDispatcher, TiaGitAddIn.UI, WpfUiDispatcher

### Community 11 - "Community 11"
Cohesion: 0.22
Nodes (4): ConfigurationService, TiaGitAddIn.Configuration, IConfigurationService, FakeConfigurationService

### Community 12 - "Community 12"
Cohesion: 0.18
Nodes (5): ICommand, AsyncCommand, TiaGitAddIn.UI, RelayCommand, TiaGitAddIn.UI.ViewModels

### Community 13 - "Community 13"
Cohesion: 0.15
Nodes (9): UserControl, BranchView, TiaGitAddIn.UI.Views, CommitView, TiaGitAddIn.UI.Views, SettingsView, TiaGitAddIn.UI.Views, StatusView (+1 more)

### Community 14 - "Community 14"
Cohesion: 0.18
Nodes (2): PathValidatorTests, TiaGitAddIn.Tests.Configuration

### Community 15 - "Community 15"
Cohesion: 0.22
Nodes (5): INotifyPropertyChanged, CommitViewModel, TiaGitAddIn.UI, TiaGitAddIn.UI, ViewModelBase

### Community 16 - "Community 16"
Cohesion: 0.2
Nodes (5): ProjectWithPath, TiaGitAddIn.Tests.Services, VciWorkspaceLocatorTests, WorkspaceFileLikeContext, WorkspaceFolderLikeContext

### Community 17 - "Community 17"
Cohesion: 0.33
Nodes (3): IGitProcessRunner, GitProcessRunner, TiaGitAddIn.Services

### Community 18 - "Community 18"
Cohesion: 0.22
Nodes (4): IRepositoryDiscovery, RepositoryDiscovery, TiaGitAddIn.Services, FakeRepositoryDiscovery

### Community 19 - "Community 19"
Cohesion: 0.36
Nodes (2): StatusViewModel, TiaGitAddIn.UI

### Community 20 - "Community 20"
Cohesion: 0.29
Nodes (4): IDisposable, Lease, OperationSerializer, TiaGitAddIn.Services

### Community 21 - "Community 21"
Cohesion: 0.39
Nodes (2): TiaGitAddIn.Services, VciWorkspaceLocator

### Community 22 - "Community 22"
Cohesion: 0.43
Nodes (2): MenuSelectionContextResolver, TiaGitAddIn.Entry

### Community 23 - "Community 23"
Cohesion: 0.33
Nodes (3): MenuSelectionContextResolverTests, ProviderWithAmbiguousSelectionMethods, TiaGitAddIn.Tests.Entry

### Community 24 - "Community 24"
Cohesion: 0.29
Nodes (2): GitOutputParserTests, TiaGitAddIn.Tests.Services

### Community 25 - "Community 25"
Cohesion: 0.38
Nodes (2): FileLogger, TiaGitAddIn.Logging

### Community 26 - "Community 26"
Cohesion: 0.33
Nodes (2): ConfigurationServiceTests, TiaGitAddIn.Tests.Configuration

### Community 27 - "Community 27"
Cohesion: 0.4
Nodes (2): IConfigurationService, TiaGitAddIn.Configuration

### Community 28 - "Community 28"
Cohesion: 0.5
Nodes (2): PathValidator, TiaGitAddIn.Configuration

### Community 29 - "Community 29"
Cohesion: 0.4
Nodes (2): TiaGitAddIn.Configuration, ValidationResult

### Community 30 - "Community 30"
Cohesion: 0.4
Nodes (3): GitVciEditorProvider, TiaGitAddIn.Entry, VciEditorAddInProvider

### Community 31 - "Community 31"
Cohesion: 0.4
Nodes (3): GitVciWorkspaceViewProvider, TiaGitAddIn.Entry, VciWorkspaceViewAddInProvider

### Community 32 - "Community 32"
Cohesion: 0.4
Nodes (2): IAddInLogger, TiaGitAddIn.Logging

### Community 33 - "Community 33"
Cohesion: 0.4
Nodes (2): OperationResult, TiaGitAddIn.Models

### Community 34 - "Community 34"
Cohesion: 0.4
Nodes (2): GitPanelLaunchResult, TiaGitAddIn.UI

### Community 35 - "Community 35"
Cohesion: 0.4
Nodes (2): IUiDispatcher, TiaGitAddIn.UI

### Community 36 - "Community 36"
Cohesion: 0.4
Nodes (2): FileStatusItemViewModel, TiaGitAddIn.UI.ViewModels

### Community 37 - "Community 37"
Cohesion: 0.5
Nodes (2): AddInPublisherConfigurationTests, TiaGitAddIn.Tests.Configuration

### Community 38 - "Community 38"
Cohesion: 0.4
Nodes (2): GitStatusTests, TiaGitAddIn.Tests.Models

### Community 39 - "Community 39"
Cohesion: 0.4
Nodes (2): OperationSerializerTests, TiaGitAddIn.Tests.Services

### Community 40 - "Community 40"
Cohesion: 0.4
Nodes (2): FileStatusItemViewModel, TiaGitAddIn.UI

### Community 41 - "Community 41"
Cohesion: 0.4
Nodes (3): GitProjectTreeProvider, TiaGitAddIn.Entry, ProjectTreeAddInProvider

### Community 42 - "Community 42"
Cohesion: 0.5
Nodes (2): GitArgumentEscaper, TiaGitAddIn.Services

### Community 43 - "Community 43"
Cohesion: 0.5
Nodes (3): InvalidOperationException, GitOperationInProgressException, TiaGitAddIn.Services

### Community 44 - "Community 44"
Cohesion: 0.5
Nodes (2): IGitProcessRunner, TiaGitAddIn.Services

### Community 45 - "Community 45"
Cohesion: 0.5
Nodes (2): IRepositoryDiscovery, TiaGitAddIn.Services

### Community 46 - "Community 46"
Cohesion: 0.5
Nodes (2): IVciWorkspaceLocator, TiaGitAddIn.Services

### Community 47 - "Community 47"
Cohesion: 0.5
Nodes (2): GitArgumentEscaperTests, TiaGitAddIn.Tests.Services

### Community 48 - "Community 48"
Cohesion: 0.67
Nodes (2): BranchInfo, TiaGitAddIn.Models

### Community 49 - "Community 49"
Cohesion: 0.67
Nodes (2): CommitInfo, TiaGitAddIn.Models

### Community 50 - "Community 50"
Cohesion: 0.67
Nodes (2): DiffEntry, TiaGitAddIn.Models

### Community 51 - "Community 51"
Cohesion: 0.67
Nodes (2): DiffHunk, TiaGitAddIn.Models

### Community 52 - "Community 52"
Cohesion: 0.67
Nodes (2): DiffLine, TiaGitAddIn.Models

### Community 53 - "Community 53"
Cohesion: 0.67
Nodes (2): DiffResult, TiaGitAddIn.Models

### Community 54 - "Community 54"
Cohesion: 0.67
Nodes (2): FileStatusEntry, TiaGitAddIn.Models

### Community 55 - "Community 55"
Cohesion: 0.67
Nodes (2): GitConfiguration, TiaGitAddIn.Models

### Community 56 - "Community 56"
Cohesion: 0.67
Nodes (2): GitStatus, TiaGitAddIn.Models

### Community 57 - "Community 57"
Cohesion: 0.67
Nodes (2): RemoteInfo, TiaGitAddIn.Models

### Community 58 - "Community 58"
Cohesion: 0.67
Nodes (2): GitProcessResult, TiaGitAddIn.Services

### Community 59 - "Community 59"
Cohesion: 1.0
Nodes (1): TiaGitAddIn.Models

### Community 60 - "Community 60"
Cohesion: 1.0
Nodes (1): TiaGitAddIn.Models

### Community 61 - "Community 61"
Cohesion: 1.0
Nodes (1): TiaGitAddIn.Models

## Knowledge Gaps
- **94 isolated node(s):** `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Entry` (+89 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `Community 4`** (25 nodes): `GitService`, `.CheckoutBranchAsync()`, `.CommitAsync()`, `.CreateBranchAsync()`, `.EnsureSuccess()`, `.FetchAsync()`, `.GetBranchesAsync()`, `.GetCommitDiffAsync()`, `.GetCommitFilesAsync()`, `.GetCommitLogAsync()`, `.GetRemotesAsync()`, `.GetStatusAsync()`, `.GetWorkingTreeDiffAsync()`, `.InitAsync()`, `.PullAsync()`, `.PushAsync()`, `.RunAsync()`, `.RunExclusiveAsync()`, `.StageAllAsync()`, `.StageAsync()`, `.SwitchBranchAsync()`, `.ToOperationResult()`, `.UnstageAsync()`, `TiaGitAddIn.Services`, `GitService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 6`** (21 nodes): `IGitService`, `.CheckoutBranchAsync()`, `.CommitAsync()`, `.CreateBranchAsync()`, `.FetchAsync()`, `.GetBranchesAsync()`, `.GetCommitDiffAsync()`, `.GetCommitFilesAsync()`, `.GetCommitLogAsync()`, `.GetRemotesAsync()`, `.GetStatusAsync()`, `.GetWorkingTreeDiffAsync()`, `.InitAsync()`, `.PullAsync()`, `.PushAsync()`, `.StageAllAsync()`, `.StageAsync()`, `.SwitchBranchAsync()`, `.UnstageAsync()`, `TiaGitAddIn.Services`, `IGitService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 8`** (17 nodes): `GitOutputParser`, `.ApplyAheadBehind()`, `.ApplyBranchStatus()`, `.IsConflictPair()`, `.MapStatus()`, `.ParseBranches()`, `.ParseCommitLog()`, `.ParseDiff()`, `.ParseDiffTree()`, `.ParseHunkHeader()`, `.ParseRemotes()`, `.ParseStatus()`, `.ParseStatusEntry()`, `.ParseUntrackedBranchName()`, `.SplitLines()`, `TiaGitAddIn.Services`, `GitOutputParser.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 14`** (11 nodes): `PathValidatorTests`, `.ValidateAcceptsValidAbsolutePaths()`, `.ValidateAcceptsValidRelativePaths()`, `.ValidateGitExecutableAcceptsGitNames()`, `.ValidateGitExecutableRejectsUnsafeNames()`, `.ValidateRejectsBlankPaths()`, `.ValidateRejectsControlCharacters()`, `.ValidateRejectsOverlongPaths()`, `.ValidateRejectsTraversalPaths()`, `TiaGitAddIn.Tests.Configuration`, `PathValidatorTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 19`** (9 nodes): `StatusViewModel.cs`, `StatusViewModel`, `.BuildOperationMessage()`, `.BuildTrackingSummary()`, `.RaiseCommandStates()`, `.RefreshAsync()`, `.StageSelectedAsync()`, `.UnstageSelectedAsync()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 21`** (8 nodes): `TiaGitAddIn.Services`, `VciWorkspaceLocator`, `.ResolvePath()`, `.ResolveStringPath()`, `.TryGetWorkspacePath()`, `.TryReadProperty()`, `.TryReadStringProperty()`, `VciWorkspaceLocator.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 22`** (7 nodes): `MenuSelectionContextResolver`, `.Normalize()`, `.Resolve()`, `.TryInvoke()`, `.TryRead()`, `TiaGitAddIn.Entry`, `MenuSelectionContextResolver.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 24`** (7 nodes): `GitOutputParserTests`, `.ParseCommitLogSkipsMalformedLines()`, `.ParseRemotesPreservesUrlsWithSpaces()`, `.ParseStatusMapsPorcelainEntries()`, `.ParseStatusReadsFreshRepositoryBranch()`, `TiaGitAddIn.Tests.Services`, `GitOutputParserTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 25`** (7 nodes): `FileLogger`, `.Error()`, `.GetDefaultLogFilePath()`, `.Info()`, `.Write()`, `TiaGitAddIn.Logging`, `FileLogger.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 26`** (6 nodes): `ConfigurationServiceTests`, `.LoadNormalizesMissingRepositoryPath()`, `.LoadReturnsDefaultConfigurationWhenFileIsMalformed()`, `.SaveThenLoadRoundTripsConfiguration()`, `TiaGitAddIn.Tests.Configuration`, `ConfigurationServiceTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 27`** (5 nodes): `IConfigurationService`, `.Load()`, `.Save()`, `TiaGitAddIn.Configuration`, `IConfigurationService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 28`** (5 nodes): `PathValidator`, `.Validate()`, `.ValidateGitExecutablePath()`, `TiaGitAddIn.Configuration`, `PathValidator.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 29`** (5 nodes): `TiaGitAddIn.Configuration`, `ValidationResult`, `.Invalid()`, `.Valid()`, `ValidationResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 32`** (5 nodes): `IAddInLogger`, `.Error()`, `.Info()`, `TiaGitAddIn.Logging`, `IAddInLogger.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 33`** (5 nodes): `OperationResult`, `.Fail()`, `.Ok()`, `TiaGitAddIn.Models`, `OperationResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 34`** (5 nodes): `GitPanelLaunchResult.cs`, `GitPanelLaunchResult`, `.Fail()`, `.Ok()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 35`** (5 nodes): `IUiDispatcher.cs`, `IUiDispatcher`, `.CheckAccess()`, `.Invoke()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 36`** (5 nodes): `FileStatusItemViewModel.cs`, `FileStatusItemViewModel`, `.GetArea()`, `.GetStatusText()`, `TiaGitAddIn.UI.ViewModels`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 37`** (5 nodes): `AddInPublisherConfigurationTests`, `.GetConfigurationPath()`, `.RequiredSecurityPermissionsIncludeWpfWindowPermission()`, `TiaGitAddIn.Tests.Configuration`, `AddInPublisherConfigurationTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 38`** (5 nodes): `GitStatusTests`, `.HasConflictsReturnsFalseForCleanStatus()`, `.HasConflictsReturnsTrueWhenAnyEntryIsConflicted()`, `TiaGitAddIn.Tests.Models`, `GitStatusTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 39`** (5 nodes): `OperationSerializerTests`, `.AcquireAsyncRejectsConcurrentOperationWhenWaitIsDisabled()`, `.DisposeReleasesOperation()`, `TiaGitAddIn.Tests.Services`, `OperationSerializerTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 40`** (5 nodes): `FileStatusItemViewModel.cs`, `FileStatusItemViewModel`, `.GetArea()`, `.GetStatusText()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 42`** (4 nodes): `GitArgumentEscaper`, `.Escape()`, `TiaGitAddIn.Services`, `GitArgumentEscaper.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 44`** (4 nodes): `IGitProcessRunner`, `.RunAsync()`, `TiaGitAddIn.Services`, `IGitProcessRunner.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 45`** (4 nodes): `IRepositoryDiscovery`, `.FindRepositoryRoot()`, `TiaGitAddIn.Services`, `IRepositoryDiscovery.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 46`** (4 nodes): `IVciWorkspaceLocator`, `.TryGetWorkspacePath()`, `TiaGitAddIn.Services`, `IVciWorkspaceLocator.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 47`** (4 nodes): `GitArgumentEscaperTests`, `.EscapePreservesWindowsArgumentMeaning()`, `TiaGitAddIn.Tests.Services`, `GitArgumentEscaperTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 48`** (3 nodes): `BranchInfo`, `TiaGitAddIn.Models`, `BranchInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 49`** (3 nodes): `CommitInfo`, `TiaGitAddIn.Models`, `CommitInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 50`** (3 nodes): `DiffEntry`, `TiaGitAddIn.Models`, `DiffEntry.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 51`** (3 nodes): `DiffHunk`, `TiaGitAddIn.Models`, `DiffHunk.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 52`** (3 nodes): `DiffLine`, `TiaGitAddIn.Models`, `DiffLine.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 53`** (3 nodes): `DiffResult`, `TiaGitAddIn.Models`, `DiffResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 54`** (3 nodes): `FileStatusEntry`, `TiaGitAddIn.Models`, `FileStatusEntry.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 55`** (3 nodes): `GitConfiguration`, `TiaGitAddIn.Models`, `GitConfiguration.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 56`** (3 nodes): `GitStatus`, `TiaGitAddIn.Models`, `GitStatus.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 57`** (3 nodes): `RemoteInfo`, `TiaGitAddIn.Models`, `RemoteInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 58`** (3 nodes): `GitProcessResult`, `TiaGitAddIn.Services`, `GitProcessResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 59`** (2 nodes): `TiaGitAddIn.Models`, `ChangeType.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 60`** (2 nodes): `TiaGitAddIn.Models`, `DiffLineType.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 61`** (2 nodes): `TiaGitAddIn.Models`, `FileStatus.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `FakeGitService` connect `Community 1` to `Community 3`?**
  _High betweenness centrality (0.053) - this node is a cross-community bridge._
- **Why does `FakeGitService` connect `Community 2` to `Community 3`?**
  _High betweenness centrality (0.037) - this node is a cross-community bridge._
- **What connects `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration` to the rest of the system?**
  _94 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.07 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.05 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.07 - nodes in this community are weakly interconnected._
- **Should `Community 3` be split into smaller, more focused modules?**
  _Cohesion score 0.09 - nodes in this community are weakly interconnected._