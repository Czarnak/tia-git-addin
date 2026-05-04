# Graph Report - tia-git-addin  (2026-05-04)

## Corpus Check
- 39 files · ~13,418 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 213 nodes · 215 edges · 39 communities detected
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS
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

## God Nodes (most connected - your core abstractions)
1. `GitService` - 14 edges
2. `GitOutputParser` - 12 edges
3. `IGitService` - 9 edges
4. `PathValidatorTests` - 9 edges
5. `ConfigurationService` - 7 edges
6. `GitProcessRunner` - 7 edges
7. `VciWorkspaceLocator` - 7 edges
8. `GitOutputParserTests` - 5 edges
9. `GitProjectTreeMenu` - 4 edges
10. `ConfigurationServiceTests` - 4 edges

## Surprising Connections (you probably didn't know these)
- None detected - all connections are within the same source files.

## Communities

### Community 0 - "Community 0"
Cohesion: 0.27
Nodes (3): IGitService, GitService, TiaGitAddIn.Services

### Community 1 - "Community 1"
Cohesion: 0.24
Nodes (2): GitOutputParser, TiaGitAddIn.Services

### Community 2 - "Community 2"
Cohesion: 0.18
Nodes (2): IGitService, TiaGitAddIn.Services

### Community 3 - "Community 3"
Cohesion: 0.18
Nodes (2): PathValidatorTests, TiaGitAddIn.Tests.Configuration

### Community 4 - "Community 4"
Cohesion: 0.36
Nodes (3): ConfigurationService, TiaGitAddIn.Configuration, IConfigurationService

### Community 5 - "Community 5"
Cohesion: 0.33
Nodes (3): IGitProcessRunner, GitProcessRunner, TiaGitAddIn.Services

### Community 6 - "Community 6"
Cohesion: 0.33
Nodes (3): IVciWorkspaceLocator, TiaGitAddIn.Services, VciWorkspaceLocator

### Community 7 - "Community 7"
Cohesion: 0.29
Nodes (4): IDisposable, Lease, OperationSerializer, TiaGitAddIn.Services

### Community 8 - "Community 8"
Cohesion: 0.29
Nodes (2): GitOutputParserTests, TiaGitAddIn.Tests.Services

### Community 9 - "Community 9"
Cohesion: 0.33
Nodes (3): ContextMenuAddIn, GitProjectTreeMenu, TiaGitAddIn.Entry

### Community 10 - "Community 10"
Cohesion: 0.33
Nodes (2): ConfigurationServiceTests, TiaGitAddIn.Tests.Configuration

### Community 11 - "Community 11"
Cohesion: 0.33
Nodes (3): ProjectWithPath, TiaGitAddIn.Tests.Services, VciWorkspaceLocatorTests

### Community 12 - "Community 12"
Cohesion: 0.4
Nodes (2): IConfigurationService, TiaGitAddIn.Configuration

### Community 13 - "Community 13"
Cohesion: 0.5
Nodes (2): PathValidator, TiaGitAddIn.Configuration

### Community 14 - "Community 14"
Cohesion: 0.4
Nodes (2): TiaGitAddIn.Configuration, ValidationResult

### Community 15 - "Community 15"
Cohesion: 0.4
Nodes (3): GitProjectTreeProvider, TiaGitAddIn.Entry, ProjectTreeAddInProvider

### Community 16 - "Community 16"
Cohesion: 0.4
Nodes (2): OperationResult, TiaGitAddIn.Models

### Community 17 - "Community 17"
Cohesion: 0.4
Nodes (3): IRepositoryDiscovery, RepositoryDiscovery, TiaGitAddIn.Services

### Community 18 - "Community 18"
Cohesion: 0.4
Nodes (2): GitStatusTests, TiaGitAddIn.Tests.Models

### Community 19 - "Community 19"
Cohesion: 0.4
Nodes (2): OperationSerializerTests, TiaGitAddIn.Tests.Services

### Community 20 - "Community 20"
Cohesion: 0.5
Nodes (2): GitArgumentEscaper, TiaGitAddIn.Services

