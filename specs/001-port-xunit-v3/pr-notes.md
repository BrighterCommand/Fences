# PR notes: 001-port-xunit-v3

## FR-14 Mutation baseline

- Merge base: `c41497ac3b61bb4816d8a5f17e8101538a7aa951`
- Run: https://github.com/BrighterCommand/Fences/actions/runs/36494033503 (`mutation-tests.yml` on `main`, completed, success)
- Retrieved: 2026-10-12

| Job | Cake target | Final mutation score |
| --- | --- | --- |
| mutations-core | MutationTestsCore | 100.00 % |
| mutations-extensions | MutationTestsExtensions | 100.00 % |
| mutations-legacy | MutationTestsLegacy | 100.00 % |
| mutations-ratelimiting | MutationTestsRateLimiting | 100.00 % |
| mutations-testing | MutationTestsTesting | 100.00 % |

Notes:

- The post-port scores come from the PR's CI run, which the human takes (T45).
- The baseline must be taken again if the merge base moves.

## NFR-4 Build duration baseline

- Merge base: `c41497ac3b61bb4816d8a5f17e8101538a7aa951`
- Source: the 3 most recent successful `build.yml` runs on `main` whose head commit is the merge base or an ancestor of it
- Retrieved: 2026-10-12
- Duration is the job's `completedAt` minus `startedAt`.

Runs:

1. https://github.com/BrighterCommand/Fences/actions/runs/36494033524 (head `c41497ac`)
2. https://github.com/BrighterCommand/Fences/actions/runs/36323895840 (head `5b4d2d70`)
3. https://github.com/BrighterCommand/Fences/actions/runs/36314866691 (head `ff509d40`)

| Job | Run 1 | Run 2 | Run 3 | Median | 120% limit |
| --- | --- | --- | --- | --- | --- |
| macos-latest | 6m32s (392 s) | 7m58s (478 s) | 7m12s (432 s) | 7m12s (432 s) | 8m38s (518.4 s) |
| ubuntu-latest | 5m27s (327 s) | 6m13s (373 s) | 4m33s (273 s) | 5m27s (327 s) | 6m32s (392.4 s) |
| windows-latest | 8m37s (517 s) | 8m38s (518 s) | 8m43s (523 s) | 8m38s (518 s) | 10m22s (621.6 s) |

Notes:

- The PR-side comparison, each job's duration in the PR's `build` run against the 120% limit for its OS, is done by the human (T45).
- The baseline must be taken again if the merge base moves.

## FR-2 Dependency versions

- Checked: 2026-10-12, with `dotnet package search <id> --exact-match --format json` (stable versions only).
- Major-version rule: stay within upstream's major (FsCheck 3, xunit.v3.* 4, MTP 2, CodeCoverage 18, Test.Sdk 18, reportgenerator 5, Stryker 5). No package's latest stable version exceeded upstream's major.
- Dependabot "merged" rule: `origin/main` carries none of the added packages, and its `Microsoft.NET.Test.Sdk` (18.9.0) and `dotnet-stryker` (4.16.0) are below the current version, so no version is kept from main.
- Later tasks (T18, T19, T24, T28) take their versions from the "chosen version" column.

