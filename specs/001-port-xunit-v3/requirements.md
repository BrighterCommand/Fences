# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #34

**Upstream commits**: [App-vNext/Polly@1a80392b](https://github.com/App-vNext/Polly/commit/1a80392b) ("Update to xunit v3", App-vNext/Polly#3131), plus the deferred `test/` half of [App-vNext/Polly@f35bc00d](https://github.com/App-vNext/Polly/commit/f35bc00d) (".NET 11 preparation", App-vNext/Polly#3225)

**Related**: #28 (first upstream sync, where this commit was triaged), #14 (coverage unreliability), #6 (Dependabot PR bumping `coverlet.msbuild` 6.0.4 → 10.0.1, already closed unmerged on 2026-09-20 pending #14), PR #19 (open: a coverage report per TFM, and a report even when the gate fails), #39 and #41 (open Dependabot PRs for `dotnet-stryker` and `Microsoft.NET.Test.Sdk`, both FR-2 packages), #44 (build-props half of f35bc00d, already merged), #46 (testhost / `mutations-core` hang)

## Glossary

These terms are used with exactly these meanings throughout the document.

| Term | Meaning |
|---|---|
| **Port** | Applying an upstream Polly commit to Fences, rebranded per ADR 0002 D1 and ADR 0003 D7. Paths map as `src/Polly.Core` → `src/Paramore.Fences.Core`, `src/Polly` → `src/Paramore.Fences`, `test/Polly.Core.Tests` → `test/Paramore.Fences.Core.Tests`, `test/Polly.Specs` → `test/Paramore.Fences.Specs`, and likewise for Extensions, RateLimiting and Testing. Namespaces map `Polly.*` → `Paramore.Fences.*`. |
| **The 1a80392b port** | The port of App-vNext/Polly@1a80392b. |
| **The f35bc00d Specs port** | The port of the three `test/Polly.Specs` edits in App-vNext/Polly@f35bc00d that #44 deferred: `ContextDictionarySpecs.cs`, `PolicyOverloadSmokeSpecs.cs` and `Timeout/TimeoutOverloadSmokeSpecs.cs`. |
| **Merge base** | The commit on `main` that the delivery branch was created from, or was last updated from. |
| **TFM** | A target framework moniker. The **mandatory TFMs** are `net10.0`, `net9.0` and `net8.0`, which every test project targets on every OS. **`net481`** is an extra TFM that test projects target only on Windows. The **built TFMs** on an OS are the TFMs test projects target there: the three mandatory TFMs on macOS and Linux, plus `net481` on Windows. |
| **Test project** | Any project matching `test/**/*{Tests,Specs}.csproj`: `Paramore.Fences.Core.Tests`, `Paramore.Fences.Extensions.Tests`, `Paramore.Fences.RateLimiting.Tests`, `Paramore.Fences.Testing.Tests` and `Paramore.Fences.Specs`. `Paramore.Fences.AotTest` and `Paramore.Fences.TestUtils` are not test projects for the purposes of this document. |
| **Covered assembly** | The production assembly a test project measures, found by removing the `.Tests` or `.Specs` suffix. For example, `Paramore.Fences.Core.Tests` → `Paramore.Fences.Core`, and `Paramore.Fences.Specs` → `Paramore.Fences`. |
| **Coverage file** | One Cobertura XML file holding the coverage for one test project on one TFM. Upstream's reference layout is `artifacts/coverage/<TestProject>/coverage.<tfm>.xml`, for example `artifacts/coverage/Paramore.Fences.Specs/coverage.net9.0.xml`. |
| **Coverage report** | The human-readable output generated from coverage files: an HTML report per test project in `artifacts/coverage-reports/<TestProject>/`, plus, on CI, a Markdown summary per test project per TFM in the job step summary. As in the upstream reference, the HTML report directory is shared by a project's TFMs, so each TFM's report overwrites the previous one and the directory ends up holding the last TFM processed. Per-TFM coverage is visible in the coverage files, the threshold check output and the CI job summary, not in the HTML. |
| **Coverage figures** | The line, branch and method coverage percentages of the covered assembly in one coverage file. |
| **Threshold triple** | The value of a test project's `<Threshold>` MSBuild property, read in the order line, branch, method. A single value applies to all three metrics: `100` means line 100, branch 100, method 100. If there are fewer values than metrics, the last value is reused for the rest (coverlet's rule, which upstream keeps). |
| **Threshold check** | Comparing each coverage file's coverage figures with its test project's threshold triple. A metric **passes** when actual ≥ required, compared on the unrounded value, so 99.995 against 100 fails. (How that figure is *printed* is upstream's format; see FR-5.) |
| **Upstream reference** | What App-vNext/Polly@1a80392b does for the same concern. Fences matches it, except for the divergences NFR-7 lists. |
| **Review fix** | A commit added to the PR in response to PR review comments or CI failures on the PR. |
| **Mutation target** | One of the five Cake mutation targets: `MutationTestsCore`, `MutationTestsExtensions`, `MutationTestsLegacy`, `MutationTestsRateLimiting` and `MutationTestsTesting`. |
| **Mutation score** | The score Stryker reports for one mutation target, in `artifacts/mutation-report/`. |
| **Authorised dependency change** | A package or tool change listed in FR-2. Any other dependency change is unauthorised. |
| **Implementation start** | The date the first implementation commit for this spec is authored. |
| **Current version** | The highest stable (non-prerelease) version of a package or tool on nuget.org at implementation start, subject to the major-version rule in FR-2. |

## Problem Statement

As a **Fences maintainer who syncs from upstream Polly**, I would like Fences' test projects, coverage pipeline and mutation-testing setup to run on xunit v3 and Microsoft.Testing.Platform (MTP) v2, as upstream's do since App-vNext/Polly@1a80392b, so that:

- later upstream commits that touch tests, build scripts, coverage or Stryker configuration can be ported one commit at a time, without re-engineering each one for xunit v2 (ADR 0003, "rebrand, don't redesign");
- the coverage figures CI reports and enforces are reproducible for every TFM, rather than coverlet 6.0.4's non-deterministic results (#14);
- the deferred half of App-vNext/Polly@f35bc00d can finally land, because the files it edits only exist after 1a80392b.

## Proposed Solution

From the maintainer's point of view, after this work:

1. `./build.ps1` (Cake target `Build`) runs every test project on every TFM it targets, using xunit v3 on MTP v2. It writes one coverage file per test project per TFM, generates a coverage report from each file, and fails the build if any coverage figure falls below that project's threshold triple. The failure message names the project, the TFM and the metric. The reports are generated even when the threshold check fails, so the project that needs inspecting is never missing from them.
2. CI's job step summary shows coverage for every test project on every built TFM, with figures from Microsoft's CodeCoverage instead of coverlet, and the threshold check is enforced per TFM. CI's `coverage-<os>` artefact keeps one HTML report per test project. As upstream's does, that report holds the last TFM processed, so the per-TFM HTML fix proposed in PR #19 is not adopted.
3. Mutation testing runs on MTP with Stryker at its current version, and no mutation target's score regresses.
4. The `Paramore.Fences.Specs` threshold rises to upstream's `96,95,93`. Every other test project stays at `100`.
5. The result matches upstream's: Fences' test, coverage and Stryker setup differs from upstream's only by rebranding and the divergences NFR-7 lists.
6. One pull request delivers the 1a80392b port, followed by the f35bc00d Specs port as a separate commit. After it merges, #14 is closed or updated, PR #19 and the superseded Dependabot PRs are closed, #6 gets a closing note, and #28's triage record links the merged PR.

## Requirements

### Functional Requirements

**FR-1 Test framework and runner.** Every test project builds and runs on xunit v3 and MTP v2, on every TFM it targets on the OS in use.

- Each test project produces an executable test host. Upstream's reference is `<OutputType>Exe</OutputType>` in each test `.csproj`.
- No source file under `test/` references the `Xunit.Abstractions` namespace. Today it is referenced by the `<Using Include="Xunit.Abstractions" />` item in `test/Paramore.Fences.Specs/Paramore.Fences.Specs.csproj`, and by `test/Paramore.Fences.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs`.
- No test project references the `xunit` (v2) package or `FsCheck.Xunit`. `Paramore.Fences.Core.Tests` references `FsCheck.Xunit.v3` instead.
- The coverlet-specific `<Include>[...]</Include>` property is removed from every test `.csproj` (5 files today), as upstream did.
- `global.json` selects Microsoft.Testing.Platform as the `dotnet test` runner. Upstream's reference is `"test": { "runner": "Microsoft.Testing.Platform" }`.
- *Example:* on macOS, `dotnet test --project ./test/Paramore.Fences.Core.Tests --framework net9.0` runs `Paramore.Fences.Core.Tests` for `net9.0` only, through the MTP host, and exits with code 0 when all tests pass.
- *Boundary:* on Windows, the same projects also run on `net481`, and those runs must pass. `net481` coverage is collected and checked too (FR-3, FR-5).

**FR-2 Authorised dependency and tool changes.** Only the following dependency and tool changes are made. Each takes the current version, reconciled with Fences' Dependabot state as the rules below describe.

| File | Change | Upstream's 1a80392b version (reference only) | Fences today |
|---|---|---|---|
| `Directory.Packages.props` | Remove `coverlet.msbuild` | — | 6.0.4 |
| `Directory.Packages.props` | Remove `ReportGenerator` (package) | — | 5.5.11 |
| `Directory.Packages.props` | Remove `JunitXml.TestLogger` | — | 8.0.0 |
| `Directory.Packages.props` | Remove `xunit` | — | 2.9.3 |
| `Directory.Packages.props` | Replace `FsCheck.Xunit` with `FsCheck.Xunit.v3` | 3.4.0 | `FsCheck.Xunit` 3.4.0 |
| `Directory.Packages.props` | Add `xunit.v3.mtp-v2` | 4.0.0 | absent |
| `Directory.Packages.props` | Add `xunit.v3.assert` | 4.0.0 | absent |
| `Directory.Packages.props` | Add `Microsoft.Testing.Platform` | 2.4.0 | absent |
| `Directory.Packages.props` | Add `Microsoft.Testing.Extensions.CodeCoverage` | 18.11.2 | absent |
| `Directory.Packages.props` | Bump `Microsoft.NET.Test.Sdk` | 18.10.0 | 18.9.0 |
| `.config/dotnet-tools.json` | Add `dotnet-reportgenerator-globaltool` (command `reportgenerator`) | 5.5.11 | absent |
| `.config/dotnet-tools.json` | Bump `dotnet-stryker` | 5.0.0 | 4.16.0 |
| `src/Snippets/Snippets.csproj` | Replace the `xunit` reference with `xunit.v3.assert` | — | references `xunit` |

Rules:

- `xunit.runner.visualstudio` (4.0.0) and `GitHubActionsTestLogger` (3.0.5) stay referenced, as upstream keeps them. No version change is authorised for either.
- **Current version:** the highest stable version on nuget.org at implementation start. *Major-version rule:* if that version is a higher major than upstream's (for example, a `Microsoft.Testing.Platform` 3.x), use the highest stable version within upstream's major instead (2.x), and record it as an open item in the PR body. MTP v2 is the target of this port.
- **Dependabot reconciliation, merged:** if `main` already carries a version at or above the current version when implementation starts, because Dependabot moved it, keep `main`'s version.
- **Dependabot reconciliation, open PRs:** any open Dependabot PR that changes a package or tool in an FR-2 row is not merged separately. After this spec's PR merges, it is closed as superseded, with a comment linking the merged PR. When drafting, those PRs were #41 (`Microsoft.NET.Test.Sdk` 18.9.0 → 18.10.1) and #39 (`dotnet-stryker` 4.16.0 → 5.0.0). The rule applies to whichever PRs are open at merge time. Open Dependabot PRs for packages outside FR-2 (for example #37, `cake.tool`) are not affected.
- The PR body contains a table listing, for each row above, the version chosen, the date it was checked and the source (nuget.org, or "already on main"), plus the Dependabot PRs it supersedes.
- *Error case:* any other change to `Directory.Packages.props`, `.config/dotnet-tools.json` or `global.json`'s `msbuild-sdks` (for example, bumping `Cake.Sdk` or `Microsoft.Extensions.*`) is unauthorised and must not appear in the diff.

**FR-3 Per-TFM coverage collection.** Every test run on a built TFM writes exactly one coverage file for that test project and TFM, at `artifacts/coverage/<TestProject>/coverage.<tfm>.xml` (the upstream reference layout). The coverage comes from `Microsoft.Testing.Extensions.CodeCoverage` in Cobertura format.

- *Example:* after `./build.ps1` on Linux, these 15 files exist, one for each of the 5 test projects on each of the 3 mandatory TFMs: `artifacts/coverage/Paramore.Fences.Core.Tests/coverage.net10.0.xml`, `.../coverage.net9.0.xml`, `.../coverage.net8.0.xml`, and the same three for `Paramore.Fences.Extensions.Tests`, `Paramore.Fences.RateLimiting.Tests`, `Paramore.Fences.Testing.Tests` and `Paramore.Fences.Specs`.
- *Boundary, Windows:* on Windows, `net481` coverage is also collected, as upstream does, giving 20 files (`.../coverage.net481.xml` per project). This is a change from today, where `eng/Test.targets` excludes .NET Framework from coverage.
- Running one TFM never overwrites another TFM's coverage file.
- Code marked `[ExcludeFromCodeCoverage]` is excluded from the coverage figures, as it is under coverlet today.
- `coverlet.msbuild` and every MSBuild property or target that only coverlet consumes (`CollectCoverage`, `CoverletOutputFormat`, `ExcludeByAttribute`, `GenerateCoverageReports` after `GenerateCoverageResultAfterTest`) are removed from `eng/Test.targets`.

**FR-4 Coverage report generation.** As in the upstream reference, the Cake `Build` target runs the `reportgenerator` tool once per coverage file, tagged with its TFM, writing into `artifacts/coverage-reports/<TestProject>/`. On GitHub Actions it also appends one collapsible Markdown summary per test project per TFM to the job step summary, with a heading in the form `<TestProject> Code Coverage report (<tfm>)`.

- *Example:* on Linux, `artifacts/coverage-reports/Paramore.Fences.Specs/` contains an HTML report for `Paramore.Fences.Specs`. A CI job summary for Linux contains three `Paramore.Fences.Specs Code Coverage report (…)` sections, for `net10.0`, `net8.0` and `net9.0`.
- *Boundary, shared HTML directory:* a project's TFMs share one report directory, so the HTML report there reflects the last TFM processed. This matches upstream and is accepted (see the glossary's "Coverage report"). No per-TFM HTML subdirectories are added.
- Reports are generated with the `reportgenerator` local tool (FR-2), not the ReportGenerator MSBuild package.
- **Reports survive a failing threshold check.** Report generation does not depend on the threshold check passing. As upstream does, `__GenerateCoverageReports` runs before `__VerifyCoverageThresholds`. So when the threshold check (FR-5) fails, every test project's HTML report and every TFM's job-summary section have already been written. This carries over PR #19's failing-gate fix, and the ordering is now required rather than incidental.
- *Boundary, failing test:* if a test fails, the build fails at the test run. Coverage reports are not required in that case, as upstream's reference does not produce them either.
- *Error case:* if `reportgenerator` exits non-zero for a coverage file, the build fails, and the message names the test project and TFM (upstream: `Failed to generate the coverage report for '<project>' (<tfm>).`). `reportgenerator` exits 0 on an empty or malformed coverage file, so that case is caught by FR-5, not here.