### Community 21 - "Community 21"
Cohesion: 0.5
Nodes (3): InvalidOperationException, GitOperationInProgressException, TiaGitAddIn.Services

### Community 22 - "Community 22"
Cohesion: 0.5
Nodes (2): IGitProcessRunner, TiaGitAddIn.Services

### Community 23 - "Community 23"
Cohesion: 0.5
Nodes (2): IRepositoryDiscovery, TiaGitAddIn.Services

### Community 24 - "Community 24"
Cohesion: 0.5
Nodes (2): IVciWorkspaceLocator, TiaGitAddIn.Services

### Community 25 - "Community 25"
Cohesion: 0.5
Nodes (2): GitArgumentEscaperTests, TiaGitAddIn.Tests.Services

### Community 26 - "Community 26"
Cohesion: 0.67
Nodes (2): BranchInfo, TiaGitAddIn.Models

### Community 27 - "Community 27"
Cohesion: 0.67
Nodes (2): CommitInfo, TiaGitAddIn.Models

### Community 28 - "Community 28"
Cohesion: 0.67
Nodes (2): DiffEntry, TiaGitAddIn.Models

### Community 29 - "Community 29"
Cohesion: 0.67
Nodes (2): DiffHunk, TiaGitAddIn.Models

### Community 30 - "Community 30"
Cohesion: 0.67
Nodes (2): DiffLine, TiaGitAddIn.Models

### Community 31 - "Community 31"
Cohesion: 0.67
Nodes (2): DiffResult, TiaGitAddIn.Models

### Community 32 - "Community 32"
Cohesion: 0.67
Nodes (2): FileStatusEntry, TiaGitAddIn.Models

### Community 33 - "Community 33"
Cohesion: 0.67
Nodes (2): GitConfiguration, TiaGitAddIn.Models

### Community 34 - "Community 34"
Cohesion: 0.67
Nodes (2): GitStatus, TiaGitAddIn.Models

### Community 35 - "Community 35"
Cohesion: 0.67
Nodes (2): RemoteInfo, TiaGitAddIn.Models

### Community 36 - "Community 36"
Cohesion: 0.67
Nodes (2): GitProcessResult, TiaGitAddIn.Services

### Community 37 - "Community 37"
Cohesion: 1.0
Nodes (1): TiaGitAddIn.Models

### Community 38 - "Community 38"
Cohesion: 1.0
Nodes (1): TiaGitAddIn.Models

