# VCI Git Workflow, Coverage, Live Acceptance, and Protected Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a hermetic real-Git VCI workflow test, merged net48/net8 coverage gate, immutable V21 candidate pipeline, live-TIA evidence lane, and protected no-rebuild release path.

**Architecture:** A new net8.0 xUnit v2 integration project references only `TiaGitAddIn.Core` and drives the existing `GitService` through a test-only `System.Diagnostics.Process` adapter against a unique working repository and local bare remote. A reusable self-hosted Windows gate merges net48 and net8 Coverlet 6.0.4 output, while separate candidate, manual live-TIA, durable-evidence, and protected-release stages preserve one package's identity and SHA-256 from build through publication.

**Tech Stack:** .NET SDK 8.0.420, C# 12, .NET Framework 4.8, .NET Standard 2.0, xUnit.net v2/VSTest, Coverlet MSBuild 6.0.4, Git for Windows, PowerShell 7, GitHub Actions, TIA Portal V21 Public API build 2100.0.121.1.

## Global Constraints

- `TiaGitAddIn.IntegrationTests` targets exactly `net8.0`, uses C# 12, and has one project reference: `..\TiaGitAddIn.Core\TiaGitAddIn.Core.csproj`.
- The integration project must not reference `TiaGitAddIn`, WPF, a Siemens assembly, or a TIA installation path.
- Keep xUnit v2 with VSTest: `Microsoft.NET.Test.Sdk` 17.10.0, `xunit` 2.9.0, and `xunit.runner.visualstudio` 2.8.2.
- Both test projects pin `coverlet.msbuild` 6.0.4 with `PrivateAssets="all"`; do not add `coverlet.collector`.
- The automated Git lane starts from a workspace file change. It never claims to execute TIA VCI synchronization.
- Every automated Git remote is a local filesystem bare repository beneath the fixture root. No automated test may use an HTTP, HTTPS, SSH, UNC, or drive-share remote.
- Automated tests receive no credential, repository token, or secret. Child Git processes set `GIT_CONFIG_NOSYSTEM=1`, a fixture-owned `GIT_CONFIG_GLOBAL`, `GIT_TERMINAL_PROMPT=0`, and `GCM_INTERACTIVE=Never` without changing process-global environment variables.
- Git arguments are discrete `ProcessStartInfo.ArgumentList` entries, `UseShellExecute=false`, and pathspec commands place `--` before repository-relative paths.
- Fixture roots are unique, short, at most 120 characters, and all created paths are at most 260 characters. Sequential and concurrent tests share no repository, configuration, hook, credential helper, or ref.
- `TIA_GIT_E2E_KEEP_TEMP=1` may retain a failed fixture only outside CI. `CI=true` always forces retrying deletion.
- The live lane alone uses TIA Portal V21, the built-in **Project to workspace synchronization** command, the packaged Add-In, `TIA.ReadWrite`, and `ProcessStartPermission`.
- The candidate job invokes the Add-In publisher exactly once, computes SHA-256 after the build, and uploads the package, hash file, and pre-upload provenance together. It never rewrites package bytes after hashing.
- The protected release job downloads the accepted candidate and durable live evidence, validates source/tag/candidate/hash/reviewer/approval equality, and contains no restore, build, pack, or publisher command.
- Merged coverage includes `TiaGitAddIn.Core` and testable `TiaGitAddIn` production classes. Exclusions are limited to `*.g.cs`, `*.g.i.cs`, `GeneratedCodeAttribute`, `CompilerGeneratedAttribute`, and individually named Siemens bootstrap files with written justification.
- Total merged line coverage is accepted at 80.00 percent and rejected at 79.99 percent. Coverlet receives `Threshold=80`, `ThresholdType=line`, and `ThresholdStat=total`.
- Pull-request jobs use only the repository-scoped `[self-hosted, Windows, tia-pr-ephemeral]` pool. Every PR runner is registered with `--ephemeral` for one job, contains no persistent secret or trusted signing/publishing material, starts from a clean image, and is deregistered and destroyed/reimaged after the job even on failure. Repository Actions settings require approval for every first-time or outside-collaborator fork workflow before it can enter this pool; never use `pull_request_target` to execute PR code.
- Trusted jobs use disjoint runner groups and labels: `[self-hosted, Windows, tia-ci-trusted]` for main/reusable gates, `[self-hosted, Windows, tia-candidate-trusted]` for candidate packaging, `[self-hosted, Windows, tia-live-v21-trusted]` for operator-controlled live TIA acceptance, and `[self-hosted, Windows, tia-release-trusted]` for protected publication. No runner is a member of more than one pool, and no trusted pool accepts `pull_request` jobs.
- Every external GitHub Action is pinned to an approved lowercase 40-character commit SHA followed by its verified release comment. Every checkout sets `persist-credentials: false`; steps needing API access receive the job-scoped token only through an explicit step environment/input.
- Evidence and diagnostics redact URL user information, credential-shaped values, private identifiers, IP/network addresses, stack traces, and machine-specific private paths.
- External upload and publication remain manual, approved actions. Ordinary tests and focused developer commands perform no external write.
- Follow RED -> GREEN -> refactor for every task. Run focused verification first, then the full project/gate command.
- After implementation changes code, run `graphify update .`; final verification must confirm `graphify-out/GRAPH_REPORT.md` identifies the current source revision.

## Verified External Action Pins

The following pins were resolved on 2026-07-16 from each action's official GitHub release/tag ref and independently matched the tag's commit object. Treat the complete `owner/repository@sha # version` scalar as the allowlist; updating a pin requires repeating that official-ref verification and updating the security test in the same reviewed commit.

| Action | Verified release | Full commit SHA | Official source |
|---|---|---|---|
| `actions/checkout` | `v4.3.1` | `34e114876b0b11c390a56381ad16ebd13914f8d5` | `https://github.com/actions/checkout/releases/tag/v4.3.1` |
| `actions/setup-dotnet` | `v4.3.1` | `67a3573c9a986a3f9c594539f4ab511d57bb3ce9` | `https://github.com/actions/setup-dotnet/releases/tag/v4.3.1` |
| `actions/upload-artifact` | `v4.6.2` | `ea165f8d65b6e75b540449e92b4886f43607fa02` | `https://github.com/actions/upload-artifact/releases/tag/v4.6.2` |
| `actions/download-artifact` | `v4.3.0` | `d3f86a106a0bac45b974a628896c90dbdf5c8093` | `https://github.com/actions/download-artifact/releases/tag/v4.3.0` |
| `softprops/action-gh-release` | `v2.6.2` | `3bb12739c298aeb8a4eeaf626c5b8d85266b0e65` | `https://github.com/softprops/action-gh-release/releases/tag/v2.6.2` |

## Ownership and Dependencies

This plan owns the integration-project scaffold, solution membership, both Coverlet pins, all coverage commands, the reusable test gate, candidate/release workflows, candidate identity stamping, live evidence schema/scripts, and the live runbook. Comparison-foundation, FBD, and SCL plans may add tests to either test project and consume this gate, but they must not edit the integration csproj, solution membership, coverage scripts, or release workflows.

Shared-file merge order is additive and fixed: VCI Task 1 → comparison-foundation Tasks 1–10 → FBD → SCL rebased over FBD for `GitPanelLaunchService.cs` and `ComparisonTemplates.xaml` → VCI Tasks 2–4 → comparison-foundation Task 11 → VCI Tasks 5–8. Foundation changes to `GitProcessRunner.cs` (raw-byte seam) and `AddInPublisherConfiguration.xml` (stale SACT cleanup) therefore land before VCI adds the safe adapter identifier and retains the final V21 permission/identity contract. FBD lands its strategy/mapper/template first; SCL rebases and retains those entries before appending its own; VCI then rebases and retains both feature registrations while adding only adapter logging. Feature branches may develop in parallel after prerequisites, but commits touching shared composition files serialize in this order.

Dependency order:

1. Task 1 first produces the project boundary used by comparison-foundation tests.
2. Foundation Tasks 1–10 then land, followed by FBD, then SCL rebased over FBD for both shared composition files.
3. Tasks 2 and 3 produce and prove the isolated real-Git adapter; Task 4 then establishes the authoritative gate and workflow-security scans over the integrated comparison tree.
4. Foundation Task 11 consumes that gate as the comparison integration acceptance point.
5. Task 5 may create a candidate only after Task 4 and foundation Task 11 pass.
6. Task 5 produces the exact artifact consumed by Task 6.
7. Task 6 durably uploads validated evidence before cleanup; Task 7 consumes that evidence and the Task 5 artifact.
8. Task 8 performs the repository-wide release-readiness pass.

## File Map

### Create

- `src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj` — net8.0 xUnit v2 test host with only the Core project reference.
- `src/TiaGitAddIn.IntegrationTests/Infrastructure/GitProcessTrace.cs` — immutable child-process transcript.
- `src/TiaGitAddIn.IntegrationTests/Infrastructure/GitTestEnvironment.cs` — per-process Git isolation dictionary.
- `src/TiaGitAddIn.IntegrationTests/Infrastructure/SystemGitProcessRunner.cs` — no-shell test adapter for `IGitProcessRunner`.
- `src/TiaGitAddIn.IntegrationTests/Infrastructure/GitFailureDiagnostics.cs` — bounded, redacted phase diagnostics.
- `src/TiaGitAddIn.IntegrationTests/Infrastructure/RetryingDirectoryCleanup.cs` — deterministic cleanup and keep-temp policy.
- `src/TiaGitAddIn.IntegrationTests/Infrastructure/VciGitRepositoryFixture.cs` — unique worktree/local-bare-remote lifecycle.
- `src/TiaGitAddIn.IntegrationTests/SystemGitProcessRunnerTests.cs` — adapter and per-process isolation tests.
- `src/TiaGitAddIn.IntegrationTests/VciGitRepositoryFixtureTests.cs` — isolation, diagnostics, and cleanup tests.
- `src/TiaGitAddIn.IntegrationTests/VciGitWorkflowTests.cs` — exact status/stage/commit/history/diff/default-push scenario.
- `src/TiaGitAddIn.IntegrationTests/TestData/VciGitWorkflow/Program.V1.xml` — sanitized baseline workspace artifact.
- `src/TiaGitAddIn.IntegrationTests/TestData/VciGitWorkflow/Program.V2.xml` — sanitized changed workspace artifact.
- `src/TiaGitAddIn.IntegrationTests/TestData/VciGitWorkflow/manifest.json` — fixture provenance and SHA-256.
- `src/TiaGitAddIn.Tests/Configuration/ProjectBoundaryTests.cs` — Core-only integration-project and production/test adapter assertions.
- `src/TiaGitAddIn.Tests/Configuration/CoverageGateTests.cs` — command, threshold, and workflow contract tests.
- `src/TiaGitAddIn.Tests/Configuration/GitHubWorkflowSecurityTests.cs` — approved action-pin, checkout-credential, trigger, and runner-pool tests.
- `src/TiaGitAddIn.Tests/Configuration/ReleaseProvenanceTests.cs` — controlled release validation matrix.
- `src/TiaGitAddIn.Tests/Configuration/LiveTiaEvidenceTests.cs` — schema, sanitization, and evidence relation tests.
- `src/TiaGitAddIn.Tests/Configuration/DocumentationContractTests.cs` — README/runbook command and lane-boundary assertions.
- `scripts/Assert-CoberturaThreshold.ps1` — exact decimal total-line threshold check.
- `scripts/Invoke-TestGate.ps1` — restore/build/net48/net8 merge/gate entry point.
- `scripts/Test-GitHubWorkflowSecurity.ps1` — offline workflow action-pin and runner-separation scan.
- `scripts/New-CandidateProvenance.ps1` — package inspection, identity extraction, hash, and provenance writer.
- `scripts/New-LiveTiaEvidenceBundle.ps1` — evidence JSON and `summary.md` writer.
- `scripts/Test-LiveTiaEvidence.ps1` — schema, relation, package, and sanitization validator.
- `scripts/Publish-LiveTiaEvidence.ps1` — explicitly approved durable draft-release upload.
- `scripts/Test-ReleaseProvenance.ps1` — protected-release equality and approval validator.
- `.github/workflows/test-gate.yml` — reusable Windows gate with isolated ephemeral PR and trusted main/caller jobs.
- `.github/workflows/release-candidate.yml` — gate, one package build, provenance, and immutable upload.
- `docs/testing/schemas/live-tia-v21-evidence.schema.json` — draft-07 evidence contract.
- `docs/testing/live-tia-v21-git-acceptance.md` — manual V21 synchronization/Add-In/push/evidence/cleanup runbook.
- `docs/testing/github-actions-runner-security.md` — administrator prerequisites and per-job runner destruction/attestation checklist.

### Modify

- `TiaGitAddIn.sln` — add `TiaGitAddIn.IntegrationTests`.
- `src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj` — pin Coverlet MSBuild 6.0.4.
- `src/TiaGitAddIn/TiaGitAddIn.csproj` — stamp candidate package/product identity before its sole publish target execution.
- `src/TiaGitAddIn/AddInPublisherConfiguration.xml` — retain V21 namespace and documented permissions; no compare-specific permission.
- `src/TiaGitAddIn/Services/GitProcessRunner.cs` — expose the safe production adapter identifier.
- `src/TiaGitAddIn/UI/GitPanelLaunchService.cs` — log the production Siemens adapter identifier through `IAddInLogger`.
- `src/TiaGitAddIn.Tests/Configuration/AddInPublisherConfigurationTests.cs` — assert exact V21 permission/identity contract.
- `src/TiaGitAddIn.Tests/Configuration/ReleaseWorkflowTests.cs` — replace direct tag-build expectations with gate/candidate/no-rebuild assertions.
- `.github/workflows/release.yml` — protected exact-artifact publication.
- `README.md` — automated/live boundary, focused/full commands, coverage commands, and release handoff.

---

### Task 1: Establish the net8 Core-only integration boundary and fixture provenance

**Acceptance criteria:** AC-004, AC-068, AC-079, AC-090, AC-100, AC-101.

**Files:**

- Create: `src/TiaGitAddIn.Tests/Configuration/ProjectBoundaryTests.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj`
- Create: `src/TiaGitAddIn.IntegrationTests/TestData/VciGitWorkflow/Program.V1.xml`
- Create: `src/TiaGitAddIn.IntegrationTests/TestData/VciGitWorkflow/Program.V2.xml`
- Create: `src/TiaGitAddIn.IntegrationTests/TestData/VciGitWorkflow/manifest.json`
- Modify: `src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj`
- Modify: `TiaGitAddIn.sln`

**Interfaces:**