**FR-5 Per-TFM threshold enforcement.** The Cake `Build` target runs the threshold check on every coverage file for every test project that declares `<Threshold>`. The build fails if any metric on any built TFM fails, including `net481` on Windows, as upstream does.

- Thresholds are still read from each test project's `<Threshold>` property (and from `<ThresholdType>` if present), so the `.csproj` stays the single source of truth.
- Coverage figures are those of the covered assembly only. Upstream's reference uses the Cobertura `package` element whose `name` equals the covered assembly name.
- Each failure message names the test project, TFM, metric, actual percentage and required percentage, in upstream's message format (actual formatted `0.##`). *Example:* `Paramore.Fences.Specs (net8.0): branch coverage of 94.87% for 'Paramore.Fences' is below the required threshold of 95%.`
- *Boundary, rounding:* upstream's format can print a failing figure that rounds up to the threshold. For example, 99.995 against 100 prints `coverage of 100% … is below the required threshold of 100%`. That is accepted, to stay on upstream's format. The comparison is still on the unrounded value, so the build fails correctly, and the message still names the project, TFM and metric to inspect.
- All violations across all projects and TFMs are reported before the build fails. The check does not stop at the first.
- On success, the figures for each project and TFM are printed. *Example:* `Paramore.Fences.Core.Tests (net10.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Core'.`
- *Boundary, one TFM short:* if `Paramore.Fences.Core.Tests` reaches 100/100/100 on `net10.0` and `net9.0` but 99.96% line on `net8.0`, the build fails with exactly one violation, for `net8.0` line.
- *Boundary, missing coverage file:* if a test project declares `<Threshold>` but the coverage file for one of its built TFMs is missing (for example, `artifacts/coverage/Paramore.Fences.Specs/coverage.net9.0.xml` was never written), the build fails, naming the project and the missing TFM. **This intentionally differs from upstream's reference, which silently skips missing files or directories.** It exists because a silently missing TFM is the failure mode #14 describes. The ADR records the divergence.
- *Boundary, covered assembly absent:* if a coverage file exists but contains no data for the covered assembly, the build fails with a message naming the project, TFM and assembly. This matches upstream.
- *Boundary, unreadable coverage file:* if a coverage file is empty or isn't valid XML, the build fails. As in upstream, the failure is the XML parser's exception, which isn't required to name the project or TFM.
- *Boundary, no `<Threshold>`:* a test project without `<Threshold>` is not checked, and that is not an error.
- *Boundary, `net481`:* on Windows, `net481` coverage files are checked against the same threshold triple as the mandatory TFMs, as upstream does, and FR-6's no-lowering rule holds for them.