| Package or tool | File | Change | Upstream 1a80392b version | Chosen version | Date checked | Source | Superseded Dependabot PRs |
| --- | --- | --- | --- | --- | --- | --- | --- |
| coverlet.msbuild | `Directory.Packages.props` | Remove (was 6.0.4) | n/a | n/a (removed) | 2026-10-12 | n/a | none |
| ReportGenerator (package) | `Directory.Packages.props` | Remove (was 5.5.11) | n/a | n/a (removed) | 2026-10-12 | n/a | none |
| JunitXml.TestLogger | `Directory.Packages.props` | Remove (was 8.0.0) | n/a | n/a (removed) | 2026-10-12 | n/a | none |
| xunit | `Directory.Packages.props` | Remove (was 2.9.3) | n/a | n/a (removed) | 2026-10-12 | n/a | none |
| FsCheck.Xunit.v3 | `Directory.Packages.props` | Replace `FsCheck.Xunit` | 3.4.0 | 3.4.0 | 2026-10-12 | nuget.org | none |
| xunit.v3.mtp-v2 | `Directory.Packages.props` | Add | 4.0.0 | 4.0.2 | 2026-10-12 | nuget.org | none |
| xunit.v3.assert | `Directory.Packages.props` | Add | 4.0.0 | 4.0.2 | 2026-10-12 | nuget.org | none |
| Microsoft.Testing.Platform | `Directory.Packages.props` | Add | 2.4.0 | 2.5.1 | 2026-10-12 | nuget.org | none |
| Microsoft.Testing.Extensions.CodeCoverage | `Directory.Packages.props` | Add | 18.11.2 | 18.12.0 | 2026-10-12 | nuget.org | none |
| Microsoft.NET.Test.Sdk | `Directory.Packages.props` | Bump from 18.9.0 | 18.10.0 | 18.10.1 | 2026-10-12 | nuget.org | #41 |
| dotnet-reportgenerator-globaltool | `.config/dotnet-tools.json` | Add (command `reportgenerator`) | 5.5.11 | 5.5.11 | 2026-10-12 | nuget.org | none |
| dotnet-stryker | `.config/dotnet-tools.json` | Bump from 4.16.0 | 5.0.0 | 5.0.0 | 2026-10-12 | nuget.org | #39 |
| Snippets.csproj | `src/Snippets/Snippets.csproj` | Replace the `xunit` reference with `xunit.v3.assert` | n/a | follows `xunit.v3.assert` (4.0.2) | 2026-10-12 | nuget.org | none |