- Consumes: `src/TiaGitAddIn.Core/TiaGitAddIn.Core.csproj` (`netstandard2.0`) and the repository SDK pin `8.0.420`.
- Produces: `TiaGitAddIn.IntegrationTests` (`net8.0`, C# 12) with only `ProjectReference Include="..\TiaGitAddIn.Core\TiaGitAddIn.Core.csproj"`; fixture keys `Program.V1.xml` and `Program.V2.xml` copied at runtime to `Program.xml`.

- [ ] **Step 1: Write the failing project-boundary tests**

Create `ProjectBoundaryTests.cs` with these exact assertions:

```csharp
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace TiaGitAddIn.Tests.Configuration
{
    public sealed class ProjectBoundaryTests
    {
        [Fact]
        public void IntegrationProjectTargetsNet8AndReferencesOnlyCore()
        {
            XDocument project = XDocument.Load(PathAt(
                "src", "TiaGitAddIn.IntegrationTests", "TiaGitAddIn.IntegrationTests.csproj"));

            Assert.Equal("net8.0", Value(project, "TargetFramework"));
            Assert.Equal("12.0", Value(project, "LangVersion"));
            Assert.Equal("true", Value(project, "IsTestProject"));
            Assert.Null(project.Descendants("UseWPF").SingleOrDefault());

            string[] references = project.Descendants("ProjectReference")
                .Select(element => ((string?)element.Attribute("Include") ?? string.Empty)
                    .Replace('/', '\\'))
                .ToArray();
            Assert.Equal(
                new[] { "..\\TiaGitAddIn.Core\\TiaGitAddIn.Core.csproj" },
                references);

            string xml = project.ToString(SaveOptions.DisableFormatting);
            Assert.DoesNotContain("TiaGitAddIn.csproj", xml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Siemens", xml, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PresentationFramework", xml, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void BothTestProjectsPinCoverletMsbuild604Privately()
        {
            AssertCoverlet(PathAt("src", "TiaGitAddIn.Tests", "TiaGitAddIn.Tests.csproj"));
            AssertCoverlet(PathAt(
                "src", "TiaGitAddIn.IntegrationTests", "TiaGitAddIn.IntegrationTests.csproj"));
        }

        private static void AssertCoverlet(string path)
        {
            XElement package = XDocument.Load(path)
                .Descendants("PackageReference")
                .Single(element => string.Equals(
                    (string?)element.Attribute("Include"),
                    "coverlet.msbuild",
                    StringComparison.OrdinalIgnoreCase));
            Assert.Equal("6.0.4", (string?)package.Attribute("Version"));
            Assert.Equal("all", (string?)package.Attribute("PrivateAssets"));
        }

        private static string? Value(XDocument project, string name) =>
            project.Descendants(name).Select(element => element.Value).SingleOrDefault();

        private static string PathAt(params string[] segments) =>
            segments.Aggregate(RepositoryRoot(), Path.Combine);

        private static string RepositoryRoot()
        {
            DirectoryInfo? current = new(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "TiaGitAddIn.sln")))
            {
                current = current.Parent;
            }

            return current?.FullName
                ?? throw new DirectoryNotFoundException("Repository root containing TiaGitAddIn.sln was not found.");
        }
    }
}
```

- [ ] **Step 2: Run the boundary tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ProjectBoundaryTests"
```

Expected: FAIL because `TiaGitAddIn.IntegrationTests.csproj` does not exist and the net48 test project does not yet reference Coverlet MSBuild 6.0.4.

- [ ] **Step 3: Create the exact integration project and pin both test projects**

Create `TiaGitAddIn.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" PrivateAssets="all" />
    <PackageReference Include="coverlet.msbuild" Version="6.0.4" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\TiaGitAddIn.Core\TiaGitAddIn.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="TestData\**\*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
</Project>
```

Add this exact package reference to `TiaGitAddIn.Tests.csproj`:

```xml
<PackageReference Include="coverlet.msbuild" Version="6.0.4" PrivateAssets="all" />
```

Do not add a package to `TiaGitAddIn` or `TiaGitAddIn.Core`.

- [ ] **Step 4: Add the integration project to the solution**

Run:

```powershell
dotnet sln TiaGitAddIn.sln add src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj
```

Expected: `Project 'src\TiaGitAddIn.IntegrationTests\TiaGitAddIn.IntegrationTests.csproj' added to the solution.`

- [ ] **Step 5: Add exact sanitized V1/V2 artifacts and manifest**

Both XML files are UTF-8 without BOM, LF line endings, and a final newline. `Program.V1.xml` is:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Document>
  <SW.Blocks.FC ID="0">
    <AttributeList>
      <Name>Program</Name>
      <ProgrammingLanguage>LAD</ProgrammingLanguage>
      <Comment>GitAcceptanceV1</Comment>
    </AttributeList>
  </SW.Blocks.FC>
</Document>
```

`Program.V2.xml` differs only in the marker:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Document>
  <SW.Blocks.FC ID="0">
    <AttributeList>
      <Name>Program</Name>
      <ProgrammingLanguage>LAD</ProgrammingLanguage>
      <Comment>GitAcceptanceV2</Comment>
    </AttributeList>
  </SW.Blocks.FC>
</Document>
```

Create `manifest.json`:

```json
{
  "schemaVersion": "1.0",
  "publicApiBuild": "2100.0.121.1",
  "fixtures": [
    {
      "file": "Program.V1.xml",
      "kind": "VciWorkspaceFile",
      "sourceType": "synthetic",
      "encoding": "utf-8",
      "bom": false,
      "sanitizationActions": [
        "Replaced project identity with TiaGitAcceptance",
        "Replaced block identity with Program",
        "Removed author, device, network, credential, and machine-path data"
      ],
      "sha256": "690C4E7735363DC9E14078A288EE4A01F2A00E9304AA15C7E81918321A718D94",
      "expectedSupportLevel": "Full"
    },
    {
      "file": "Program.V2.xml",
      "kind": "VciWorkspaceFile",
      "sourceType": "synthetic",
      "encoding": "utf-8",
      "bom": false,
      "sanitizationActions": [
        "Replaced project identity with TiaGitAcceptance",
        "Replaced block identity with Program",
        "Removed author, device, network, credential, and machine-path data"
      ],
      "sha256": "7E847FEA78F94489BCF3A200AC147C31657B4068D8F5B28CCDF9DF6FA646E54B",
      "expectedSupportLevel": "Full"
    }
  ]
}
```

- [ ] **Step 6: Add manifest hash and sanitization assertions**

Extend `ProjectBoundaryTests` with `VciGitFixturesMatchManifestAndContainNoSensitiveData`. Parse the manifest, recompute each SHA-256, require the two markers, and reject these case-insensitive patterns from every fixture: `password`, `token`, `authorization`, `customer`, an IPv4 address, `http://`, `https://`, and `[A-Z]:\`. Use `SHA256.Create()` and compare uppercase hex.

- [ ] **Step 7: Run focused tests and confirm GREEN**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ProjectBoundaryTests"
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release
```

Expected: both commands PASS; the second reports zero tests until Task 2 adds its first test class.

- [ ] **Step 8: Refactor and verify the solution boundary**

Run:

```powershell
dotnet sln TiaGitAddIn.sln list
dotnet list src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj reference
dotnet list src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj package
```

Expected: four solution projects; one project reference to Core; only the four test packages listed above.

- [ ] **Step 9: Commit the independently reviewable scaffold**

```powershell
git add TiaGitAddIn.sln src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj src/TiaGitAddIn.Tests/Configuration/ProjectBoundaryTests.cs src/TiaGitAddIn.IntegrationTests
git commit -m "test: scaffold core git integration project"
```

---

### Task 2: Build the isolated no-shell Git process adapter and safe diagnostics

**Acceptance criteria:** AC-068, AC-070, AC-076, AC-077, AC-097, AC-111.

**Files:**

- Create: `src/TiaGitAddIn.IntegrationTests/Infrastructure/GitProcessTrace.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/Infrastructure/GitTestEnvironment.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/Infrastructure/SystemGitProcessRunner.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/Infrastructure/GitFailureDiagnostics.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/SystemGitProcessRunnerTests.cs`
- Modify: `src/TiaGitAddIn/Services/GitProcessRunner.cs`
- Modify: `src/TiaGitAddIn/UI/GitPanelLaunchService.cs`
- Modify: `src/TiaGitAddIn.Tests/Configuration/ProjectBoundaryTests.cs`

**Interfaces:**

- Consumes: `IGitProcessRunner.RunAsync(string gitExecutablePath, string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)` and `GitProcessResult`.
- Produces: `SystemGitProcessRunner(IReadOnlyDictionary<string,string> environment, TimeSpan timeout, TimeProvider timeProvider)`; `IReadOnlyList<GitProcessTrace> Traces`; `GitFailureDiagnostics.Format(string phase, string gitVersion, GitProcessTrace trace, string testRoot)`.
- Production remains `GitProcessRunner` based on `Siemens.Engineering.AddIn.Utilities.Process`; the test adapter is internal to the integration assembly and unreachable from production.

- [ ] **Step 1: Write failing adapter tests**

Create tests named exactly:

```csharp
[Fact]
public void StartInfoUsesArgumentListWithoutShell()
{
    IReadOnlyDictionary<string, string> environment = GitTestEnvironment.Create("C:\\tge2e\\a");
    ProcessStartInfo info = SystemGitProcessRunner.CreateStartInfo(
        "git",
        "C:\\tge2e\\a",
        ["add", "--", "-literal & value.xml"],
        environment);

    Assert.False(info.UseShellExecute);
    Assert.True(info.RedirectStandardOutput);
    Assert.True(info.RedirectStandardError);
    Assert.True(info.CreateNoWindow);
    Assert.Equal(["add", "--", "-literal & value.xml"], info.ArgumentList);
}

[Fact]
public async Task SeparateRunnersReadOnlyTheirOwnGlobalConfig()
{
    await using IsolatedRunnerPair pair = await IsolatedRunnerPair.CreateAsync();
    GitProcessResult left = await pair.Left.RunAsync("git", pair.LeftRoot, ["config", "--global", "test.marker"], default);
    GitProcessResult right = await pair.Right.RunAsync("git", pair.RightRoot, ["config", "--global", "test.marker"], default);

    Assert.Equal("left", left.StandardOutput.Trim());
    Assert.Equal("right", right.StandardOutput.Trim());
    Assert.NotEqual(pair.LeftGlobalConfig, pair.RightGlobalConfig);
}

[Fact]
public void FailureDiagnosticsAreBoundedRedactedAndPhaseSpecific()
{
    GitProcessTrace trace = new(
        "git version 2.50.0.windows.1",
        "C:\\tge2e\\fixed\\work",
        ["commit", "-m", "token=secret-value", "https://user:password@example.invalid/repo"],
        128,
        false,
        TimeSpan.FromMilliseconds(125),
        new string('o', 6000),
        "controlled-failure password=hunter2");

    string text = GitFailureDiagnostics.Format("Commit", "git version 2.50.0.windows.1", trace, "C:\\tge2e\\fixed");

    Assert.Contains("phase=Commit", text);
    Assert.Contains("exitCode=128", text);
    Assert.Contains("controlled-failure", text);
    Assert.Contains("elapsedMs=125", text);
    Assert.Contains("<TEST_ROOT>", text);
    Assert.DoesNotContain("secret-value", text);
    Assert.DoesNotContain("hunter2", text);
    Assert.DoesNotContain("user:password", text);
    Assert.True(text.Length <= 10000);
}
```

- [ ] **Step 2: Run the adapter tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~SystemGitProcessRunnerTests"
```

Expected: compilation FAIL because the four infrastructure types do not exist.

- [ ] **Step 3: Add the immutable trace and per-process environment contract**

Use these exact types:

```csharp
internal sealed record GitProcessTrace(
    string GitVersion,
    string WorkingDirectory,
    IReadOnlyList<string> Arguments,
    int ExitCode,
    bool TimedOut,
    TimeSpan Elapsed,
    string StandardOutput,
    string StandardError);

internal static class GitTestEnvironment
{
    public static IReadOnlyDictionary<string, string> Create(string root) =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GIT_CONFIG_NOSYSTEM"] = "1",
            ["GIT_CONFIG_GLOBAL"] = Path.Combine(root, "git-global.config"),
            ["GIT_TERMINAL_PROMPT"] = "0",
            ["GCM_INTERACTIVE"] = "Never",
            ["GIT_ASKPASS"] = string.Empty,
            ["SSH_ASKPASS"] = string.Empty,
            ["HOME"] = Path.Combine(root, "home"),
            ["XDG_CONFIG_HOME"] = Path.Combine(root, "xdg"),
            ["GIT_AUTHOR_DATE"] = "2026-07-16T10:00:00Z",
            ["GIT_COMMITTER_DATE"] = "2026-07-16T10:00:00Z",
            ["TZ"] = "UTC",
            ["LC_ALL"] = "C"
        };
}
```

Copy the dictionary in the runner constructor; never expose or mutate the caller's dictionary.

- [ ] **Step 4: Implement `SystemGitProcessRunner` with discrete arguments**

The implementation must preserve these exact construction and process-start boundaries:

```csharp
internal sealed class SystemGitProcessRunner : IGitProcessRunner
{
    public const string AdapterId = "system-diagnostics-process-test-only";
    private readonly IReadOnlyDictionary<string, string> environment;
    private readonly TimeSpan timeout;
    private readonly TimeProvider timeProvider;
    private readonly object traceLock = new();
    private GitProcessTrace[] traces = [];

    public SystemGitProcessRunner(
        IReadOnlyDictionary<string, string> environment,
        TimeSpan timeout,
        TimeProvider timeProvider)
    {
        this.environment = new Dictionary<string, string>(environment, StringComparer.OrdinalIgnoreCase);
        this.timeout = timeout;
        this.timeProvider = timeProvider;
    }

    public IReadOnlyList<GitProcessTrace> Traces
    {
        get
        {
            lock (traceLock)
            {
                return traces.ToArray();
            }
        }
    }

    internal static ProcessStartInfo CreateStartInfo(
        string gitExecutablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> environment)
    {
        ProcessStartInfo info = new()
        {
            FileName = gitExecutablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (string key in info.Environment.Keys
            .Where(key => key.StartsWith("GIT_", StringComparison.OrdinalIgnoreCase)
                || key.StartsWith("GCM_", StringComparison.OrdinalIgnoreCase)
                || key.Equals("SSH_ASKPASS", StringComparison.OrdinalIgnoreCase))
            .ToArray())
        {
            info.Environment.Remove(key);
        }

        foreach ((string key, string value) in environment)
        {
            info.Environment[key] = value;
        }

        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return info;
    }

    public async Task<GitProcessResult> RunAsync(
        string gitExecutablePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo info = CreateStartInfo(
            gitExecutablePath,
            workingDirectory,
            arguments.ToArray(),
            environment);
        using Process process = new() { StartInfo = info };
        long started = timeProvider.GetTimestamp();
        process.Start();
        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        string stdout = await stdoutTask.ConfigureAwait(false);
        string stderr = await stderrTask.ConfigureAwait(false);
        GitProcessResult result = new()
        {
            ExitCode = timedOut ? -1 : process.ExitCode,
            TimedOut = timedOut,
            StandardOutput = stdout,
            StandardError = timedOut ? "Git operation timed out." : stderr
        };

        AppendTrace(new GitProcessTrace(
            GitVersion: arguments.SequenceEqual(["--version"]) ? stdout.Trim() : string.Empty,
            WorkingDirectory: workingDirectory,
            Arguments: arguments.ToArray(),
            ExitCode: result.ExitCode,
            TimedOut: result.TimedOut,
            Elapsed: timeProvider.GetElapsedTime(started),
            StandardOutput: result.StandardOutput,
            StandardError: result.StandardError));
        return result;
    }

    private void AppendTrace(GitProcessTrace trace)
    {
        lock (traceLock)
        {
            traces = [.. traces, trace];
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
```

- [ ] **Step 5: Implement bounded, redacted phase diagnostics**

`GitFailureDiagnostics.Format` must output these keys in stable order: `phase`, `gitVersion`, `command`, `workingDirectory`, `exitCode`, `timedOut`, `elapsedMs`, `stdout`, `stderr`, `testRoot`. Limit stdout and stderr to 4096 characters each; replace the root with `<TEST_ROOT>`; replace URL user information and values following `password`, `passwd`, `token`, `api_key`, `apikey`, and `authorization` with `<REDACTED>`.

- [ ] **Step 6: Keep the production/test composition roots distinct**

Add this constant to the net48 runner:

```csharp
public const string AdapterId = "siemens-addin-utilities";
```

In `GitPanelLaunchService.CreateViewModel`, immediately after constructing the production runner, add:

```csharp
logger.Info("Git process adapter: " + GitProcessRunner.AdapterId);
```

Extend `ProjectBoundaryTests` to assert that production source contains `new GitProcessRunner()` and `GitProcessRunner.AdapterId`, contains no `SystemGitProcessRunner`, and that the integration source contains `system-diagnostics-process-test-only` but no Siemens namespace.

- [ ] **Step 7: Run focused adapter and boundary tests**

Run:

```powershell
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~SystemGitProcessRunnerTests"
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ProjectBoundaryTests"
```

Expected: PASS; neither test mutates the parent process environment or invokes a shell.

- [ ] **Step 8: Refactor defensive copies and cancellation paths**

Run the adapter test class twice and inspect that every invocation's argument array and environment dictionary remain unchanged after the caller mutates its original inputs. Add a cancellation test that starts a controlled long-running Git alias, cancels, observes `OperationCanceledException`, and confirms the child process exits.

- [ ] **Step 9: Commit the adapter boundary**

```powershell
git add src/TiaGitAddIn.IntegrationTests src/TiaGitAddIn/Services/GitProcessRunner.cs src/TiaGitAddIn/UI/GitPanelLaunchService.cs src/TiaGitAddIn.Tests/Configuration/ProjectBoundaryTests.cs
git commit -m "test: isolate real git process execution"
```

---

### Task 3: Implement the hermetic real-Git fixture and exact VCI workflow scenario

**Acceptance criteria:** AC-068 through AC-078, AC-097, AC-112.

**Files:**

- Create: `src/TiaGitAddIn.IntegrationTests/Infrastructure/RetryingDirectoryCleanup.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/Infrastructure/VciGitRepositoryFixture.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/VciGitRepositoryFixtureTests.cs`
- Create: `src/TiaGitAddIn.IntegrationTests/VciGitWorkflowTests.cs`

**Interfaces:**

- Consumes: `GitService(IGitProcessRunner runner, OperationSerializer serializer, string gitExecutablePath, string repositoryRoot)` and existing Core methods `GetStatusAsync`, `StageAsync`, `CommitAsync`, `GetCommitLogAsync`, `GetCommitFilesAsync`, `GetCommitDiffAsync`, and parameterless `PushAsync`.
- Produces: `VciGitRepositoryFixture.CreateAsync(IReadOnlyDictionary<string,string>? hostEnvironment = null, IDirectoryDeletionBoundary? deletionBoundary = null, CancellationToken cancellationToken = default)`; properties `RootPath`, `WorkingRepositoryPath`, `BareRemotePath`, `BaselineHash`, `Git`, and `Runner`; methods `ApplyV2WorkspaceChangeAsync`, `ReadRemoteMainHashAsync`, and `ReadRemoteProgramBytesAsync`.

- [ ] **Step 1: Write the failing workflow test from the workspace-change boundary**

Create `VciGitWorkflowTests.cs` with this scenario and exact assertions:

```csharp
public sealed class VciGitWorkflowTests
{
    [Fact]
    [Trait("Category", "GitE2E")]
    public async Task FixtureWorkspaceChangeCanBeCommittedAndPushed()
    {
        await using VciGitRepositoryFixture fixture = await VciGitRepositoryFixture.CreateAsync();
        await fixture.ApplyV2WorkspaceChangeAsync();

        GitStatus unstaged = await fixture.Git.GetStatusAsync();
        FileStatusEntry unstagedEntry = Assert.Single(unstaged.Entries);
        Assert.Equal("Program.xml", unstagedEntry.FilePath);
        Assert.Equal(FileStatus.Unmodified, unstagedEntry.IndexStatus);
        Assert.Equal(FileStatus.Modified, unstagedEntry.WorkTreeStatus);
        Assert.True(unstagedEntry.IsUnstaged);
        Assert.False(unstagedEntry.IsStaged);

        OperationResult stage = await fixture.Git.StageAsync(["Program.xml"]);
        Assert.True(stage.Success, stage.DisplayMessage);
        GitStatus staged = await fixture.Git.GetStatusAsync();
        FileStatusEntry stagedEntry = Assert.Single(staged.Entries);
        Assert.Equal("Program.xml", stagedEntry.FilePath);
        Assert.Equal(FileStatus.Modified, stagedEntry.IndexStatus);
        Assert.Equal(FileStatus.Unmodified, stagedEntry.WorkTreeStatus);
        Assert.True(stagedEntry.IsStaged);
        Assert.False(stagedEntry.IsUnstaged);

        OperationResult commit = await fixture.Git.CommitAsync(VciGitRepositoryFixture.V2CommitSubject);
        Assert.True(commit.Success, commit.DisplayMessage);
        Assert.True((await fixture.Git.GetStatusAsync()).IsClean);

        CommitInfo head = Assert.Single((await fixture.Git.GetCommitLogAsync(1)));
        Assert.Equal(VciGitRepositoryFixture.V2CommitSubject, head.Subject);
        Assert.Matches("^[0-9a-f]{40}$", head.Hash);
        Assert.Equal(fixture.BaselineHash, head.ParentHash);

        Assert.Equal(new[] { "Program.xml" }, await fixture.Git.GetCommitFilesAsync(head.Hash));
        DiffEntry entry = Assert.Single((await fixture.Git.GetCommitDiffAsync(head.Hash)).Entries);
        Assert.Equal("Program.xml", entry.FilePath);
        Assert.Contains(entry.Hunks.SelectMany(hunk => hunk.Lines),
            line => line.Type == DiffLineType.Deleted && line.Content.Contains("GitAcceptanceV1", StringComparison.Ordinal));
        Assert.Contains(entry.Hunks.SelectMany(hunk => hunk.Lines),
            line => line.Type == DiffLineType.Added && line.Content.Contains("GitAcceptanceV2", StringComparison.Ordinal));

        int beforePush = fixture.Runner.Traces.Count;
        OperationResult push = await fixture.Git.PushAsync();
        Assert.True(push.Success, push.DisplayMessage);
        GitProcessTrace pushTrace = fixture.Runner.Traces[beforePush];
        Assert.Equal(["push", "origin"], pushTrace.Arguments);
        Assert.Equal(head.Hash, await fixture.ReadRemoteMainHashAsync());
        Assert.Equal(
            await File.ReadAllBytesAsync(fixture.V2FixturePath),
            await fixture.ReadRemoteProgramBytesAsync());
        Assert.True((await fixture.Git.GetStatusAsync()).IsClean);
    }
}
```

- [ ] **Step 2: Run the scenario and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~VciGitWorkflowTests.FixtureWorkspaceChangeCanBeCommittedAndPushed"
```

Expected: compilation FAIL because `VciGitRepositoryFixture` does not exist.

- [ ] **Step 3: Implement exact fixture creation and baseline commands**

Use these constants and public surface:

```csharp
internal sealed class VciGitRepositoryFixture : IAsyncDisposable
{
    public const string WorkspaceRelativePath = "Program.xml";
    public const string BaselineCommitSubject = "test: establish Program V1 baseline";
    public const string V2CommitSubject = "test: synchronize Program V2";

    public string RootPath { get; }
    public string WorkingRepositoryPath { get; }
    public string BareRemotePath { get; }
    public string V2FixturePath { get; }
    public string BaselineHash { get; private set; } = string.Empty;
    public GitService Git { get; }
    public SystemGitProcessRunner Runner { get; }

    public static Task<VciGitRepositoryFixture> CreateAsync(
        IReadOnlyDictionary<string, string>? hostEnvironment = null,
        IDirectoryDeletionBoundary? deletionBoundary = null,
        CancellationToken cancellationToken = default);

    public Task ApplyV2WorkspaceChangeAsync(CancellationToken cancellationToken = default);
    public Task<string> ReadRemoteMainHashAsync(CancellationToken cancellationToken = default);
    public Task<byte[]> ReadRemoteProgramBytesAsync(CancellationToken cancellationToken = default);
    public ValueTask DisposeAsync();
}
```

`CreateAsync` performs these discrete argument arrays through `SystemGitProcessRunner`; none is concatenated into a command string:

```csharp
await Required("GitVersion", root, ["--version"]);
await Required("InitBareRemote", root, ["init", "--bare", "--initial-branch=main", bareRemote]);
await Required("InitWorkingRepository", root, ["init", "--initial-branch=main", workingRepository]);
await Required("ConfigureUserName", workingRepository, ["config", "--local", "user.name", "TIA Git Acceptance"]);
await Required("ConfigureUserEmail", workingRepository, ["config", "--local", "user.email", "tia-git-acceptance@example.invalid"]);
await Required("DisableSigning", workingRepository, ["config", "--local", "commit.gpgsign", "false"]);
await Required("IsolateHooks", workingRepository, ["config", "--local", "core.hooksPath", hooksDirectory]);
await Required("DisableAutocrlf", workingRepository, ["config", "--local", "core.autocrlf", "false"]);
await Required("DisableCredentialHelper", workingRepository, ["config", "--local", "credential.helper", string.Empty]);
await Required("AddOrigin", workingRepository, ["remote", "add", "origin", bareRemote]);
File.Copy(v1Fixture, Path.Combine(workingRepository, WorkspaceRelativePath), overwrite: false);
await Required("StageBaseline", workingRepository, ["add", "--", WorkspaceRelativePath]);
await Required("CommitBaseline", workingRepository, ["commit", "-m", BaselineCommitSubject]);
await Required("PushBaseline", workingRepository, ["push", "--set-upstream", "origin", "main"]);
```

Then independently require equality of `git rev-parse HEAD`, `git rev-parse refs/remotes/origin/main`, and `git --git-dir=$bareRemote rev-parse refs/heads/main`, and require remote `show refs/heads/main:Program.xml` bytes to equal the V1 fixture before returning the fixture.

- [ ] **Step 4: Enforce short unique roots and local-only remotes**

Resolve the base root from `TIA_GIT_E2E_ROOT`; otherwise use `Path.Combine(Path.GetTempPath(), "tge2e")`. Append a 12-character lowercase GUID segment. Reject a non-rooted base, a resulting root longer than 120 characters, or any created path longer than 260 characters. After `remote add`, call `GetRemotesAsync` and require both fetch/push URLs to resolve beneath `RootPath`; reject URI schemes, UNC roots, and paths outside the fixture root.

- [ ] **Step 5: Implement deterministic cleanup with injectable lock failures**

Use these exact interfaces and result:

```csharp
internal interface IDirectoryDeletionBoundary
{
    void Delete(string rootPath);
}

internal sealed record CleanupResult(bool Deleted, bool Preserved, int Attempts, string RootPath);

internal sealed class RetryingDirectoryCleanup(
    IDirectoryDeletionBoundary boundary,
    int maxAttempts,
    TimeSpan retryDelay,
    TimeProvider timeProvider)
{
    public Task<CleanupResult> DeleteAsync(
        string rootPath,
        bool preserveLocally,
        bool isCi,
        CancellationToken cancellationToken = default);
}
```

Retry only `IOException` and `UnauthorizedAccessException`, with five attempts at 100, 200, 400, and 800 ms between attempts. `isCi` overrides `preserveLocally`. An exhausted cleanup throws `IOException` containing the retained root and attempt count. `CreateAsync` wraps all setup in `try/catch` and invokes cleanup before rethrowing, so setup failures cannot leak repositories.

- [ ] **Step 6: Add fixture isolation and cleanup tests**

Create exact tests for:

```text
SequentialAndConcurrentFixturesUseDistinctRootsConfigsAndRefs
EveryCreatedPathIsAtMost260Characters
LocalAndSystemGitConfigurationCannotLeakIntoChildProcesses
FailureDiagnosticsIdentifyCommitAndRedactSecrets
CleanupRetriesNLocksAndSucceedsOnAttemptNPlusOne
CleanupReportsRetainedPathAfterRetryBudget
RealFilesystemCleanupRemovesRoot
KeepTempPreservesOnlyForLocalRuns
CiIgnoresKeepTempAndDeletes
SetupFailureAndTestBodyFailureBothDispose
LeadingDashAndShellMetacharacterPathIsStagedLiterallyAfterDoubleDash
```

The leading-dash test creates `-literal & value.xml`, calls `Git.StageAsync([path])`, asserts the runner trace is `add`, `--`, exact path, and verifies that no second process or file named `whoami` appears.

- [ ] **Step 7: Run the focused real-Git suite**

Run:

```powershell
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~VciGitRepositoryFixtureTests|FullyQualifiedName~VciGitWorkflowTests"
```

Expected: PASS using only local repositories; the workflow test begins after `ApplyV2WorkspaceChangeAsync` replaces V1 with V2.

- [ ] **Step 8: Run the test twice and concurrently**

Run the focused command twice, then:

```powershell
$jobs = 1..2 | ForEach-Object {
  Start-Job -ScriptBlock {
    Set-Location $using:PWD
    dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~VciGitWorkflowTests"
  }
}
$jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
```

Expected: all four total runs PASS, roots/configs differ, and no root remains when `TIA_GIT_E2E_KEEP_TEMP` is unset.

- [ ] **Step 9: Refactor phase naming and failure output**

Centralize fixture raw-Git calls in `Required(string phase, string workingDirectory, IReadOnlyList<string> arguments)`. On a failed result, throw one `InvalidOperationException` containing `GitFailureDiagnostics.Format`; never expose raw credentials or an unbounded stream.

- [ ] **Step 10: Commit the end-to-end Git lane**

```powershell
git add src/TiaGitAddIn.IntegrationTests
git commit -m "test: cover vci workspace git workflow"
```

---

### Task 4: Pin merged coverage and create the reusable Windows test gate

**Acceptance criteria:** AC-079, AC-090 through AC-094, AC-103, AC-109.

**Files:**

- Create: `scripts/Assert-CoberturaThreshold.ps1`
- Create: `scripts/Invoke-TestGate.ps1`
- Create: `scripts/Test-GitHubWorkflowSecurity.ps1`
- Create: `.github/workflows/test-gate.yml`
- Create: `src/TiaGitAddIn.Tests/Configuration/CoverageGateTests.cs`
- Create: `src/TiaGitAddIn.Tests/Configuration/GitHubWorkflowSecurityTests.cs`
- Create: `docs/testing/github-actions-runner-security.md`
- Modify: `src/TiaGitAddIn.Tests/Configuration/ReleaseWorkflowTests.cs`
- Modify: `.github/workflows/release.yml` only to pin its existing actions, disable persisted checkout credentials, and select the trusted release pool; Task 7 still replaces its release behavior.

**Interfaces:**

- Consumes: Task 1's two Coverlet MSBuild 6.0.4 projects and Task 3's unfiltered `GitE2E` test.
- Produces: `pwsh -NoProfile -File scripts/Invoke-TestGate.ps1`; `pwsh -NoProfile -File scripts/Test-GitHubWorkflowSecurity.ps1`; intermediate `TestResults/Coverage/unit/coverage.json`; merged `TestResults/Coverage/coverage.json`; final `TestResults/Coverage/coverage.cobertura.xml`; reusable workflow `./.github/workflows/test-gate.yml`; runner-policy administrator checklist.

- [ ] **Step 1: Replace the old direct-release expectation with failing gate tests**

Create `CoverageGateTests.cs` and update `ReleaseWorkflowTests.cs` so the RED suite asserts all of the following exact contracts:

```csharp
[Fact]
public void GatePinsMergedCoverletCommandsAndRunsGitE2EUnfiltered()
{
    string script = File.ReadAllText(RepositoryFile("scripts", "Invoke-TestGate.ps1"));
    Assert.Contains("coverlet.msbuild", File.ReadAllText(RepositoryFile("src", "TiaGitAddIn.Tests", "TiaGitAddIn.Tests.csproj")));
    Assert.Contains("/p:CollectCoverage=true", script);
    Assert.Contains("xUnit.AppDomain=denied", script);
    Assert.Contains("/p:MergeWith=$unitJson", script);
    Assert.Contains("/p:CoverletOutputFormat=\\\"json,cobertura\\\"", script);
    Assert.Contains("/p:Threshold=80", script);
    Assert.Contains("/p:ThresholdType=line", script);
    Assert.Contains("/p:ThresholdStat=total", script);
    Assert.Contains("coverage.cobertura.xml", script);
    Assert.DoesNotContain("--filter", script, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void ReusableGateSeparatesEphemeralPullRequestsFromTrustedCalls()
{
    string workflow = File.ReadAllText(RepositoryFile(".github", "workflows", "test-gate.yml"));
    Assert.Contains("workflow_call:", workflow);
    Assert.Contains("pull_request:", workflow);
    Assert.Contains("branches: [ main ]", workflow);
    Assert.Contains("if: github.event_name == 'pull_request'", workflow);
    Assert.Contains("runs-on: [ self-hosted, Windows, tia-pr-ephemeral ]", workflow);
    Assert.Contains("if: github.event_name != 'pull_request'", workflow);
    Assert.Contains("runs-on: [ self-hosted, Windows, tia-ci-trusted ]", workflow);
    Assert.Contains("pwsh -NoProfile -File scripts/Invoke-TestGate.ps1", workflow);
    Assert.Contains("actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2", workflow);
    Assert.Contains("TestResults/Coverage/coverage.cobertura.xml", workflow);
    Assert.DoesNotContain("pull_request_target", workflow);
}
```

Add a process-based threshold theory that writes minimal Cobertura documents with line rates `0.7999` and `0.8000`, stores the controlled report path in `reportPath`, runs `pwsh -NoProfile -File scripts/Assert-CoberturaThreshold.ps1 -ReportPath $reportPath -Minimum 80.00`, and asserts exit codes 1 and 0 respectively. The helper must set `UseShellExecute=false`, redirect output/error, and pass each argument separately.

Create `GitHubWorkflowSecurityTests.cs` with an immutable approved-action map containing exactly `actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1`, `actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1`, `actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2`, `actions/download-artifact@d3f86a106a0bac45b974a628896c90dbdf5c8093 # v4.3.0`, and `softprops/action-gh-release@3bb12739c298aeb8a4eeaf626c5b8d85266b0e65 # v2.6.2`. Tests enumerate every `.github/workflows/*.yml` `uses:` line, ignore only repository-local `./` targets, and fail unless each external target equals one approved entry with a lowercase 40-character SHA and version comment. A second test fails if any checkout block omits `persist-credentials: false`. A third invokes `scripts/Test-GitHubWorkflowSecurity.ps1` and expects exit zero. A fourth proves the runner-policy document requires outside-collaborator approval, `--ephemeral`, one job, deregistration, destruction/reimage after success or failure, and disjoint `tia-pr-ephemeral`, `tia-ci-trusted`, `tia-candidate-trusted`, `tia-live-v21-trusted`, and `tia-release-trusted` pools.

- [ ] **Step 2: Run the configuration suite and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~CoverageGateTests|FullyQualifiedName~GitHubWorkflowSecurityTests|FullyQualifiedName~ReleaseWorkflowTests"
```

Expected: FAIL because both scripts and `test-gate.yml` are absent and `release.yml` still publishes directly from a tag build.

- [ ] **Step 3: Implement the exact decimal threshold script**

Create `scripts/Assert-CoberturaThreshold.ps1`:

```powershell
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ReportPath,

    [ValidateRange(0, 100)]
    [decimal] $Minimum = 80.00
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$fullPath = [IO.Path]::GetFullPath($ReportPath)
if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
    throw "Cobertura report not found: $fullPath"
}

[xml] $document = Get-Content -Raw -LiteralPath $fullPath
$rawRate = [string] $document.coverage.'line-rate'
if ([string]::IsNullOrWhiteSpace($rawRate)) {
    throw "Cobertura root line-rate is missing: $fullPath"
}

$rate = [decimal]::Parse($rawRate, [Globalization.CultureInfo]::InvariantCulture)
$percent = $rate * 100
Write-Output ("Total line coverage: {0:N2}% (required: {1:N2}%)" -f $percent, $Minimum)
if ($percent -lt $Minimum) {
    Write-Error ("Total line coverage {0:N2}% is below {1:N2}%." -f $percent, $Minimum)
    exit 1
}

exit 0
```

- [ ] **Step 4: Implement the PowerShell-safe merged Coverlet gate**

Create `scripts/Invoke-TestGate.ps1` exactly as follows:

```powershell
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$coverageRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot "TestResults\Coverage"))
if (-not $coverageRoot.StartsWith($repoRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Coverage root escaped the repository: $coverageRoot"
}

if (Test-Path -LiteralPath $coverageRoot) {
    Remove-Item -LiteralPath $coverageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path (Join-Path $coverageRoot "unit") -Force | Out-Null

$solution = Join-Path $repoRoot "TiaGitAddIn.sln"
$unitProject = Join-Path $repoRoot "src\TiaGitAddIn.Tests\TiaGitAddIn.Tests.csproj"
$integrationProject = Join-Path $repoRoot "src\TiaGitAddIn.IntegrationTests\TiaGitAddIn.IntegrationTests.csproj"
$unitBase = Join-Path $coverageRoot "unit\coverage"
$unitJson = $unitBase + ".json"
$mergedBase = Join-Path $coverageRoot "coverage"
$cobertura = $mergedBase + ".cobertura.xml"

function Invoke-DotNet {
    param([Parameter(Mandatory)][string[]] $Arguments)
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet failed with exit code $LASTEXITCODE: $($Arguments -join ' ')"
    }
}

Push-Location $repoRoot
try {
    Invoke-DotNet -Arguments @("restore", $solution)
    Invoke-DotNet -Arguments @(
        "build", $solution,
        "--configuration", "Release",
        "--no-restore",
        "/p:EnableTiaAddInPackaging=false",
        "/p:ContinuousIntegrationBuild=true")

    Invoke-DotNet -Arguments @(
        "test", $unitProject,
        "--configuration", "Release",
        "--no-build", "--no-restore", "-m:1",
        "/p:EnableTiaAddInPackaging=false",
        "/p:CollectCoverage=true",
        "/p:CoverletOutput=$unitBase",
        "/p:CoverletOutputFormat=json",
        '/p:Include=\"[TiaGitAddIn.Core]*,[TiaGitAddIn]*\"',
        '/p:ExcludeByFile=\"**/*.g.cs,**/*.g.i.cs\"',
        '/p:ExcludeByAttribute=\"GeneratedCodeAttribute,CompilerGeneratedAttribute\"',
        "--", "xUnit.AppDomain=denied")

    if (-not (Test-Path -LiteralPath $unitJson -PathType Leaf)) {
        throw "net48 coverage JSON missing: $unitJson"
    }

    Invoke-DotNet -Arguments @(
        "test", $integrationProject,
        "--configuration", "Release",
        "--no-build", "--no-restore", "-m:1",
        "/p:CollectCoverage=true",
        "/p:MergeWith=$unitJson",
        "/p:CoverletOutput=$mergedBase",
        '/p:CoverletOutputFormat=\"json,cobertura\"',
        '/p:Include=\"[TiaGitAddIn.Core]*,[TiaGitAddIn]*\"',
        '/p:ExcludeByFile=\"**/*.g.cs,**/*.g.i.cs\"',
        '/p:ExcludeByAttribute=\"GeneratedCodeAttribute,CompilerGeneratedAttribute\"',
        "/p:Threshold=80",
        "/p:ThresholdType=line",
        "/p:ThresholdStat=total")

    if (-not (Test-Path -LiteralPath ($mergedBase + ".json") -PathType Leaf)) {
        throw "Merged coverage JSON missing: $($mergedBase).json"
    }
    if (-not (Test-Path -LiteralPath $cobertura -PathType Leaf)) {
        throw "Merged Cobertura report missing: $cobertura"
    }

    & (Join-Path $PSScriptRoot "Assert-CoberturaThreshold.ps1") `
        -ReportPath $cobertura `
        -Minimum 80.00
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
```

The net48 command emits only JSON and denies the xUnit AppDomain. The net8 command is unfiltered, merges that JSON, emits JSON plus Cobertura, and supplies all three Coverlet threshold properties.

- [ ] **Step 5: Create the reusable PR/main/candidate workflow**

Create `.github/workflows/test-gate.yml`:

```yaml
name: Test and coverage gate

on:
  workflow_call:
  pull_request:
  push:
    branches: [ main ]

permissions:
  contents: read

concurrency:
  group: test-gate-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  pr_test:
    if: github.event_name == 'pull_request'
    name: PR net48 + net8 merged coverage
    runs-on: [ self-hosted, Windows, tia-pr-ephemeral ]
    timeout-minutes: 45
    steps:
      - name: Check out repository
        uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
        with:
          persist-credentials: false
          clean: true
          fetch-depth: 0

      - name: Set up pinned .NET SDK
        uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1
        with:
          dotnet-version: "8.0.420"

      - name: Restore, build, test, merge, and enforce coverage
        shell: pwsh
        run: pwsh -NoProfile -File scripts/Invoke-TestGate.ps1

      - name: Upload merged coverage
        uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
        with:
          name: coverage-pr-${{ github.run_id }}
          path: |
            TestResults/Coverage/coverage.json
            TestResults/Coverage/coverage.cobertura.xml
          if-no-files-found: error
          retention-days: 30

  trusted_test:
    if: github.event_name != 'pull_request'
    name: Trusted net48 + net8 merged coverage
    runs-on: [ self-hosted, Windows, tia-ci-trusted ]
    timeout-minutes: 45
    steps:
      - name: Check out repository
        uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
        with:
          persist-credentials: false
          clean: true
          fetch-depth: 0

      - name: Set up pinned .NET SDK
        uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1
        with:
          dotnet-version: "8.0.420"

      - name: Restore, build, test, merge, and enforce coverage
        shell: pwsh
        run: pwsh -NoProfile -File scripts/Invoke-TestGate.ps1

      - name: Upload merged coverage
        uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
        with:
          name: coverage-trusted-${{ github.run_id }}
          path: |
            TestResults/Coverage/coverage.json
            TestResults/Coverage/coverage.cobertura.xml
          if-no-files-found: error
          retention-days: 30
```

Before enabling either trigger, create `docs/testing/github-actions-runner-security.md` and complete its administrator checklist: restrict every runner group to this repository; enable approval for every first-time or outside-collaborator fork workflow; register `tia-pr-ephemeral` agents with `--ephemeral`; allow one job only; capture deregistration plus VM/container destruction/reimage evidence after both success and forced failure; and prove the four trusted labels are separate groups with no PR access. `tia-live-v21-trusted` is an operator-controlled V21 host/pool and is never selected by `test-gate.yml`, `release-candidate.yml`, or `release.yml`.

Implement `scripts/Test-GitHubWorkflowSecurity.ps1` as an offline fail-closed scan. It enumerates all workflow YAML, rejects `pull_request_target`, mutable/non-approved external `uses:` targets, checkout without `persist-credentials: false`, an unlabelled self-hosted job, a PR job without `tia-pr-ephemeral`, or a trusted job containing the PR label. It also validates the exact approved SHA/version map from Step 1. In Task 4, mechanically pin actions and set `persist-credentials: false` in the existing `.github/workflows/release.yml`, and change its runner selector to `[ self-hosted, Windows, tia-release-trusted ]`; Task 7 still replaces its behavior. The scan performs no network call and reads no credential.

- [ ] **Step 6: Verify RED/GREEN thresholds and every gate phase**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~CoverageGateTests|FullyQualifiedName~GitHubWorkflowSecurityTests|FullyQualifiedName~ReleaseWorkflowTests"
pwsh -NoProfile -File scripts/Test-GitHubWorkflowSecurity.ps1
pwsh -NoProfile -File scripts/Invoke-TestGate.ps1
```

Expected: configuration tests PASS; the complete gate exits zero only when merged total line coverage is at least 80.00. Temporarily run each test fixture's controlled 79.99 and 80.00 reports and retain their exit-code assertions; do not weaken the threshold to make the current branch pass.

- [ ] **Step 7: Refactor the gate into one authoritative command**

Ensure workflows call only `scripts/Invoke-TestGate.ps1`; do not duplicate Coverlet properties in YAML. Ensure comparison plans refer to this script and do not own coverage configuration. Run `scripts/Test-GitHubWorkflowSecurity.ps1` before the gate in local release-readiness and CI configuration tests; it owns workflow security policy, not coverage behavior.

- [ ] **Step 8: Commit the reusable gate**

```powershell
git add scripts/Assert-CoberturaThreshold.ps1 scripts/Invoke-TestGate.ps1 scripts/Test-GitHubWorkflowSecurity.ps1 .github/workflows/test-gate.yml .github/workflows/release.yml src/TiaGitAddIn.Tests/Configuration/CoverageGateTests.cs src/TiaGitAddIn.Tests/Configuration/GitHubWorkflowSecurityTests.cs src/TiaGitAddIn.Tests/Configuration/ReleaseWorkflowTests.cs docs/testing/github-actions-runner-security.md
git commit -m "ci: enforce merged line coverage gate"
```

---

### Task 5: Build, stamp, hash, and upload one immutable V21 candidate

**Acceptance criteria:** AC-080, AC-089, AC-094, AC-106, AC-108.

**Files:**

- Create: `scripts/New-CandidateProvenance.ps1`
- Create: `.github/workflows/release-candidate.yml`
- Modify: `src/TiaGitAddIn/TiaGitAddIn.csproj`
- Modify: `src/TiaGitAddIn/AddInPublisherConfiguration.xml`
- Modify: `src/TiaGitAddIn.Tests/Configuration/AddInPublisherConfigurationTests.cs`
- Modify: `src/TiaGitAddIn.Tests/Configuration/ReleaseWorkflowTests.cs`
- Verify: `src/TiaGitAddIn.Tests/Configuration/GitHubWorkflowSecurityTests.cs`
- Verify: `scripts/Test-GitHubWorkflowSecurity.ps1`

**Interfaces:**

- Consumes: passing `./.github/workflows/test-gate.yml`, `TiaGitAddIn.addin`, V21 publisher namespace `http://www.siemens.com/automation/Openness/AddIn/Publisher/V21`.
- Produces: artifact name `"tia-git-addin-candidate-$candidateId-$sourceCommit"` where both variables come from validated workflow inputs/context; files `TiaGitAddIn.addin`, `TiaGitAddIn.addin.sha256`, and `candidate-provenance.json`; upload step outputs `artifact-id`, `artifact-url`, and `artifact-digest` recorded in the job summary without altering uploaded files.

- [ ] **Step 1: Write failing candidate and permission contract tests**

Add these assertions:

```csharp
[Fact]
public void CandidateCallsReusableGateAndBuildsPackageExactlyOnce()
{
    string workflow = ReadWorkflow("release-candidate.yml");
    Assert.Contains("uses: ./.github/workflows/test-gate.yml", workflow);
    Assert.Contains("needs: test_gate", workflow);
    Assert.Equal(1, CountOccurrences(workflow, "dotnet build"));
    Assert.Contains("New-CandidateProvenance.ps1", workflow);
    Assert.Contains("steps.upload.outputs.artifact-id", workflow);
    Assert.Contains("runs-on: [ self-hosted, Windows, tia-candidate-trusted ]", workflow);
    Assert.Contains("actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1", workflow);
    Assert.Contains("persist-credentials: false", workflow);
    Assert.DoesNotContain("softprops/action-gh-release", workflow);
}

[Fact]
public void PublisherDeclaresDocumentedV21GitPermissionsAndNoComparePermission()
{
    XDocument document = XDocument.Load(PublisherConfigurationPath());
    XNamespace ns = "http://www.siemens.com/automation/Openness/AddIn/Publisher/V21";
    Assert.Single(document.Descendants(ns + "TIA.ReadWrite"));
    Assert.Single(document.Descendants(ns + "Siemens.Engineering.AddIn.Permissions.ProcessStartPermission"));
    Assert.DoesNotContain(document.Descendants(), element =>
        element.Name.LocalName.Contains("Compare", StringComparison.OrdinalIgnoreCase));
}
```

- [ ] **Step 2: Run candidate tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ReleaseWorkflowTests|FullyQualifiedName~AddInPublisherConfigurationTests|FullyQualifiedName~GitHubWorkflowSecurityTests"
```

Expected: FAIL because `release-candidate.yml` and the candidate identity target do not exist.

- [ ] **Step 3: Add output-only candidate identity stamping**

Add this target before `PublishTiaPortalAddIn`; it changes the copied output configuration, not the tracked source XML:

```xml
<Target Name="StampTiaCandidateIdentity"
        BeforeTargets="PublishTiaPortalAddIn"
        Condition="'$(TiaCandidateVersion)' != ''">
  <Error Condition="'$(AssemblyVersion)' != '$(TiaCandidateVersion)'"
         Text="AssemblyVersion must equal TiaCandidateVersion for an immutable candidate." />
  <Error Condition="'$(FileVersion)' != '$(TiaCandidateVersion)'"
         Text="FileVersion must equal TiaCandidateVersion for an immutable candidate." />
  <XmlPoke XmlInputPath="$(TargetDir)AddInPublisherConfiguration.xml"
           Query="/*[local-name()='PackageConfiguration']/*[local-name()='AddInVersion']"
           Value="$(TiaCandidateVersion)" />
  <XmlPoke XmlInputPath="$(TargetDir)AddInPublisherConfiguration.xml"
           Query="/*[local-name()='PackageConfiguration']/*[local-name()='Product']/*[local-name()='Version']"
           Value="$(TiaCandidateVersion)" />
</Target>
```

Keep `TargetFramework=net48`, `PlatformTarget=AnyCPU`, the V21 namespace, `TIA.ReadWrite`, and `ProcessStartPermission`. Do not add a compare permission or an internal Siemens assembly.

- [ ] **Step 4: Implement package inspection and pre-upload provenance**

`scripts/New-CandidateProvenance.ps1` has this exact parameter contract:

```powershell
param(
    [Parameter(Mandatory)][string] $PackagePath,
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]{2,63}$')][string] $CandidateId,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string] $SourceCommit,
    [Parameter(Mandatory)][ValidatePattern('^[0-9]+$')][string] $WorkflowRunId,
    [Parameter(Mandatory)][ValidatePattern('^21\.[0-9]+\.[0-9]+\.[0-9]+$')][string] $ExpectedVersion,
    [Parameter(Mandatory)][string] $OutputDirectory
)
```

Open the `.addin` with `System.IO.Compression.ZipFile`; require `EngineeringVersion` text exactly `V21`, `Meta/PublisherTarget` exactly `http://www.siemens.com/automation/Openness/AddIn/Publisher/V21`, `Meta/Version` exactly `$ExpectedVersion`, and one assembly entry whose URL-decoded name contains `version=$ExpectedVersion`. Compute uppercase SHA-256 and write:

```json
{
  "schemaVersion": "1.0",
  "candidateId": "validated workflow input",
  "sourceCommit": "40 lowercase hex characters",
  "workflowRunId": "decimal GitHub run ID",
  "package": {
    "fileName": "TiaGitAddIn.addin",
    "sha256": "64 uppercase hex characters",
    "identity": {
      "engineeringVersion": "V21",
      "publisherTarget": "http://www.siemens.com/automation/Openness/AddIn/Publisher/V21",
      "addInVersion": "21.0.build.attempt",
      "assemblyVersion": "21.0.build.attempt"
    }
  }
}
```

The quoted values above describe validation shapes; the script writes the actual validated input values. Serialize with UTF-8 without BOM and stable property order. With `$sha256` holding the uppercase digest, write `TiaGitAddIn.addin.sha256` as `"$sha256 *TiaGitAddIn.addin"` with ASCII encoding and a final newline.

- [ ] **Step 5: Create the candidate workflow with one publisher execution**

Create `.github/workflows/release-candidate.yml`:

```yaml
name: Release candidate

on:
  workflow_dispatch:
    inputs:
      candidate_id:
        description: Stable client candidate identifier
        required: true
        type: string

permissions:
  contents: read
  actions: read

jobs:
  test_gate:
    uses: ./.github/workflows/test-gate.yml

  build_candidate:
    name: Build immutable V21 candidate
    needs: test_gate
    runs-on: [ self-hosted, Windows, tia-candidate-trusted ]
    timeout-minutes: 30
    steps:
      - name: Check out tested source
        uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
        with:
          persist-credentials: false
          clean: true
          fetch-depth: 0

      - name: Set up pinned .NET SDK
        uses: actions/setup-dotnet@67a3573c9a986a3f9c594539f4ab511d57bb3ce9 # v4.3.1
        with:
          dotnet-version: "8.0.420"

      - name: Validate candidate ID and derive unique V21 identity
        id: identity
        shell: pwsh
        env:
          CANDIDATE_ID: ${{ inputs.candidate_id }}
          RUN_NUMBER: ${{ github.run_number }}
          RUN_ATTEMPT: ${{ github.run_attempt }}
        run: |
          if ($env:CANDIDATE_ID -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{2,63}$') {
            throw "Invalid candidate_id."
          }
          $build = [int] $env:RUN_NUMBER
          $attempt = [int] $env:RUN_ATTEMPT
          if ($build -gt 65534 -or $attempt -gt 65534) {
            throw "GitHub run number/attempt exceeds the AssemblyVersion component limit."
          }
          "version=21.0.$build.$attempt" >> $env:GITHUB_OUTPUT
          "artifact_name=tia-git-addin-candidate-$($env:CANDIDATE_ID)-${{ github.sha }}" >> $env:GITHUB_OUTPUT

      - name: Restore production project
        run: dotnet restore src/TiaGitAddIn/TiaGitAddIn.csproj

      - name: Build and publish package once
        shell: pwsh
        env:
          CANDIDATE_VERSION: ${{ steps.identity.outputs.version }}
        run: |
          dotnet build src/TiaGitAddIn/TiaGitAddIn.csproj `
            --configuration Release `
            --no-restore `
            /p:EnableTiaAddInPackaging=true `
            /p:TiaCandidateVersion=$env:CANDIDATE_VERSION `
            /p:AssemblyVersion=$env:CANDIDATE_VERSION `
            /p:FileVersion=$env:CANDIDATE_VERSION `
            /p:ContinuousIntegrationBuild=true
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

      - name: Inspect, hash, and record pre-upload provenance
        shell: pwsh
        env:
          CANDIDATE_ID: ${{ inputs.candidate_id }}
          CANDIDATE_VERSION: ${{ steps.identity.outputs.version }}
        run: |
          $out = Join-Path $env:RUNNER_TEMP "candidate"
          New-Item -ItemType Directory -Path $out -Force | Out-Null
          $package = "src/TiaGitAddIn/bin/Release/net48/TiaGitAddIn.addin"
          Copy-Item -LiteralPath $package -Destination (Join-Path $out "TiaGitAddIn.addin")
          pwsh -NoProfile -File scripts/New-CandidateProvenance.ps1 `
            -PackagePath (Join-Path $out "TiaGitAddIn.addin") `
            -CandidateId $env:CANDIDATE_ID `
            -SourceCommit "${{ github.sha }}" `
            -WorkflowRunId "${{ github.run_id }}" `
            -ExpectedVersion $env:CANDIDATE_VERSION `
            -OutputDirectory $out

      - name: Upload immutable candidate
        id: upload
        uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02 # v4.6.2
        with:
          name: ${{ steps.identity.outputs.artifact_name }}
          path: |
            ${{ runner.temp }}/candidate/TiaGitAddIn.addin
            ${{ runner.temp }}/candidate/TiaGitAddIn.addin.sha256
            ${{ runner.temp }}/candidate/candidate-provenance.json
          if-no-files-found: error
          retention-days: 90
          overwrite: false

      - name: Record server-assigned artifact metadata
        shell: pwsh
        run: |
          @"
          Candidate artifact ID: ${{ steps.upload.outputs.artifact-id }}
          Candidate artifact URL: ${{ steps.upload.outputs.artifact-url }}
          Candidate artifact digest: ${{ steps.upload.outputs.artifact-digest }}
          "@ >> $env:GITHUB_STEP_SUMMARY
```

- [ ] **Step 6: Run focused configuration tests and a local candidate build**

Run with a version not previously loaded into TIA:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ReleaseWorkflowTests|FullyQualifiedName~AddInPublisherConfigurationTests|FullyQualifiedName~GitHubWorkflowSecurityTests"
pwsh -NoProfile -File scripts/Test-GitHubWorkflowSecurity.ps1
dotnet build src/TiaGitAddIn/TiaGitAddIn.csproj -c Release --no-restore /p:EnableTiaAddInPackaging=true /p:TiaCandidateVersion=21.0.1.1 /p:AssemblyVersion=21.0.1.1 /p:FileVersion=21.0.1.1
pwsh -NoProfile -File scripts/New-CandidateProvenance.ps1 -PackagePath src/TiaGitAddIn/bin/Release/net48/TiaGitAddIn.addin -CandidateId local-21-0-1-1 -SourceCommit (git rev-parse HEAD) -WorkflowRunId 1 -ExpectedVersion 21.0.1.1 -OutputDirectory TestResults/Candidate
```

Expected: tests PASS; the package contains V21 publisher metadata and version `21.0.1.1`; the hash file matches recomputation. Do not install this local verification package if that identity was previously loaded.

- [ ] **Step 7: Refactor candidate validation and prove no second build**

Search `release-candidate.yml` and require exactly one `dotnet build`, no `dotnet publish`, and no release action. Confirm the gate passes before `build_candidate` and uses `EnableTiaAddInPackaging=false`, so the candidate publisher runs only in `Build and publish package once`.

- [ ] **Step 8: Commit candidate creation**

```powershell
git add scripts/New-CandidateProvenance.ps1 .github/workflows/release-candidate.yml src/TiaGitAddIn/TiaGitAddIn.csproj src/TiaGitAddIn/AddInPublisherConfiguration.xml src/TiaGitAddIn.Tests/Configuration/AddInPublisherConfigurationTests.cs src/TiaGitAddIn.Tests/Configuration/ReleaseWorkflowTests.cs
git commit -m "ci: create immutable tia v21 candidate"
```

---

### Task 6: Define and execute the live-TIA V21 acceptance/evidence lane

**Acceptance criteria:** AC-005, AC-080 through AC-088, AC-106, AC-110 through AC-112.

**Files:**

- Create: `docs/testing/schemas/live-tia-v21-evidence.schema.json`
- Create: `scripts/New-LiveTiaEvidenceBundle.ps1`
- Create: `scripts/Test-LiveTiaEvidence.ps1`
- Create: `scripts/Publish-LiveTiaEvidence.ps1`
- Create: `docs/testing/live-tia-v21-git-acceptance.md`
- Create: `src/TiaGitAddIn.Tests/Configuration/LiveTiaEvidenceTests.cs`

**Interfaces:**

- Consumes: Task 5 candidate package/provenance/artifact ID; TIA Portal V21; disposable `TiaGitAcceptance` project; mapped `Program` -> `Program.xml`; a local bare remote.
- Produces: `"TestResults/LiveTiaV21/$runId/evidence.json"`, `summary.md`, `git-transcript.txt`, `addin.log`, and the durable draft-release asset `"live-tia-v21-$candidateId-$runId-evidence.zip"`; release metadata records the evidence asset ID and the post-upload cleanup asset `"live-tia-v21-$candidateId-$runId-cleanup.json"`.
- The live run is manual and opt-in on an operator-controlled host or runner group labelled only `tia-live-v21-trusted`. It shares no runner, image, workspace, registration token, or service account with PR, main CI, candidate, or release pools. Scripts create/validate/package evidence, but no ordinary CI job launches TIA and no repository workflow may select this label.

- [ ] **Step 1: Write failing schema and sanitization tests**

Create `LiveTiaEvidenceTests.cs` with these cases:

```text
PassingBundleMatchesSchemaAndCandidateHash
FailureBeforeRemoteCreationAllowsNotReachedAndRequiresFailedStep
PassRequiresReadWriteAndProcessStartPermissionConfirmations
PassRequiresReviewerAndVerifiedRemoteHash
CandidateSourceRunArtifactIdentityAndHashMustMatch
SummaryReferencesTranscriptAndAddInLog
CredentialUrlTokenIpPrivatePathAndStackTraceAreRejected
PublishScriptRefusesWithoutApprovedExternalUploadSwitch
```

Each test creates files beneath its own temporary directory and invokes PowerShell with discrete arguments and `UseShellExecute=false`. The first test uses a local dummy package and its actual SHA-256. No test calls `gh`, contacts GitHub, or starts TIA.

- [ ] **Step 2: Run live-evidence tests and confirm RED**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~LiveTiaEvidenceTests"
```

Expected: FAIL because the schema and three evidence scripts do not exist.

- [ ] **Step 3: Create the exact live evidence schema**

Create `docs/testing/schemas/live-tia-v21-evidence.schema.json`:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://example.invalid/tia-git-addin/live-tia-v21-evidence.schema.json",
  "title": "TIA Git Add-In live TIA V21 evidence",
  "type": "object",
  "additionalProperties": false,
  "required": [
    "schemaVersion",
    "runId",
    "outcome",
    "failedStep",
    "startedUtc",
    "finishedUtc",
    "sourceCommit",
    "candidate",
    "tia",
    "reviewer",
    "permissions",
    "workspace",
    "files",
    "cleanup",
    "sanitization"
  ],
  "properties": {
    "schemaVersion": { "const": "1.0" },
    "runId": { "type": "string", "pattern": "^[0-9]{8}T[0-9]{6}Z-[A-Za-z0-9._-]{3,64}$" },
    "outcome": { "enum": ["pass", "fail"] },
    "failedStep": { "type": ["string", "null"], "minLength": 1, "maxLength": 120 },
    "startedUtc": { "type": "string", "format": "date-time" },
    "finishedUtc": { "type": "string", "format": "date-time" },
    "sourceCommit": { "type": "string", "pattern": "^[0-9a-f]{40}$" },
    "candidate": {
      "type": "object",
      "additionalProperties": false,
      "required": ["id", "workflowRunId", "artifactId", "artifactName", "package"],
      "properties": {
        "id": { "type": "string", "pattern": "^[A-Za-z0-9][A-Za-z0-9._-]{2,63}$" },
        "workflowRunId": { "type": "string", "pattern": "^[0-9]+$" },
        "artifactId": { "type": "string", "pattern": "^[0-9]+$" },
        "artifactName": { "type": "string", "pattern": "^tia-git-addin-candidate-[A-Za-z0-9._-]+-[0-9a-f]{40}$" },
        "package": {
          "type": "object",
          "additionalProperties": false,
          "required": ["fileName", "sha256", "installedSha256", "identity"],
          "properties": {
            "fileName": { "const": "TiaGitAddIn.addin" },
            "sha256": { "$ref": "#/definitions/sha256" },
            "installedSha256": { "$ref": "#/definitions/sha256" },
            "identity": {
              "type": "object",
              "additionalProperties": false,
              "required": ["engineeringVersion", "publisherTarget", "addInVersion", "assemblyVersion"],
              "properties": {
                "engineeringVersion": { "const": "V21" },
                "publisherTarget": { "const": "http://www.siemens.com/automation/Openness/AddIn/Publisher/V21" },
                "addInVersion": { "$ref": "#/definitions/v21Version" },
                "assemblyVersion": { "$ref": "#/definitions/v21Version" }
              }
            }
          }
        }
      }
    },
    "tia": {
      "type": "object",
      "additionalProperties": false,
      "required": ["productVersion", "publicApiBuild"],
      "properties": {
        "productVersion": { "type": "string", "minLength": 1, "maxLength": 80 },
        "publicApiBuild": { "const": "2100.0.121.1" }
      }
    },
    "reviewer": { "type": "string", "minLength": 1, "maxLength": 120 },
    "permissions": {
      "type": "object",
      "additionalProperties": false,
      "required": ["tiaReadWrite", "processStartPermission"],
      "properties": {
        "tiaReadWrite": { "type": "boolean" },
        "processStartPermission": { "type": "boolean" }
      }
    },
    "workspace": {
      "type": "object",
      "additionalProperties": false,
      "required": ["project", "mappedObject", "mappedFile", "v1Sha256", "v2Sha256", "baselineHash", "localCommitHash", "remoteHash"],
      "properties": {
        "project": { "const": "TiaGitAcceptance" },
        "mappedObject": { "const": "Program" },
        "mappedFile": { "const": "Program.xml" },
        "v1Sha256": { "$ref": "#/definitions/sha256" },
        "v2Sha256": { "$ref": "#/definitions/sha256" },
        "baselineHash": { "$ref": "#/definitions/gitHash" },
        "localCommitHash": { "$ref": "#/definitions/gitHash" },
        "remoteHash": {
          "oneOf": [
            { "$ref": "#/definitions/gitHash" },
            { "const": "not-reached" }
          ]
        }
      }
    },
    "files": {
      "type": "object",
      "additionalProperties": false,
      "required": ["summary", "transcript", "addInLog"],
      "properties": {
        "summary": { "const": "summary.md" },
        "transcript": { "const": "git-transcript.txt" },
        "addInLog": { "const": "addin.log" }
      }
    },
    "cleanup": {
      "type": "object",
      "additionalProperties": false,
      "required": ["statusAtEvidenceUpload", "preserveRequested"],
      "properties": {
        "statusAtEvidenceUpload": { "const": "pending" },
        "preserveRequested": { "type": "boolean" }
      }
    },
    "sanitization": {
      "type": "object",
      "additionalProperties": false,
      "required": ["credentials", "urlUserInfo", "privateIdentifiers", "networkAddresses", "machinePaths", "stackTraces"],
      "properties": {
        "credentials": { "const": true },
        "urlUserInfo": { "const": true },
        "privateIdentifiers": { "const": true },
        "networkAddresses": { "const": true },
        "machinePaths": { "const": true },
        "stackTraces": { "const": true }
      }
    }
  },
  "definitions": {
    "sha256": { "type": "string", "pattern": "^[A-F0-9]{64}$" },
    "gitHash": { "type": "string", "pattern": "^[0-9a-f]{40}$" },
    "v21Version": { "type": "string", "pattern": "^21\.[0-9]+\.[0-9]+\.[0-9]+$" }
  },
  "allOf": [
    {
      "if": { "properties": { "outcome": { "const": "pass" } } },
      "then": {
        "properties": {
          "failedStep": { "type": "null" },
          "permissions": {
            "properties": {
              "tiaReadWrite": { "const": true },
              "processStartPermission": { "const": true }
            }
          },
          "workspace": {
            "properties": {
              "remoteHash": { "$ref": "#/definitions/gitHash" }
            }
          },
          "cleanup": {
            "properties": {
              "preserveRequested": { "const": false }
            }
          }
        }
      },
      "else": {
        "properties": {
          "failedStep": { "type": "string", "minLength": 1 }
        }
      }
    },
    {
      "if": {
        "properties": {
          "workspace": {
            "properties": { "remoteHash": { "const": "not-reached" } }
          }
        }
      },
      "then": { "properties": { "failedStep": { "type": "string", "minLength": 1 } } }
    }
  ]
}
```

- [ ] **Step 4: Implement evidence creation and summary generation**

`New-LiveTiaEvidenceBundle.ps1` accepts every schema field as a validated parameter plus source paths for transcript and Add-In log. It copies those two files under the fixed names, writes `evidence.json`, and generates `summary.md` with these headings and values sourced from the JSON object:

```markdown
# Live TIA V21 Git Acceptance

- Outcome
- Failed step
- Source commit
- Candidate ID
- Candidate workflow run ID
- Candidate artifact ID and name
- Package filename, identity, and SHA-256
- Installed package SHA-256
- TIA product version and Public API build
- Reviewer
- TIA.ReadWrite confirmed
- ProcessStartPermission confirmed
- V1 file SHA-256
- V2 file SHA-256
- Baseline, local, and remote Git hashes
- Transcript: git-transcript.txt
- Add-In log: addin.log
- Cleanup status at upload: pending
```

The script refuses a pass if V1 and V2 hashes are equal, local and remote hashes differ, candidate and installed package hashes differ, either permission is false, reviewer is empty, or preservation is requested.

- [ ] **Step 5: Implement schema, candidate, and sanitization validation**

`Test-LiveTiaEvidence.ps1` has this exact interface:

```powershell
param(
    [Parameter(Mandatory)][string] $EvidenceRoot,
    [Parameter(Mandatory)][string] $SchemaPath,
    [Parameter(Mandatory)][string] $CandidateProvenancePath,
    [Parameter(Mandatory)][string] $PackagePath,
    [Parameter(Mandatory)][ValidatePattern('^[0-9]+$')][string] $CandidateArtifactId,
    [Parameter(Mandatory)][ValidatePattern('^tia-git-addin-candidate-[A-Za-z0-9._-]+-[0-9a-f]{40}$')][string] $CandidateArtifactName
)
```

It runs `Test-Json -SchemaFile`, verifies every candidate/source/run/artifact/package/identity/hash field against candidate provenance and package bytes, confirms summary references, and scans all four evidence files. Reject URL user information, credential assignments, bearer/basic authorization, IPv4/IPv6 addresses, `C:\Users\`, `C:\ProgramData\`, `C:\Windows\`, `C:\Program Files\`, UNC paths, lines beginning `at ` that resemble stack frames, and any identifier outside the allowlist `TiaGitAcceptance`, `Program`, `Program.xml`, `main`, `origin`, `GitAcceptanceV1`, `GitAcceptanceV2`.

Candidate provenance is authoritative for candidate ID, source commit, workflow run, package identity, and package hash. The validated `CandidateArtifactId` and `CandidateArtifactName` parameters are authoritative for the server-assigned artifact fields, and the name must also equal the value reconstructed from candidate ID plus source commit.

- [ ] **Step 6: Implement explicitly approved durable upload**

`Publish-LiveTiaEvidence.ps1` requires `[switch] $ApprovedExternalUpload`; without it, throw before running `gh`. With approval, rerun `Test-LiveTiaEvidence.ps1`, read validated `$candidateId` and `$runId`, create `"live-tia-v21-$candidateId-$runId-evidence.zip"`, compute its SHA-256, create the draft/prerelease tag `"tia-v21-evidence-$candidateId"` targeted at the candidate source commit if absent, upload without `--clobber`, query its server asset ID, and write that ID/hash to the draft release body. Do not accept a token parameter and do not write `GH_TOKEN` to disk; use the operator's already-approved `gh` authentication.

After cleanup, the script's `-RecordCleanupCompleted` mode validates a receipt containing `schemaVersion`, `runId`, `evidenceAssetId`, `completedUtc`, `deleted=true`, and `preserved=false`, uploads it as `"live-tia-v21-$candidateId-$runId-cleanup.json"` without `--clobber`, then updates the draft release body. This mode refuses a missing evidence asset ID, a run mismatch, or a retained live root.

- [ ] **Step 7: Write the live runbook with exact preflight and baseline commands**

The runbook begins with an explicit boundary statement: automated tests begin from a fixture workspace change; only this runbook proves TIA's project-to-workspace synchronization and packaged Add-In behavior.

Use this preflight PowerShell block after an operator has downloaded the Task 5 artifact:

```powershell
$ErrorActionPreference = "Stop"
$runId = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ") + "-final"
$candidateRoot = [IO.Path]::GetFullPath("TestResults\CandidateDownload")
if (-not (Test-Path -LiteralPath $candidateRoot -PathType Container)) { throw "Candidate download directory is missing." }
$liveBase = if ($env:TIA_GIT_LIVE_ROOT) { $env:TIA_GIT_LIVE_ROOT } else { "C:\\tialive" }
$liveRoot = Join-Path $liveBase $runId
$workspace = Join-Path $liveRoot "workspace"
$bareRemote = Join-Path $liveRoot "remote.git"
$projectCopy = Join-Path $liveRoot "TiaGitAcceptance"
$evidenceRoot = Join-Path (Resolve-Path ".") "TestResults\LiveTiaV21\$runId"
New-Item -ItemType Directory -Path $workspace,$projectCopy,$evidenceRoot -Force | Out-Null
if ($liveRoot.Length -gt 120) { throw "Live root is too long: $liveRoot" }

$provenance = Get-Content -Raw -LiteralPath "$candidateRoot\candidate-provenance.json" | ConvertFrom-Json
$package = "$candidateRoot\TiaGitAddIn.addin"
$downloadHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $package).Hash
if ($downloadHash -ne $provenance.package.sha256) { throw "Candidate package hash mismatch." }
```

Copy the package into the operator-supplied V21 Add-Ins directory, recompute the installed file hash, and require equality before TIA starts. In TIA's Add-Ins view, record the loaded Add-In version and require it to equal both provenance identity versions. Confirm `TIA.ReadWrite` and `ProcessStartPermission`; abort the run before Git operations if either is denied.

After built-in **Project to workspace synchronization** creates V1 `Program.xml`, establish the deterministic baseline with discrete Git arguments:

```powershell
git init --bare --initial-branch=main $bareRemote
git -C $workspace init --initial-branch=main
git -C $workspace config --local user.name "TIA Git Acceptance"
git -C $workspace config --local user.email "tia-git-acceptance@example.invalid"
git -C $workspace config --local commit.gpgsign false
git -C $workspace config --local core.hooksPath (Join-Path $liveRoot "hooks")
git -C $workspace config --local core.autocrlf false
git -C $workspace config --local credential.helper ""
git -C $workspace remote add origin $bareRemote
git -C $workspace add -- Program.xml
git -C $workspace commit -m "test: establish Program V1 baseline"
git -C $workspace push --set-upstream origin main
$baselineHash = (git -C $workspace rev-parse HEAD).Trim()
if ((git --git-dir=$bareRemote rev-parse refs/heads/main).Trim() -ne $baselineHash) {
    throw "Baseline remote ref mismatch."
}
```

- [ ] **Step 8: Write the exact manual TIA/Add-In V2 procedure**

The runbook requires the operator to:

1. Open a fresh disposable copy of `TiaGitAcceptance` and its VCI workspace.
2. Verify the built-in mapping `Program` -> `Program.xml`.
3. Run built-in **Project to workspace synchronization** and record the V1 file SHA-256 and `GitAcceptanceV1` marker.
4. Establish and independently verify the baseline Git state using Step 7's commands.
5. Change only the marker to `GitAcceptanceV2` inside TIA.
6. Run the same built-in synchronization, require a different SHA-256, and require decoded content to contain `GitAcceptanceV2`.
7. Select the mapped workspace item and invoke **Open Git Panel** from the packaged Add-In.
8. Require exactly one unstaged `Program.xml`, stage it, require exactly one staged entry, and commit with `test: synchronize Program V2`.
9. Require clean status; require history head subject/hash; open its diff and record the V1 removal and V2 addition.
10. Invoke the Add-In's parameterless push; no credential prompt is permitted.
11. Independently require `refs/heads/main` in the bare remote to equal the Add-In commit and `git --git-dir=$bareRemote show refs/heads/main:Program.xml` to contain `GitAcceptanceV2`.
12. Capture redacted transcript/log copies and create/validate `evidence.json` plus `summary.md`.
13. Save and close the project, close the isolated TIA process, and wait for its process ID to exit.
14. Run `Publish-LiveTiaEvidence.ps1 -ApprovedExternalUpload` before deleting the project copy, workspace, or bare remote.
15. Retry-delete those three disposable resources only after TIA exits; a final-release pass may not preserve them.
16. Upload the completed cleanup receipt and record its asset ID in the draft evidence release.

- [ ] **Step 9: Add deterministic live cleanup commands**

The runbook uses this bounded deletion function after evidence upload:

```powershell
function Remove-LivePathWithRetry {
    param([Parameter(Mandatory)][string] $Path)
    if (-not (Test-Path -LiteralPath $Path)) { return }
    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            Remove-Item -LiteralPath $Path -Recurse -Force
            return
        }
        catch [IO.IOException], [UnauthorizedAccessException] {
            if ($attempt -eq 5) { throw }
            Start-Sleep -Milliseconds (100 * [math]::Pow(2, $attempt - 1))
        }
    }
}

