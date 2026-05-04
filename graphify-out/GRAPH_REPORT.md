# Graph Report - tia-git-addin  (2026-05-04)

## Corpus Check
- 57 files · ~17,398 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 417 nodes · 456 edges · 59 communities detected
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 2 edges (avg confidence: 0.8)
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

## God Nodes (most connected - your core abstractions)
1. `GitService` - 14 edges
2. `GitOutputParser` - 12 edges
3. `GitPanelWindow` - 10 edges
4. `FakeGitService` - 10 edges
5. `FakeGitService` - 10 edges
6. `FakeGitService` - 10 edges
7. `GitPanelWindow` - 10 edges
8. `IGitService` - 9 edges
9. `PathValidatorTests` - 9 edges
10. `GitVciWorkspaceMenu` - 8 edges

## Surprising Connections (you probably didn't know these)
- `GitService` --inherits--> `IGitService`  [EXTRACTED]
  TiaGitAddIn\Services\GitService.cs →   _Bridges community 4 → community 5_
- `IGitService` --inherits--> `FakeGitService`  [EXTRACTED]
   → TiaGitAddIn.Tests\UI\GitPanelLaunchServiceTests.cs  _Bridges community 5 → community 3_
- `IGitService` --inherits--> `FakeGitService`  [EXTRACTED]
   → TiaGitAddIn.Tests\UI\StatusViewModelTests.cs  _Bridges community 5 → community 7_
- `CommitViewModel` --inherits--> `ViewModelBase`  [EXTRACTED]
  TiaGitAddIn\UI\ViewModels\CommitViewModel.cs →   _Bridges community 0 → community 1_
- `ViewModelBase` --inherits--> `StatusViewModel`  [EXTRACTED]
   → TiaGitAddIn\UI\StatusViewModel.cs  _Bridges community 1 → community 17_

## Communities

### Community 0 - "Community 0"
Cohesion: 0.11
Nodes (9): INotifyPropertyChanged, CommitViewModel, TiaGitAddIn.UI, TiaGitAddIn.UI, ViewModelBase, CommitViewModel, TiaGitAddIn.UI.ViewModels, TiaGitAddIn.UI.ViewModels (+1 more)

### Community 1 - "Community 1"
Cohesion: 0.14
Nodes (7): MainViewModel, TiaGitAddIn.UI, ViewModelBase, MainViewModel, TiaGitAddIn.UI.ViewModels, StatusViewModel, TiaGitAddIn.UI.ViewModels

### Community 2 - "Community 2"
Cohesion: 0.18
Nodes (5): ContextMenuAddIn, GitProjectTreeMenu, TiaGitAddIn.Entry, GitVciWorkspaceMenu, TiaGitAddIn.Entry

### Community 3 - "Community 3"
Cohesion: 0.12
Nodes (4): FakeGitService, GitPanelLaunchServiceTests, TiaGitAddIn.Tests.UI, WorkspaceContext

### Community 4 - "Community 4"
Cohesion: 0.3
Nodes (2): GitService, TiaGitAddIn.Services

### Community 5 - "Community 5"
Cohesion: 0.15
Nodes (4): IGitService, CommitViewModelTests, FakeGitService, TiaGitAddIn.Tests.UI

### Community 6 - "Community 6"
Cohesion: 0.24
Nodes (2): GitOutputParser, TiaGitAddIn.Services

### Community 7 - "Community 7"
Cohesion: 0.14
Nodes (3): FakeGitService, StatusViewModelTests, TiaGitAddIn.Tests.UI

### Community 8 - "Community 8"
Cohesion: 0.27
Nodes (3): GitPanelWindow, TiaGitAddIn.UI.Views, Window

### Community 9 - "Community 9"
Cohesion: 0.22
Nodes (4): IAddInLogger, FileLogger, TiaGitAddIn.Logging, NullLogger

### Community 10 - "Community 10"
Cohesion: 0.18
Nodes (2): IGitService, TiaGitAddIn.Services