## Knowledge Gaps
- **51 isolated node(s):** `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Entry` (+46 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `Community 1`** (14 nodes): `GitOutputParser`, `.ApplyAheadBehind()`, `.ApplyBranchStatus()`, `.IsConflictPair()`, `.MapStatus()`, `.ParseBranches()`, `.ParseCommitLog()`, `.ParseRemotes()`, `.ParseStatus()`, `.ParseStatusEntry()`, `.ParseUntrackedBranchName()`, `.SplitLines()`, `TiaGitAddIn.Services`, `GitOutputParser.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 2`** (11 nodes): `IGitService`, `.CheckoutBranchAsync()`, `.CommitAsync()`, `.GetBranchesAsync()`, `.GetCommitLogAsync()`, `.GetRemotesAsync()`, `.GetStatusAsync()`, `.StageAsync()`, `.UnstageAsync()`, `TiaGitAddIn.Services`, `IGitService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 3`** (11 nodes): `PathValidatorTests`, `.ValidateAcceptsValidAbsolutePaths()`, `.ValidateAcceptsValidRelativePaths()`, `.ValidateGitExecutableAcceptsGitNames()`, `.ValidateGitExecutableRejectsUnsafeNames()`, `.ValidateRejectsBlankPaths()`, `.ValidateRejectsControlCharacters()`, `.ValidateRejectsOverlongPaths()`, `.ValidateRejectsTraversalPaths()`, `TiaGitAddIn.Tests.Configuration`, `PathValidatorTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 8`** (7 nodes): `GitOutputParserTests`, `.ParseCommitLogSkipsMalformedLines()`, `.ParseRemotesPreservesUrlsWithSpaces()`, `.ParseStatusMapsPorcelainEntries()`, `.ParseStatusReadsFreshRepositoryBranch()`, `TiaGitAddIn.Tests.Services`, `GitOutputParserTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 10`** (6 nodes): `ConfigurationServiceTests`, `.LoadNormalizesMissingRepositoryPath()`, `.LoadReturnsDefaultConfigurationWhenFileIsMalformed()`, `.SaveThenLoadRoundTripsConfiguration()`, `TiaGitAddIn.Tests.Configuration`, `ConfigurationServiceTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 12`** (5 nodes): `IConfigurationService`, `.Load()`, `.Save()`, `TiaGitAddIn.Configuration`, `IConfigurationService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 13`** (5 nodes): `PathValidator`, `.Validate()`, `.ValidateGitExecutablePath()`, `TiaGitAddIn.Configuration`, `PathValidator.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 14`** (5 nodes): `TiaGitAddIn.Configuration`, `ValidationResult`, `.Invalid()`, `.Valid()`, `ValidationResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 16`** (5 nodes): `OperationResult`, `.Fail()`, `.Ok()`, `TiaGitAddIn.Models`, `OperationResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 18`** (5 nodes): `GitStatusTests`, `.HasConflictsReturnsFalseForCleanStatus()`, `.HasConflictsReturnsTrueWhenAnyEntryIsConflicted()`, `TiaGitAddIn.Tests.Models`, `GitStatusTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 19`** (5 nodes): `OperationSerializerTests`, `.AcquireAsyncRejectsConcurrentOperationWhenWaitIsDisabled()`, `.DisposeReleasesOperation()`, `TiaGitAddIn.Tests.Services`, `OperationSerializerTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 20`** (4 nodes): `GitArgumentEscaper`, `.Escape()`, `TiaGitAddIn.Services`, `GitArgumentEscaper.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 22`** (4 nodes): `IGitProcessRunner`, `.RunAsync()`, `TiaGitAddIn.Services`, `IGitProcessRunner.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 23`** (4 nodes): `IRepositoryDiscovery`, `.FindRepositoryRoot()`, `TiaGitAddIn.Services`, `IRepositoryDiscovery.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 24`** (4 nodes): `IVciWorkspaceLocator`, `.TryGetWorkspacePath()`, `TiaGitAddIn.Services`, `IVciWorkspaceLocator.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 25`** (4 nodes): `GitArgumentEscaperTests`, `.EscapePreservesWindowsArgumentMeaning()`, `TiaGitAddIn.Tests.Services`, `GitArgumentEscaperTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 26`** (3 nodes): `BranchInfo`, `TiaGitAddIn.Models`, `BranchInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 27`** (3 nodes): `CommitInfo`, `TiaGitAddIn.Models`, `CommitInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 28`** (3 nodes): `DiffEntry`, `TiaGitAddIn.Models`, `DiffEntry.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 29`** (3 nodes): `DiffHunk`, `TiaGitAddIn.Models`, `DiffHunk.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 30`** (3 nodes): `DiffLine`, `TiaGitAddIn.Models`, `DiffLine.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 31`** (3 nodes): `DiffResult`, `TiaGitAddIn.Models`, `DiffResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 32`** (3 nodes): `FileStatusEntry`, `TiaGitAddIn.Models`, `FileStatusEntry.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 33`** (3 nodes): `GitConfiguration`, `TiaGitAddIn.Models`, `GitConfiguration.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 34`** (3 nodes): `GitStatus`, `TiaGitAddIn.Models`, `GitStatus.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 35`** (3 nodes): `RemoteInfo`, `TiaGitAddIn.Models`, `RemoteInfo.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 36`** (3 nodes): `GitProcessResult`, `TiaGitAddIn.Services`, `GitProcessResult.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 37`** (2 nodes): `TiaGitAddIn.Models`, `DiffLineType.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Community 38`** (2 nodes): `TiaGitAddIn.Models`, `FileStatus.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What connects `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration`, `TiaGitAddIn.Configuration` to the rest of the system?**
  _51 weakly-connected nodes found - possible documentation gaps or missing edges._