Remove-LivePathWithRetry -Path $projectCopy
Remove-LivePathWithRetry -Path $workspace
Remove-LivePathWithRetry -Path $bareRemote
```

Require all three paths absent before recording `deleted=true`. Preservation is allowed only for a non-release local failure and must be explicit in failing evidence.

- [ ] **Step 10: Run schema tests and a no-network evidence dry run**

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~LiveTiaEvidenceTests"
pwsh -NoProfile -File scripts/Test-LiveTiaEvidence.ps1 -EvidenceRoot TestResults/LiveTiaV21/schema-pass -SchemaPath docs/testing/schemas/live-tia-v21-evidence.schema.json -CandidateProvenancePath TestResults/LiveTiaV21/schema-pass/candidate-provenance.json -PackagePath TestResults/LiveTiaV21/schema-pass/TiaGitAddIn.addin -CandidateArtifactId 1 -CandidateArtifactName tia-git-addin-candidate-schema-pass-0123456789abcdef0123456789abcdef01234567
```

Expected: PASS for the controlled pass and early-failure bundles; every sensitive control fails. Do not run the publish script in this automated verification.

- [ ] **Step 11: Record the initial smoke dependency and final acceptance rule**

The runbook includes two separate evidence labels: `smoke` immediately after Task 3 and the first fresh packaged candidate, and `final` after integration. A passing final record must reference the exact release candidate and have a later timestamp than smoke; smoke cannot authorize release.