- `xunit.runner.visualstudio` (4.0.0) and `GitHubActionsTestLogger` (3.0.5) are unchanged.
- Open Dependabot PRs outside FR-2 (#37, #38, #40, #36, #47) are not affected.

## Open items

- none (no package was held back by the major-version rule).

## FR-8 Stryker suppressions and src changes

### Suppressions added (`// stryker disable once all : <reason>`)

| File | Member | Mutation target | Reason (verbatim) |
| --- | --- | --- | --- |
| `src/Paramore.Fences.Core/Retry/RetryHelper.cs` | `MaxTimeSpanTicks` | Core | per-test coverage capture cannot attribute static field initialization to a specific test |
| `src/Paramore.Fences.Extensions/Telemetry/TelemetryUtil.cs` | `True` | Extensions | per-test coverage capture cannot attribute static field initialization to a specific test |
| `src/Paramore.Fences.Extensions/Telemetry/TelemetryUtil.cs` | `False` | Extensions | per-test coverage capture cannot attribute static field initialization to a specific test |

Other `stryker disable` comments under `src/` (in `Paramore.Fences`, `TelemetryListenerImpl.cs`, `TagsList.cs`, `ChaosStrategyHelper.cs`, `RetryResilienceStrategy.cs`, `ExecutionTrackingComponent.cs`, `CircuitStateController.cs` and `RetryHelper.cs` line 30) pre-date this branch and are not listed.

### Source changes

- Polyfill: `src/LegacySupport/ConcurrentDictionaryExtensions.cs` deleted (unused `GetOrAdd` polyfill). Core, Extensions and RateLimiting built with `-warnaserror` on net462, net472, netstandard2.0, net6.0 and net8.0 on 2026-10-12.
- `TaskExecution` refactor: the secondary-action creation moved into a private `TryCreateSecondaryActionAsync` (`src/Paramore.Fences.Core/Hedging/Controller/TaskExecution.cs`); no PublicAPI change.
- `TelemetryOptions` copy constructor now chains to `: this()`; no PublicAPI change.

Mutants removed by suppressions per target: to be filled from the PR's CI Stryker reports (T45)

## AC-13 Threshold check, one TFM short (T25)

- 2026-10-15, macOS. Unedited Core.Tests already gives 6 violations under MS CodeCoverage: line
  99.97 % and branch 99.47 % on net10.0, net9.0 and net8.0. Method is 100 % on all three. T30
  owns that gap. *(Correction, same day: that gap was a Debug-build artefact. In Release, as Cake
  and CI run, Core is 100 % on every metric and TFM; see "Local verification (macOS)".)*
- Because of that, the experiment uses `method`, not `line` (Ian, 2026-10-15). It sets one
  `method` in the net8.0 `Paramore.Fences.Core` package to `line-rate="0"`. The check then
  reports 7 violations. The new one is
  `Paramore.Fences.Core.Tests (net8.0): method coverage of 99.83% for 'Paramore.Fences.Core' is below the required threshold of 100%.`
  The run exits non-zero and the net10.0 and net9.0 figures still print.

## FR-6 Fences-only tests and exclusions

- none. No Fences-only tests or coverage exclusions were added, and no threshold was lowered.
- AC-17: the count of added `ExcludeFromCodeCoverage` lines under `src/` is 0.

## Local verification (macOS)

- Date: 2026-10-15
- HEAD: `fc68d3008fc74db6a2687a44c475ef229c56cf1c`
- Command: `dotnet tool restore && NO_COLOR=1 dotnet cake.cs -- --target=Default --configuration=Release` (exit 0, no threshold violation)
- net481: pending Windows CI

### Threshold figures (15)

```text
Paramore.Fences.Specs (net10.0): line=96.77%, branch=95.16%, method=93.46% for 'Paramore.Fences'.
Paramore.Fences.Specs (net8.0): line=96.77%, branch=95.16%, method=93.46% for 'Paramore.Fences'.
Paramore.Fences.Specs (net9.0): line=96.8%, branch=95.16%, method=93.53% for 'Paramore.Fences'.
Paramore.Fences.Core.Tests (net10.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Core'.
Paramore.Fences.Core.Tests (net8.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Core'.
Paramore.Fences.Core.Tests (net9.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Core'.
Paramore.Fences.Extensions.Tests (net10.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Extensions'.
Paramore.Fences.Extensions.Tests (net8.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Extensions'.
Paramore.Fences.Extensions.Tests (net9.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Extensions'.
Paramore.Fences.RateLimiting.Tests (net10.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.RateLimiting'.
Paramore.Fences.RateLimiting.Tests (net8.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.RateLimiting'.
Paramore.Fences.RateLimiting.Tests (net9.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.RateLimiting'.
Paramore.Fences.Testing.Tests (net10.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Testing'.
Paramore.Fences.Testing.Tests (net8.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Testing'.
Paramore.Fences.Testing.Tests (net9.0): line=100%, branch=100%, method=100% for 'Paramore.Fences.Testing'.
```

### Coverage files (15)

- `artifacts/coverage/Paramore.Fences.Core.Tests/coverage.net10.0.xml`
- `artifacts/coverage/Paramore.Fences.Core.Tests/coverage.net8.0.xml`
- `artifacts/coverage/Paramore.Fences.Core.Tests/coverage.net9.0.xml`
- `artifacts/coverage/Paramore.Fences.Extensions.Tests/coverage.net10.0.xml`
- `artifacts/coverage/Paramore.Fences.Extensions.Tests/coverage.net8.0.xml`
- `artifacts/coverage/Paramore.Fences.Extensions.Tests/coverage.net9.0.xml`
- `artifacts/coverage/Paramore.Fences.RateLimiting.Tests/coverage.net10.0.xml`
- `artifacts/coverage/Paramore.Fences.RateLimiting.Tests/coverage.net8.0.xml`
- `artifacts/coverage/Paramore.Fences.RateLimiting.Tests/coverage.net9.0.xml`
- `artifacts/coverage/Paramore.Fences.Specs/coverage.net10.0.xml`
- `artifacts/coverage/Paramore.Fences.Specs/coverage.net8.0.xml`
- `artifacts/coverage/Paramore.Fences.Specs/coverage.net9.0.xml`
- `artifacts/coverage/Paramore.Fences.Testing.Tests/coverage.net10.0.xml`
- `artifacts/coverage/Paramore.Fences.Testing.Tests/coverage.net8.0.xml`
- `artifacts/coverage/Paramore.Fences.Testing.Tests/coverage.net9.0.xml`

### HTML reports (5)

- `artifacts/coverage-reports/Paramore.Fences.Core.Tests/index.html`
- `artifacts/coverage-reports/Paramore.Fences.Extensions.Tests/index.html`
- `artifacts/coverage-reports/Paramore.Fences.RateLimiting.Tests/index.html`
- `artifacts/coverage-reports/Paramore.Fences.Specs/index.html`
- `artifacts/coverage-reports/Paramore.Fences.Testing.Tests/index.html`

## FR-5 Threshold-check experiments

### AC-10 and AC-12

Date: 2026-10-15 (macOS).

Command (Core.Tests `<Threshold>` temporarily changed from 100 to 101, restored afterwards):

```sh
p=test/Paramore.Fences.Core.Tests/Paramore.Fences.Core.Tests.csproj; cp "$p" "$p.bak" && perl -pi -e 's{<Threshold>100</Threshold>}{<Threshold>101</Threshold>}' "$p" && dotnet tool restore && NO_COLOR=1 dotnet cake.cs -- --target=Default --configuration=Release; mv "$p.bak" "$p"
```

The build exited non-zero (1). Every task up to and including `__GenerateCoverageReports` succeeded (tests ran, reports generated); the failing Cake task was `__VerifyCoverageThresholds`. Exactly 9 unique violations, all for Core.Tests, one per metric per TFM:

```text
Paramore.Fences.Core.Tests (net10.0): branch coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
Paramore.Fences.Core.Tests (net10.0): line coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
Paramore.Fences.Core.Tests (net10.0): method coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
Paramore.Fences.Core.Tests (net8.0): branch coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
Paramore.Fences.Core.Tests (net8.0): line coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
Paramore.Fences.Core.Tests (net8.0): method coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
Paramore.Fences.Core.Tests (net9.0): branch coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
Paramore.Fences.Core.Tests (net9.0): line coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
Paramore.Fences.Core.Tests (net9.0): method coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 101%
```

The HTML reports survived the failing check (AC-10 ordering: reports are generated before the check runs):

- `artifacts/coverage-reports/Paramore.Fences.Core.Tests/index.html`
- `artifacts/coverage-reports/Paramore.Fences.Extensions.Tests/index.html`
- `artifacts/coverage-reports/Paramore.Fences.RateLimiting.Tests/index.html`
- `artifacts/coverage-reports/Paramore.Fences.Specs/index.html`
- `artifacts/coverage-reports/Paramore.Fences.Testing.Tests/index.html`

### AC-13 rounding and AC-15 assembly absent

Date: 2026-10-15 (macOS).

Both experiments ran in one command against the Release coverage files from the full build (isolated `__VerifyCoverageThresholds --exclusive`). Two edits were injected, then both files were restored:

1. AC-13 (rounding): in `artifacts/coverage/Paramore.Fences.Core.Tests/coverage.net8.0.xml`, the `line-rate` of the `Paramore.Fences.Core` package was set to `0.99996` (99.996%). 0.99996 is used rather than 0.99995 because 0.99995 is not exact in binary and can format as `99.99`. The check formats the value as 100% but compares the unrounded value, so it still fails.
2. AC-15 (covered assembly absent): in `artifacts/coverage/Paramore.Fences.Specs/coverage.net8.0.xml`, the `Paramore.Fences` package was renamed to `Renamed.Assembly`.

The check exited non-zero (exit code 1) and printed:

```text
Paramore.Fences.Specs (net8.0): no coverage data was found for assembly 'Paramore.Fences'.
Paramore.Fences.Core.Tests (net8.0): line coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 100%.
```

Both coverage files were restored afterwards (byte-identical; md5 matched before and after, no `.bak` files left).

### AC-15 empty file and no threshold

Date: 2026-10-15 (macOS).

Both experiments ran in one command against the Release coverage files (isolated `__VerifyCoverageThresholds --exclusive`). Each edit was reverted afterwards.

1. Empty file: `artifacts/coverage/Paramore.Fences.RateLimiting.Tests/coverage.net10.0.xml` was truncated to zero bytes. The check failed at the XML load (exit code r1 = 1) and printed:

   ```text
   XmlException: Root element is missing.
   ```

2. No threshold: the `<Threshold>` line was removed from `test/Paramore.Fences.RateLimiting.Tests/Paramore.Fences.RateLimiting.Tests.csproj`. With no threshold declared the project is not checked: the check passed (exit code r2 = 0), printed no `Paramore.Fences.RateLimiting.Tests (` line, and printed figures for the other four projects: `Paramore.Fences.Specs`, `Paramore.Fences.Core.Tests`, `Paramore.Fences.Extensions.Tests` and `Paramore.Fences.Testing.Tests`.

The coverage file and the csproj were restored afterwards (byte-identical; md5 matched before and after, no `.bak` files left).

## FR-9 Local mutation run (stryker-net#3094)

Local runs affected by stryker-net#3094: yes

Issue stryker-mutator/stryker-net#3094 is the open "Add Testing Platform Support" feature request (`gh issue view`, read-only). Its sub-issues track MTP runner gaps (4 of 7 complete).

- Date: 2026-10-15
- HEAD: `f2b97756958d2f47e96a8f5050d57687ee2d3b5e`
- Machine: macOS (Darwin 25.3.0), `uname -m` = arm64

### Run 1: without `MinVerSkip`

Command:

```text
dotnet tool restore && env -u MinVerSkip NO_COLOR=1 dotnet cake.cs -- --target=MutationTestsCore
```

- Duration: 22:31:31 to 22:38:32 (about 7 minutes; cake total 00:06:59)
- Exit code 0, but Stryker produced no score. Every per-test coverage capture failed because the MTP test host died:

  ```text
  WRN MtpRunner-0: Failed to capture per-test coverage for ... after 2 attempts
  System.InvalidOperationException: The JSON-RPC connection with the remote party was lost before the request could complete.
  Disconnected reason: RemotePartyTerminated
  ERR It looks like the test coverage capture failed. Disable coverage based optimisation.
  ...
  INF Stryker was unable to calculate a mutation score
  ```

- There was no `The final mutation score is` line. The log does not name MinVer as the cause. The test host's reason for exiting is not logged.

### Run 2: with `MinVerSkip=true` (AC-23 control)

Command:

```text
MinVerSkip=true NO_COLOR=1 dotnet cake.cs -- --target=MutationTestsCore
```

- Duration: 22:39:11 to 22:45:05 (about 6 minutes; Stryker time elapsed 00:05:46)
- Exit code 0. Per-test coverage capture complete: 702 tests captured.
- `The final mutation score is 100.00 %`

### Conclusion

The only difference between the two runs is `MinVerSkip`. Without it the MTP test host terminates during Stryker's coverage capture and no score is produced, though the exit code is still 0. With it the run completes at 100.00 %. Local runs are therefore affected, and ADR 0004 divergence 3 (`MinVerSkip`) applies (T36). Failure mode: the run exits 0 with "unable to calculate a mutation score", so a check on the exit code alone would miss it.

Local score is FR-9 evidence only; FR-14 compares CI with CI.

## FR-15 Coverage reproducibility

- HEAD: `114f9a9e0340197bb29eccb70b9205e420a0a7e4`
- Date: 2026-10-15
- OS: macOS (Darwin 25.3.0), arm64
- Command (each run): `NO_COLOR=1 dotnet cake.cs -- --target=Default --configuration=Release`, after `dotnet tool restore`. All three runs exited 0.
- Durations (Cake `Total:`): run 1 00:02:11, run 2 00:02:20, run 3 00:01:44 (wall clock 23:10:57 to 23:13:09, 23:13:09 to 23:15:29, 23:15:29 to 23:17:15 BST)
- net481: pending Windows CI
- Test counts: MTP prints only one total per project, summed over its three TFMs. The per-TFM count is unavailable, so each project's total is repeated in every TFM row. Totals are identical across the 3 runs (0 failed, 0 skipped).
- Cells are line/branch/method (%).

| Project | TFM | Run 1 l/b/m | Run 1 tests (project total) | Run 2 l/b/m | Run 2 tests (project total) | Run 3 l/b/m | Run 3 tests (project total) | Identical? |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Paramore.Fences.Specs | net10.0 | 96.82/95.16/93.6 | 6393 | 96.84/95.16/93.66 | 6393 | 96.82/95.16/93.6 | 6393 | no |
| Paramore.Fences.Specs | net8.0 | 96.84/95.16/93.66 | 6393 | 96.84/95.16/93.66 | 6393 | 96.84/95.16/93.66 | 6393 | yes |
| Paramore.Fences.Specs | net9.0 | 96.87/95.16/93.73 | 6393 | 96.87/95.16/93.73 | 6393 | 96.87/95.16/93.73 | 6393 | yes |
| Paramore.Fences.Core.Tests | net10.0 | 100/100/100 | 2343 | 100/100/100 | 2343 | 100/100/100 | 2343 | yes |
| Paramore.Fences.Core.Tests | net8.0 | 100/100/100 | 2343 | 100/100/100 | 2343 | 100/100/100 | 2343 | yes |
| Paramore.Fences.Core.Tests | net9.0 | 100/100/100 | 2343 | 100/100/100 | 2343 | 100/100/100 | 2343 | yes |
| Paramore.Fences.Extensions.Tests | net10.0 | 100/100/100 | 333 | 100/100/100 | 333 | 100/100/100 | 333 | yes |
| Paramore.Fences.Extensions.Tests | net8.0 | 100/100/100 | 333 | 100/100/100 | 333 | 100/100/100 | 333 | yes |
| Paramore.Fences.Extensions.Tests | net9.0 | 100/100/100 | 333 | 100/100/100 | 333 | 100/100/100 | 333 | yes |
| Paramore.Fences.RateLimiting.Tests | net10.0 | 100/100/100 | 105 | 100/100/100 | 105 | 100/100/100 | 105 | yes |
| Paramore.Fences.RateLimiting.Tests | net8.0 | 100/100/100 | 105 | 100/100/100 | 105 | 100/100/100 | 105 | yes |
| Paramore.Fences.RateLimiting.Tests | net9.0 | 100/100/100 | 105 | 100/100/100 | 105 | 100/100/100 | 105 | yes |
| Paramore.Fences.Testing.Tests | net10.0 | 100/100/100 | 21 | 100/100/100 | 21 | 100/100/100 | 21 | yes |
| Paramore.Fences.Testing.Tests | net8.0 | 100/100/100 | 21 | 100/100/100 | 21 | 100/100/100 | 21 | yes |
| Paramore.Fences.Testing.Tests | net9.0 | 100/100/100 | 21 | 100/100/100 | 21 | 100/100/100 | 21 | yes |

Identical across all 3 runs: no

Differences (for the #14 update):

- `Paramore.Fences.Specs` (net10.0): run1 96.82/95.16/93.6, run2 96.84/95.16/93.66, run3 96.82/95.16/93.6 (line/branch/method %). Run 2 differs from runs 1 and 3 in line (96.84 vs 96.82) and method (93.66 vs 93.6); branch is 95.16 in all three.

Only this one project and TFM differs; run 1 and run 3 agree. The other 14 rows are identical, and test counts are identical.

## Static checks: configuration

- Date: 2026-10-15
- Merge-base: c41497ac3b61bb4816d8a5f17e8101538a7aa951 (`git merge-base HEAD origin/main`, no fetch)
- HEAD: 6f1ee4a6269cca7f76fa587d76122678867b0d2f

- AC-7: PASS. `eng/Test.targets` contains no `coverlet`, `CollectCoverage`, `CoverletOutputFormat` or `ReportGenerator`.
- AC-4: PASS. Every changed line in the `Directory.Packages.props` diff is an FR-2 package. Packages appearing in the diff: `coverlet.msbuild`, `ReportGenerator`, `JunitXml.TestLogger`, `xunit`, `FsCheck.Xunit`, `FsCheck.Xunit.v3`, `xunit.v3.mtp-v2`, `xunit.v3.assert`, `Microsoft.Testing.Platform`, `Microsoft.Testing.Extensions.CodeCoverage`, `Microsoft.NET.Test.Sdk`. The `global.json` diff is only the `test` / `runner: Microsoft.Testing.Platform` block. The `.config/dotnet-tools.json` part of AC-4 is not scripted; it is reviewed by hand in T45.
- AC-22: PASS. The `eng/stryker-config.json` diff is only `"test-runner": "mtp",`. `.github/workflows/mutation-tests.yml` carries `MinVerSkip: true` and the `stryker-net/issues/3094` link. `cake.cs` has no `MinVerSkip`.
- AC-25: PASS. Every coverage path in `.github/workflows/` (`coverage-reports`, `coverage/`, `codecov`) points at `artifacts/coverage-reports`.
- AC-29: PASS. No `CHANGELOG.md` change against the merge-base.
- Unchanged files: PASS. `.github/workflows/build.yml`, `build.ps1`, `.github/dependabot.yml` and `.github/wordlist.txt` have no diff against the merge-base.

## Static checks: source and docs

- Date: 2026-10-15
- Merge-base: c41497ac3b61bb4816d8a5f17e8101538a7aa951 (`git merge-base HEAD origin/main`, no fetch)
- HEAD: 5957127e558c4543bbaf06f560780bea6438d9a8

- AC-2: PASS. No `Xunit.Abstractions`, no `Include="xunit"` or `Include="FsCheck.Xunit"`, and no coverlet `<Include>` in test projects.
- AC-16: PASS. Specs threshold is `96,95,93` and four projects carry `<Threshold>100</Threshold>`.
- AC-17: PASS. 0 added `ExcludeFromCodeCoverage` lines under `src` and `test`.
- AC-35: PASS. No diff in `src/*/.PublicAPI/*` against the merge-base.
- AC-36: PASS. No added `Skip = `, `"Quarantine"` or `Explicit = true` in `test/`.
- AC-20: PASS. No FakeItEasy, Moq or FluentAssertions usings added; the added `[Fact]`/`[Theory]` method names equal the set 1a80392b adds (empty `diff`).
- AC-26: PASS. No positional `dotnet test ./`, `FullyQualifiedName~` or coverlet in the FR-11 documents.
- AC-27: PASS. `dotnet mdsnippets` produced no Markdown diff outside `specs`.

## Post-merge checklist (for the human)

T45 confirmed that this file has the FR-2, FR-6, FR-8, FR-9, FR-14, FR-15 and NFR-4 sections and
"Open items". The checklist is in the order the human works through it: before the PR, on the PR,
then after the merge.

### Before the PR

- [ ] **Loop leftovers.** Stop the ralph loop. Delete `RALPH_STOP` if it exists. Review
  `coverage-gap-report.md` if it exists.
  - Neither file exists now. A first T30 attempt wrote both after measuring Core in Debug, and
    commit `fc68d300` withdrew them. Coverage has to be judged in Release, as Cake and CI do.
  - The "AC-13 Threshold check, one TFM short (T25)" section above still says Core.Tests has 6
    violations that T30 owns. That was the Debug-build artefact. The Release build in "Local
    verification (macOS)" has none. The section stays as history.
- [ ] **Squash per FR-13.** Each commit message ends with `Ralph task: N/45`, and from T19 on also
  `(squash group X)`. The commits for T01 to T18 carry no group tag in the message. Use the
  `Squash group` field of the task in `ralph-tasks.md` (all of T01 to T18 are group A). Order the
  result A, B, C, E. Group D exists only if one was ever approved, and none was.
  - **A** (T01 to T30, plus the `src/` and `test/` commits): one commit,
    `Port App-vNext/Polly@1a80392b: Update to xunit v3`.
  - **B** (T31 to T34): one commit, the missing-coverage-file divergence (`cd6ba2a7`, the Cake
    check that fails when a built TFM has no coverage file).
  - **C** (T35 to T40): one commit, the FR-11 docs (`3bd781bc`, `67cd5400`, `fb1700d8`,
    `5d5e2429`, `f2b80687`).
  - **E** (T41): one commit, `Port App-vNext/Polly@f35bc00d: .NET 11 preparation (Specs)`
    (`114f9a9e`). It must touch exactly 3 files (AC-28). Check with
    `git show --stat --format= <sha>`.
  - **No group, and out of A to E.** Everything that touches only `specs/**` is bookkeeping:
    the pr-notes and task-marker commits and the `chore:` and `docs:` commits for T19, T21, T25
    and T30 (`37e7c997`, `8c2c0892`, `d9fc5cda`, `a5869fb6`, `e0cc2493`, `fc68d300`) and
    `34135346` (the T30 record).
  - **Decide:** `0ff548b7` (spec and ADR) is not `specs/**`-only. It also adds
    `docs/adr/0004-port-xunit-v3.md` and the `docs/adr/index.md` entry. Either fold those two
    files into A or keep them as a separate docs commit. Some of these carry a group tag in the message anyway, and so do the
    `feat: record ...` commits for T42 to T44 (tagged E) and T32 to T34 (tagged B). Tags do not
    override this rule. Find them with
    `git log --oneline --name-only <merge-base>..HEAD` and drop the ones whose files are all under
    `specs/**`, or keep them as a separate bookkeeping commit. After squashing, A to E together must
    show no `specs/**` file.
  - A commit that mixes `specs/**` and other files goes in its task's group, without its
    `specs/**` part. For example, `35dc17c5` (T25's `__VerifyCoverageThresholds`, `cake.cs`) goes in A.
- [ ] **Reviews.** Review A, B and E against upstream (AC-39): A and E against
  `App-vNext/Polly@1a80392b` and `@f35bc00d`, B against the intended Fences divergence.
- [ ] **Manual AC-4 review of `.config/dotnet-tools.json`.** Only `dotnet-reportgenerator-globaltool`
  5.5.11 (added) and `dotnet-stryker` 5.0.0 (bumped from 4.16.0) may change. The script checks
  `Directory.Packages.props` and `global.json`, not this file.
- [ ] **Open the PR.** Push a non-main branch (not `main`; Fences keeps migration work off it). Use a
  title in Fences' voice. ADR suggestion: "Move tests to xunit v3 and Microsoft Testing Platform v2,
  with per-framework coverage thresholds". Build the body from this file. Link #28 and #34 and name
  both SHAs, `1a80392b` and `f35bc00d`.

### CI checks on the PR

- [ ] **`build` is green** on macOS, Linux and Windows with zero warnings (AC-34).
- [ ] **net481 on Windows.** net481 tests pass and net481 coverage meets the thresholds (AC-1,
  AC-11). Every net481 figure in this file reads "pending Windows CI". A net481 gap follows the
  decision-3 process by hand.
- [ ] **Artefacts and summary.**
  - The `coverage-<os>` artefacts each have HTML for 5 projects (AC-24).
  - The job summary has one section per project per TFM (AC-8) and lists no passed tests (AC-24).
- [ ] **FR-14 mutation scores.** All five CI scores (`mutations-core`, `mutations-extensions`,
  `mutations-legacy`, `mutations-ratelimiting`, `mutations-testing`) must be at or above the T01
  baseline, which is 100.00 % each. Add the scores to the PR body. Also add the mutants removed by
  the suppressions, as a count per target and the file (see the FR-8 section: three suppressions,
  Core `RetryHelper.cs` and Extensions `TelemetryUtil.cs` twice). That line in FR-8 still says "to
  be filled".
  - If `mutations-core` hangs (#46), rerun it. After two hung reruns, use the like-for-like local
    fallback (run both SHAs, with `MinVerSkip=true`, per the FR-9 section) and comment on #46
    (AC-32).
- [ ] **NFR-4 build time.** Compare the PR's `build` durations with the T02 medians (macOS 7m12s,
  Ubuntu 5m27s, Windows 8m38s). Record the times and the cause if any job exceeds 120% (macOS
  8m38s, Ubuntu 6m32s, Windows 10m22s) (AC-37).
- [ ] **Moving merge base.** If the merge base moves from `c41497ac`, take the FR-14 and NFR-4
  baselines again.
- [ ] **FR-2 Dependabot.** Re-check which Dependabot PRs for FR-2 rows are open at merge time. The
  FR-2 table names #41 (Microsoft.NET.Test.Sdk) and #39 (dotnet-stryker). #37, #38, #40, #36 and #47
  are outside FR-2.
- [ ] **FR-9.** Local mutation runs need `MinVerSkip=true` (FR-9 answer: yes, divergence 3). It is
  documented in `CONTRIBUTING.md` and `.agent_instructions/testing.md`. Check that the PR body
  mentions it.
- [ ] **Approval.** Get Ian's approval before the merge (C-5).

### After the merge

- [ ] **#28:** make the `1a80392b` entry and the `f35bc00d` Specs entry link the merged PR.
- [ ] **#6:** comment that coverlet was removed. It stays closed.
- [ ] **PR #19:** close it unmerged. The comment says the failing-gate fix is carried and the
  per-TFM HTML fix deliberately is not.
- [ ] **Dependabot:** close each superseded Dependabot PR (for example #39 and #41) with a link to
  the merged PR.
- [ ] **#14 (FR-15 verdict: no, so update it, do not close it).** Three full macOS Release builds
  were not identical. Only `Paramore.Fences.Specs` on net10.0 varied. Line was 96.82 / 96.84 /
  96.82 and method was 93.6 / 93.66 / 93.6 across runs 1 to 3. Branch was 95.16 in every run, and
  the other 14 project and TFM rows and every test count were identical. Post that, with the FR-15
  table. net481 is pending Windows CI. If the CI runs turn out identical, close #14 with the table.
- [ ] **Optional:** advance `polly-upstream` per `.agent_instructions/upstream_sync.md` Step 5.

### Open items, still accurate?

- The "Open items" section above is the FR-2 one ("none (no package was held back by the
  major-version rule)"). It is still accurate. Nothing later changed a version.
- Two lines elsewhere were open when written and are resolved or still pending: the AC-13 "T30 owns
  that gap" line is resolved (Debug artefact, see Loop leftovers), and "Mutants removed by
  suppressions per target" plus all net481 figures are still pending CI.