**FR-6 Threshold values.** After the port, the `<Threshold>` values are exactly:

| Test project | Before | After |
|---|---|---|
| `Paramore.Fences.Core.Tests` | `100` | `100` |
| `Paramore.Fences.Extensions.Tests` | `100` | `100` |
| `Paramore.Fences.RateLimiting.Tests` | `100` | `100` |
| `Paramore.Fences.Testing.Tests` | `100` | `100` |
| `Paramore.Fences.Specs` | `94,94,91` | `96,95,93` |

- No threshold value is lowered, for any project or metric, by any commit in the delivery.
- Upstream reached these thresholds under the same tool with the tests, Stryker suppressions and `src/` changes in 1a80392b. Those are ported first (FR-7, FR-8). Fences' code differs from upstream's only by rebranding, so once they are ported Fences is expected to meet the thresholds too.
- If a coverage figure still falls short on any built TFM (including `net481` on Windows) after that, the gap is investigated as a likely porting omission first: a missed upstream test, guard or suppression, or a rebranding slip. Only a gap that is Fences-specific (it traces to code or tests that upstream doesn't have) may be closed with a Fences-only test or a documented exclusion. A documented exclusion is an `[ExcludeFromCodeCoverage]` attribute, or a coverage-settings exclusion, that carries a comment explaining why the code cannot be covered. Every Fences-only test or exclusion added this way is listed in the PR body with the gap it closes, and is recorded as an NFR-7 divergence.

**FR-7 Fences-native port of upstream's test changes.** Upstream's `test/` changes in 1a80392b are applied to Fences' own versions of the files, rebranded, not copied over them. Specifically:

| Upstream file | Fences file | Required outcome |
|---|---|---|
| `test/Shared/TestCancellation.cs` | `test/Shared/TestCancellation.cs` (exists, namespace `Paramore.Fences`) | `Token` returns the xunit v3 per-test cancellation token (`TestContext.Current.CancellationToken`), not `CancellationToken.None`. |
| `Helpers/Bulkhead/AnnotatedOutputHelper.cs`, `SilentOutputHelper.cs` | `test/Paramore.Fences.Specs/Helpers/Bulkhead/` (both exist) | Implement xunit v3's `ITestOutputHelper`: the `Output` property and the `Write` overloads. `SilentOutputHelper` stays silent. |
| `Issues/IssuesTests.InfiniteRetry_2163.cs` | `test/Paramore.Fences.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs` (exists) | The 20-second guard is linked to the test-context cancellation token. |
| `Caching/RelativeTtlSpecs.cs` | `test/Paramore.Fences.Specs/Caching/RelativeTtlSpecs.cs` (exists) | Joins the existing `Constants.SystemClockDependentTestCollection` (defined in `test/Paramore.Fences.Specs/Helpers/Constants.cs`) and resets `SystemClock` on dispose. |
| `ResiliencePipelineTests.cs`, `Utils/Pipeline/PipelineComponentFactoryTests.cs` | Fences equivalents (exist) | Compile under xunit v3's stricter `TheoryData` typing. |
| `Hedging/HedgingResiliencePipelineBuilderExtensionsTests.cs` | Fences equivalent (exists) | Gains upstream's added test (action generator reusing the primary callback). |
| `Retry/RetryHelperTests.cs` | `test/Paramore.Fences.Core.Tests/Retry/RetryHelperTests.cs` (exists) | Gains upstream's added test (decorrelated jitter backoff overflowing at attempt 1024). The `using FsCheck;` / `using FsCheck.Fluent;` directives and the `Arbitraries` class are wrapped in `#if !NETFRAMEWORK`, as upstream did, so the file compiles on `net481` with `FsCheck.Xunit.v3`. Today both are unguarded (lines 1–2 and about line 316). |
| `ContextDictionarySpecs.cs`, `PolicyBuilderAndHandleSyntaxSpecs.cs`, `PolicyOverloadSmokeSpecs.cs`, `Retry/RetryOverloadSmokeSpecs.cs`, `Timeout/TimeoutOverloadSmokeSpecs.cs` | New files under `test/Paramore.Fences.Specs/` (none exist today) | Added, rebranded to `Paramore.Fences.*`. |

- Apart from the rebranding, each Fences file ends up with the same test logic and the same test method names as upstream's: the same cases, data, guards, assertions and names.
- **New test methods keep upstream's names verbatim.**
  - Upstream's two new `Core.Tests` methods, `AddHedging_ActionGeneratorUsesCallback_InvokesPrimaryCallback` and `DecorrelatedJitterBackoffV2_AttemptCausesInfinity_ReturnsMaxTimeSpanTicksMinusOneThousand`, already follow `Member_Scenario_Outcome`.
  - Upstream's new `Specs` methods use snake_case (for example `Generic_dictionary_members_should_delegate_to_the_wrapped_dictionary`). That matches `Paramore.Fences.Specs`' existing legacy style, and the names are kept so that f35bc00d (FR-12) and later upstream edits to those files apply cleanly. `Paramore.Fences.Specs` already keeps legacy names (see Out of Scope), so this is not a new exception to `.agent_instructions/testing.md`.
  - Existing test methods are not renamed.
- Test doubles use NSubstitute only, as upstream's do. No FakeItEasy, Moq or FluentAssertions is introduced.
- Every test runs in process, with no external process, container or network service.
- The system-clock collection is reused, not duplicated. Fences already defines `SystemClockDependentTestCollection`, contrary to the issue's wording that a collection "is added".

**FR-8 Production (`src/`) changes from 1a80392b.** Upstream's `src/` changes are ported, rebranded, with no public API change.

| Upstream file | Fences file | Nature |
|---|---|---|
| `src/Polly.Core/Hedging/Controller/TaskExecution.cs` | `src/Paramore.Fences.Core/Hedging/Controller/TaskExecution.cs` | Internal refactor: secondary-action creation is extracted into a private method. No behaviour change. |
| `src/Polly.Core/Retry/RetryHelper.cs` | `src/Paramore.Fences.Core/Retry/RetryHelper.cs` | `// stryker disable once all : <reason>` on `MaxTimeSpanTicks`. The `en-us` slug is removed from the docs URL in the comment. |
| `src/Polly.Extensions/Telemetry/TelemetryUtil.cs` | `src/Paramore.Fences.Extensions/Telemetry/TelemetryUtil.cs` | `// stryker disable once all : <reason>` on the `True` and `False` static fields. |
| `src/Polly.Extensions/Telemetry/TelemetryOptions.cs` | `src/Paramore.Fences.Extensions/Telemetry/TelemetryOptions.cs` | The copy constructor chains to `: this()`. The signature is unchanged (already in `PublicAPI.Shipped.txt`). |
| `src/LegacySupport/ConcurrentDictionaryExtensions.cs` | `src/LegacySupport/ConcurrentDictionaryExtensions.cs` (exists) | Deleted, as upstream's "unused polyfill" removal. This is a removal, not the coverage extension the issue implies. |

- Each Stryker suppression comment carries the reason text upstream used, and is listed in the PR body.
- *Error case:* if deleting `ConcurrentDictionaryExtensions.cs` breaks any `netstandard2.0`, `net472` or `net462` build of `Paramore.Fences.Core`, `Paramore.Fences.Extensions` or `Paramore.Fences.RateLimiting` (all three set `<LegacySupport>true</LegacySupport>`), the file is kept. The divergence is recorded per ADR 0003 D7: a code comment, plus a note on #28. A-3 records why this isn't expected.

**FR-9 Mutation testing on MTP.** All five mutation targets run with the current `dotnet-stryker` (FR-2) against the MTP test runner.

- `eng/stryker-config.json` selects the MTP test runner. Upstream's reference is `"test-runner": "mtp"`. The existing `net10.0` / `Debug` / thresholds 100 settings stay unchanged.
- **On CI**, MinVer is disabled during mutation runs, as a workaround for stryker-mutator/stryker-net#3094. As in upstream's reference, `MinVerSkip: true` is set as a job-level `env` in `.github/workflows/mutation-tests.yml`, with a comment linking the Stryker issue. This lives in the workflow, not in `eng/stryker-config.json` as the issue states.
- **Locally**, each mutation target (`./build.ps1 -Target MutationTests<Name>`) must complete on a developer machine. Whether stryker-net#3094 affects local runs is determined during implementation by running `MutationTestsCore` locally without `MinVerSkip`, and the PR body records the result:
  - *Not affected:* nothing further. No local workaround is added.
  - *Affected:* the workaround is documented, not coded. The Mutation Testing section of `CONTRIBUTING.md` (and the mutation section of `.agent_instructions/testing.md`) tells the developer to set `MinVerSkip=true` for local mutation runs, with a link to stryker-net#3094. `cake.cs` is not changed for this, which keeps it aligned with upstream's. This documentation is part of FR-11, and is listed as an NFR-7 divergence.

**FR-10 CI reporting.** `.github/workflows/build.yml` keeps publishing the `coverage-<os>` artefact for each of macOS, Linux and Windows, and that artefact contains the FR-4 HTML coverage report for every test project. Because of FR-4's "reports survive a failing threshold check" rule, and the upload step's existing `if: always()`, this also holds when the threshold check fails.

- On GitHub Actions, test results are reported to the job summary through MTP's GitHub reporter, with passed tests omitted. Upstream's reference passes `--report-github --report-github-summary-include-passed false` on CI.
- That reporter identifies a failing test, its project and its TFM in the job summary. This is relied on as the reporter's behaviour, and checked through the configuration (AC-24). No deliberately failing CI run is required.
- Every workflow step that reads coverage output keeps working with FR-3's and FR-4's locations. As found at the merge base, the only such step is "Upload Coverage Reports" in `build.yml`, which reads `./artifacts/coverage-reports`, the same directory FR-4 writes, so it needs no path change. No workflow uploads to Codecov, and `validate-packages` reads only the `packages-windows` artefact, so neither needs changing.

**FR-11 Documentation of the new test and coverage workflow.** Contributor-facing documentation describes the tooling as it is after the port, and every command it shows works.

- **The documentation files** are `CONTRIBUTING.md`, `.agent_instructions/testing.md`, `.agent_instructions/build_and_development.md` and `.agent_instructions/dependency_management.md`.
- **Every `dotnet test` invocation** in the documentation files, inline in prose or in a code block, is updated to work under the MTP runner selected in `global.json` (FR-1). A project path moves from a positional argument to `--project`: `dotnet test ./test/Paramore.Fences.Core.Tests` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests`. A VSTest `--filter "FullyQualifiedName~…"` is replaced by a working MTP / xunit v3 equivalent. The ADR picks the syntax. At the merge base, the invocations are at `CONTRIBUTING.md:48`, `:331`, `:334`, `:337` and `:340`; `.agent_instructions/build_and_development.md:59`, `:62`, `:65` and `:69`; and `.agent_instructions/testing.md:122`. That list is a starting point, not the scope. The scope is every invocation in those files.
- **Tooling descriptions** are updated to match: the coverage paragraph at `CONTRIBUTING.md:236` ("Coverage is collected by coverlet…"); the property-based and coverage rows of the tooling table at `.agent_instructions/testing.md:20` and `:22` (FsCheck.Xunit → FsCheck.Xunit.v3, and coverlet / ReportGenerator → Microsoft CodeCoverage / the `reportgenerator` tool); and the test-stack sentence at `.agent_instructions/dependency_management.md:42`, which lists coverlet.
- **Mutation testing docs** change only if FR-9 finds local runs affected, in which case FR-9's `MinVerSkip` note is added to the Mutation Testing section of `CONTRIBUTING.md` (line 219 onwards) and the mutation section of `.agent_instructions/testing.md` (line 94 onwards).
- `.github/wordlist.txt`'s `coverlet` entry stays. This spec's files and the ADR for this work mention coverlet, and the spell check (`.github/spellcheck.yml`) reads `specs/**/*.md` and `docs/adr/*.md`. (`fork-migration-plan.md`, which also uses the word, is excluded from the spell check.)
- *Example:* every `dotnet test` command in the documentation files exits with code 0 when run from the repository root on the delivery branch.
- If `src/Snippets` is changed (FR-2 changes `Snippets.csproj`), `dotnet mdsnippets` is run and any regenerated Markdown is committed. No Markdown diff is expected, because no snippet source changes.

**FR-12 The f35bc00d Specs port.** After the 1a80392b port, a separate commit applies f35bc00d's edits to the three Fences Specs files created by FR-7:

- `ContextDictionarySpecs.cs`: the explicitly typed local becomes `var context = new Context("operation-key");`.
- `PolicyOverloadSmokeSpecs.cs`: the async smoke test gains upstream's three extra calls (`ExecuteAsync` returning 53, and the two `ExecuteAndCaptureAsync` calls) passing `continueOnCapturedContext: true`.
- `Timeout/TimeoutOverloadSmokeSpecs.cs`: a private static helper is **added** to the class, `private static Context CreateContext() => new("operation-key");` (f35bc00d introduces it; it doesn't exist at 1a80392b), and every `new Context("operation-key")` argument to `ShouldTimeout` is replaced by a call to `CreateContext()`.
- This commit touches only these three files. Because FR-7 keeps upstream's method names, f35bc00d's hunks land in methods of the same name.

**FR-13 Delivery, traceability and issue housekeeping.**

- One pull request, from a non-`main` branch, contains (in order) a commit whose subject begins `Port App-vNext/Polly@1a80392b: ` and a later commit whose subject begins `Port App-vNext/Polly@f35bc00d: `. Fences-only commits, such as documentation (FR-11) or closing a Fences-specific coverage gap (FR-6), may sit between them or before the first. After the f35bc00d commit, only review fixes may follow.
- The PR title is written in Fences' own voice and does not start with `Port App-vNext/Polly@` (ADR 0003 D8). *Example:* "Move tests to xunit v3 and Microsoft Testing Platform v2, with per-framework coverage".
- The PR body links #28 and #34, names both upstream SHAs, and includes:
  - the FR-2 version table and the Dependabot PRs it supersedes, plus, if FR-2's major-version rule held any package back, an open item naming it;
  - the FR-6 list of Fences-only tests and exclusions (or "none");
  - the FR-8 suppression list;
  - the FR-9 finding on local mutation runs;
  - the FR-14 baseline (with its SHA and source) and result scores, with rerun or local-run evidence if used;
  - the FR-15 reproducibility evidence;
  - if NFR-4's 20% limit was exceeded, the measured build times and the cause.
- `CHANGELOG.md` is not edited.
- The PR is not merged without Ian's approval.
- After merge:
  - #28's triage entry for `1a80392b` (and the f35bc00d Specs half) links the merged PR.
  - #6, a Dependabot PR already closed unmerged, gets a comment linking the merged PR and recording that coverlet was removed, so the `coverlet.msbuild` 10.x bump it held is moot. It is not reopened.
  - PR #19 is closed unmerged, with a comment linking the merged PR. The comment records that its failing-gate fix is carried (FR-4, reports survive a failing threshold check). It also records that its per-TFM HTML report fix is deliberately *not* carried, to stay on upstream's report layout, and that per-TFM coverage is visible instead in the coverage files, the threshold check output and the CI job summary.
  - Each open Dependabot PR superseded under FR-2 is closed, with a comment linking the merged PR.
  - #14 is handled as FR-15 specifies.

**FR-14 No mutation score regression.** Before any implementation change, the mutation score of each of the five mutation targets is recorded as a baseline. After the port, each target's score is greater than or equal to its baseline.

- **Baseline source:** the completed `mutation-tests` CI run on `main` at the merge-base SHA. `mutation-tests` runs on every push to `main`, and `workflow_dispatch` can start one if needed. The PR body records the SHA and the run link.
- **Moving merge base:** if the branch is updated from `main` and the merge base changes, the baseline is re-taken from the `mutation-tests` run at the new merge-base SHA, and the PR body is updated to match.
- **Like for like:** a CI result is compared with the CI baseline. A local result (the fallback below) is compared with a local `MutationTestsCore` run at the merge-base SHA, on the same machine.

- Mutants removed from scoring by FR-8's Stryker suppressions are reported separately in the PR body: count per target, and file.
- *Boundary, `mutations-core` hang:* #46 already records that `mutations-core` intermittently hangs to the 60-minute job timeout on unmodified `main` (13 of 77 runs since 2026-08-26, each cleared by a rerun). That record is enough evidence that the hang predates this port, and it doesn't have to be reproduced again. If `mutations-core` hangs on the PR:
  1. *Primary:* rerun the failed job (`gh run rerun <id> --failed`). If the rerun completes, its score is the FR-14 result, and the PR body links both the hung run and the completed rerun.
  2. *Fallback:* if CI reruns keep hanging (two consecutive hung reruns), a completed local `MutationTestsCore` run on the PR's final commit supplies the score, compared with a local run at the merge-base SHA on the same machine. The PR body reports both, with SHAs, and #46 gets a comment linking the hung runs.
  - In either case the score must still be ≥ the baseline. A hang that neither path resolves counts as a failure of this requirement.

**FR-15 Coverage reproducibility and #14 disposition.** For the final PR commit, the full test run (`./build.ps1`) is executed 3 times on the same OS. For every test project and built TFM on that OS, the line, branch and method figures (to 2 decimal places), and the number of tests executed, are identical across all 3 runs.

- If they are identical, #14 is closed with a comment linking the merged PR and the evidence.
- If they are not, #14 stays open and is updated to describe the remaining non-determinism under Microsoft CodeCoverage: which project, TFM and metric, and the observed values. That update is required for this spec to be done, but a reproducibility failure alone does not block the merge, provided every threshold check passes in CI.

### Non-functional Requirements

- **NFR-1 Analyser-clean.** The solution builds with zero warnings under the repository's warnings-as-errors settings (StyleCop, Sonar, BannedApiAnalyzers, PublicApiAnalyzers), on all three CI operating systems.
- **NFR-2 No public API change.** `git diff <merge-base> -- 'src/**/.PublicAPI/*'` is empty, except for D7-divergence entries in `PublicAPI.Unshipped.txt`. FR-8's changes are expected to need none. If a port change does need one, it is added to `PublicAPI.Unshipped.txt` (never `Shipped.txt`), commented at the point of divergence, and flagged on #28 for a maintainer decision.
- **NFR-3 No observable behaviour change in `src/`.** Every existing test passes, unmodified apart from FR-7's xunit v3 API adaptations, on every TFM.
- **NFR-4 Build duration.** On each CI OS, the `build` job's wall-clock time on the PR's final commit is no more than 20% longer than the median of the last 3 successful `build` runs on `main` before the merge base. If it is longer, the PR body records the measured times and the cause.
- **NFR-5 Stability.** No test is skipped, quarantined or marked `Explicit` to make the build pass. The number of tests executed per project per TFM is reported in the FR-15 evidence.
- **NFR-6 In-process tests only.** No new test needs Docker, a database, a broker, network access or an external process.
- **NFR-7 Upstream alignment.** Upstream has already made this migration, and Fences' code differs from upstream's only by rebranding, so the port is not expected to drift from upstream. The build-script, coverage, test and Stryker changes match the upstream reference, rebranded, so future upstream commits apply with only rebranding. The only exceptions are the divergences below. Each is recorded in the ADR, and the list is closed:
  1. FR-5's failure on a missing coverage file (upstream silently skips it).
  2. FR-8's fallback, if `ConcurrentDictionaryExtensions.cs` has to be kept.
  3. FR-9's documented local `MinVerSkip` note, if local mutation runs are affected.
  4. FR-6's Fences-only tests or exclusions, if a Fences-specific coverage gap needs them.

  No FR leaves a choice to the ADR that would diverge from upstream. Any divergence not on this list needs the requirements amended first. FR-11's documentation changes are Fences-owned files, not divergences.

### Constraints and Assumptions

**Constraints**

- C-1. ADR 0003 D7: rebrand, don't redesign. No public API change except as D7 allows.
- C-2. ADR 0003 D8 and the runbook (`.agent_instructions/upstream_sync.md`): the commit message carries the upstream SHA, the PR title is the CHANGELOG entry, and `CHANGELOG.md` is never edited by hand.
- C-3. CLAUDE.md verification gate 6 ("no dependency was added or updated") is waived **only** for the authorised dependency changes in FR-2. Issue #34 and the user's decision 1 are the authorisation.
- C-4. Thresholds are never lowered (user decision 2; #14's acceptance criteria).
- C-5. No commits to `main`, no licence headers, and no merge without Ian.
- C-6. Test projects keep multi-targeting `net10.0;net9.0;net8.0`, plus `net481` on Windows. This spec does not add or remove TFMs.
- C-7. Stryker keeps running against `net10.0`, in `Debug`, with high and low thresholds of 100.

**Assumptions** (each to be confirmed during design or implementation)

- A-1. `Microsoft.Testing.Extensions.CodeCoverage` is deterministic enough to satisfy FR-15 on every built TFM. If it isn't, FR-15's #14 update path applies.
- A-2. Under Microsoft CodeCoverage, all five test projects reach FR-6's thresholds on every built TFM, including `net481` on Windows, once upstream's 1a80392b tests, guards, suppressions and `src/` changes are ported, because upstream reached them with exactly those changes and Fences' code differs only by rebranding. A shortfall that remains is presumed to be a porting omission until shown otherwise (FR-6).
- A-3. *Verified while drafting:* Fences' `src/LegacySupport/ConcurrentDictionaryExtensions.cs` 3-argument `GetOrAdd` polyfill is unused, as upstream found. Every `GetOrAdd(` call in the three `<LegacySupport>` projects uses a 2-argument overload (`src/Paramore.Fences.Core/Registry/ResiliencePipelineRegistry.cs` and `ResiliencePipelineRegistry.TResult.cs`). The legacy `src/Paramore.Fences` project doesn't compile LegacySupport. Deletion is expected to succeed, and FR-8's error case is only a safety net.
- A-4. Fences' Dependabot `xunit` group (pattern `xunit*` in `.github/dependabot.yml`) will cover the new `xunit.v3.*` packages after the port, with no Dependabot configuration change. Packages outside that pattern (`Microsoft.Testing.*`, `FsCheck.Xunit.v3`) are tracked as ungrouped updates.
- A-5. The stray `coverage.net*.cobertura.xml` files found in `test/Paramore.Fences.Specs/` are local, untracked coverlet output, ignored by `.gitignore` (`coverage.*.cobertura.xml`). They are not part of the repository. New coverage output goes under `artifacts/`, which is already ignored.
- A-6. The #46 testhost hang and the varying `Paramore.Fences.Core.Tests` test count (774 / 777 / 779) may be caused by the xunit v2 / VSTest host, and may disappear under MTP. This is not assumed to be true. It is measured by FR-15 and FR-14.

### Out of Scope

- Porting any other upstream commit, including any later commit that builds on 1a80392b. The non-Specs half of f35bc00d is already ported by #44.
- Any dependency or tool change not listed in FR-2: for example `Cake.Sdk` (Fences 6.3.0, upstream 6.2.0), `Microsoft.Extensions.*`, removing `GitHubActionsTestLogger` or `xunit.runner.visualstudio`, or migrating to a different assertion or mocking library.
- Renaming existing test methods (including `Paramore.Fences.Specs`' legacy `Should_...` names) to `Member_Scenario_Outcome`, or renaming test methods ported from upstream (FR-7).
- Per-TFM HTML coverage reports (PR #19's per-TFM report fix). FR-4 keeps upstream's shared report directory.
- Raising any threshold other than `Paramore.Fences.Specs`' (FR-6), or raising Specs beyond `96,95,93`.
- Fixing #46's root cause, beyond FR-14's evidence requirement.
- Adding an external coverage service such as Codecov.
- Changing Stryker's mutation scope, ignored methods or ignored mutation kinds.
- Changing which TFMs the libraries or the tests target.
- Rewriting Polly-branded prose under `docs/` beyond FR-11's files.

## Acceptance Criteria

**AC-1 (FR-1)** Given the delivery branch on macOS, Linux and Windows, when `./build.ps1` runs, then every test project runs on each mandatory TFM (and on `net481` on Windows) through the MTP host, and all tests pass.

**AC-2 (FR-1)** Given the delivery branch, when `git grep -n "Xunit.Abstractions" -- test/` and `git grep -nE 'Include="(xunit|FsCheck\.Xunit)"' -- test/ eng/ Directory.Packages.props` run, then both produce no output. When `git grep -n "<Include>\[" -- 'test/*.csproj'` runs, then it also produces no output.

**AC-3 (FR-1)** Given `global.json` on the delivery branch, when it is read, then it selects `Microsoft.Testing.Platform` as the test runner. Given each of the 5 test `.csproj` files, then each produces an executable test host.

**AC-4 (FR-2)** Given `git diff <merge-base> -- Directory.Packages.props .config/dotnet-tools.json global.json src/Snippets/Snippets.csproj`, when the diff is compared with FR-2's table, then every added, removed or changed package or tool appears in the table, and no other package, tool or SDK version has changed.

**AC-5 (FR-2)** Given the PR body, when the version table is read, then every FR-2 row lists a version, a checked date on or after implementation start, and a source. For each row, the version is either the current version under FR-2's major-version rule, or the version already on `main` if that is higher.

**AC-6 (FR-3)** Given a clean checkout on Linux, when `./build.ps1` completes, then exactly 15 coverage files exist, one for each of the 5 test projects × 3 mandatory TFMs, each named so that its TFM is identifiable, each valid Cobertura XML containing a `package` for its covered assembly.

**AC-7 (FR-3)** Given `eng/Test.targets` on the delivery branch, when it is searched for `coverlet`, `CollectCoverage`, `CoverletOutputFormat` and `ReportGenerator`, then there are no matches.

**AC-8 (FR-4)** Given a completed `./build.ps1`, when `artifacts/coverage-reports/<TestProject>/` is opened for each of the 5 test projects, then each contains an HTML report (`index.html`). Given a CI run on any OS, when the `build` job summary is opened, then it contains one collapsible "`<TestProject>` Code Coverage report (`<tfm>`)" section per test project per built TFM on that OS. Given `cake.cs`, then the report step runs `reportgenerator` once per coverage file, tagged with its TFM, into `artifacts/coverage-reports/<TestProject>/`, with no per-TFM subdirectory.

**AC-9 (FR-4)** Given the report step in `cake.cs` on the delivery branch, when its handling of the `reportgenerator` exit code is read, then a non-zero exit fails the build with a message naming the test project and the TFM tag. (`reportgenerator` exits 0 on an empty or malformed file, so this is checked by inspection, not by corrupting a file. The empty-file case is AC-15.)

**AC-10 (FR-4, FR-10)** Given `Paramore.Fences.Core.Tests` with `<Threshold>` temporarily set to `101` in a local, uncommitted experiment, when `./build.ps1` runs and the threshold check fails, then `artifacts/coverage-reports/` still contains an HTML report for every test project, including `Paramore.Fences.Core.Tests`. Given `cake.cs`, then the report step runs before the threshold check and doesn't depend on it. Given `.github/workflows/build.yml`, then the "Upload Coverage Reports" step keeps `if: always()` and uploads `./artifacts/coverage-reports`.

**AC-11 (FR-5)** Given all coverage figures meet their thresholds, when `./build.ps1` runs, then the threshold check passes and prints line, branch and method figures for each test project and built TFM. On Windows, that includes `net481`.

**AC-12 (FR-5)** Given macOS or Linux (where only the three mandatory TFMs are built), and `Paramore.Fences.Core.Tests` with `<Threshold>` temporarily set to `101` in a local, uncommitted experiment, when `./build.ps1` runs, then the build fails and reports one violation per metric per mandatory TFM (9 in total) for `Paramore.Fences.Core.Tests`. Each names the project, TFM, metric, actual figure and required 101%.

**AC-13 (FR-5)** Given coverage files where only `net8.0` of one project falls short on one metric, when the threshold check runs, then the build fails with exactly one violation, naming that project, `net8.0` and that metric. The other TFMs' figures are still printed. Given a figure of 99.995 against a requirement of 100, then the check fails, even though upstream's format prints the actual figure as `100%`.

**AC-14 (FR-5)** Given `artifacts/coverage/Paramore.Fences.Specs/coverage.net9.0.xml` is deleted after the test run, when the threshold check runs, then the build fails, naming `Paramore.Fences.Specs` and `net9.0` as missing.

**AC-15 (FR-5)** Given a coverage file that contains no `package` for the covered assembly, when the threshold check runs, then the build fails, naming the project, TFM and assembly. Given a coverage file replaced with an empty file after the test run, when the threshold check runs, then the build fails. Given `<Threshold>` temporarily removed from `Paramore.Fences.RateLimiting.Tests` in a local, uncommitted experiment, when the threshold check runs, then that project is not checked, no violation is reported for it, and the other four projects are checked as usual.

**AC-16 (FR-6)** Given the delivery branch, when `<Threshold>` is read from each test `.csproj`, then the values match FR-6's "After" column exactly. Given every commit in the PR, when each commit's `<Threshold>` values are compared with the merge base, then no value is lower for any project or metric.

**AC-17 (FR-6)** Given the PR body, when the FR-6 list is read, then every Fences-only test and exclusion added by the PR appears with its file, member, reason, and the coverage gap it closes (or the list says "none"). The number of added exclusion attributes, `git diff <merge-base> -- src test | grep -cE '^\+[^+].*ExcludeFromCodeCoverage'`, equals the number of attribute entries listed. Every coverage-settings exclusion added (in any `.runsettings`, `.config` or MSBuild coverage setting introduced by the PR) is also listed.

**AC-18 (FR-7)** Given the delivery branch, when `test/Shared/TestCancellation.cs` is read, then `Token` returns xunit v3's `TestContext.Current.CancellationToken`. When `AnnotatedOutputHelper` and `SilentOutputHelper` are compiled, then they implement xunit v3's `ITestOutputHelper`, and `SilentOutputHelper.Output` returns an empty string. When `test/Paramore.Fences.Core.Tests/Retry/RetryHelperTests.cs` is read, then its `FsCheck` using directives and its `Arbitraries` class are inside `#if !NETFRAMEWORK`.

**AC-19 (FR-7)** Given the delivery branch, when `test/Paramore.Fences.Specs/` is listed, then `ContextDictionarySpecs.cs`, `PolicyBuilderAndHandleSyntaxSpecs.cs`, `PolicyOverloadSmokeSpecs.cs`, `Retry/RetryOverloadSmokeSpecs.cs` and `Timeout/TimeoutOverloadSmokeSpecs.cs` exist, in `Paramore.Fences.*` namespaces, with no `Polly` namespace references. `RelativeTtlSpecs` carries `[Collection(Constants.SystemClockDependentTestCollection)]` and resets `SystemClock` on dispose.

**AC-20 (FR-7)** Given `git diff <merge-base> -- test/`, when added `[Fact]` and `[Theory]` methods are listed, then the set of names equals the set of test methods 1a80392b adds under `test/` (`git show 1a80392b -- test/`), name for name, plus any Fences-only tests listed under FR-6. No added `using` references `FakeItEasy`, `Moq` or `FluentAssertions`, and no new `[CollectionDefinition]` duplicates `SystemClockDependentTestCollection`.

**AC-21 (FR-8)** Given the delivery branch, when FR-8's four modified files are diffed against the merge base, then each contains only the rebranded equivalent of upstream's change: the extracted private method in `TaskExecution`, the Stryker suppression comments with reasons in `RetryHelper` and `TelemetryUtil`, the slug-free URL, and `: this()` on the `TelemetryOptions` copy constructor. Given `src/LegacySupport/ConcurrentDictionaryExtensions.cs`, then it is deleted, or kept with a D7 divergence comment and a #28 note under FR-8's error case.

**AC-22 (FR-9)** Given `eng/stryker-config.json` on the delivery branch, then it selects the MTP test runner, and its `target-framework`, `configuration`, `thresholds`, `ignore-methods` and `ignore-mutations` are unchanged from the merge base. Given `.github/workflows/mutation-tests.yml`, then MinVer is disabled for the mutation jobs, with a comment linking stryker-mutator/stryker-net#3094. Given `cake.cs`, then no `MinVerSkip` setting has been added to it.

**AC-23 (FR-9)** Given a developer machine set up per `CONTRIBUTING.md` (including FR-9's `MinVerSkip` note, if FR-9 found local runs affected), when `./build.ps1 -Target MutationTestsCore` runs, then Stryker completes and writes a report to `artifacts/mutation-report/`. The PR body states whether local runs were affected by stryker-net#3094. If they were, `CONTRIBUTING.md`'s Mutation Testing section contains the `MinVerSkip` note linking that issue.

**AC-24 (FR-10)** Given a CI run of `build.yml` on the PR, when the `coverage-macos`, `coverage-linux` and `coverage-windows` artefacts are downloaded, then each contains an HTML coverage report for every test project. Given the test-run configuration on the delivery branch (`eng/Test.targets` and/or `cake.cs`), then on CI it passes `--report-github` and `--report-github-summary-include-passed false` to every test host. Given the PR's final green `build` run, when its job summary is opened, then it lists no passed tests.

**AC-25 (FR-10)** Given `.github/workflows/*.yml` on the delivery branch, when they are searched for `coverage-reports`, `coverage/` and `codecov`, then every path match points at a location the new build actually writes.

**AC-26 (FR-11)** Given the delivery branch, when every `dotnet test` invocation in the four FR-11 documentation files, inline or in a code block, runs from the repository root, then each exits with code 0. `git grep -nE 'dotnet test \./' -- CONTRIBUTING.md .agent_instructions/` produces no output (no positional project paths remain). When the four documentation files are searched for `coverlet`, `FullyQualifiedName~` and `FsCheck.Xunit` not followed by `.v3`, then there are no matches.

**AC-27 (FR-11)** Given the delivery branch, when `dotnet mdsnippets` runs, then `git status --porcelain` reports no changes.

**AC-28 (FR-12)** Given the PR's commit list, when the commit whose subject begins `Port App-vNext/Polly@f35bc00d: ` is inspected, then it modifies exactly `ContextDictionarySpecs.cs`, `PolicyOverloadSmokeSpecs.cs` and `Timeout/TimeoutOverloadSmokeSpecs.cs` under `test/Paramore.Fences.Specs/`, with the three FR-12 edits, including the added `CreateContext()` helper in `TimeoutOverloadSmokeSpecs`. After that commit, `Paramore.Fences.Specs` passes on every built TFM.

**AC-29 (FR-13)** Given the PR, then its head branch is not `main`. Its commits include `Port App-vNext/Polly@1a80392b: …` before `Port App-vNext/Polly@f35bc00d: …`, and only review fixes come after the latter. Its title does not start with `Port App-vNext/Polly@`. Its body links #28 and #34 and contains the FR-2 (with superseded Dependabot PRs), FR-6, FR-8, FR-9, FR-14 (with baseline SHA and source) and FR-15 sections. Where they apply, it also contains the FR-2 major-version open item and the NFR-4 timings. `git diff <merge-base> -- CHANGELOG.md` is empty.

**AC-30 (FR-2, FR-13)** Given the PR has merged, when #28, #6, PR #19 and the Dependabot PRs listed in the PR body as superseded are viewed, then:
- #28's `1a80392b` entry links the merged PR;
- #6 (still closed) has a comment linking the merged PR and stating that coverlet was removed;
- PR #19 is closed unmerged, with a comment linking the merged PR, stating that its failing-gate fix is carried, and stating that its per-TFM HTML report fix is deliberately not carried;
- each superseded Dependabot PR is closed unmerged, with a comment linking the merged PR.

**AC-31 (FR-14)** Given baseline mutation scores for all five mutation targets, taken from the `mutation-tests` CI run at the current merge-base SHA and recorded in the PR body with that SHA and run link, when the same targets run on the PR's final commit, then each score is ≥ its baseline, and the PR body lists both scores, plus the count of mutants removed by suppressions, for each target.

**AC-32 (FR-14)** Given `mutations-core` times out in CI on the PR, when the PR is reviewed, then it is acceptable only if either (a) the PR body links the hung run and a completed rerun of the same job whose `MutationTestsCore` score is ≥ baseline, or (b) after two consecutive hung reruns, the PR body reports a completed local `MutationTestsCore` run on the PR's final commit whose score is ≥ a local run at the merge-base SHA on the same machine, both with SHAs, and #46 has a comment linking the hung runs. Otherwise AC-31 fails.

**AC-33 (FR-15)** Given the PR's final commit, when `./build.ps1` runs 3 times on the same OS, then the PR body tabulates line, branch and method figures (2 decimal places) and test counts per test project per built TFM for each run. If all 3 runs are identical, #14 is closed after merge with a link to that table. If they are not, #14 is updated with the differing project, TFM, metric and values.

**AC-34 (NFR-1)** Given the PR's final commit, when CI's `build` job runs on macOS, Linux and Windows, then it succeeds with zero analyser warnings.

**AC-35 (NFR-2)** Given the delivery branch, when `git diff <merge-base> -- 'src/**/.PublicAPI/*'` runs, then it is empty, or every change is in `PublicAPI.Unshipped.txt`, with a matching D7 divergence comment and #28 flag.

**AC-36 (NFR-3, NFR-5)** Given the delivery branch, when `git diff <merge-base> -- test/` is searched for added `Skip =`, `[Trait("Category", "Quarantine")]` or `Explicit = true`, then there are no matches. All pre-existing tests pass on every TFM.

**AC-37 (NFR-4)** Given the PR's final `build` run and the last 3 successful `main` runs before the merge base, when wall-clock durations per OS are compared, then each OS is within 120% of its median, or the PR body records the times and cause.

**AC-38 (NFR-6)** Given the tests added by the PR, when they run on a machine with networking disabled and no Docker daemon, then they pass.

**AC-39 (NFR-7)** Given the ADR's divergence list, when each change in `git diff <merge-base>` to `eng/`, `cake.cs`, `eng/stryker-config.json`, `.github/workflows/`, `test/` and `src/` is compared with the rebranded upstream reference (`git show 1a80392b` for the 1a80392b port, `git show f35bc00d` for FR-12), then every difference beyond rebranding is either one of NFR-7's four permitted divergences, recorded in the ADR, or an FR-11 documentation change.

### Traceability

| Requirement | Acceptance criteria |
|---|---|
| FR-1 | AC-1, AC-2, AC-3 |
| FR-2 | AC-4, AC-5, AC-30 |
| FR-3 | AC-6, AC-7 |
| FR-4 | AC-8, AC-9, AC-10 |
| FR-5 | AC-11, AC-12, AC-13, AC-14, AC-15 |
| FR-6 | AC-16, AC-17 |
| FR-7 | AC-18, AC-19, AC-20 |
| FR-8 | AC-21, AC-35 |
| FR-9 | AC-22, AC-23 |
| FR-10 | AC-10, AC-24, AC-25 |
| FR-11 | AC-26, AC-27 |
| FR-12 | AC-28 |
| FR-13 | AC-29, AC-30 |
| FR-14 | AC-31, AC-32 |
| FR-15 | AC-33 |
| NFR-1 | AC-34 |
| NFR-2 | AC-35 |
| NFR-3 | AC-36 |
| NFR-4 | AC-37 |
| NFR-5 | AC-36, AC-33 |
| NFR-6 | AC-38 |
| NFR-7 | AC-39 (with AC-4 and AC-22 for packages and Stryker) |

## Additional Context

### Where the issue and the repository differ (found during drafting)

- **The tool bumped 4.16.0 → 5.0.0 is `dotnet-stryker`.** Fences is on 4.16.0 today. The tool added is `dotnet-reportgenerator-globaltool` 5.5.11.
- **The issue omits some upstream package and config changes.** Upstream also bumps `Microsoft.NET.Test.Sdk` 18.9.0 → 18.10.0, removes `JunitXml.TestLogger`, and adds a `"test": { "runner": "Microsoft.Testing.Platform" }` section to `global.json`. These are included in FR-1 and FR-2 as part of "the package and tool changes upstream's commit makes".
- **The `eng/stryker-config.json` change only selects the MTP test runner** (`"test-runner": "mtp"`). MinVer is disabled through `MinVerSkip: true` in `mutation-tests.yml`, not in the Stryker config.
- **`ConcurrentDictionaryExtensions.cs` is deleted** as an unused polyfill. It is not extended for coverage.
- **The system-clock test collection already exists in Fences** (`Constants.SystemClockDependentTestCollection`). Upstream only applies it to `RelativeTtlSpecs`.
- **No Fences workflow uploads to Codecov** (`codecov` appears only in `.github/wordlist.txt`). `validate-packages` does not read coverage. The only coverage consumer is the "Upload Coverage Reports" step in `build.yml`.
- **#6 is a Dependabot PR, not an issue,** and it was already closed unmerged on 2026-09-20, after "Holding this pending #14". FR-13 therefore asks for a closing comment, not a close.
- **Two open Dependabot PRs touch FR-2 packages:** #41 (`Microsoft.NET.Test.Sdk` → 18.10.1, above upstream's 18.10.0) and #39 (`dotnet-stryker` → 5.0.0). FR-2 supersedes them.
- **PR #19 makes two fixes,** per-TFM HTML reports and reports on a failing gate. Upstream's report step writes every TFM of a project into one directory, so its HTML report holds only the last TFM processed. That was checked by running ReportGenerator 5.5.11 three times into one directory. FR-4 carries the failing-gate fix and, to stay on upstream's layout, not the per-TFM HTML fix. The closing note on PR #19 records that.
- **`reportgenerator` exits 0 on an empty or malformed coverage file,** so upstream's non-zero-exit check doesn't catch it. The build still fails at the threshold check's XML load (FR-5).
- **Upstream's new test names need no renaming.** Its two new `Core.Tests` methods already follow `Member_Scenario_Outcome`, and its new `Specs` methods follow the Specs project's legacy snake_case style. Polly uses NSubstitute too, so test conventions add no divergence.
- **Upstream's threshold check silently skips a missing coverage directory or file.** FR-5 deliberately requires a failure instead, because of #14's history. This divergence is in build tooling, not public API, so D7 does not govern it, but NFR-7 requires the ADR to record it.
- **Under coverlet, `eng/Test.targets` excludes .NET Framework from coverage.** Upstream's new targets drop that condition, and its threshold check reads every `coverage.*.xml`, so upstream collects and checks `net481` on Windows. FR-3 and FR-5 do the same.
- **The f35bc00d `TimeoutOverloadSmokeSpecs` edit adds a private static `CreateContext()` helper to that file.** It doesn't exist at 1a80392b, and it isn't on `TimeoutSpecsBase`.
- **Upstream's `RetryHelperTests.cs` change also adds `#if !NETFRAMEWORK` guards** around the FsCheck usings and `Arbitraries`, which `FsCheck.Xunit.v3` needs on `net481`.
- **`TelemetryOptions(TelemetryOptions other)` is already in `src/Paramore.Fences.Extensions/.PublicAPI/PublicAPI.Shipped.txt`.** Adding `: this()` changes no signature.

### Upstream reference behaviour (for the ADR, not binding as design)

- Cake gains `__GenerateCoverageReports` and `__VerifyCoverageThresholds`, both dependent on `__RunTests` and both wired into `Build`. Method coverage is computed as the percentage of Cobertura `method` elements with `line-rate > 0`.
- `eng/Test.targets` passes `--coverage --coverage-output-format cobertura --coverage-output <path>` to the MTP host. On CI it also passes `--report-github --report-github-summary-include-passed false --report-xunit-junit` (JUnit file `<Project>.<tfm>.junit.xml`). The VSTest `Loggers` in Cake's `__RunTests` are removed.

### Open questions (to be settled in the ADR or at implementation start)

1. Whether the varying `Paramore.Fences.Core.Tests` test count (774 / 777 / 779) and the intermittent 99.96% line figure at `ComponentDisposeHelper.cs:30` persist under xunit v3 and Microsoft CodeCoverage. FR-15 measures this, and it decides #14's disposition.
2. Whether `dotnet-stryker` 5.x with MTP changes the `mutations-core` hang (#46) for better or worse. FR-14 measures this.
3. Whether stryker-net#3094 affects local mutation runs. FR-9 measures this, and the answer decides whether the documented `MinVerSkip` note is needed.
4. If a current version under FR-2 has moved to a higher major than upstream's, whether a maintainer wants a follow-up issue to adopt it. FR-2 pins to upstream's major in the meantime.
5. The exact replacement for the documented `--filter "FullyQualifiedName~…"` command under MTP v2 (FR-11). It must be a command that works. Its syntax is a design detail.