- [ ] **Step 12: Commit live acceptance and evidence contracts**

```powershell
git add docs/testing scripts/New-LiveTiaEvidenceBundle.ps1 scripts/Test-LiveTiaEvidence.ps1 scripts/Publish-LiveTiaEvidence.ps1 src/TiaGitAddIn.Tests/Configuration/LiveTiaEvidenceTests.cs
git commit -m "test: define live tia v21 acceptance evidence"
```

---

### Task 7: Protect publication and release the accepted candidate without rebuilding

**Acceptance criteria:** AC-089, AC-108, AC-110

**Files:**

- Create: `scripts/Test-ReleaseProvenance.ps1`
- Create: `src/TiaGitAddIn.Tests/Configuration/ReleaseProvenanceTests.cs`
- Modify: `src/TiaGitAddIn.Tests/Configuration/ReleaseWorkflowTests.cs`
- Replace: `.github/workflows/release.yml`
- Verify: `src/TiaGitAddIn.Tests/Configuration/GitHubWorkflowSecurityTests.cs`
- Verify: `scripts/Test-GitHubWorkflowSecurity.ps1`

**Dependencies:** Tasks 4, 5, and 6. The candidate artifact, final live evidence bundle, evidence asset ID, and cleanup receipt must already exist. The repository administrator must configure required reviewers on the GitHub environment named `tia-production` before the first release.