### Community 11 - "Community 11"
Cohesion: 0.25
Nodes (4): IVciWorkspaceLocator, TiaGitAddIn.Services, VciWorkspaceLocator, ThrowingWorkspaceLocator

### Community 12 - "Community 12"
Cohesion: 0.18
Nodes (2): PathValidatorTests, TiaGitAddIn.Tests.Configuration

### Community 13 - "Community 13"
Cohesion: 0.31
Nodes (2): GitPanelWindow, TiaGitAddIn.UI

### Community 14 - "Community 14"
Cohesion: 0.2
Nodes (5): ProjectWithPath, TiaGitAddIn.Tests.Services, VciWorkspaceLocatorTests, WorkspaceFileLikeContext, WorkspaceFolderLikeContext

### Community 15 - "Community 15"
Cohesion: 0.36
Nodes (3): ConfigurationService, TiaGitAddIn.Configuration, IConfigurationService

### Community 16 - "Community 16"
Cohesion: 0.33
Nodes (3): IGitProcessRunner, GitProcessRunner, TiaGitAddIn.Services

### Community 17 - "Community 17"
Cohesion: 0.36
Nodes (2): StatusViewModel, TiaGitAddIn.UI

### Community 18 - "Community 18"
Cohesion: 0.29
Nodes (4): IDisposable, Lease, OperationSerializer, TiaGitAddIn.Services

### Community 19 - "Community 19"
Cohesion: 0.43
Nodes (2): MenuSelectionContextResolver, TiaGitAddIn.Entry

### Community 20 - "Community 20"
Cohesion: 0.38
Nodes (3): ICommand, AsyncCommand, TiaGitAddIn.UI

### Community 21 - "Community 21"
Cohesion: 0.33
Nodes (3): MenuSelectionContextResolverTests, ProviderWithAmbiguousSelectionMethods, TiaGitAddIn.Tests.Entry

### Community 22 - "Community 22"
Cohesion: 0.29
Nodes (2): GitOutputParserTests, TiaGitAddIn.Tests.Services

### Community 23 - "Community 23"
Cohesion: 0.33
Nodes (2): ConfigurationServiceTests, TiaGitAddIn.Tests.Configuration

### Community 24 - "Community 24"
Cohesion: 0.4
Nodes (2): IConfigurationService, TiaGitAddIn.Configuration

### Community 25 - "Community 25"
Cohesion: 0.5
Nodes (2): PathValidator, TiaGitAddIn.Configuration

### Community 26 - "Community 26"
Cohesion: 0.4
Nodes (2): TiaGitAddIn.Configuration, ValidationResult

### Community 27 - "Community 27"
Cohesion: 0.4
Nodes (3): GitVciEditorProvider, TiaGitAddIn.Entry, VciEditorAddInProvider

### Community 28 - "Community 28"
Cohesion: 0.4
Nodes (3): GitVciWorkspaceViewProvider, TiaGitAddIn.Entry, VciWorkspaceViewAddInProvider

### Community 29 - "Community 29"
Cohesion: 0.4
Nodes (2): IAddInLogger, TiaGitAddIn.Logging

### Community 30 - "Community 30"
Cohesion: 0.4
Nodes (2): OperationResult, TiaGitAddIn.Models

### Community 31 - "Community 31"
Cohesion: 0.4
Nodes (3): IRepositoryDiscovery, RepositoryDiscovery, TiaGitAddIn.Services

### Community 32 - "Community 32"
Cohesion: 0.4
Nodes (2): GitPanelLaunchResult, TiaGitAddIn.UI

### Community 33 - "Community 33"
Cohesion: 0.4
Nodes (2): FileStatusItemViewModel, TiaGitAddIn.UI.ViewModels

### Community 34 - "Community 34"
Cohesion: 0.5
Nodes (2): AddInPublisherConfigurationTests, TiaGitAddIn.Tests.Configuration

### Community 35 - "Community 35"
Cohesion: 0.4
Nodes (2): GitStatusTests, TiaGitAddIn.Tests.Models