**Interfaces:**

- Consumes: immutable candidate files `TiaGitAddIn.addin` and `candidate-provenance.json`; schema-valid `evidence.json`; durable evidence asset ID/name; final cleanup receipt; protected environment state; release tag and resolved commit.
- Produces: `Test-ReleaseProvenance.ps1` authorization JSON with `authorized`, source/candidate/artifact/hash/tag/evidence identities; a `workflow_dispatch` release job gated by `environment: tia-production`; release assets containing the unchanged candidate, provenance, final evidence ZIP, and cleanup receipt.

- [ ] **Step 1: Write failing provenance-policy tests**

Create `ReleaseProvenanceTests.cs` with a disposable directory and a helper that invokes PowerShell as a child process with a discrete `ArgumentList`. Build one complete passing set:

- `candidate-provenance.json` copied from the candidate artifact;
- `TiaGitAddIn.addin` whose SHA-256 and inspected identity match candidate provenance;
- final `evidence.json` with `outcome=pass`, a non-empty reviewer, the same source commit/candidate/run/artifact/name/hash/identity, `installedSha256` equal to the candidate hash, both permission flags true, `cleanup.statusAtEvidenceUpload=pending`, and `cleanup.preserveRequested=false`;
- `cleanup-receipt.json` with the same live run ID and evidence asset ID, `deleted=true`, and `preserved=false`;
- release tag `v21.0.42.1` resolving to the same 40-character source commit;
- protected approval state `approved`.

Add one theory row per controlled denial. Every row changes only the named field and expects a non-zero exit:

```csharp
[Theory]
[InlineData(ReleaseMutation.OutcomeFail, "Live evidence outcome is not pass.")]
[InlineData(ReleaseMutation.ReviewerMissing, "Live evidence reviewer is missing.")]
[InlineData(ReleaseMutation.ApprovalPending, "Protected approval is not approved.")]
[InlineData(ReleaseMutation.ApprovalRejected, "Protected approval is not approved.")]
[InlineData(ReleaseMutation.TagCommitMismatch, "Tag commit does not match candidate source.")]
[InlineData(ReleaseMutation.EvidenceSourceMismatch, "Evidence source does not match candidate source.")]
[InlineData(ReleaseMutation.CandidateIdMismatch, "Evidence candidate ID does not match provenance.")]
[InlineData(ReleaseMutation.WorkflowRunMismatch, "Evidence candidate run does not match provenance.")]
[InlineData(ReleaseMutation.ArtifactIdMismatch, "Evidence artifact ID does not match the selected artifact.")]
[InlineData(ReleaseMutation.ArtifactNameMismatch, "Evidence artifact name does not match the selected artifact.")]
[InlineData(ReleaseMutation.PackageHashMismatch, "Candidate package hash does not match provenance.")]
[InlineData(ReleaseMutation.InstalledHashMismatch, "Installed package hash does not match candidate hash.")]
[InlineData(ReleaseMutation.PackageIdentityMismatch, "Evidence package identity does not match provenance.")]
[InlineData(ReleaseMutation.ReleaseVersionMismatch, "Release tag does not match package identity.")]
[InlineData(ReleaseMutation.EvidenceAssetMismatch, "Cleanup receipt evidence asset does not match selection.")]
[InlineData(ReleaseMutation.CleanupIncomplete, "Cleanup receipt is not final.")]
public async Task Rejects_single_invalid_release_condition(
    ReleaseMutation mutation,
    string expectedDiagnostic)
```