### Community 36 - "Community 36"
Cohesion: 0.4
Nodes (2): OperationSerializerTests, TiaGitAddIn.Tests.Services

### Community 37 - "Community 37"
Cohesion: 0.4
Nodes (2): FileStatusItemViewModel, TiaGitAddIn.UI

### Community 38 - "Community 38"
Cohesion: 0.4
Nodes (3): GitProjectTreeProvider, TiaGitAddIn.Entry, ProjectTreeAddInProvider

### Community 39 - "Community 39"
Cohesion: 0.5
Nodes (2): GitArgumentEscaper, TiaGitAddIn.Services

### Community 40 - "Community 40"
Cohesion: 0.5
Nodes (3): InvalidOperationException, GitOperationInProgressException, TiaGitAddIn.Services

### Community 41 - "Community 41"
Cohesion: 0.5
Nodes (2): IGitProcessRunner, TiaGitAddIn.Services

### Community 42 - "Community 42"
Cohesion: 0.5
Nodes (2): IRepositoryDiscovery, TiaGitAddIn.Services

### Community 43 - "Community 43"
Cohesion: 0.5
Nodes (2): IVciWorkspaceLocator, TiaGitAddIn.Services

### Community 44 - "Community 44"
Cohesion: 0.5
Nodes (2): GitPanelLaunchService, TiaGitAddIn.UI

### Community 45 - "Community 45"
Cohesion: 0.5
Nodes (2): GitArgumentEscaperTests, TiaGitAddIn.Tests.Services

### Community 46 - "Community 46"
Cohesion: 0.67
Nodes (2): BranchInfo, TiaGitAddIn.Models

### Community 47 - "Community 47"
Cohesion: 0.67
Nodes (2): CommitInfo, TiaGitAddIn.Models

### Community 48 - "Community 48"
Cohesion: 0.67
Nodes (2): DiffEntry, TiaGitAddIn.Models

### Community 49 - "Community 49"
Cohesion: 0.67
Nodes (2): DiffHunk, TiaGitAddIn.Models

### Community 50 - "Community 50"
Cohesion: 0.67
Nodes (2): DiffLine, TiaGitAddIn.Models

### Community 51 - "Community 51"
Cohesion: 0.67
Nodes (2): DiffResult, TiaGitAddIn.Models

### Community 52 - "Community 52"
Cohesion: 0.67
Nodes (2): FileStatusEntry, TiaGitAddIn.Models

### Community 53 - "Community 53"
Cohesion: 0.67
Nodes (2): GitConfiguration, TiaGitAddIn.Models

### Community 54 - "Community 54"
Cohesion: 0.67
Nodes (2): GitStatus, TiaGitAddIn.Models

### Community 55 - "Community 55"
Cohesion: 0.67
Nodes (2): RemoteInfo, TiaGitAddIn.Models

### Community 56 - "Community 56"
Cohesion: 0.67
Nodes (2): GitProcessResult, TiaGitAddIn.Services

### Community 57 - "Community 57"
Cohesion: 1.0
Nodes (1): TiaGitAddIn.Models

### Community 58 - "Community 58"
Cohesion: 1.0
Nodes (1): TiaGitAddIn.Models