The passing case expects exit 0 and exactly one JSON summary on stdout containing `authorized=true`, the source commit, candidate ID, artifact ID, package hash, release tag, evidence run ID, and evidence asset ID. Diagnostics may contain field names and fixed IDs but no token, credential-bearing URL, runner-private path, stack trace, or evidence transcript content.

Extend `ReleaseWorkflowTests.cs` with static workflow assertions that initially fail:

```csharp
[Fact]
public void Protected_release_downloads_and_validates_stored_candidate_without_building()
{
    string workflow = ReadWorkflow(".github/workflows/release.yml");

    Assert.Contains("environment: tia-production", workflow);
    Assert.Contains("runs-on: [ self-hosted, Windows, tia-release-trusted ]", workflow);
    Assert.Contains("actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1", workflow);
    Assert.Contains("persist-credentials: false", workflow);
    Assert.Contains("actions/download-artifact@d3f86a106a0bac45b974a628896c90dbdf5c8093 # v4.3.0", workflow);
    Assert.Contains("run-id:", workflow);
    Assert.Contains("Test-ReleaseProvenance.ps1", workflow);
    Assert.Contains("softprops/action-gh-release@3bb12739c298aeb8a4eeaf626c5b8d85266b0e65 # v2.6.2", workflow);
    Assert.DoesNotContain("dotnet build", workflow, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("dotnet publish", workflow, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("AddInPublisher", workflow, StringComparison.OrdinalIgnoreCase);
}
```

Run RED:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ReleaseProvenanceTests|FullyQualifiedName~ReleaseWorkflowTests|FullyQualifiedName~GitHubWorkflowSecurityTests"
```

Expected: FAIL because the validator and protected workflow contract do not exist.

- [ ] **Step 2: Implement the release-provenance validator**

Create `scripts/Test-ReleaseProvenance.ps1` with this exact public contract:

```powershell
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $CandidateProvenancePath,
    [Parameter(Mandatory)][string] $EvidenceJsonPath,
    [Parameter(Mandatory)][string] $CleanupReceiptPath,
    [Parameter(Mandatory)][string] $PackagePath,
    [Parameter(Mandatory)][ValidatePattern('^[0-9]+$')][string] $CandidateArtifactId,
    [Parameter(Mandatory)][ValidatePattern('^tia-git-addin-candidate-[A-Za-z0-9._-]+-[0-9a-f]{40}$')][string] $CandidateArtifactName,
    [Parameter(Mandatory)][ValidatePattern('^[0-9]+$')][string] $CandidateWorkflowRunId,
    [Parameter(Mandatory)][ValidatePattern('^[0-9]+$')][string] $EvidenceAssetId,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string] $TagCommit,
    [Parameter(Mandatory)][ValidatePattern('^v21\.[0-9]+\.[0-9]+\.[0-9]+$')][string] $ReleaseTag,
    [Parameter(Mandatory)][ValidateSet('approved', 'pending', 'rejected')][string] $ApprovalStatus
)
```

Use `Set-StrictMode -Version Latest` and `$ErrorActionPreference = "Stop"`. Resolve every path with `GetFullPath` and require a regular file. Parse JSON with `ConvertFrom-Json`; reject missing properties before comparisons. Validate in this order so failures are stable:

1. `ApprovalStatus` is `approved`.
2. Evidence `outcome` is `pass`, `failedStep` is null, reviewer is non-empty, and both required permission flags are true.
3. `TagCommit` equals candidate `sourceCommit` and live evidence `sourceCommit`, using exact ordinal lowercase comparison.
4. Candidate `workflowRunId` equals `CandidateWorkflowRunId` and live `candidate.workflowRunId`.
5. Live `candidate.id` equals candidate `candidateId`.
6. Live `candidate.artifactId` and `artifactName` equal the selected artifact inputs; the expected artifact name reconstructed as `tia-git-addin-candidate-{candidateId}-{sourceCommit}` also equals both.
7. Recomputed uppercase package SHA-256 equals candidate `package.sha256`, live `candidate.package.sha256`, live `installedSha256`, and the adjacent checksum file when present.
8. Candidate and evidence identity objects match exactly; `engineeringVersion` is `V21`; publisher target is the V21 namespace; Add-In and assembly versions are equal.
9. `ReleaseTag.TrimStart('v')` equals both package identity versions.
10. Evidence cleanup records `statusAtEvidenceUpload=pending` and `preserveRequested=false`; the separate cleanup receipt `runId` equals evidence `runId`, its `evidenceAssetId` equals `EvidenceAssetId`, `deleted` is true, and `preserved` is false.

Do not trust filenames, JSON-supplied paths, or archive entry paths. The script receives resolved files from the workflow, never expands an archive, never invokes Git, and never starts the publisher. On success emit a compressed JSON authorization summary; on failure write one sanitized `Write-Error` message and exit non-zero.

- [ ] **Step 3: Make every validator test GREEN, then refactor comparisons**

Implement small private functions, each below 50 lines:

```powershell
function Read-RequiredJsonFile([string] $Path, [string] $Label)
function Assert-EqualText([string] $Actual, [string] $Expected, [string] $Message)
function Assert-True([bool] $Condition, [string] $Message)
function Get-UpperSha256([string] $Path)
function Assert-V21Identity($CandidateIdentity, $EvidenceIdentity, [string] $ReleaseTag)
```

Return new `[pscustomobject]` values from helpers; do not add fields to parsed JSON objects. Rerun the focused command after each function, then all release tests:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ReleaseProvenanceTests"
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ReleaseWorkflowTests|FullyQualifiedName~AddInPublisherConfigurationTests|FullyQualifiedName~GitHubWorkflowSecurityTests"
pwsh -NoProfile -File scripts/Test-GitHubWorkflowSecurity.ps1
```

Expected: PASS; controlled invalid cases perform no network call and publish nothing.

- [ ] **Step 4: Replace the tag-triggered workflow with a protected exact-candidate release**

Replace `.github/workflows/release.yml` with:

```yaml
name: Protected release

on:
  workflow_dispatch:
    inputs:
      release_tag:
        description: Existing annotated or lightweight V21 tag
        required: true
        type: string
      candidate_run_id:
        description: Successful release-candidate workflow run ID
        required: true
        type: string
      candidate_artifact_id:
        description: Immutable candidate artifact ID recorded by upload-artifact
        required: true
        type: string
      candidate_artifact_name:
        description: Exact immutable candidate artifact name
        required: true
        type: string
      evidence_tag:
        description: Draft evidence release tag
        required: true
        type: string
      evidence_asset_id:
        description: Durable final evidence asset ID
        required: true
        type: string
      evidence_asset_name:
        description: Exact final evidence ZIP asset name
        required: true
        type: string
      cleanup_receipt_name:
        description: Exact cleanup receipt asset name
        required: true
        type: string

permissions:
  contents: write
  actions: read

concurrency:
  group: protected-release
  cancel-in-progress: false

jobs:
  publish:
    name: Publish accepted candidate
    runs-on: [ self-hosted, Windows, tia-release-trusted ]
    environment: tia-production
    timeout-minutes: 20
    steps:
      - name: Check out release policy
        uses: actions/checkout@34e114876b0b11c390a56381ad16ebd13914f8d5 # v4.3.1
        with:
          persist-credentials: false
          clean: true
          fetch-depth: 0

      - name: Validate workflow inputs
        shell: pwsh
        env:
          RELEASE_TAG: ${{ inputs.release_tag }}
          CANDIDATE_RUN_ID: ${{ inputs.candidate_run_id }}
          CANDIDATE_ARTIFACT_ID: ${{ inputs.candidate_artifact_id }}
          CANDIDATE_ARTIFACT_NAME: ${{ inputs.candidate_artifact_name }}
          EVIDENCE_TAG: ${{ inputs.evidence_tag }}
          EVIDENCE_ASSET_ID: ${{ inputs.evidence_asset_id }}
          EVIDENCE_ASSET_NAME: ${{ inputs.evidence_asset_name }}
          CLEANUP_RECEIPT_NAME: ${{ inputs.cleanup_receipt_name }}
        run: |
          if ($env:RELEASE_TAG -notmatch '^v21\.[0-9]+\.[0-9]+\.[0-9]+$') { throw "Invalid release tag." }
          if ($env:CANDIDATE_RUN_ID -notmatch '^[0-9]+$') { throw "Invalid candidate run ID." }
          if ($env:CANDIDATE_ARTIFACT_ID -notmatch '^[0-9]+$') { throw "Invalid candidate artifact ID." }
          if ($env:CANDIDATE_ARTIFACT_NAME -notmatch '^tia-git-addin-candidate-[A-Za-z0-9._-]+-[0-9a-f]{40}$') { throw "Invalid candidate artifact name." }
          if ($env:EVIDENCE_TAG -notmatch '^tia-v21-evidence-[A-Za-z0-9._-]{3,80}$') { throw "Invalid evidence tag." }
          if ($env:EVIDENCE_ASSET_ID -notmatch '^[0-9]+$') { throw "Invalid evidence asset ID." }
          if ($env:EVIDENCE_ASSET_NAME -notmatch '^live-tia-v21-[A-Za-z0-9._-]+-[0-9]{8}T[0-9]{6}Z-[A-Za-z0-9._-]{3,64}-evidence\.zip$') { throw "Invalid evidence asset name." }
          if ($env:CLEANUP_RECEIPT_NAME -notmatch '^live-tia-v21-[A-Za-z0-9._-]+-[0-9]{8}T[0-9]{6}Z-[A-Za-z0-9._-]{3,64}-cleanup\.json$') { throw "Invalid cleanup receipt name." }

      - name: Download immutable candidate from selected run
        uses: actions/download-artifact@d3f86a106a0bac45b974a628896c90dbdf5c8093 # v4.3.0
        with:
          name: ${{ inputs.candidate_artifact_name }}
          path: ${{ runner.temp }}/candidate
          github-token: ${{ github.token }}
          repository: ${{ github.repository }}
          run-id: ${{ inputs.candidate_run_id }}

      - name: Download durable evidence and cleanup receipt
        shell: pwsh
        env:
          GH_TOKEN: ${{ github.token }}
          EVIDENCE_TAG: ${{ inputs.evidence_tag }}
          EVIDENCE_ASSET_NAME: ${{ inputs.evidence_asset_name }}
          CLEANUP_RECEIPT_NAME: ${{ inputs.cleanup_receipt_name }}
        run: |
          $download = Join-Path $env:RUNNER_TEMP "release-evidence"
          New-Item -ItemType Directory -Path $download -Force | Out-Null
          gh release download $env:EVIDENCE_TAG --pattern $env:EVIDENCE_ASSET_NAME --dir $download
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
          gh release download $env:EVIDENCE_TAG --pattern $env:CLEANUP_RECEIPT_NAME --dir $download
          if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
          Expand-Archive -LiteralPath (Join-Path $download $env:EVIDENCE_ASSET_NAME) -DestinationPath (Join-Path $download "expanded")

      - name: Resolve the protected tag commit
        id: tag
        shell: pwsh
        env:
          RELEASE_TAG: ${{ inputs.release_tag }}
        run: |
          $commit = (git rev-list -n 1 "$($env:RELEASE_TAG)^{commit}").Trim()
          if ($LASTEXITCODE -ne 0 -or $commit -notmatch '^[0-9a-f]{40}$') { throw "Release tag does not resolve to one commit." }
          "commit=$commit" >> $env:GITHUB_OUTPUT

      - name: Authorize exact candidate publication
        shell: pwsh
        env:
          RELEASE_TAG: ${{ inputs.release_tag }}
          CANDIDATE_RUN_ID: ${{ inputs.candidate_run_id }}
          CANDIDATE_ARTIFACT_ID: ${{ inputs.candidate_artifact_id }}
          CANDIDATE_ARTIFACT_NAME: ${{ inputs.candidate_artifact_name }}
          EVIDENCE_ASSET_ID: ${{ inputs.evidence_asset_id }}
          EVIDENCE_ASSET_NAME: ${{ inputs.evidence_asset_name }}
          CLEANUP_RECEIPT_NAME: ${{ inputs.cleanup_receipt_name }}
          TAG_COMMIT: ${{ steps.tag.outputs.commit }}
        run: |
          $candidate = Join-Path $env:RUNNER_TEMP "candidate"
          $evidence = Join-Path $env:RUNNER_TEMP "release-evidence"
          pwsh -NoProfile -File scripts/Test-ReleaseProvenance.ps1 `
            -CandidateProvenancePath (Join-Path $candidate "candidate-provenance.json") `
            -EvidenceJsonPath (Join-Path $evidence "expanded\evidence.json") `
            -CleanupReceiptPath (Join-Path $evidence $env:CLEANUP_RECEIPT_NAME) `
            -PackagePath (Join-Path $candidate "TiaGitAddIn.addin") `
            -CandidateArtifactId $env:CANDIDATE_ARTIFACT_ID `
            -CandidateArtifactName $env:CANDIDATE_ARTIFACT_NAME `
            -CandidateWorkflowRunId $env:CANDIDATE_RUN_ID `
            -EvidenceAssetId $env:EVIDENCE_ASSET_ID `
            -TagCommit $env:TAG_COMMIT `
            -ReleaseTag $env:RELEASE_TAG `
            -ApprovalStatus approved

      - name: Publish stored candidate and acceptance evidence
        uses: softprops/action-gh-release@3bb12739c298aeb8a4eeaf626c5b8d85266b0e65 # v2.6.2
        with:
          tag_name: ${{ inputs.release_tag }}
          fail_on_unmatched_files: true
          draft: false
          prerelease: false
          files: |
            ${{ runner.temp }}/candidate/TiaGitAddIn.addin
            ${{ runner.temp }}/candidate/candidate-provenance.json
            ${{ runner.temp }}/release-evidence/${{ inputs.evidence_asset_name }}
            ${{ runner.temp }}/release-evidence/${{ inputs.cleanup_receipt_name }}
```

The workflow intentionally has no push or tag trigger, restore, compilation, test execution, publisher invocation, package mutation, or artifact overwrite. Test execution and package creation belong to the reusable gate and release-candidate workflow; this job only authenticates stored bytes after protected human approval.

- [ ] **Step 5: Prove approval, provenance, and no-rebuild controls**

Add static tests that parse the workflow as YAML or inspect its exact scalar nodes and prove:

- `publish.environment` is exactly `tia-production`;
- `publish.runs-on` selects only `[ self-hosted, Windows, tia-release-trusted ]` and never a PR/CI/candidate/live label;
- `actions: read` and `contents: write` are the only required permissions;
- every external action equals its approved full SHA plus version comment, and checkout sets `persist-credentials: false`;
- download uses all of `name`, `repository`, `run-id`, and `github-token`;
- evidence is downloaded from the selected durable evidence tag;
- the authorization step precedes pinned `softprops/action-gh-release@3bb12739c298aeb8a4eeaf626c5b8d85266b0e65 # v2.6.2`;
- candidate package path passed to the release action is the same download path passed to the validator;
- no step contains `dotnet`, `msbuild`, `AddInPublisher`, `Compress-Archive`, `Copy-Item` against the package, or `upload-artifact`;
- publication is reachable only from `workflow_dispatch` and the protected job.