## Knowledge Gaps
- **80 isolated node(s):** `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Entry` (+75 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `Community 4`** (15 nodes): `GitService`, `.CheckoutBranchAsync()`, `.CommitAsync()`, `.EnsureSuccess()`, `.GetBranchesAsync()`, `.GetCommitLogAsync()`, `.GetRemotesAsync()`, `.GetStatusAsync()`, `.RunAsync()`, `.RunExclusiveAsync()`, `.StageAsync()`, `.ToOperationResult()`, `.UnstageAsync()`, `TiaGitAddIn.Services`, `GitService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 6`** (14 nodes): `GitOutputParser`, `.ApplyAheadBehind()`, `.ApplyBranchStatus()`, `.IsConflictPair()`, `.MapStatus()`, `.ParseBranches()`, `.ParseCommitLog()`, `.ParseRemotes()`, `.ParseStatus()`, `.ParseStatusEntry()`, `.ParseUntrackedBranchName()`, `.SplitLines()`, `TiaGitAddIn.Services`, `GitOutputParser.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 10`** (11 nodes): `IGitService`, `.CheckoutBranchAsync()`, `.CommitAsync()`, `.GetBranchesAsync()`, `.GetCommitLogAsync()`, `.GetRemotesAsync()`, `.GetStatusAsync()`, `.StageAsync()`, `.UnstageAsync()`, `TiaGitAddIn.Services`, `IGitService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 12`** (11 nodes): `PathValidatorTests`, `.ValidateAcceptsValidAbsolutePaths()`, `.ValidateAcceptsValidRelativePaths()`, `.ValidateGitExecutableAcceptsGitNames()`, `.ValidateGitExecutableRejectsUnsafeNames()`, `.ValidateRejectsBlankPaths()`, `.ValidateRejectsControlCharacters()`, `.ValidateRejectsOverlongPaths()`, `.ValidateRejectsTraversalPaths()`, `TiaGitAddIn.Tests.Configuration`, `PathValidatorTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 13`** (11 nodes): `GitPanelWindow.cs`, `GitPanelWindow`, `.BuildCommitTab()`, `.BuildContent()`, `.BuildHeader()`, `.BuildStatusTab()`, `.BuildTabs()`, `.CreateButton()`, `.OnLoaded()`, `.RefreshOnLoadAsync()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 17`** (9 nodes): `StatusViewModel.cs`, `StatusViewModel`, `.BuildOperationMessage()`, `.BuildTrackingSummary()`, `.RaiseCommandStates()`, `.RefreshAsync()`, `.StageSelectedAsync()`, `.UnstageSelectedAsync()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 19`** (7 nodes): `MenuSelectionContextResolver`, `.Normalize()`, `.Resolve()`, `.TryInvoke()`, `.TryRead()`, `TiaGitAddIn.Entry`, `MenuSelectionContextResolver.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 22`** (7 nodes): `GitOutputParserTests`, `.ParseCommitLogSkipsMalformedLines()`, `.ParseRemotesPreservesUrlsWithSpaces()`, `.ParseStatusMapsPorcelainEntries()`, `.ParseStatusReadsFreshRepositoryBranch()`, `TiaGitAddIn.Tests.Services`, `GitOutputParserTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 23`** (6 nodes): `ConfigurationServiceTests`, `.LoadNormalizesMissingRepositoryPath()`, `.LoadReturnsDefaultConfigurationWhenFileIsMalformed()`, `.SaveThenLoadRoundTripsConfiguration()`, `TiaGitAddIn.Tests.Configuration`, `ConfigurationServiceTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 24`** (5 nodes): `IConfigurationService`, `.Load()`, `.Save()`, `TiaGitAddIn.Configuration`, `IConfigurationService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 25`** (5 nodes): `PathValidator`, `.Validate()`, `.ValidateGitExecutablePath()`, `TiaGitAddIn.Configuration`, `PathValidator.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 26`** (5 nodes): `TiaGitAddIn.Configuration`, `ValidationResult`, `.Invalid()`, `.Valid()`, `ValidationResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 29`** (5 nodes): `IAddInLogger`, `.Error()`, `.Info()`, `TiaGitAddIn.Logging`, `IAddInLogger.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 30`** (5 nodes): `OperationResult`, `.Fail()`, `.Ok()`, `TiaGitAddIn.Models`, `OperationResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 32`** (5 nodes): `GitPanelLaunchResult.cs`, `GitPanelLaunchResult`, `.Fail()`, `.Ok()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 33`** (5 nodes): `FileStatusItemViewModel.cs`, `FileStatusItemViewModel`, `.GetArea()`, `.GetStatusText()`, `TiaGitAddIn.UI.ViewModels`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 34`** (5 nodes): `AddInPublisherConfigurationTests`, `.GetConfigurationPath()`, `.RequiredSecurityPermissionsIncludeWpfWindowPermission()`, `TiaGitAddIn.Tests.Configuration`, `AddInPublisherConfigurationTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 35`** (5 nodes): `GitStatusTests`, `.HasConflictsReturnsFalseForCleanStatus()`, `.HasConflictsReturnsTrueWhenAnyEntryIsConflicted()`, `TiaGitAddIn.Tests.Models`, `GitStatusTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 36`** (5 nodes): `OperationSerializerTests`, `.AcquireAsyncRejectsConcurrentOperationWhenWaitIsDisabled()`, `.DisposeReleasesOperation()`, `TiaGitAddIn.Tests.Services`, `OperationSerializerTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 37`** (5 nodes): `FileStatusItemViewModel.cs`, `FileStatusItemViewModel`, `.GetArea()`, `.GetStatusText()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 39`** (4 nodes): `GitArgumentEscaper`, `.Escape()`, `TiaGitAddIn.Services`, `GitArgumentEscaper.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 41`** (4 nodes): `IGitProcessRunner`, `.RunAsync()`, `TiaGitAddIn.Services`, `IGitProcessRunner.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 42`** (4 nodes): `IRepositoryDiscovery`, `.FindRepositoryRoot()`, `TiaGitAddIn.Services`, `IRepositoryDiscovery.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 43`** (4 nodes): `IVciWorkspaceLocator`, `.TryGetWorkspacePath()`, `TiaGitAddIn.Services`, `IVciWorkspaceLocator.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 44`** (4 nodes): `GitPanelLaunchService.cs`, `GitPanelLaunchService`, `.CreateViewModel()`, `TiaGitAddIn.UI`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 45`** (4 nodes): `GitArgumentEscaperTests`, `.EscapePreservesWindowsArgumentMeaning()`, `TiaGitAddIn.Tests.Services`, `GitArgumentEscaperTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 46`** (3 nodes): `BranchInfo`, `TiaGitAddIn.Models`, `BranchInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 47`** (3 nodes): `CommitInfo`, `TiaGitAddIn.Models`, `CommitInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 48`** (3 nodes): `DiffEntry`, `TiaGitAddIn.Models`, `DiffEntry.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 49`** (3 nodes): `DiffHunk`, `TiaGitAddIn.Models`, `DiffHunk.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 50`** (3 nodes): `DiffLine`, `TiaGitAddIn.Models`, `DiffLine.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 51`** (3 nodes): `DiffResult`, `TiaGitAddIn.Models`, `DiffResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 52`** (3 nodes): `FileStatusEntry`, `TiaGitAddIn.Models`, `FileStatusEntry.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 53`** (3 nodes): `GitConfiguration`, `TiaGitAddIn.Models`, `GitConfiguration.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 54`** (3 nodes): `GitStatus`, `TiaGitAddIn.Models`, `GitStatus.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 55`** (3 nodes): `RemoteInfo`, `TiaGitAddIn.Models`, `RemoteInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 56`** (3 nodes): `GitProcessResult`, `TiaGitAddIn.Services`, `GitProcessResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 57`** (2 nodes): `TiaGitAddIn.Models`, `DiffLineType.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 58`** (2 nodes): `TiaGitAddIn.Models`, `FileStatus.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `FakeGitService` connect `Community 3` to `Community 5`?**
  _High betweenness centrality (0.022) - this node is a cross-community bridge._
- **Why does `GitService` connect `Community 4` to `Community 5`?**
  _High betweenness centrality (0.012) - this node is a cross-community bridge._
- **Why does `FakeGitService` connect `Community 7` to `Community 5`?**
  _High betweenness centrality (0.011) - this node is a cross-community bridge._
- **What connects `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration` to the rest of the system?**
  _80 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.11 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.14 - nodes in this community are weakly interconnected._
- **Should `Community 3` be split into smaller, more focused modules?**
  _Cohesion score 0.12 - nodes in this community are weakly interconnected._