Run:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~ReleaseProvenanceTests|FullyQualifiedName~ReleaseWorkflowTests|FullyQualifiedName~GitHubWorkflowSecurityTests"
pwsh -NoProfile -File scripts/Test-GitHubWorkflowSecurity.ps1
```

Expected: PASS. Re-run one controlled negative case and confirm the authorization step exits before the release action in a local workflow-structure simulation; never exercise a real release from automated tests.

- [ ] **Step 6: Commit protected publication**

```powershell
git add scripts/Test-ReleaseProvenance.ps1 .github/workflows/release.yml src/TiaGitAddIn.Tests/Configuration/ReleaseProvenanceTests.cs src/TiaGitAddIn.Tests/Configuration/ReleaseWorkflowTests.cs
git commit -m "ci: protect exact candidate publication"
```

---

### Task 8: Document the lanes, run the complete gate, and close acceptance traceability

**Acceptance criteria:** AC-003, AC-004, AC-005, AC-068, AC-069, AC-070, AC-071, AC-072, AC-073, AC-074, AC-075, AC-076, AC-077, AC-078, AC-079, AC-080, AC-081, AC-082, AC-083, AC-084, AC-085, AC-086, AC-087, AC-088, AC-089, AC-090, AC-091, AC-092, AC-093, AC-094, AC-095, AC-096, AC-097, AC-098, AC-099, AC-100, AC-101, AC-102, AC-103, AC-104, AC-105, AC-106, AC-107, AC-108, AC-109, AC-110, AC-111, AC-112

**Files:**

- Modify: `README.md`
- Modify: `docs/testing/live-tia-v21-git-acceptance.md`
- Verify: `docs/testing/schemas/live-tia-v21-evidence.schema.json`
- Verify: `graphify-out/GRAPH_REPORT.md`

**Dependencies:** All prior tasks and every comparison-feature plan merged into the integration branch. The final live pass must use a candidate created from the exact final source commit.

**Interfaces:**

- Consumes: the reusable `scripts/Invoke-TestGate.ps1` contract, immutable candidate provenance, `docs/testing/schemas/live-tia-v21-evidence.schema.json`, the live runbook, protected-release inputs, and comparison-plan test suites.
- Produces: README commands copied verbatim from the gate, a completed final operator checklist, merged `coverage.json`/`coverage.cobertura.xml`, durable final evidence plus cleanup receipt, current graph report, and the AC-003/004/005/068–112 verification matrix.

- [ ] **Step 1: Write failing documentation-contract tests**

Create `src/TiaGitAddIn.Tests/Configuration/DocumentationContractTests.cs`; keep all README/runbook assertions in that focused file. Require README to contain these commands verbatim:

```powershell
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~VciGitWorkflowTests"
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release
pwsh -NoProfile -File scripts/Invoke-TestGate.ps1
```

Also require the README and runbook to state:

- automated `GitE2E` starts with a controlled fixture workspace file change and uses no TIA/Siemens/WPF assembly;
- only the live V21 lane proves built-in project-to-workspace synchronization and packaged Add-In behavior;
- candidate creation occurs only after the reusable gate;
- publication consumes the accepted immutable candidate without rebuilding;
- merged coverage paths are `TestResults/Coverage/unit/coverage.json`, `TestResults/Coverage/coverage.json`, and `TestResults/Coverage/coverage.cobertura.xml`;
- `TIA_GIT_E2E_KEEP_TEMP=1` is local-only and ignored in CI;
- final evidence must be uploaded before disposal and the cleanup receipt must be complete before release.

Run RED:

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~DocumentationContractTests"
```

Expected: FAIL until documentation is updated.

- [ ] **Step 2: Update README with one authoritative command surface**

Add a concise **Testing and release lanes** section. Link the live runbook, runner-security checklist, and evidence format. Include the three exact commands from Step 1, the two explicit lane boundaries, prerequisites (`.NET SDK 8.0.420`, Git for Windows, the five disjoint runner labels/pools from Task 4; TIA Portal V21 only on `tia-live-v21-trusted`), output paths, and this progression:

```text
Reusable test gate -> immutable candidate -> live V21 smoke/final evidence
-> evidence upload -> cleanup receipt -> protected exact-candidate release
```

Document the direct reproducible coverage commands from Task 4 as a single copyable PowerShell block, including `-- xUnit.AppDomain=denied`, `/p:MergeWith`, and `Threshold=80;ThresholdType=line;ThresholdStat=total`. Explain that `Invoke-TestGate.ps1` is the canonical wrapper and direct commands exist for diagnosis and AC-109 reproduction.

- [ ] **Step 3: Complete operator and evidence documentation**

Cross-link `docs/testing/live-tia-v21-git-acceptance.md` and `docs/testing/schemas/live-tia-v21-evidence.schema.json`. Add a final operator checklist with explicit boxes for:

1. selected candidate run ID, artifact ID/name, source commit, package identity, and SHA-256;
2. V21 product/API build and Add-In permissions;
3. V1 synchronization and independently verified baseline hashes;
4. V2 synchronization in TIA;
5. packaged Add-In status, stage, commit, history, diff, and parameterless push;
6. bare-remote ref/content verification;
7. schema validation and redaction scan;
8. approved evidence upload and returned asset ID;
9. TIA exit, bounded cleanup, and durable cleanup receipt;
10. protected release inputs matching the recorded evidence.

Keep smoke and final tables separate. A smoke failure may retain a local path only when `preserved=true` and cannot authorize publication. A final run requires `outcome=pass`, `preserved=false`, `deleted=true`, and a non-empty reviewer.

- [ ] **Step 4: Make documentation tests GREEN and run focused lane tests**

```powershell
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~DocumentationContractTests|FullyQualifiedName~ReleaseWorkflowTests|FullyQualifiedName~LiveTiaEvidenceTests|FullyQualifiedName~GitHubWorkflowSecurityTests"
pwsh -NoProfile -File scripts/Test-GitHubWorkflowSecurity.ps1
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~VciGitWorkflowTests"
dotnet test src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj -c Release
```

Expected: PASS. Confirm the integration test log reports only roots under the disposable test base and a local filesystem bare remote.

- [ ] **Step 5: Run the reusable full gate and inspect merged coverage**

```powershell
pwsh -NoProfile -File scripts/Invoke-TestGate.ps1
pwsh -NoProfile -File scripts/Assert-CoberturaThreshold.ps1 -ReportPath TestResults/Coverage/coverage.cobertura.xml -Minimum 80.00
```

Expected: restore, Release build, net48 unit tests with AppDomain disabled, unfiltered net8 integration tests, coverage merge, exact total-line 80 percent threshold, and Cobertura generation all PASS. Inspect the report and require modules for `TiaGitAddIn.Core` plus designated testable `TiaGitAddIn` production classes; reject any broad source exclusion.

- [ ] **Step 6: Run security and boundary scans**

Use repository-local tooling only:

```powershell
rg -n -i "Siemens\.Automation\.CommonServices\.Compare|CompareEditorStarter|CompareToOnline|PlcSoftware\.CompareTo" src -g "*.cs" -g "*.csproj"
rg -n -i "password\s*=|token\s*=|api[_-]?key\s*=|https?://[^/\s]+:[^@\s]+@" src scripts .github docs/testing
rg -n "UseShellExecute\s*=\s*true|cmd(\.exe)?\s{0,}/c|powershell(\.exe)?\s+-Command" src/TiaGitAddIn.IntegrationTests
pwsh -NoProfile -File scripts/Test-GitHubWorkflowSecurity.ps1
dotnet test src/TiaGitAddIn.Tests/TiaGitAddIn.Tests.csproj -c Release -p:EnableTiaAddInPackaging=false --filter "FullyQualifiedName~GitHubWorkflowSecurityTests"
dotnet list src/TiaGitAddIn.IntegrationTests/TiaGitAddIn.IntegrationTests.csproj package --include-transitive
```

Expected: no internal Siemens compare reference, no committed credential, and no shell launch in the integration adapter. Every external action matches the approved full-SHA/version allowlist, every checkout disables persisted credentials, PR and trusted runner labels are disjoint, and the administrator has signed the ephemeral-runner destruction/approval checklist for both a successful and forced-failure PR job. Review package output to confirm only Core plus test dependencies and no Siemens/WPF package. Review `git diff --check` and every workflow permission before proceeding.

- [ ] **Step 7: Build and inspect the final candidate once**

Dispatch `release-candidate.yml` only after the exact source commit passes Task 4's reusable gate. Record its run ID, server artifact ID/name/digest, candidate ID, source commit, package filename, SHA-256, and V21 identity. Download the artifact into a new directory and run:

```powershell
pwsh -NoProfile -File scripts/New-CandidateProvenance.ps1 -PackagePath TestResults/CandidateDownload/TiaGitAddIn.addin -CandidateId final-v21 -SourceCommit (git rev-parse HEAD) -WorkflowRunId 424242 -ExpectedVersion 21.0.42.1 -OutputDirectory TestResults/CandidateInspection
```

The values `final-v21`, `424242`, and `21.0.42.1` form a controlled local example. For an actual run, replace all three with the literal values recorded by the selected candidate workflow before execution; never edit or repack the downloaded artifact. Require the recomputed provenance fields and hash to match the artifact's original `candidate-provenance.json` byte-for-value.

- [ ] **Step 8: Perform final live V21 acceptance and durable cleanup**

Follow `docs/testing/live-tia-v21-git-acceptance.md` from a fresh TIA process and disposable project copy. Use the Task 7 candidate, not a local build. Complete the final evidence bundle, validate it, upload with `-ApprovedExternalUpload`, record the asset ID, exit TIA, retry cleanup, upload the receipt, and verify both assets remain downloadable. Do not start protected publication if any evidence field, hash, reviewer, permission, remote verification, or cleanup field fails.

- [ ] **Step 9: Exercise the protected release validator locally, then publish with approval**

Before requesting environment approval, download the exact candidate, evidence ZIP, and cleanup receipt into isolated directories and invoke `Test-ReleaseProvenance.ps1` with their recorded literal IDs, tag commit, tag, and `approved` state. Expect one `authorized=true` summary.

Then dispatch `release.yml` with those exact values. The `tia-production` reviewer independently checks the workflow inputs against the durable evidence record. After approval, verify the published `TiaGitAddIn.addin` SHA-256 equals the candidate hash and that the release contains candidate provenance, final evidence, and cleanup receipt. No build log or publisher invocation may exist in the protected-release job.

- [ ] **Step 10: Refresh the repository graph after all code changes**

```powershell
graphify update .
```

Open `graphify-out/GRAPH_REPORT.md` and require its source revision to equal `git rev-parse HEAD`. Review new god nodes and community changes; if the integration fixture or workflow validator becomes a high-coupling node, split responsibilities before acceptance.

- [ ] **Step 11: Run the final repository verification**

```powershell
pwsh -NoProfile -File scripts/Invoke-TestGate.ps1
dotnet build TiaGitAddIn.sln -c Release -p:EnableTiaAddInPackaging=false
git diff --check
git status --short
```

Expected:

- every test and the merged 80 percent total-line gate passes;
- `TestResults/Coverage/coverage.cobertura.xml` exists and names the approved production scope;
- one immutable V21 candidate exists for the final commit;
- final schema-valid evidence and cleanup receipt match that candidate;
- internal-reference and secret scans are clean;
- graphify reports the current source revision;
- only intentional implementation, test, workflow, script, fixture, runbook, README, and graph outputs are changed.

- [ ] **Step 12: Complete the acceptance-criteria audit**

Record the following mapping in the integration PR description and tick an ID only after its objective evidence exists:

| Scope | Acceptance criteria | Evidence |
|---|---|---|
| Project/test boundary | AC-003, AC-004, AC-005, AC-068, AC-111 | project references, composition tests, README lane statement |
| Isolated real Git | AC-069 through AC-078, AC-097, AC-112 | `VciGitWorkflowTests`, child-process traces, cleanup/concurrency controls |
| Developer commands | AC-079, AC-109 | README contract tests and copied command execution |
| Live V21 lane | AC-080 through AC-088, AC-110 | schema-valid smoke/final evidence, durable asset ID, cleanup receipt |
| Exact-candidate release | AC-089, AC-108 | candidate provenance, protected workflow, controlled denial tests |
| Coverage/CI | AC-090 through AC-094, AC-103 | reusable gate logs, merged JSON/Cobertura, exact 80 percent result |
| Shared security/fixtures | AC-095 through AC-101, AC-106 | parser/path/redaction tests, manifest scans, publisher permission test |

AC-102, AC-104, AC-105, AC-107, AC-113 through AC-118 are owned by the comparison-feature plans but remain prerequisites for Task 8's full gate. Do not duplicate their implementations here; require their passing tests and evidence before closing AC-103.

- [ ] **Step 13: Commit documentation and final graph refresh**

```powershell
git add README.md docs/testing graphify-out
git commit -m "docs: document vci git release lanes"
```

---

## Definition of Done

- `TiaGitAddIn.IntegrationTests` targets only `net8.0`, references only Core plus pinned test packages, and requires no TIA, Siemens, or WPF runtime.
- The test-only `SystemGitProcessRunner` executes Git directly with discrete arguments and a hermetic per-process environment.
- Sequential and concurrent real-Git workflows prove baseline, status, stage, commit, history, files, diff, parameterless push, remote ref/content, diagnostics, and cleanup.
- Both test projects pin `coverlet.msbuild` `6.0.4`; net48 JSON merges into net8 JSON/Cobertura; exact total line coverage below 80 percent fails.
- The reusable Windows gate is the required predecessor for candidate creation and never publishes; untrusted PR code runs only on single-use `tia-pr-ephemeral` agents, while main CI, candidate, live V21, and release work use four disjoint trusted pools.
- Candidate creation executes the publisher once, hashes and inspects one package, uploads it immutably, and records client plus server provenance.
- Smoke and final live-TIA V21 runs use the exact candidate, prove actual synchronization and packaged Add-In Git behavior, upload sanitized evidence before cleanup, and record cleanup durably.
- The protected release job has reviewer approval, accepts only matching pass evidence and completed cleanup, and publishes the stored candidate without rebuilding.
- Every external action uses its officially verified full commit SHA with version comment, every checkout disables persisted credentials, and the offline workflow-security scan plus configuration tests pass.
- Final tests, security scans, `git diff --check`, graph refresh, source-revision check, and AC-003/004/005/068–112 evidence all pass.

## Plan self-review checklist

- [ ] Every implementation step names an exact repository path and an observable result.
- [ ] Every behavior change begins with a failing test, reaches green with the minimum implementation, and includes a refactor/re-run step.
- [ ] Focused, project-wide, merged-coverage, live-runbook, and final verification commands are copyable PowerShell.
- [ ] C# signatures are concrete and limited to C# 12 in the `net8.0` integration project; shared Core remains compatible with `netstandard2.0` and production remains `net48`.
- [ ] Workflow YAML names exact triggers, permissions, runner labels, dependencies, artifact paths, and failure boundaries.
- [ ] Git child processes never use a shell, external network, real credentials, host hooks, signing, system/global config, or interactive prompts.
- [ ] Evidence schemas are closed, versioned, hash-bound, reviewer-bound, redaction-tested, durable before cleanup, and tied to the protected release.
- [ ] No task asks an implementer to discover an interface, invent a schema, choose a filename, or infer a command.
- [ ] Conventional commits split scaffolding, adapter, workflow, gate, candidate, evidence, protected release, and documentation changes.
- [ ] No unfinished marker or deferred design decision remains.
