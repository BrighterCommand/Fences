# Ralph Tasks: 001-port-xunit-v3

> Auto-generated from the approved design for unattended TDD execution.
> Each task is self-contained with all context a fresh Claude session needs.

## Spec Context

- **Spec**: 001-port-xunit-v3
- **Requirements**: specs/001-port-xunit-v3/requirements.md
- **ADRs**: docs/adr/0004-port-xunit-v3.md
- **Squash groups** (applied by a human before the PR, per FR-13): A = `Port App-vNext/Polly@1a80392b: …`, B = missing-coverage-file divergence, C = FR-11 docs, E = `Port App-vNext/Polly@f35bc00d: …`
- **Loop rules for this spec**: no push/PR/issue actions; stop-and-report on a coverage gap (RALPH_STOP); PR evidence goes to specs/001-port-xunit-v3/pr-notes.md

### Conventions every task relies on (sub-agents: read this section; never edit this file)

1. **Merge base and upstream.** The merge base is `$(git merge-base HEAD origin/main)` (c41497ac when drafted). Both upstream commits are local objects: use `git show 1a80392b -- <path>` for a diff and `git show 1a80392b:<path>` for a whole file. f35bc00d is 1a80392b's direct child.
2. **Rebranding** (ADR 0002 D1; requirements glossary "Port"). Paths map `src/Polly.X` → `src/Paramore.Fences.X` and `test/Polly.X` → `test/Paramore.Fences.X`. Namespaces map `Polly.*` → `Paramore.Fences.*`. URLs are never rewritten: `https://github.com/App-vNext/Polly/issues/…`, `https://www.pollydocs.org/…` and `Polly.Contrib.WaitAndRetry` stay verbatim (`eng/rename-to-fences.py`, "URLS ARE PROTECTED"). No licence headers.
   - **Rebrand filter.** Most verifies compare a Fences file with upstream's file passed through `sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g'`. At the merge base, this filter applied to the 1a80392b parent reproduces Fences' `cake.cs`, `eng/Test.targets` and every FR-7/FR-8 file byte for byte, except two files: `RetryHelper.cs` (prose plus Polly-Contrib URLs) and `RetryHelperTests.cs` (using order).
   - **New upstream files** are best created as `git show 1a80392b:<upstream path> | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g' > <fences path>`.
3. **Shell and build.** The shell is zsh on macOS, and every command is written to work in both zsh and bash. PowerShell is not installed, so `./build.ps1 [-Target X]` is run as `dotnet tool restore && dotnet cake.cs -- --target=<X or Default> --configuration=Release`. This is the fallback CONTRIBUTING.md documents.
4. **Long commands.**
   - A full Cake `Default` run takes about 15–25 minutes. Stryker `MutationTestsCore` takes 30–60+ minutes. A bare `dotnet test` at the root takes several minutes.
   - The Bash tool's foreground limit is 10 minutes. Run these with `run_in_background: true` and poll until they finish. This includes running a RALPH-VERIFY that contains one of them.
5. **Test command syntax depends on the phase.**
   - **Before T19 (the switch), use VSTest:** `dotnet test test/<Project> --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~<Namespace.Class>"`.
     - `-p:CollectCoverage=false` is required. Coverlet enforces `<Threshold>` on every run, so a filtered run would fail on coverage.
     - VSTest exits 0 when a filter matches nothing. Verifies therefore pipe the output to `grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`.
   - **From T19 on, use MTP:** `dotnet test --project test/<Project> --framework net10.0 [--filter-class "*Name*" | --filter-method "*.Method"]` (ADR, Technology Choices, "Why `--filter-class`…").
     - MTP fails a run that executes zero tests.
     - Positional project paths and `--filter "FullyQualifiedName~…"` stop working.
   - **From T22 on,** every MTP run writes `artifacts/coverage/<Project>/coverage.<tfm>.xml`.
     - A filtered or single-TFM run overwrites that file with partial figures.
     - Tasks that need complete files say how to regenerate them.
6. **Analyser-clean.** CI builds with `TreatAllWarningsAs=Error` (`cake.cs`, `__BuildSolutions`). Locally, use `dotnet build <project or Fences.slnx> -warnaserror`. Never suppress a rule to get a build through, and never set `SKIP_FENCES_ANALYZERS`.
7. **Ported tests (FR-7, AC-20).**
   - Port upstream's test logic and method names verbatim, rebranded. Upstream's Specs methods are snake_case, which is correct here. Never rename a ported test to Member_Scenario_Outcome.
   - Use NSubstitute only.
   - RED for a ported test is "the test does not exist". Adding the test is the change, so the expected status is **GREEN**, even though `src/` is unchanged. Do not return ALREADY_COMPLETE for these tasks.
8. **Coverage rules** (decision 3, C-4, FR-6). Never lower a threshold. Never add a Fences-only test. Never add `[ExcludeFromCodeCoverage]` or a coverage-settings exclusion.
9. **Stop protocol.** When a task says **stop**:
   1. Restore any temporary experiment edits.
   2. Write the report the task names, if any.
   3. Run `touch RALPH_STOP` at the repository root.
   4. Return `STATUS: FAILED` with a FAILURE_REASON, and list **no** paths under TEST_FILES/IMPL_FILES.

   The report and `RALPH_STOP` stay untracked for the human. The main agent then commits only the failure marker.
10. **Experiments.** Make and restore every temporary edit (csproj thresholds, coverage XML) inside the same command, using `cp X X.bak` … `mv X.bak X`. Afterwards, the working tree holds only the task's own files.
11. **pr-notes.md.** `specs/001-port-xunit-v3/pr-notes.md` collects the PR-body evidence.
    - If the file is absent, create it with the title `# PR notes: 001-port-xunit-v3`.
    - Append the section the task names, using its exact `## …` heading. Use `###` for subsections, because verifies extract sections with `awk '/^## <heading> /…'`.
    - Never edit another task's section.
    - List the file under IMPL_FILES.
12. **No outward-facing actions.** No push, PR, rerun, workflow dispatch, or comment on, close or edit of any issue or PR. Read-only `gh` is fine: `gh run list`, `gh run view`, `gh pr list`, `gh pr view`, `gh issue view`.
13. **net481 runs only on Windows.** macOS verifies net10.0, net9.0 and net8.0. CI checks net481 tests and coverage on Windows after the human pushes (T45 checklist).
14. **Cake output.** Prefix any Cake run whose output is captured with `NO_COLOR=1`, so greps see plain text.
15. **Squash bookkeeping.** Every loop commit also stages ralph-tasks.md. Changes under `specs/001-port-xunit-v3/` (ralph-tasks.md, pr-notes.md) are spec bookkeeping and are not part of groups A–E. The human keeps them out of the squashed A–E commits. A task whose only file is under `specs/` carries the group letter of its position, but contributes nothing to that commit.

## Tasks

- [x] **T01: Record the FR-14 mutation baseline from main's `mutation-tests` run at the merge base**
  - **Behavior**: When this runs, only read-only `gh` is used:
    1. Get `mb=$(git merge-base HEAD origin/main)`, then run `gh run list --workflow mutation-tests.yml --branch main --commit "$mb" --json databaseId,conclusion,status,url`.
    2. Take the completed run at that SHA. From `gh run view <id> --log`, take each job's `The final mutation score is X %` line.
    3. Create pr-notes.md if absent, then append `## FR-14 Mutation baseline`. It holds the full merge-base SHA, the run URL, the retrieval date, and one table row per job in the form `| mutations-core | MutationTestsCore | 100.00 % |`.
    4. Add two notes. The post-port scores come from the PR's CI run, which the human does (T45). The baseline must be taken again if the merge base moves.
    5. If no completed run exists at the merge base, or a job has no score: **stop**. Dispatching a workflow is outward-facing.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The section exists and contains the full merge-base SHA.
    - It links an `actions/runs/` URL.
    - It has five job rows, each with a percentage score.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - create; append the FR-14 section
  - **RALPH-VERIFY**: `s=$(awk '/^## FR-14 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md 2>/dev/null); echo "$s" | grep -q "$(git merge-base HEAD origin/main)" && echo "$s" | grep -q 'github.com/BrighterCommand/Fences/actions/runs/' && [ "$(echo "$s" | grep -cE 'mutations-(core|extensions|legacy|ratelimiting|testing).*[0-9]+\.[0-9]+ ?%')" -ge 5 ]`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-14, AC-31; ADR "Implementation Approach" step 0; `.github/workflows/mutation-tests.yml` (job names are `mutations-<name>`)

- [x] **T02: Record the NFR-4 build-duration baseline (median of the last 3 successful `build` runs on main)**
  - **Behavior**: When this runs, only read-only `gh` is used:
    1. Run `gh run list --workflow build.yml --branch main --status success --limit 30 --json databaseId,headSha,url,createdAt`.
    2. Keep runs whose `headSha` is the merge base or an ancestor of it (`git merge-base --is-ancestor <sha> "$(git merge-base HEAD origin/main)"`), and take the 3 most recent.
    3. For each run, use `gh run view <id> --json jobs` to get the wall-clock time of the `macos-latest`, `ubuntu-latest` and `windows-latest` jobs (completedAt − startedAt).
    4. Append `## NFR-4 Build duration baseline`. It holds the three run URLs, the per-OS durations, the per-OS median, the 120% limit per OS, and a note that the PR-side comparison is done by the human (T45).
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - All three OS job names appear.
    - A median is stated.
    - At least three run URLs appear.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the NFR-4 section
  - **RALPH-VERIFY**: `s=$(awk '/^## NFR-4 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md 2>/dev/null); echo "$s" | grep -q 'macos-latest' && echo "$s" | grep -q 'ubuntu-latest' && echo "$s" | grep -q 'windows-latest' && echo "$s" | grep -qi 'median' && [ "$(echo "$s" | grep -c 'actions/runs/')" -ge 3 ]`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements NFR-4, AC-37; ADR "Implementation Approach" step 0; `.github/workflows/build.yml` (matrix `job-name` values)

- [x] **T03: Choose and record the FR-2 version for every authorised package and tool change**
  - **Behavior**: When this runs:
    1. Look up the current version of each FR-2 row: FsCheck.Xunit.v3, xunit.v3.mtp-v2, xunit.v3.assert, Microsoft.Testing.Platform, Microsoft.Testing.Extensions.CodeCoverage, Microsoft.NET.Test.Sdk, dotnet-reportgenerator-globaltool and dotnet-stryker. Use `dotnet package search <id> --exact-match --format json` (stable versions only, listed in ascending order).
    2. Apply the major-version rule. Stay within upstream's major: FsCheck 3, xunit.v3.* 4, MTP 2, CodeCoverage 18, Test.Sdk 18, reportgenerator 5, Stryker 5.
    3. Apply the Dependabot "merged" rule: keep main's version if it is already higher (`git show origin/main:Directory.Packages.props` and `git show origin/main:.config/dotnet-tools.json`).
    4. Find the open Dependabot PRs that touch an FR-2 row: `gh pr list --state open --author app/dependabot --json number,title`.
    5. Append `## FR-2 Dependency versions`. It is a table with one row per FR-2 row, 13 rows in all, including the four removals and the `src/Snippets/Snippets.csproj` swap. Columns: package or tool | file | change | upstream 1a80392b version | chosen version | date checked (YYYY-MM-DD) | source (nuget.org, or "already on main") | superseded Dependabot PRs.
    6. State that `xunit.runner.visualstudio` and `GitHubActionsTestLogger` are unchanged.
    7. Append `## Open items`. It names any package held back by the major-version rule, or says "none".
    8. Later tasks (T18, T19, T24, T28) take their versions from the "chosen version" column.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - All 13 rows are present, each with a date.
    - An Open items section exists.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the FR-2 section and the Open items section
  - **RALPH-VERIFY**: `s=$(awk '/^## FR-2 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md 2>/dev/null); [ -n "$s" ] && ( for p in coverlet.msbuild ReportGenerator JunitXml.TestLogger FsCheck.Xunit.v3 xunit.v3.mtp-v2 xunit.v3.assert Microsoft.Testing.Platform Microsoft.Testing.Extensions.CodeCoverage Microsoft.NET.Test.Sdk dotnet-reportgenerator-globaltool dotnet-stryker Snippets.csproj; do echo "$s" | grep -qF -- "$p" || exit 1; done ) && echo "$s" | grep -qE '20[0-9]{2}-[0-9]{2}-[0-9]{2}' && grep -q '^## Open items' specs/001-port-xunit-v3/pr-notes.md`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-2 (table and rules), AC-4, AC-5; ADR "Implementation Approach" step 0; `git show 1a80392b -- Directory.Packages.props .config/dotnet-tools.json`; `Directory.Packages.props`; `.config/dotnet-tools.json`

- [x] **T04: Port upstream's `AddHedging_ActionGeneratorUsesCallback_InvokesPrimaryCallback` test**
  - **Behavior**: Add the `[Fact]` from upstream to `HedgingResiliencePipelineBuilderExtensionsTests`, rebranded and verbatim.
    - Keep the `pollydocs.org` URL in its comment unchanged.
    - The test passes against today's `src/`, because it covers the action-generator path that T05 refactors. RED is "the test is absent". Expected status: GREEN.
  - **Test file**: `test/Paramore.Fences.Core.Tests/Hedging/HedgingResiliencePipelineBuilderExtensionsTests.cs`
  - **Test should verify**:
    - With `MaxHedgedAttempts = 2` and an `ActionGenerator` that reuses `args.Callback(args.ActionContext)`, the result is `"success"`.
    - The primary callback is invoked twice.
    - The whole file equals the rebranded upstream file.
  - **Implementation files**:
    - none (test-only port)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Core.Tests/Hedging/HedgingResiliencePipelineBuilderExtensionsTests.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Core.Tests/Hedging/HedgingResiliencePipelineBuilderExtensionsTests.cs && dotnet build test/Paramore.Fences.Core.Tests -warnaserror && dotnet test test/Paramore.Fences.Core.Tests --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Core.Tests.Hedging.HedgingResiliencePipelineBuilderExtensionsTests" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7 (table row "Hedging/…BuilderExtensionsTests"), AC-20; ADR "Implementation Approach" step 1.6; `git show 1a80392b -- test/Polly.Core.Tests/Hedging/HedgingResiliencePipelineBuilderExtensionsTests.cs`

- [x] **T05: Extract secondary-action creation in `TaskExecution` into `TryCreateSecondaryActionAsync` (no behaviour change)**
  - **Behavior**: Apply upstream's internal refactor exactly.
    - The `if (type == HedgedTaskType.Secondary)` block now calls the private `TryCreateSecondaryActionAsync<TState>(primaryCallback, primaryContext, state, attemptNumber)`.
    - That method returns `(bool? EarlyReturn, Func<ValueTask<Outcome<T>>>? Action)` and keeps today's reset, null and exception semantics.
    - The public API does not change.
    - This is a refactor, so RED is structural: the file does not yet equal upstream's. The existing hedging tests, including T04's, are the safety net.
  - **Test file**: none — verified by RALPH-VERIFY (existing tests in `test/Paramore.Fences.Core.Tests/Hedging/`)
  - **Test should verify**:
    - The file equals the rebranded upstream 1a80392b file.
    - Core builds analyser-clean on every TFM.
    - Every hedging test passes.
  - **Implementation files**:
    - `src/Paramore.Fences.Core/Hedging/Controller/TaskExecution.cs` - add the extracted private method; replace the inline block (about lines 108–128)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:src/Polly.Core/Hedging/Controller/TaskExecution.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') src/Paramore.Fences.Core/Hedging/Controller/TaskExecution.cs && dotnet build src/Paramore.Fences.Core -c Release -warnaserror && dotnet test test/Paramore.Fences.Core.Tests --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Core.Tests.Hedging" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-8 (TaskExecution row), NFR-2, NFR-3, AC-21; ADR "Implementation Approach" step 1.5; `git show 1a80392b -- src/Polly.Core/Hedging/Controller/TaskExecution.cs`

- [x] **T06: Port upstream's `RetryHelperTests` change (decorrelated-jitter overflow test plus `#if !NETFRAMEWORK` guards)**
  - **Behavior**: Apply upstream's diff.
    - Add `DecorrelatedJitterBackoffV2_AttemptCausesInfinity_ReturnsMaxTimeSpanTicksMinusOneThousand`. Keep the `App-vNext/Polly/issues/2163` URL verbatim.
    - Wrap `using FsCheck;` and `using FsCheck.Fluent;` (lines 1–2) in `#if !NETFRAMEWORK` … `#endif`.
    - Wrap the `Arbitraries` class (about line 316) in `#if !NETFRAMEWORK` as well. On Windows, net481 needs these guards with FsCheck.Xunit.v3; on macOS they change nothing.
    - Keep Fences' using order, where `Polly.Contrib.WaitAndRetry` comes after the `Paramore.Fences.*` usings.
    - Expected status: GREEN, because the test is new.
  - **Test file**: `test/Paramore.Fences.Core.Tests/Retry/RetryHelperTests.cs`
  - **Test should verify**:
    - `RetryHelper.DecorrelatedJitterBackoffV2(1024, 1s, ref state, () => 0.0)` returns `TimeSpan.FromTicks((long)((double)TimeSpan.MaxValue.Ticks - 1_000))`.
    - Line 1 is the `#if !NETFRAMEWORK` guard.
    - The file matches upstream's, ignoring the `Polly.Contrib.WaitAndRetry` using line.
  - **Implementation files**:
    - none (test-only port)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Core.Tests/Retry/RetryHelperTests.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g' | grep -v 'Contrib.WaitAndRetry;') <(grep -v 'Contrib.WaitAndRetry;' test/Paramore.Fences.Core.Tests/Retry/RetryHelperTests.cs) && dotnet build test/Paramore.Fences.Core.Tests -warnaserror && dotnet test test/Paramore.Fences.Core.Tests --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Core.Tests.Retry.RetryHelperTests" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7 (RetryHelperTests row), AC-18, AC-20; ADR "Implementation Approach" step 1.6; `git show 1a80392b -- test/Polly.Core.Tests/Retry/RetryHelperTests.cs`. net481 is verified by Windows CI (convention 13).

- [x] **T07: Remove the `en-us` slug and add upstream's Stryker suppression on `RetryHelper.MaxTimeSpanTicks`**
  - **Behavior**: In the comment above `MaxTimeSpanTicks`:
    - The docs URL becomes `https://docs.microsoft.com/dotnet/csharp/language-reference/builtin-types/numeric-conversions`.
    - Directly above the field, add `// stryker disable once all : per-test coverage capture cannot attribute static field initialization to a specific test`.
    - Change nothing else.
  - **Test file**: none — verified by RALPH-VERIFY (T06's test covers the field)
  - **Test should verify**:
    - The slug-free URL is present and `en-us` is gone.
    - The suppression comment is the line immediately before the field.
    - Core builds analyser-clean.
  - **Implementation files**:
    - `src/Paramore.Fences.Core/Retry/RetryHelper.cs` - lines 10–12
  - **RALPH-VERIFY**: `grep -qF 'https://docs.microsoft.com/dotnet/csharp/language-reference/builtin-types/numeric-conversions' src/Paramore.Fences.Core/Retry/RetryHelper.cs && ! grep -q 'en-us' src/Paramore.Fences.Core/Retry/RetryHelper.cs && grep -B1 'private static readonly double MaxTimeSpanTicks' src/Paramore.Fences.Core/Retry/RetryHelper.cs | grep -qF '// stryker disable once all : per-test coverage capture cannot attribute static field initialization to a specific test' && dotnet build src/Paramore.Fences.Core -c Release -warnaserror`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-8 (RetryHelper row), AC-21; ADR "Implementation Approach" step 1.5; `git show 1a80392b -- src/Polly.Core/Retry/RetryHelper.cs`

- [x] **T08: Add upstream's Stryker suppressions on `TelemetryUtil.True` and `TelemetryUtil.False`**
  - **Behavior**: Add `// stryker disable once all : per-test coverage capture cannot attribute static field initialization to a specific test` directly above `private static readonly object True = true;` and directly above `private static readonly object False = false;`.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The file equals the rebranded upstream file.
    - Extensions builds analyser-clean.
  - **Implementation files**:
    - `src/Paramore.Fences.Extensions/Telemetry/TelemetryUtil.cs` - above lines 11 and 13
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:src/Polly.Extensions/Telemetry/TelemetryUtil.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') src/Paramore.Fences.Extensions/Telemetry/TelemetryUtil.cs && dotnet build src/Paramore.Fences.Extensions -c Release -warnaserror`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-8 (TelemetryUtil row), AC-21; ADR "Implementation Approach" step 1.5; `git show 1a80392b -- src/Polly.Extensions/Telemetry/TelemetryUtil.cs`

- [x] **T09: Chain the `TelemetryOptions` copy constructor to `: this()`**
  - **Behavior**: `public TelemetryOptions(TelemetryOptions other)` gains `: this()` on the next line.
    - The signature is unchanged, because it is already in `PublicAPI.Shipped.txt`. No `.PublicAPI` change.
    - Existing telemetry tests still pass.
  - **Test file**: none — verified by RALPH-VERIFY (existing `test/Paramore.Fences.Extensions.Tests/Telemetry/TelemetryOptionsTests.cs`)
  - **Test should verify**:
    - The file equals the rebranded upstream file.
    - No `.PublicAPI` file changed.
    - The telemetry tests pass.
  - **Implementation files**:
    - `src/Paramore.Fences.Extensions/Telemetry/TelemetryOptions.cs` - line 25
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:src/Polly.Extensions/Telemetry/TelemetryOptions.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') src/Paramore.Fences.Extensions/Telemetry/TelemetryOptions.cs && git diff --quiet -- src/Paramore.Fences.Extensions/.PublicAPI && dotnet build src/Paramore.Fences.Extensions -c Release -warnaserror && dotnet test test/Paramore.Fences.Extensions.Tests --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Extensions.Tests.Telemetry" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-8 (TelemetryOptions row), NFR-2, AC-21, AC-35; ADR "Implementation Approach" step 1.5; `git show 1a80392b -- src/Polly.Extensions/Telemetry/TelemetryOptions.cs`; `src/Paramore.Fences.Extensions/.PublicAPI/PublicAPI.Shipped.txt`

- [x] **T10: Delete the unused LegacySupport `GetOrAdd` polyfill**
  - **Behavior**: Delete `src/LegacySupport/ConcurrentDictionaryExtensions.cs`. Core, Extensions and RateLimiting must still build analyser-clean on every TFM, including netstandard2.0, net472 and net462 (all three set `<LegacySupport>true</LegacySupport>`). A-3 found that every `GetOrAdd(` call uses a 2-argument overload.
    - If any of these builds fails, restore the file with `git checkout -- src/LegacySupport/ConcurrentDictionaryExtensions.cs` and **stop**. The FAILURE_REASON must say: "divergence 2 (keep the polyfill) needs a human: D7 comment, #28 note, own commit".
    - List the deleted path under IMPL_FILES, so the main agent stages the deletion.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The file is gone.
    - All three libraries build on all their TFMs with `-warnaserror`.
  - **Implementation files**:
    - `src/LegacySupport/ConcurrentDictionaryExtensions.cs` - delete
  - **RALPH-VERIFY**: `test ! -e src/LegacySupport/ConcurrentDictionaryExtensions.cs && dotnet build src/Paramore.Fences.Core -c Release -warnaserror && dotnet build src/Paramore.Fences.Extensions -c Release -warnaserror && dotnet build src/Paramore.Fences.RateLimiting -c Release -warnaserror`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context (stop protocol); requirements FR-8 (polyfill row and error case), A-3, NFR-7 item 2, AC-21; ADR "Implementation Approach" step 1.5 and the divergence table, row 2; `git show 1a80392b -- src/LegacySupport/ConcurrentDictionaryExtensions.cs`; `eng/Common.props` (LegacySupport compile item)

- [x] **T11: Record the FR-8 suppression list and the `src/` change summary in pr-notes**
  - **Behavior**: Append `## FR-8 Stryker suppressions and src changes`. It contains:
    - a table of every `stryker disable once all` comment added: file, member, mutation target (Core or Extensions), and the reason text verbatim;
    - the polyfill outcome: `ConcurrentDictionaryExtensions.cs` deleted, with the netstandard2.0/net472/net462 builds that passed and the date;
    - one line each for the `TaskExecution` refactor and for `TelemetryOptions` `: this()` (no PublicAPI change);
    - the line "Mutants removed by suppressions per target: to be filled from the PR's CI Stryker reports (T45)".
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - Both suppressed files, the reason text and the polyfill are listed.
    - The suppressions really exist in `src/`.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the FR-8 section
  - **RALPH-VERIFY**: `s=$(awk '/^## FR-8 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md 2>/dev/null); echo "$s" | grep -q 'RetryHelper.cs' && echo "$s" | grep -q 'TelemetryUtil.cs' && echo "$s" | grep -qF 'per-test coverage capture cannot attribute static field initialization to a specific test' && echo "$s" | grep -q 'ConcurrentDictionaryExtensions.cs' && grep -q 'stryker disable once all' src/Paramore.Fences.Core/Retry/RetryHelper.cs && [ "$(grep -c 'stryker disable once all' src/Paramore.Fences.Extensions/Telemetry/TelemetryUtil.cs)" -eq 2 ]`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-8 (bullets), FR-13 (PR body list), FR-14 (mutants removed); ADR "Implementation Approach" step 7

- [x] **T12: Put `RelativeTtlSpecs` in the existing system-clock collection and reset `SystemClock` on dispose**
  - **Behavior**: Apply upstream's diff.
    - Add `[Collection(Constants.SystemClockDependentTestCollection)]`.
    - The class implements `IDisposable` with `public void Dispose() => SystemClock.Reset();`.
    - Reuse the existing constant in `test/Paramore.Fences.Specs/Helpers/Constants.cs`. Add no new `[CollectionDefinition]`.
  - **Test file**: `test/Paramore.Fences.Specs/Caching/RelativeTtlSpecs.cs`
  - **Test should verify**:
    - The file equals the rebranded upstream file.
    - The RelativeTtlSpecs tests pass.
  - **Implementation files**:
    - none (test-only port)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Specs/Caching/RelativeTtlSpecs.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Specs/Caching/RelativeTtlSpecs.cs && dotnet build test/Paramore.Fences.Specs -warnaserror && dotnet test test/Paramore.Fences.Specs --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Specs.Caching.RelativeTtlSpecs" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7 (RelativeTtlSpecs row and the "system-clock collection is reused" bullet), AC-19, AC-20; ADR "Implementation Approach" step 1.6; `git show 1a80392b -- test/Polly.Specs/Caching/RelativeTtlSpecs.cs`; `test/Paramore.Fences.Specs/Helpers/Constants.cs`

- [x] **T13: Port upstream's new `ContextDictionarySpecs`**
  - **Behavior**: Create the file as the rebranded upstream file, namespace `Paramore.Fences.Specs`, with 2 snake_case `[Fact]` methods and their names verbatim (create it with the `git show … | sed …` form in convention 2). Expected status: GREEN.
  - **Test file**: `test/Paramore.Fences.Specs/ContextDictionarySpecs.cs`
  - **Test should verify**:
    - `Generic_dictionary_members_should_delegate_to_the_wrapped_dictionary` passes.
    - `Nongeneric_dictionary_members_should_delegate_to_the_wrapped_dictionary` passes.
    - The file contains no `Polly` reference.
  - **Implementation files**:
    - none (test-only port)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Specs/ContextDictionarySpecs.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Specs/ContextDictionarySpecs.cs && ! grep -q 'Polly' test/Paramore.Fences.Specs/ContextDictionarySpecs.cs && dotnet build test/Paramore.Fences.Specs -warnaserror && dotnet test test/Paramore.Fences.Specs --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Specs.ContextDictionarySpecs" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7 (new Specs files row and naming bullets), AC-19, AC-20; `git show 1a80392b:test/Polly.Specs/ContextDictionarySpecs.cs`

- [x] **T14: Port upstream's new `PolicyBuilderAndHandleSyntaxSpecs`**
  - **Behavior**: Create the file as the rebranded upstream file, with 2 snake_case `[Fact]` methods. Expected status: GREEN.
  - **Test file**: `test/Paramore.Fences.Specs/PolicyBuilderAndHandleSyntaxSpecs.cs`
  - **Test should verify**:
    - `Policy_builders_should_support_object_members` passes.
    - `Generic_handle_overloads_should_match_direct_and_inner_exceptions` passes.
    - The file contains no `Polly` reference.
  - **Implementation files**:
    - none (test-only port)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Specs/PolicyBuilderAndHandleSyntaxSpecs.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Specs/PolicyBuilderAndHandleSyntaxSpecs.cs && ! grep -q 'Polly' test/Paramore.Fences.Specs/PolicyBuilderAndHandleSyntaxSpecs.cs && dotnet build test/Paramore.Fences.Specs -warnaserror && dotnet test test/Paramore.Fences.Specs --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Specs.PolicyBuilderAndHandleSyntaxSpecs" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7, AC-19, AC-20; `git show 1a80392b:test/Polly.Specs/PolicyBuilderAndHandleSyntaxSpecs.cs`

- [x] **T15: Port upstream's new `PolicyOverloadSmokeSpecs` (1a80392b version, without f35bc00d's additions)**
  - **Behavior**: Create the file as the rebranded 1a80392b file, about 204 lines, with 2 snake_case tests. Do **not** add f35bc00d's three `continueOnCapturedContext: true` calls; T41 adds them. Expected status: GREEN.
  - **Test file**: `test/Paramore.Fences.Specs/PolicyOverloadSmokeSpecs.cs`
  - **Test should verify**:
    - `Sync_policy_execute_overloads_with_cancellation_and_context_data_should_delegate_correctly` passes.
    - `Async_policy_execute_overloads_with_cancellation_and_context_data_should_delegate_correctly` passes.
    - The file equals 1a80392b, rebranded.
  - **Implementation files**:
    - none (test-only port)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Specs/PolicyOverloadSmokeSpecs.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Specs/PolicyOverloadSmokeSpecs.cs && ! grep -q 'Polly' test/Paramore.Fences.Specs/PolicyOverloadSmokeSpecs.cs && dotnet build test/Paramore.Fences.Specs -warnaserror && dotnet test test/Paramore.Fences.Specs --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Specs.PolicyOverloadSmokeSpecs" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7, FR-12, AC-19, AC-20; `git show 1a80392b:test/Polly.Specs/PolicyOverloadSmokeSpecs.cs`

- [x] **T16: Port upstream's new `Retry/RetryOverloadSmokeSpecs`**
  - **Behavior**: Create the file as the rebranded upstream file, namespace `Paramore.Fences.Specs.Retry`, about 648 lines with 6 snake_case tests. The port is mechanical and verbatim, so the large size is acceptable. Expected status: GREEN.
  - **Test file**: `test/Paramore.Fences.Specs/Retry/RetryOverloadSmokeSpecs.cs`
  - **Test should verify**:
    - These tests pass: `Result_retry_overloads_should_retry_successfully`, `Result_wait_and_retry_overloads_should_retry_successfully`, `Async_result_retry_overloads_should_retry_successfully`, `Async_result_wait_and_retry_overloads_should_retry_successfully`, `Async_exception_retry_overloads_should_retry_successfully` and `Async_exception_wait_and_retry_overloads_should_retry_successfully`.
    - The file contains no `Polly` reference.
  - **Implementation files**:
    - none (test-only port)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Specs/Retry/RetryOverloadSmokeSpecs.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Specs/Retry/RetryOverloadSmokeSpecs.cs && ! grep -q 'Polly' test/Paramore.Fences.Specs/Retry/RetryOverloadSmokeSpecs.cs && dotnet build test/Paramore.Fences.Specs -warnaserror && dotnet test test/Paramore.Fences.Specs --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Specs.Retry.RetryOverloadSmokeSpecs" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7, AC-19, AC-20; `git show 1a80392b:test/Polly.Specs/Retry/RetryOverloadSmokeSpecs.cs`

- [x] **T17: Port upstream's new `Timeout/TimeoutOverloadSmokeSpecs` (1a80392b version, without `CreateContext()`)**
  - **Behavior**: Create the file as the rebranded 1a80392b file. It carries `[Collection(Constants.SystemClockDependentTestCollection)]`, derives from the existing `TimeoutSpecsBase`, and has 4 snake_case tests and its own private `ShouldTimeout` / `ShouldTimeoutAsync` helpers.
    - Keep the 8 `new Context("operation-key")` arguments. T41 (f35bc00d) replaces them with `CreateContext()`.
    - Expected status: GREEN.
  - **Test file**: `test/Paramore.Fences.Specs/Timeout/TimeoutOverloadSmokeSpecs.cs`
  - **Test should verify**:
    - These tests pass: `Sync_timeout_overloads_should_execute_and_timeout`, `Async_timeout_overloads_should_execute_and_timeout`, `Generic_timeout_overloads_should_execute_and_timeout` and `Generic_async_timeout_overloads_should_execute_and_timeout`.
    - The file contains no `Polly` reference.
  - **Implementation files**:
    - none (test-only port)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Specs/Timeout/TimeoutOverloadSmokeSpecs.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Specs/Timeout/TimeoutOverloadSmokeSpecs.cs && ! grep -q 'Polly' test/Paramore.Fences.Specs/Timeout/TimeoutOverloadSmokeSpecs.cs && dotnet build test/Paramore.Fences.Specs -warnaserror && dotnet test test/Paramore.Fences.Specs --framework net10.0 -p:CollectCoverage=false --filter "FullyQualifiedName~Paramore.Fences.Specs.Timeout.TimeoutOverloadSmokeSpecs" | grep -qE 'Passed! +- Failed: +0, Passed: +[1-9]'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7, FR-12, AC-19, AC-20; `git show 1a80392b:test/Polly.Specs/Timeout/TimeoutOverloadSmokeSpecs.cs`; `test/Paramore.Fences.Specs/Timeout/TimeoutSpecsBase.cs`

- [x] **T18: Switch `src/Snippets` from `xunit` to `xunit.v3.assert` and confirm `mdsnippets` produces no Markdown change**
  - **Behavior**:
    - Add `<PackageVersion Include="xunit.v3.assert" Version="<T03 chosen>" />` to `Directory.Packages.props`, directly after `xunit.runner.visualstudio` (line 39).
    - Change `src/Snippets/Snippets.csproj:35` from `<PackageReference Include="xunit" />` to `<PackageReference Include="xunit.v3.assert" />`.
    - Leave the `xunit` PackageVersion for now: the test projects still use it until T19.
    - Then run `dotnet mdsnippets`. No Markdown file should change, because no snippet source changes.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - Snippets references `xunit.v3.assert` and builds analyser-clean.
    - `dotnet mdsnippets` leaves every `.md` outside `specs/` unchanged (AC-27).
  - **Implementation files**:
    - `Directory.Packages.props` - add the `xunit.v3.assert` version
    - `src/Snippets/Snippets.csproj` - swap the reference
  - **RALPH-VERIFY**: `grep -q '<PackageReference Include="xunit.v3.assert" />' src/Snippets/Snippets.csproj && grep -q 'PackageVersion Include="xunit.v3.assert"' Directory.Packages.props && dotnet build src/Snippets -c Release -warnaserror && dotnet tool restore && dotnet mdsnippets && git diff --quiet -- '*.md' ':!specs'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-2 (Snippets and xunit.v3.assert rows), FR-11 (mdsnippets bullet), AC-27; ADR "Implementation Approach" steps 1.1 and 1.8; `git show 1a80392b -- src/Snippets/Snippets.csproj Directory.Packages.props`; `specs/001-port-xunit-v3/pr-notes.md` (FR-2 table)

- [x] **T19: Move every test project to xunit v3 on Microsoft.Testing.Platform v2 (the central switch)**
  - **Behavior**: After this task, `dotnet test` runs through MTP and all five test projects pass on net10.0.
    - **Why this is one task.** It cannot be split without a broken build:
      - xunit v3 requires `OutputType=Exe`.
      - `Xunit.Abstractions` disappears.
      - v3's `ITestOutputHelper` adds members.
      - v3's `TheoryData<T>` makes the upstream casts necessary, while under v2 IDE0004 (an error here) would flag them as redundant.
      - FsCheck.Xunit (v2) cannot run under v3.
      - MTP v2 projects cannot run under VSTest-mode `dotnet test`, so `global.json` must switch at the same time.
      - The ReportGenerator MSBuild task in `eng/Test.targets` disappears with its package.
      - xunit v3's analyser raises xUnit1069 (an error here) on `IssuesTests.InfiniteRetry_2163`, whose `Timeout` does not use `TestContext.Current.CancellationToken`. Upstream's change to that file therefore lands here. (Amended 2026-10-13 after the first T19 attempt failed on exactly this error; this was T21's edit.)
    - Coverage arguments (T22), Cake (T23–T26) and `TestCancellation` (T20) are deliberately left out.
    - **Edits:**
      1. `Directory.Packages.props`:
         - remove `coverlet.msbuild` (:5), `JunitXml.TestLogger` (:11), `ReportGenerator` (:29) and `xunit` (:38);
         - replace `FsCheck.Xunit` (:7) with `FsCheck.Xunit.v3`;
         - bump `Microsoft.NET.Test.Sdk` (:22);
         - add `Microsoft.Testing.Extensions.CodeCoverage` and `Microsoft.Testing.Platform` after :22, and `xunit.v3.mtp-v2` after `xunit.v3.assert`.

         All versions come from the T03 table. No other row changes.
      2. `global.json`: add `"test": { "runner": "Microsoft.Testing.Platform" }` after `msbuild-sdks`. Keep `"Cake.Sdk": "6.3.0"` and the `sdk` block unchanged.
      3. `eng/Test.targets`: replace the package ItemGroup (lines 14–24) with upstream's. Delete everything from line 26 to the closing `</Project>`: the coverlet PropertyGroups, the `WriteLinesToFileWithRetry` UsingTask and the `GenerateCoverageReports` target. Lines 1–13 stay.
      4. In each of the 5 test csproj files, add `<OutputType>Exe</OutputType>` at upstream's position and delete the coverlet-only `<Include>[…]*</Include>` (Core.Tests :10; Extensions, RateLimiting, Testing and Specs :8). Core.Tests :18 becomes `FsCheck.Xunit.v3`. In Specs, remove `<Using Include="Xunit.Abstractions" />` (:25). Leave `<Threshold>` alone; T27 changes it.
      5. Test sources, exactly as upstream:
         - remove `using Xunit.Abstractions;` from `HedgingResilienceStrategyTests.cs:6`;
         - `AnnotatedOutputHelper` gains `public string Output => _innerOutputHelper.Output;`;
         - `SilentOutputHelper` gains `Output => string.Empty` and silent `Write(string)` and `Write(string, params object[])`;
         - apply the `TheoryData` casts in `ResiliencePipelineTests.cs` (`null as ResilienceContextPool`) and `Utils/Pipeline/PipelineComponentFactoryTests.cs`;
         - in `Issues/IssuesTests.InfiniteRetry_2163.cs`, apply upstream's diff: `timeout` is a `CancellationTokenSource` with the 20-second (or debugger) limit; `linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, TestContext.Current.CancellationToken)`; execution, the loop condition and the assert use `linked`. The test name `InfiniteRetry_Delay_Does_Not_Overflow_2163` is unchanged.
    - **If it goes wrong:**
      - If restore fails because of a version conflict between the chosen FR-2 versions (NU1605/NU1608), **stop** with the conflict. FR-2 needs a human decision.
      - If a pre-existing test fails under v3, compare it with upstream's 1a80392b version of the same file. Fix only to match upstream; otherwise **stop**.
  - **Test file**: none — verified by RALPH-VERIFY (the whole existing suite)
  - **Test should verify**:
    - `global.json` selects MTP and Cake.Sdk is still 6.3.0.
    - The six adapted test files equal the rebranded upstream files.
    - `test/` has no `Xunit.Abstractions`.
    - No reference to `xunit` (v2), `FsCheck.Xunit`, coverlet, JunitXml or the ReportGenerator package remains.
    - No coverlet `<Include>` remains, and there are 5 Exe test projects.
    - The solution builds with `-warnaserror`, and `cake.cs` still compiles (dry run).
    - All five projects pass on net10.0 under MTP.
  - **Implementation files**:
    - `Directory.Packages.props`, `global.json`, `eng/Test.targets` - as above
    - `test/Paramore.Fences.Core.Tests/Paramore.Fences.Core.Tests.csproj`, `test/Paramore.Fences.Extensions.Tests/Paramore.Fences.Extensions.Tests.csproj`, `test/Paramore.Fences.RateLimiting.Tests/Paramore.Fences.RateLimiting.Tests.csproj`, `test/Paramore.Fences.Testing.Tests/Paramore.Fences.Testing.Tests.csproj`, `test/Paramore.Fences.Specs/Paramore.Fences.Specs.csproj` - Exe, no `<Include>`, FsCheck.Xunit.v3, no Abstractions using
    - `test/Paramore.Fences.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs`, `test/Paramore.Fences.Core.Tests/ResiliencePipelineTests.cs`, `test/Paramore.Fences.Core.Tests/Utils/Pipeline/PipelineComponentFactoryTests.cs`, `test/Paramore.Fences.Specs/Helpers/Bulkhead/AnnotatedOutputHelper.cs`, `test/Paramore.Fences.Specs/Helpers/Bulkhead/SilentOutputHelper.cs`, `test/Paramore.Fences.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs` - v3 adaptations
  - **RALPH-VERIFY**: `grep -q '"runner": "Microsoft.Testing.Platform"' global.json && grep -q '"Cake.Sdk": "6.3.0"' global.json && ( for p in Core.Tests/Hedging/HedgingResilienceStrategyTests.cs Core.Tests/ResiliencePipelineTests.cs Core.Tests/Utils/Pipeline/PipelineComponentFactoryTests.cs Specs/Helpers/Bulkhead/AnnotatedOutputHelper.cs Specs/Helpers/Bulkhead/SilentOutputHelper.cs Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs; do diff <(git show 1a80392b:test/Polly.$p | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.$p || exit 1; done ) && ! git grep -q 'Xunit.Abstractions' -- test/ && ! git grep -qE 'Include="(xunit|FsCheck\.Xunit|coverlet\.msbuild|JunitXml\.TestLogger|ReportGenerator)"' -- Directory.Packages.props eng/ test/ src/ && ! grep -qE 'coverlet|CollectCoverage|CoverletOutputFormat|ReportGenerator' eng/Test.targets && ! git grep -q '<Include>\[' -- 'test/*.csproj' && [ "$(git grep -l '<OutputType>Exe</OutputType>' -- 'test/*Tests.csproj' 'test/*Specs.csproj' | wc -l | tr -d ' ')" -eq 5 ] && dotnet build Fences.slnx -warnaserror && NO_COLOR=1 dotnet cake.cs -- --target=Default --dryrun && dotnet test --project test/Paramore.Fences.Core.Tests --framework net10.0 && dotnet test --project test/Paramore.Fences.Extensions.Tests --framework net10.0 && dotnet test --project test/Paramore.Fences.RateLimiting.Tests --framework net10.0 && dotnet test --project test/Paramore.Fences.Testing.Tests --framework net10.0 && dotnet test --project test/Paramore.Fences.Specs --framework net10.0`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context (conventions 5, 6, 9); requirements FR-1, FR-2, FR-7 (output helpers, TheoryData), AC-2, AC-3, AC-7; ADR "Implementation Approach" steps 1.1, 1.2, 1.3, 1.6, and "Where each type is touched"; `git show 1a80392b -- Directory.Packages.props global.json eng/Test.targets 'test/*.csproj' test/Polly.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs test/Polly.Core.Tests/ResiliencePipelineTests.cs test/Polly.Core.Tests/Utils/Pipeline/PipelineComponentFactoryTests.cs test/Polly.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs test/Polly.Specs/Helpers/Bulkhead`; `specs/001-port-xunit-v3/pr-notes.md` (FR-2 table); `cake.cs:1-6` (`ProjectType=Test`, `XunitAutoGeneratedEntryPoint=false`; cake.cs imports `eng/Test.targets`). net481 is verified by Windows CI (convention 13).

- [x] **T20: Make `TestCancellation.Token` return xunit v3's per-test cancellation token**
  - **Behavior**: `test/Shared/TestCancellation.cs` gains `using Xunit;` and `Token => TestContext.Current.CancellationToken`, exactly as upstream. The file is linked into all 5 test projects, which must still pass.
  - **Test file**: none — verified by RALPH-VERIFY (all 5 suites)
  - **Test should verify**:
    - The file equals the rebranded upstream file.
    - The solution builds analyser-clean.
    - All 5 projects pass on net10.0.
  - **Implementation files**:
    - `test/Shared/TestCancellation.cs` - line 5, plus the using
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Shared/TestCancellation.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Shared/TestCancellation.cs && dotnet build Fences.slnx -warnaserror && dotnet test --project test/Paramore.Fences.Core.Tests --framework net10.0 && dotnet test --project test/Paramore.Fences.Extensions.Tests --framework net10.0 && dotnet test --project test/Paramore.Fences.RateLimiting.Tests --framework net10.0 && dotnet test --project test/Paramore.Fences.Testing.Tests --framework net10.0 && dotnet test --project test/Paramore.Fences.Specs --framework net10.0`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7 (TestCancellation row), AC-18; ADR "Implementation Approach" step 1.6; `git show 1a80392b -- test/Shared/TestCancellation.cs`

- [x] **T21: Check that `IssuesTests.InfiniteRetry_2163` links its 20-second guard to the test-context cancellation token (edit made in T19)**
  - **Behavior**: Check only. The edit was folded into T19 on 2026-10-13, because xunit v3's xUnit1069 analyser error blocks T19's build without it.
    - Run RALPH-VERIFY. Do not edit the file.
    - If it passes, the expected status is **ALREADY_COMPLETE**. This is the one exception to convention 7.
    - If it fails, return FAILED with the diff or the test output.
  - **Test file**: `test/Paramore.Fences.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs`
  - **Test should verify**:
    - The file equals the rebranded upstream file.
    - The test passes under MTP.
  - **Implementation files**:
    - none (verification only)
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:test/Polly.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs && dotnet build test/Paramore.Fences.Core.Tests -warnaserror && dotnet test --project test/Paramore.Fences.Core.Tests --framework net10.0 --filter-method "*.InfiniteRetry_Delay_Does_Not_Overflow_2163"`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-7 (IssuesTests row); ADR "Implementation Approach" step 1.6; `git show 1a80392b -- test/Polly.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs`

- [x] **T22: Make every MTP test run write per-TFM Cobertura coverage and pass the CI reporter flags**
  - **Behavior**: Add upstream's `PropertyGroup` and `_UpdateTestPlatformProperties` target to `eng/Test.targets`, so the file equals upstream's 1a80392b file. Upstream's file has no Polly names.
    - The PropertyGroup holds `CoverageFormat=cobertura`, `CoverageOutputPath` = `$(ArtifactsPath)/coverage`, and `CoverageOutput` = `…/<MSBuildProjectName>/coverage.$(TargetFramework).xml`.
    - The target runs `BeforeTargets="_MTPAddArguments;InvokeTestingPlatform"`. It passes `--results-directory`, `--coverage`, `--coverage-output-format` and `--coverage-output`, and on CI it adds `--report-github --report-github-summary-include-passed false --report-xunit-junit --report-xunit-junit-filename <Project>.<tfm>.junit.xml`.
    - After this, `dotnet test --project test/<P>` writes one file per TFM. Unlike coverlet, net481 is included on Windows.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - `eng/Test.targets` equals upstream's byte for byte.
    - A run of Testing.Tests writes `coverage.net10.0.xml`, `coverage.net9.0.xml` and `coverage.net8.0.xml`, each with a `package` named `Paramore.Fences.Testing`.
  - **Implementation files**:
    - `eng/Test.targets` - add the PropertyGroup and the target
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:eng/Test.targets) eng/Test.targets && rm -rf artifacts/coverage/Paramore.Fences.Testing.Tests && dotnet test --project test/Paramore.Fences.Testing.Tests && ( for t in net10.0 net9.0 net8.0; do grep -q 'name="Paramore.Fences.Testing"' artifacts/coverage/Paramore.Fences.Testing.Tests/coverage.$t.xml || exit 1; done )`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-3, FR-10, AC-6, AC-7, AC-24; ADR "Key Components", `_UpdateTestPlatformProperties`; `git show 1a80392b -- eng/Test.targets`; `Directory.Build.props` (`UseArtifactsOutput`). net481 is verified by Windows CI.

- [x] **T23: Remove the VSTest loggers from Cake's `__RunTests`**
  - **Behavior**: Replace `__RunTests` (`cake.cs:137-163`) with upstream's version.
    - The `loggers` array and the `Loggers = loggers` setting go. VSTest `--logger` arguments break `dotnet test` under MTP, and on CI they would break `Build`.
    - Add the per-project `var projectName = proj.GetFilenameWithoutExtension().ToString();` line, verbatim from upstream.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - No `Loggers` and no `junit;LogFilePath` remain in `cake.cs`.
    - The upstream line is present.
    - `cake.cs` still compiles and dry-runs.
  - **Implementation files**:
    - `cake.cs` - the `__RunTests` task
  - **RALPH-VERIFY**: `! grep -q 'Loggers' cake.cs && ! grep -q 'junit;LogFilePath' cake.cs && grep -A12 'Task("__RunTests")' cake.cs | grep -qF 'var projectName = proj.GetFilenameWithoutExtension().ToString();' && NO_COLOR=1 dotnet cake.cs -- --target=Default --dryrun`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-10; ADR "Implementation Approach" step 1.4; `git show 1a80392b -- cake.cs` (the `__RunTests` hunk)

- [x] **T24: Add the `reportgenerator` local tool and Cake's `__GenerateCoverageReports` task**
  - **Behavior**:
    - In `.config/dotnet-tools.json`, add `dotnet-reportgenerator-globaltool` (command `reportgenerator`, version from T03, `rollForward: false`) before `dotnet-stryker`, as upstream does.
    - In `cake.cs`, add upstream's `coverageDir` and `coverageReportsDir` variables after line 29, the `__GenerateCoverageReports` task after `__RunTests` (with `IsDependentOn("__RunTests")`), and the `AppendAllLinesWithRetry` helper before `PatchStrykerConfig`. All three are verbatim upstream, rebranded.
    - What the task does:
      - It runs `dotnet reportgenerator` once per `artifacts/coverage/<P>/coverage.*.xml`, with tag = TFM, writing into `artifacts/coverage-reports/<P>/`.
      - On GitHub Actions it appends a `<P> Code Coverage report (<tfm>)` section to the job summary.
      - On a non-zero exit it throws `Failed to generate the coverage report for '<project>' (<tag>).`
    - Do not wire it into `Build` yet; T26 does that.
    - **First, confirm the isolated run works.** `NO_COLOR=1 dotnet cake.cs -- --target=__GenerateCoverageReports --exclusive --dryrun` must not list `__RunTests`. If it does, Cake.Sdk 6.3.0 is not honouring `--exclusive` and every later isolated check is invalid: **stop**. The ADR's fallback is a human-run local edit.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The tool is in the manifest at major 5.
    - The upstream error message is in `cake.cs`.
    - `--exclusive` isolates the task.
    - After a Testing.Tests run, the isolated task writes `artifacts/coverage-reports/Paramore.Fences.Testing.Tests/index.html`.
  - **Implementation files**:
    - `.config/dotnet-tools.json` - add the tool
    - `cake.cs` - variables, task, helper
  - **RALPH-VERIFY**: `grep -A1 '"dotnet-reportgenerator-globaltool"' .config/dotnet-tools.json | grep -qE '"version": "5\.' && grep -qF "Failed to generate the coverage report for '{projectName}' ({tag})." cake.cs && ! (NO_COLOR=1 dotnet cake.cs -- --target=__GenerateCoverageReports --exclusive --dryrun 2>&1 | grep -q '__RunTests') && dotnet tool restore && dotnet test --project test/Paramore.Fences.Testing.Tests && rm -rf artifacts/coverage-reports && NO_COLOR=1 dotnet cake.cs -- --target=__GenerateCoverageReports --exclusive && test -f artifacts/coverage-reports/Paramore.Fences.Testing.Tests/index.html`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context (conventions 9, 14); requirements FR-2 (reportgenerator row), FR-4, AC-8, AC-9; ADR "Key Components", `__GenerateCoverageReports`; ADR "Technology Choices", "How does the threshold check run on its own?"; `git show 1a80392b -- cake.cs .config/dotnet-tools.json`; `specs/001-port-xunit-v3/pr-notes.md` (FR-2 table)

- [x] **T25: Add Cake's `__VerifyCoverageThresholds` task and upstream's coverage helpers**
  - **Behavior**: Add upstream's `__VerifyCoverageThresholds` task after `__GenerateCoverageReports` (with `IsDependentOn("__RunTests")`), and add `GetCoverageThresholds`, `GetCoverageAssemblyName` and `GetAssemblyCoverage` next to `AppendAllLinesWithRetry`. All verbatim, rebranded.
    - The task applies ladder rows 1, 2 and 4–9: it reads `<Threshold>` and `<ThresholdType>` with coverlet's reuse-the-last-value rule, and uses the Cobertura `package` named after the covered assembly.
    - Figures print as `<P> (<tfm>): line=…%, branch=…%, method=…% for '<asm>'.`
    - Each violation is `<P> (<tfm>): <metric> coverage of <actual 0.##>% for '<asm>' is below the required threshold of <req>%.`
    - Every violation is logged first, then the task throws `<n> coverage threshold violation(s) found. See the errors above.`
    - Upstream's `if (!DirectoryExists(projectCoverageDir)) continue;` stays here. T31 replaces it.
    - **The verify is AC-13's "one TFM short" experiment.**
      1. It runs Core.Tests on all three TFMs and counts the unedited Core.Tests violations.
      2. It sets the `line-rate` of the first `method` in the net8.0 file's `Paramore.Fences.Core` package to 0, so that one method counts as uncovered.
      3. It expects exactly one more violation, the net8.0 method, while the other TFMs' figures still print. Then it restores the file.
    - The experiment uses `method` because it is 100 % on every TFM, while line and branch already fall short under MS CodeCoverage (Ian, 2026-10-15). AC-13 asks for one metric, not line specifically. The verify compares counts, so the existing line/branch violations don't affect it. Don't fix any gap here; T30 owns coverage gaps.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The net8.0-only shortfall gives exactly one new violation, `Paramore.Fences.Core.Tests (net8.0): method coverage of <N>% …`.
    - The run exits non-zero.
    - The net10.0 figures are still printed.
  - **Implementation files**:
    - `cake.cs` - the task and three helpers
  - **RALPH-VERIFY**: `f=artifacts/coverage/Paramore.Fences.Core.Tests/coverage.net8.0.xml; dotnet test --project test/Paramore.Fences.Core.Tests && cp "$f" "$f.bak" && n0=$(NO_COLOR=1 dotnet cake.cs -- --target=__VerifyCoverageThresholds --exclusive 2>&1 | grep -o 'Paramore.Fences.Core.Tests (net[0-9.]*): [a-z]* coverage of .*' | sort -u | wc -l | tr -d ' '); perl -0777 -pi -e 's{(<package\b[^>]*\bname="Paramore\.Fences\.Core"[^>]*>.*?<method\b[^>]*?\b)line-rate="[^"]*"}{${1}line-rate="0"}s' "$f"; o=$(NO_COLOR=1 dotnet cake.cs -- --target=__VerifyCoverageThresholds --exclusive 2>&1); rc=$?; mv "$f.bak" "$f"; [ $rc -ne 0 ] && echo "$o" | grep -qE "Paramore\.Fences\.Core\.Tests \(net8\.0\): method coverage of [0-9.]+% for 'Paramore\.Fences\.Core' is below the required threshold of 100%\." && [ "$(echo "$o" | grep -o 'Paramore.Fences.Core.Tests (net[0-9.]*): [a-z]* coverage of .*' | sort -u | wc -l | tr -d ' ')" -eq $((n0+1)) ] && echo "$o" | grep -q 'Paramore.Fences.Core.Tests (net10.0): line='`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-5, AC-11, AC-13; ADR decision-ladder table, ADR "Key Components", `__VerifyCoverageThresholds` and its helpers; ADR "Implementation Approach" step 6 (AC-13); `git show 1a80392b -- cake.cs`

- [x] **T26: Wire report generation and the threshold check into `Build`, after `__RunTests`**
  - **Behavior**: `Build` gains `.IsDependentOn("__GenerateCoverageReports")` and `.IsDependentOn("__VerifyCoverageThresholds")` after `__RunTests` and before `__CreateNuGetPackages` (`cake.cs:219-223`).
    - Reports therefore run before the check and survive a failing check (FR-4).
    - After this, `cake.cs` equals upstream's 1a80392b `cake.cs` passed through the rebrand filter. If an earlier cake task drifted from upstream, fix it here to match.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - `cake.cs` equals rebranded upstream.
    - A `Default` dry run executes `__RunTests` → `__GenerateCoverageReports` → `__VerifyCoverageThresholds` → `__CreateNuGetPackages`, in that order (AC-10 ordering).
  - **Implementation files**:
    - `cake.cs` - the `Build` task
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:cake.cs | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') cake.cs && o=$(NO_COLOR=1 dotnet cake.cs -- --target=Default --dryrun 2>&1) && [ "$(echo "$o" | grep -oE '__(RunTests|GenerateCoverageReports|VerifyCoverageThresholds|CreateNuGetPackages)' | awk '!s[$0]++' | tr '\n' ' ')" = "__RunTests __GenerateCoverageReports __VerifyCoverageThresholds __CreateNuGetPackages " ]`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-4 ("Reports survive a failing threshold check"), FR-5, AC-10; ADR "The mechanism, end to end" and its invariants; `git show 1a80392b -- cake.cs`

- [x] **T27: Raise the `Paramore.Fences.Specs` threshold to upstream's `96,95,93`**
  - **Behavior**: `test/Paramore.Fences.Specs/Paramore.Fences.Specs.csproj:7` changes from `<Threshold>94,94,91</Threshold>` to `<Threshold>96,95,93</Threshold>`. The other four projects stay at `100`. Whether Specs reaches it is checked in T30.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - Specs is `96,95,93` and the old value is gone.
    - Exactly four test projects are at `100`.
    - Specs builds.
  - **Implementation files**:
    - `test/Paramore.Fences.Specs/Paramore.Fences.Specs.csproj` - line 7
  - **RALPH-VERIFY**: `grep -q '<Threshold>96,95,93</Threshold>' test/Paramore.Fences.Specs/Paramore.Fences.Specs.csproj && ! grep -q '94,94,91' test/Paramore.Fences.Specs/Paramore.Fences.Specs.csproj && [ "$(git grep -h '<Threshold>100</Threshold>' -- 'test/*.csproj' | wc -l | tr -d ' ')" -eq 4 ] && dotnet build test/Paramore.Fences.Specs -warnaserror`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-6, C-4, AC-16; ADR "Implementation Approach" step 1.2; `git show 1a80392b -- test/Polly.Specs/Polly.Specs.csproj`

- [x] **T28: Run Stryker on MTP: bump `dotnet-stryker` and select the `mtp` test runner**
  - **Behavior**: These two changes depend on each other, because Stryker 4.16 cannot drive MTP projects.
    - `.config/dotnet-tools.json`: `dotnet-stryker` goes to the T03 version (5.x).
    - `eng/stryker-config.json`: add `"test-runner": "mtp",` after `"target-framework": "net10.0",` (:25).
    - Nothing else in the Stryker config changes (C-7).
    - The real run happens in T35.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The Stryker config equals upstream's.
    - The tool manifest has Stryker 5.x, and it restores.
  - **Implementation files**:
    - `.config/dotnet-tools.json` - Stryker version
    - `eng/stryker-config.json` - test-runner
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:eng/stryker-config.json) eng/stryker-config.json && grep -A1 '"dotnet-stryker"' .config/dotnet-tools.json | grep -qE '"version": "5\.[0-9]+\.[0-9]+"' && dotnet tool restore && dotnet tool list --local | grep -qE 'dotnet-stryker +5\.'`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-2 (dotnet-stryker row), FR-9, C-7, AC-22; ADR "Implementation Approach" step 1.7; `git show 1a80392b -- eng/stryker-config.json .config/dotnet-tools.json`; `specs/001-port-xunit-v3/pr-notes.md` (FR-2 table)

- [x] **T29: Disable MinVer in CI mutation jobs (`MinVerSkip: true`, stryker-net#3094 workaround)**
  - **Behavior**: In `.github/workflows/mutation-tests.yml`, in the `mutations` job, add these lines after `timeout-minutes: 60` and before `strategy:`, verbatim from upstream:

    ```yaml
    # HACK Workaround for https://github.com/stryker-mutator/stryker-net/issues/3094
    env:
      MinVerSkip: true
    ```

    Do not change `cake.cs`.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The workflow equals upstream's apart from the existing rebranded `SKIP_FENCES_ANALYZERS` line.
    - `cake.cs` has no `MinVerSkip`.
  - **Implementation files**:
    - `.github/workflows/mutation-tests.yml` - job-level env
  - **RALPH-VERIFY**: `diff <(git show 1a80392b:.github/workflows/mutation-tests.yml | sed 's/SKIP_POLLY_ANALYZERS/SKIP_FENCES_ANALYZERS/') .github/workflows/mutation-tests.yml && ! grep -q 'MinVerSkip' cake.cs`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context; requirements FR-9 ("On CI" bullet), AC-22; ADR "Implementation Approach" step 1.7; `git show 1a80392b -- .github/workflows/mutation-tests.yml`

- [x] **T30: Gate: the full Cake build passes, with every threshold met on every macOS TFM (stop and report on a coverage gap)**
  - **Behavior**: Run `dotnet tool restore && NO_COLOR=1 dotnet cake.cs -- --target=Default --configuration=Release` in the background. It takes about 20 minutes.
    - **Expected result.** All tests pass on net10.0, net9.0 and net8.0. The 15 coverage files and 5 HTML reports exist. The threshold check prints 15 figure lines with no violation, and packages are built. The build stays analyser-clean across both `.slnx` files (NFR-1).
    - **If a test, build or analyser error occurs,** compare the failing area with upstream's 1a80392b. Fix only porting slips (a missed hunk, or a rebranding error) in the A files; otherwise **stop**.
    - **If the threshold check reports a violation,** treat it as a porting omission first (decision 3):
      1. Compare `git diff $(git merge-base HEAD origin/main) -- src test` with `git show 1a80392b -- src test`, rebranded. Every hunk must be present, and every new test must exist and run.
      2. Check the `#if !NETFRAMEWORK` guards and the Stryker comments.
      3. Check for rebranding slips, such as a coverage `package` name or Threshold typo.
      4. Check `[ExcludeFromCodeCoverage]` parity: `git grep -l ExcludeFromCodeCoverage -- src` should match `git grep -l ExcludeFromCodeCoverage 1a80392b -- src` (13 files, rebranded).
      5. Fix only a genuine omission, to match upstream.
    - **If a gap remains,** write `specs/001-port-xunit-v3/coverage-gap-report.md`. For each gap, give the project, TFM, metric, actual against required, the uncovered lines (from the Cobertura file or HTML), and what was checked. Then **stop**: `touch RALPH_STOP`, return FAILED, list no files. Never add Fences-only tests or exclusions, and never lower a threshold.
    - **On success,** append two sections to pr-notes:
      - `## FR-6 Fences-only tests and exclusions`, which says "none" and records that the AC-17 count of added `ExcludeFromCodeCoverage` lines is 0;
      - `## Local verification (macOS)`, which holds the date, the full HEAD SHA, the 15 printed figure lines, the 15 coverage file paths, the 5 report paths, and the note "net481: pending Windows CI".
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The full build exits 0.
    - Exactly 15 coverage files exist.
    - All 5 `index.html` reports exist.
    - The FR-6 section says none, and the local-verification section exists.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the two sections
    - (only if a porting omission is found) the omitted upstream hunk in the matching `src/` or `test/` file
  - **RALPH-VERIFY**: `dotnet tool restore && NO_COLOR=1 dotnet cake.cs -- --target=Default --configuration=Release && [ "$(ls artifacts/coverage/*/coverage.net*.xml | wc -l | tr -d ' ')" -eq 15 ] && ls artifacts/coverage-reports/Paramore.Fences.{Core.Tests,Extensions.Tests,RateLimiting.Tests,Testing.Tests,Specs}/index.html >/dev/null && awk '/^## FR-6 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md | grep -qi 'none' && grep -q '^## Local verification (macOS)' specs/001-port-xunit-v3/pr-notes.md`
  - **Squash group**: A
  - **References**: ralph-tasks.md § Spec Context (conventions 4, 8, 9); requirements FR-1, FR-3, FR-4, FR-5, FR-6, NFR-1, A-2, AC-1, AC-6, AC-8, AC-11, AC-16, AC-17; ADR "Implementation Approach" step 4, and the "Technology Choices" answer on `[ExcludeFromCodeCoverage]`; `git show 1a80392b -- src test`. net481 is verified by Windows CI after push (T45).

- [x] **T31: Fail the threshold check when a built TFM has no coverage file (Fences divergence 1)**
  - **Behavior**: In `__VerifyCoverageThresholds`, after the `thresholds.Count == 0` skip (ladder row 1 stays first), replace upstream's `if (!DirectoryExists(projectCoverageDir)) { continue; }`.
    - The replacement loops over `GetBuiltTargetFrameworks(proj.FullPath)`. For each TFM whose `coverage.<tfm>.xml` is missing, it adds the violation `<Project> (<tfm>): no coverage file was found at '<path>'.` Then the existing loop over the files that exist continues unchanged.
    - Add `GetBuiltTargetFrameworks(string csprojPath)` next to the other coverage helpers. It runs `dotnet msbuild "<csproj>" -getProperty:TargetFrameworks` through `StartProcess` with `RedirectStandardOutput`, splits the output on `;`, and throws with the project name on a non-zero exit.
    - Put one comment at the change: `// Fences divergence (ADR 0004): upstream skips a missing coverage file. Fences fails, so a TFM cannot drop out of the gate silently.`
    - Change nothing else in `cake.cs`.
    - **Precondition:** T30's 15 Release coverage files exist. If they don't, regenerate them first with `dotnet tool restore && dotnet cake.cs -- --target=Default --configuration=Release` (in the background).
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify** (AC-14):
    - With all files present, the isolated check passes.
    - With `Paramore.Fences.Specs/coverage.net9.0.xml` removed, it fails and names `Paramore.Fences.Specs (net9.0)` as missing. The file is then restored.
    - The divergence comment and the MSBuild call are present.
  - **Implementation files**:
    - `cake.cs` - the `__VerifyCoverageThresholds` change and the new helper
  - **RALPH-VERIFY**: `f=artifacts/coverage/Paramore.Fences.Specs/coverage.net9.0.xml; grep -q 'Fences divergence (ADR 0004)' cake.cs && grep -q 'getProperty:TargetFrameworks' cake.cs && NO_COLOR=1 dotnet cake.cs -- --target=__VerifyCoverageThresholds --exclusive && mv "$f" "$f.bak" && o=$(NO_COLOR=1 dotnet cake.cs -- --target=__VerifyCoverageThresholds --exclusive 2>&1); rc=$?; mv "$f.bak" "$f"; [ $rc -ne 0 ] && echo "$o" | grep -qF "Paramore.Fences.Specs (net9.0): no coverage file was found at"`
  - **Squash group**: B
  - **References**: ralph-tasks.md § Spec Context; requirements FR-5 ("missing coverage file" boundary), NFR-7 item 1, AC-14; ADR divergence table row 1, decision-ladder row 3, "Key Components" `GetBuiltTargetFrameworks`, "Implementation Approach" step 2, and "Why ask MSBuild for the built TFMs…"

- [x] **T32: Run the AC-10 and AC-12 experiments (Threshold 101 on Core.Tests) and record them**
  - **Behavior**: Run the verify command, which does the following in the background (about 20 minutes):
    1. Back up the Core.Tests csproj and set `<Threshold>101</Threshold>`.
    2. Run the full `Default` build. It must fail at the threshold check with exactly 9 unique Core.Tests violations, one per metric per TFM, each naming the project, TFM, metric, actual and `101%`.
    3. Confirm that `index.html` still exists for all 5 projects (the reports survived the failing check).
    4. Restore the csproj.

    Then append `## FR-5 Threshold-check experiments` with a `### AC-10 and AC-12` subsection: the command, the 9 violation lines, the 5 report paths and the date. T33 and T34 add subsections under this heading.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The build fails.
    - There are 9 unique `… below the required threshold of 101%` lines for Core.Tests.
    - All 5 reports exist.
    - The csproj is restored.
    - The section is recorded.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the FR-5 section
  - **RALPH-VERIFY**: `p=test/Paramore.Fences.Core.Tests/Paramore.Fences.Core.Tests.csproj; cp "$p" "$p.bak" && perl -pi -e 's{<Threshold>100</Threshold>}{<Threshold>101</Threshold>}' "$p" && dotnet tool restore && o=$(NO_COLOR=1 dotnet cake.cs -- --target=Default --configuration=Release 2>&1); rc=$?; mv "$p.bak" "$p"; [ $rc -ne 0 ] && [ "$(echo "$o" | grep -o 'Paramore.Fences.Core.Tests (net[0-9.]*): [a-z]* coverage of .* is below the required threshold of 101%' | sort -u | wc -l | tr -d ' ')" -eq 9 ] && ls artifacts/coverage-reports/Paramore.Fences.{Core.Tests,Extensions.Tests,RateLimiting.Tests,Testing.Tests,Specs}/index.html >/dev/null && awk '/^## FR-5 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md | grep -q 'AC-12' && git diff --quiet -- "$p"`
  - **Squash group**: B
  - **References**: ralph-tasks.md § Spec Context (conventions 4, 10, 14); requirements FR-4, FR-5, AC-10, AC-12; ADR "Implementation Approach" step 6 (AC-12, AC-10)

- [x] **T33: Run the AC-13 rounding and AC-15 "covered assembly absent" experiments and record them**
  - **Behavior**: Use the isolated check against T32's Release coverage files. If those files are missing, regenerate them with a full `Default` run in the background. In one command:
    1. Set the `line-rate` of Core.Tests' net8.0 `Paramore.Fences.Core` package to 0.99996 (99.996%). The run must fail and print `line coverage of 100% … below the required threshold of 100%`, because the comparison uses the unrounded value. 0.99996 is used rather than 0.99995 because 0.99995 is not exact in binary and can format as `99.99`.
    2. Rename the `Paramore.Fences` package in Specs' net8.0 file. The run must report `Paramore.Fences.Specs (net8.0): no coverage data was found for assembly 'Paramore.Fences'.`
    3. Restore both files.

    Append a `### AC-13 rounding and AC-15 assembly absent` subsection at the end of pr-notes, under the FR-5 section (it is the last `##` section), with the output lines.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The 99.996 figure fails and prints `100%`.
    - The renamed package gives the "no coverage data" violation.
    - The exit code is non-zero, and the files are restored.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the subsection
  - **RALPH-VERIFY**: `f=artifacts/coverage/Paramore.Fences.Core.Tests/coverage.net8.0.xml; g=artifacts/coverage/Paramore.Fences.Specs/coverage.net8.0.xml; cp "$f" "$f.bak" && cp "$g" "$g.bak" && perl -pi -e 's{<package\b[^>]*\bname="Paramore\.Fences\.Core"[^>]*>}{my $t=$&; $t=~s/\bline-rate="[^"]*"/line-rate="0.99996"/; $t}ge' "$f" && perl -pi -e 's{(<package\b[^>]*\bname=")Paramore\.Fences(")}{${1}Renamed.Assembly$2}g' "$g" && o=$(NO_COLOR=1 dotnet cake.cs -- --target=__VerifyCoverageThresholds --exclusive 2>&1); rc=$?; mv "$f.bak" "$f"; mv "$g.bak" "$g"; [ $rc -ne 0 ] && echo "$o" | grep -qF "Paramore.Fences.Core.Tests (net8.0): line coverage of 100% for 'Paramore.Fences.Core' is below the required threshold of 100%." && echo "$o" | grep -qF "Paramore.Fences.Specs (net8.0): no coverage data was found for assembly 'Paramore.Fences'." && awk '/^## FR-5 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md | grep -q 'AC-13'`
  - **Squash group**: B
  - **References**: ralph-tasks.md § Spec Context; requirements FR-5 (rounding and "covered assembly absent" boundaries), AC-13, AC-15; ADR decision-ladder rows 5 and 7; ADR "Implementation Approach" step 6

- [x] **T34: Run the AC-15 "empty file" and "no `<Threshold>`" experiments and record them**
  - **Behavior**: Use the isolated check against the existing Release coverage files (regenerate them as in T33 if they are missing). In one command:
    1. Truncate RateLimiting.Tests' net10.0 file to zero bytes. The check must fail at the XML load (`Root element is missing` / `XmlException`). Restore the file.
    2. Remove the `<Threshold>` line from the RateLimiting.Tests csproj. The check must pass, print no `Paramore.Fences.RateLimiting.Tests (` line, and print figures for the other four projects. Restore the csproj.

    Append a `### AC-15 empty file and no threshold` subsection under FR-5.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The empty file causes a non-zero exit with an XML-load error.
    - With no threshold, RateLimiting is skipped, the other 4 projects are checked, and the exit code is 0.
    - Both files are restored.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the subsection
  - **RALPH-VERIFY**: `f=artifacts/coverage/Paramore.Fences.RateLimiting.Tests/coverage.net10.0.xml; p=test/Paramore.Fences.RateLimiting.Tests/Paramore.Fences.RateLimiting.Tests.csproj; cp "$f" "$f.bak" && : > "$f" && o1=$(NO_COLOR=1 dotnet cake.cs -- --target=__VerifyCoverageThresholds --exclusive 2>&1); r1=$?; mv "$f.bak" "$f"; cp "$p" "$p.bak" && perl -ni -e 'print unless m{<Threshold>}' "$p" && o2=$(NO_COLOR=1 dotnet cake.cs -- --target=__VerifyCoverageThresholds --exclusive 2>&1); r2=$?; mv "$p.bak" "$p"; [ $r1 -ne 0 ] && echo "$o1" | grep -qiE 'Root element is missing|XmlException' && [ $r2 -eq 0 ] && ! echo "$o2" | grep -q 'Paramore.Fences.RateLimiting.Tests (' && [ "$(echo "$o2" | grep -oE 'Paramore\.Fences\.[A-Za-z.]+ \(net[0-9.]+\): line=' | sed 's/ (.*//' | sort -u | wc -l | tr -d ' ')" -eq 4 ] && awk '/^## FR-5 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md | grep -q 'no threshold' && git diff --quiet -- "$p"`
  - **Squash group**: B
  - **References**: ralph-tasks.md § Spec Context; requirements FR-5 ("unreadable coverage file" and "no `<Threshold>`" boundaries), AC-15; ADR decision-ladder rows 1 and 4; ADR "Implementation Approach" step 6

- [x] **T35: Run `MutationTestsCore` locally once, without `MinVerSkip`, and record the FR-9 finding**
  - **Behavior**:
    1. Read the issue with `gh issue view 3094 --repo stryker-mutator/stryker-net` (read-only) to learn its symptom.
    2. Run `dotnet tool restore && env -u MinVerSkip NO_COLOR=1 dotnet cake.cs -- --target=MutationTestsCore` in the background. It takes about 30–60+ minutes. Note that `__Setup` cleans `artifacts/`.
    3. Interpret the result:
       - **It completes:** local runs are not affected.
       - **It fails with the stryker-net#3094 symptom (MinVer during Stryker's MTP run):** local runs are affected. Re-run with `MinVerSkip=true` to confirm the run then completes (AC-23).
       - **It fails for another reason, or makes no progress for 75 minutes (#46-style hang):** record that, then **stop**. FR-9 needs a human.
    4. Append `## FR-9 Local mutation run (stryker-net#3094)`. It holds the line `Local runs affected by stryker-net#3094: yes` or `…: no`, the command(s), the date, the full HEAD SHA, the machine (macOS and `uname -m`), the duration, the `The final mutation score is …` line, and the note "local score is FR-9 evidence only; FR-14 compares CI with CI". If the answer is yes, also record that divergence 3 applies (T36).
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - The section exists with an explicit yes or no and a mutation score.
    - A Stryker HTML report exists under `artifacts/mutation-report/`.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the FR-9 section
  - **RALPH-VERIFY**: `s=$(awk '/^## FR-9 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md 2>/dev/null); echo "$s" | grep -qE 'Local runs affected by stryker-net#3094: (yes|no)' && echo "$s" | grep -qi 'mutation score' && find artifacts/mutation-report -name 'mutation-report.html' | grep -q .`
  - **Squash group**: C
  - **References**: ralph-tasks.md § Spec Context (conventions 4, 9, 12); requirements FR-9 ("Locally" bullets), FR-14 ("Like for like"), A-6, AC-23; ADR "Why is the local `MinVerSkip` step documentation only…" and divergence table row 3; `cake.cs` (`MutationTestsCore`, `RunMutationTests`); `eng/stryker-config.json`

- [x] **T36: If FR-9 found local runs affected, document `MinVerSkip=true` for local mutation runs (divergence 3); otherwise nothing**
  - **Behavior**: Read the FR-9 section.
    - **If it says `no`,** the verify already passes. Return ALREADY_COMPLETE with no edits. This task is conditional by design.
    - **If it says `yes`,** add a short note in two places: in `CONTRIBUTING.md`'s Mutation Testing section (from about line 219) and in `.agent_instructions/testing.md`'s "Mutation testing" section (from about line 94).
      - The note says to set `MinVerSkip=true` for local mutation runs (for example `MinVerSkip=true dotnet cake.cs -- --target=MutationTestsCore`, or `$env:MinVerSkip='true'` before `./build.ps1 -Target MutationTestsCore`).
      - It links `https://github.com/stryker-mutator/stryker-net/issues/3094`.
      - It notes that CI already sets this in `mutation-tests.yml`.
    - Do not change `cake.cs`.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - Either FR-9 says `no`, or both documents carry `MinVerSkip=true` and the issue link.
  - **Implementation files**:
    - `CONTRIBUTING.md`, `.agent_instructions/testing.md` - the note (only if affected)
  - **RALPH-VERIFY**: `awk '/^## FR-9 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md | grep -q 'affected by stryker-net#3094: no' || ( grep -q 'MinVerSkip=true' CONTRIBUTING.md && grep -q 'stryker-net/issues/3094' CONTRIBUTING.md && grep -q 'MinVerSkip=true' .agent_instructions/testing.md && grep -q 'stryker-net/issues/3094' .agent_instructions/testing.md && ! grep -q 'MinVerSkip' cake.cs )`
  - **Squash group**: C
  - **References**: ralph-tasks.md § Spec Context; requirements FR-9 ("Affected" bullet), FR-11 ("Mutation testing docs" bullet), NFR-7 item 3, AC-23; ADR "Implementation Approach" step 3 (last bullet); `specs/001-port-xunit-v3/pr-notes.md` (FR-9 section)

- [x] **T37: Update every `dotnet test` invocation in `CONTRIBUTING.md` for the MTP runner**
  - **Behavior**:
    - `CONTRIBUTING.md:48` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests`.
    - `:334` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests`.
    - `:337` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests --filter-class "*CircuitBreaker*"`, and its comment is adjusted, for example "# Tests in matching classes, by filter".
    - `:340` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests --framework net10.0`.
    - The bare `dotnet test` at `:331` stays.
    - Change no other text, and search the whole file for any further invocation.
    - The verify runs every distinct invocation in the file, including the bare one, which takes several minutes, so run it in the background.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - No positional project path and no `FullyQualifiedName~` remain.
    - The `--filter-class` form is present.
    - Every invocation in the file exits 0 from the repo root (AC-26).
  - **Implementation files**:
    - `CONTRIBUTING.md` - lines 48, 334, 337, 340
  - **RALPH-VERIFY**: `! grep -nE 'dotnet test \./|FullyQualifiedName~' CONTRIBUTING.md && grep -qF 'dotnet test --project ./test/Paramore.Fences.Core.Tests --filter-class "*CircuitBreaker*"' CONTRIBUTING.md && grep -ohE 'dotnet test.*' CONTRIBUTING.md | cut -d "$(printf '\140')" -f1 | sed 's/[[:space:]]*$//' | sort -u | while read -r c; do echo "== $c"; eval "$c" < /dev/null || exit 1; done`
  - **Squash group**: C
  - **References**: ralph-tasks.md § Spec Context (conventions 4, 5); requirements FR-11, AC-26; ADR "Why `--filter-class` replaces `--filter "FullyQualifiedName~…"`?" and "Implementation Approach" step 3

- [x] **T38: Update every `dotnet test` invocation in `.agent_instructions/build_and_development.md` for the MTP runner**
  - **Behavior**:
    - `:62` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests`.
    - `:65` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests --filter-class "*CircuitBreaker*"`, with its comment adjusted.
    - `:69` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests --framework net10.0`.
    - The bare `dotnet test` at `:59` stays. T37 already ran it, so the verify skips it.
    - Search the whole file for any further invocation.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - No positional project path and no `FullyQualifiedName~` remain.
    - Every non-bare invocation exits 0.
  - **Implementation files**:
    - `.agent_instructions/build_and_development.md` - lines 62, 65, 69
  - **RALPH-VERIFY**: `! grep -nE 'dotnet test \./|FullyQualifiedName~' .agent_instructions/build_and_development.md && grep -qF -- '--filter-class "*CircuitBreaker*"' .agent_instructions/build_and_development.md && grep -ohE 'dotnet test.*' .agent_instructions/build_and_development.md | cut -d "$(printf '\140')" -f1 | sed 's/[[:space:]]*$//' | grep -vx 'dotnet test' | sort -u | while read -r c; do echo "== $c"; eval "$c" < /dev/null || exit 1; done`
  - **Squash group**: C
  - **References**: ralph-tasks.md § Spec Context; requirements FR-11, AC-26; ADR "Implementation Approach" step 3

- [x] **T39: Update the `dotnet test` invocation and the `--filter` advice in `.agent_instructions/testing.md` for MTP**
  - **Behavior**:
    - `:122` becomes `dotnet test --project ./test/Paramore.Fences.Core.Tests --framework net10.0 --filter-class "*CircuitBreaker*"`.
    - `:62` currently says "re-run that test on its own with `--filter`". VSTest `--filter` no longer works under MTP, so it becomes "with `--filter-method`".
    - Search the whole file for any further invocation.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - No positional path, no `FullyQualifiedName~` and no bare `` `--filter` `` advice remain.
    - The invocation exits 0.
  - **Implementation files**:
    - `.agent_instructions/testing.md` - lines 62, 122
  - **RALPH-VERIFY**: `! grep -nE 'dotnet test \./|FullyQualifiedName~' .agent_instructions/testing.md && ! grep -qE 'with .--filter.\.' .agent_instructions/testing.md && grep -q 'with .--filter-method' .agent_instructions/testing.md && grep -ohE 'dotnet test.*' .agent_instructions/testing.md | cut -d "$(printf '\140')" -f1 | sed 's/[[:space:]]*$//' | sort -u | while read -r c; do echo "== $c"; eval "$c" < /dev/null || exit 1; done`
  - **Squash group**: C
  - **References**: ralph-tasks.md § Spec Context; requirements FR-11, AC-26; ADR "Implementation Approach" step 3

- [x] **T40: Update the coverage and property-testing tool descriptions in the FR-11 documents**
  - **Behavior**:
    - `CONTRIBUTING.md:236`: "Coverage is collected by coverlet…" now describes the Microsoft CodeCoverage extension (`Microsoft.Testing.Extensions.CodeCoverage`), which writes `artifacts/coverage/<Project>/coverage.<tfm>.xml` per project per TFM, including net481 on Windows. HTML comes from the `reportgenerator` tool into `artifacts/coverage-reports/<Project>/`. The thresholds are enforced per TFM by Cake's `__VerifyCoverageThresholds`. The sentence about the CI artefact, the job summary and no external service stays.
    - `.agent_instructions/testing.md:20`: `FsCheck.Xunit` becomes `FsCheck.Xunit.v3`.
    - `.agent_instructions/testing.md:22`: the coverage row reads Microsoft CodeCoverage (`Microsoft.Testing.Extensions.CodeCoverage`), "collected on every run, per TFM; HTML via the `reportgenerator` tool".
    - `.agent_instructions/dependency_management.md:42`: "coverlet" in the test-stack sentence becomes the current stack (for example "xUnit v3, Shouldly, NSubstitute or the coverage extension").
    - Leave `.github/wordlist.txt` alone.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - No `coverlet` remains in the four documents.
    - No `FsCheck.Xunit` remains that is not followed by `.v3`.
    - testing.md names FsCheck.Xunit.v3, reportgenerator and the CodeCoverage package.
  - **Implementation files**:
    - `CONTRIBUTING.md` (line 236), `.agent_instructions/testing.md` (lines 20, 22), `.agent_instructions/dependency_management.md` (line 42)
  - **RALPH-VERIFY**: `! grep -niE 'coverlet' CONTRIBUTING.md .agent_instructions/testing.md .agent_instructions/build_and_development.md .agent_instructions/dependency_management.md && ! grep -nE 'FsCheck\.Xunit([^.]|$)' CONTRIBUTING.md .agent_instructions/testing.md .agent_instructions/build_and_development.md .agent_instructions/dependency_management.md && grep -qF 'FsCheck.Xunit.v3' .agent_instructions/testing.md && grep -q 'reportgenerator' .agent_instructions/testing.md && grep -q 'Microsoft.Testing.Extensions.CodeCoverage' .agent_instructions/testing.md`
  - **Squash group**: C
  - **References**: ralph-tasks.md § Spec Context; requirements FR-11 ("Tooling descriptions" and wordlist bullets), AC-26; ADR "Implementation Approach" step 3

- [x] **T41: Port f35bc00d's three Specs edits (exactly three files)**
  - **Behavior**: Apply f35bc00d's `test/` diff, rebranded:
    - `ContextDictionarySpecs.cs`: `Context context = new Context("operation-key");` becomes `var context = new Context("operation-key");`.
    - `PolicyOverloadSmokeSpecs.cs`: the async smoke test gains upstream's three calls with `continueOnCapturedContext` `true`: `ExecuteAsync` returning 53, and the two `ExecuteAndCaptureAsync` calls (`TaskHelper.EmptyTask`, and 59).
    - `Timeout/TimeoutOverloadSmokeSpecs.cs`: add `private static Context CreateContext() => new("operation-key");` at the end of the class, and replace all 8 `new Context("operation-key")` arguments with `CreateContext()`.
    - Touch no other file.
    - Expected status: GREEN.
  - **Test file**: `test/Paramore.Fences.Specs/ContextDictionarySpecs.cs`, `test/Paramore.Fences.Specs/PolicyOverloadSmokeSpecs.cs`, `test/Paramore.Fences.Specs/Timeout/TimeoutOverloadSmokeSpecs.cs`
  - **Test should verify**:
    - The working-tree changes outside `specs/` are exactly these three files.
    - Each file equals f35bc00d's file, rebranded.
    - Specs builds analyser-clean and passes on all three macOS TFMs (AC-28).
  - **Implementation files**:
    - none beyond the three test files
  - **RALPH-VERIFY**: `[ "$(git diff --name-only -- . ':!specs' | sort | tr '\n' ' ')" = "test/Paramore.Fences.Specs/ContextDictionarySpecs.cs test/Paramore.Fences.Specs/PolicyOverloadSmokeSpecs.cs test/Paramore.Fences.Specs/Timeout/TimeoutOverloadSmokeSpecs.cs " ] && ( for p in ContextDictionarySpecs.cs PolicyOverloadSmokeSpecs.cs Timeout/TimeoutOverloadSmokeSpecs.cs; do diff <(git show f35bc00d:test/Polly.Specs/$p | sed -e 's/Polly/Paramore.Fences/g' -e 's#App-vNext/Paramore.Fences#App-vNext/Polly#g') test/Paramore.Fences.Specs/$p || exit 1; done ) && dotnet build test/Paramore.Fences.Specs -warnaserror && dotnet test --project test/Paramore.Fences.Specs`
  - **Squash group**: E
  - **References**: ralph-tasks.md § Spec Context; requirements FR-12, AC-28, FR-13; ADR "Implementation Approach" step 5; `git show f35bc00d -- test/`. net481 is verified by Windows CI.

- [x] **T42: FR-15 reproducibility: run the full build 3 times on the final code commit and tabulate the figures and test counts**
  - **Behavior**:
    1. Run `dotnet tool restore`, then run `NO_COLOR=1 dotnet cake.cs -- --target=Default --configuration=Release` three times, one after another, each in the background (about 20 minutes each). Save each log outside the repository, for example `$TMPDIR/fr15-run<N>.log`.
    2. From each log, take the 15 `<Project> (<tfm>): line=…%, branch=…%, method=…%` lines, and the number of tests executed per project per TFM from the MTP output.
       - If the output gives only per-project totals, record those, mark the per-TFM count as unavailable, and say so.
    3. If any run fails, **stop** with the reason. Do not change code after E.
    4. Append `## FR-15 Coverage reproducibility`. It holds:
       - the full HEAD SHA (`git rev-parse HEAD`), the date, the OS (macOS and `uname -m`) and each run's duration;
       - a table with one row per project and TFM: `| Paramore.Fences.Core.Tests | net10.0 | run1 l/b/m | run1 tests | run2 l/b/m | run2 tests | run3 l/b/m | run3 tests | identical? |`;
       - the line `Identical across all 3 runs: yes` or `…: no`;
       - if the answer is no, every differing project, TFM, metric and set of values, for the #14 update.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - There are 15 project/TFM rows.
    - The identical verdict is explicit.
    - The section names the current HEAD SHA.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the FR-15 section
  - **RALPH-VERIFY**: `s=$(awk '/^## FR-15 /{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md 2>/dev/null); [ "$(echo "$s" | grep -cE '^\| Paramore\.Fences\.[A-Za-z.]+ \| net(8|9|10)\.0 \|')" -ge 15 ] && echo "$s" | grep -qE 'Identical across all 3 runs: (yes|no)' && echo "$s" | grep -q "$(git rev-parse HEAD)"`
  - **Squash group**: E
  - **References**: ralph-tasks.md § Spec Context (conventions 4, 9, 15); requirements FR-15, NFR-5, A-1, A-6, AC-33; ADR "Implementation Approach" step 6 (FR-15)

- [x] **T43: Run the static configuration acceptance checks and record them**
  - **Behavior**: Run the verify's checks:
    - AC-7: `eng/Test.targets` has no coverlet or ReportGenerator.
    - AC-4: the `Directory.Packages.props` diff touches only FR-2 packages, and the `global.json` diff is only the test runner.
    - AC-22: the Stryker config diff is only `test-runner`; the workflow carries `MinVerSkip` with the issue link; `cake.cs` has no `MinVerSkip`.
    - AC-25: the workflows' coverage paths point only at `artifacts/coverage-reports`.
    - AC-29: no `CHANGELOG.md` change.
    - Unchanged files: `build.yml`, `build.ps1`, `dependabot.yml`, `wordlist.txt`.

    Append `## Static checks: configuration`, listing each AC with its result and the merge-base SHA. Also note that the `.config/dotnet-tools.json` part of AC-4 is reviewed by hand (T45).
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - Every check passes.
    - The section exists and mentions AC-4.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the section
  - **RALPH-VERIFY**: `mb=$(git merge-base HEAD origin/main); ! grep -qE 'coverlet|CollectCoverage|CoverletOutputFormat|ReportGenerator' eng/Test.targets && [ -z "$(git diff $mb -- Directory.Packages.props | grep -E '^[-+] ' | grep -vE 'Include="(coverlet\.msbuild|ReportGenerator|JunitXml\.TestLogger|xunit|FsCheck\.Xunit|FsCheck\.Xunit\.v3|xunit\.v3\.mtp-v2|xunit\.v3\.assert|Microsoft\.Testing\.Platform|Microsoft\.Testing\.Extensions\.CodeCoverage|Microsoft\.NET\.Test\.Sdk)"')" ] && [ -z "$(git diff $mb -- global.json | grep -E '^[-+] ' | grep -vE '"test": \{|"runner": "Microsoft\.Testing\.Platform"|^[-+] *\},?$')" ] && [ -z "$(git diff $mb -- eng/stryker-config.json | grep -E '^[-+] ' | grep -vF '"test-runner": "mtp",')" ] && grep -q 'MinVerSkip: true' .github/workflows/mutation-tests.yml && grep -qF 'stryker-net/issues/3094' .github/workflows/mutation-tests.yml && ! grep -q 'MinVerSkip' cake.cs && [ -z "$(git grep -nE 'coverage-reports|coverage/|codecov' -- .github/workflows/ | grep -v 'artifacts/coverage-reports')" ] && git diff --quiet $mb -- CHANGELOG.md .github/workflows/build.yml build.ps1 .github/dependabot.yml .github/wordlist.txt && awk '/^## Static checks: configuration/{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md | grep -q 'AC-4'`
  - **Squash group**: E
  - **References**: ralph-tasks.md § Spec Context; requirements AC-4, AC-7, AC-22, AC-25, AC-29, FR-2 (error case); ADR "These stay deliberately unchanged"

- [x] **T44: Run the static source and documentation acceptance checks and record them**
  - **Behavior**: Run the verify's checks:
    - AC-2: no Abstractions, no v2 or FsCheck.Xunit references, no coverlet `<Include>`.
    - AC-16: Specs is `96,95,93` and four projects are at `100`.
    - AC-17: 0 added `ExcludeFromCodeCoverage` lines.
    - AC-35: no `.PublicAPI` diff.
    - AC-36: no added Skip, Quarantine or Explicit.
    - AC-20: no FakeItEasy, Moq or FluentAssertions usings; the added `[Fact]`/`[Theory]` method names equal the set 1a80392b adds.
    - AC-26: no positional paths, `FullyQualifiedName~` or coverlet in the FR-11 documents.
    - AC-27: `mdsnippets` gives no Markdown diff.

    Append `## Static checks: source and docs`, listing each AC with its result.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - Every check passes.
    - The section exists and mentions AC-20.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the section
  - **RALPH-VERIFY**: `mb=$(git merge-base HEAD origin/main); ! git grep -q 'Xunit.Abstractions' -- test/ && ! git grep -qE 'Include="(xunit|FsCheck\.Xunit)"' -- test/ eng/ Directory.Packages.props && ! git grep -q '<Include>\[' -- 'test/*.csproj' && grep -q '<Threshold>96,95,93</Threshold>' test/Paramore.Fences.Specs/Paramore.Fences.Specs.csproj && [ "$(git grep -h '<Threshold>100</Threshold>' -- 'test/*.csproj' | wc -l | tr -d ' ')" -eq 4 ] && [ "$(git diff $mb -- src test | grep -cE '^\+[^+].*ExcludeFromCodeCoverage')" -eq 0 ] && git diff --quiet $mb -- 'src/*/.PublicAPI/*' && ! git diff $mb -- test/ | grep -qE '^\+.*(Skip = |"Quarantine"|Explicit = true)' && ! git diff $mb -- test/ | grep -qE '^\+using (FakeItEasy|Moq|FluentAssertions)' && diff <(git diff $mb -- test/ | grep -A3 -E '^\+ *\[(Fact|Theory)' | grep -oE '(void|Task) [A-Za-z0-9_]+\(' | sort -u) <(git show 1a80392b -- test/ | grep -A3 -E '^\+ *\[(Fact|Theory)' | grep -oE '(void|Task) [A-Za-z0-9_]+\(' | sort -u) && ! grep -nE 'dotnet test \./|FullyQualifiedName~|coverlet' CONTRIBUTING.md .agent_instructions/testing.md .agent_instructions/build_and_development.md .agent_instructions/dependency_management.md && dotnet tool restore && dotnet mdsnippets && git diff --quiet -- '*.md' ':!specs' && awk '/^## Static checks: source and docs/{f=1;next}/^## /{f=0}f' specs/001-port-xunit-v3/pr-notes.md | grep -q 'AC-20'`
  - **Squash group**: E
  - **References**: ralph-tasks.md § Spec Context; requirements AC-2, AC-16, AC-17, AC-20, AC-26, AC-27, AC-35, AC-36, NFR-2, NFR-3, NFR-5

- [x] **T45: Check that pr-notes.md has every FR-13 section, and append the human's pre-PR and post-merge checklist**
  - **Behavior**: Confirm these sections exist: `## FR-2 `, `## FR-6 `, `## FR-8 `, `## FR-9 `, `## FR-14 `, `## FR-15 `, `## NFR-4 `, `## Open items`. If one is missing, report which in FAILURE_REASON and return FAILED; do not invent evidence.
    Then append `## Post-merge checklist (for the human)`, written as `- [ ] …` items. It covers:
    - **Loop leftovers:** stop the loop; delete `RALPH_STOP`; review `coverage-gap-report.md` if present.
    - **Squash per FR-13** using each task's Squash group:
      - A → `Port App-vNext/Polly@1a80392b: Update to xunit v3`; then B; then C (D only if one was ever approved); then E → `Port App-vNext/Polly@f35bc00d: .NET 11 preparation (Specs)`, which must touch exactly 3 files (AC-28).
      - `specs/**` bookkeeping stays out of A–E.
    - **Reviews:** an AC-39 review of A, B and E against upstream; a manual AC-4 review of `.config/dotnet-tools.json`.
    - **Open the PR:** push a non-main branch; title in Fences' voice (ADR suggestion: "Move tests to xunit v3 and Microsoft Testing Platform v2, with per-framework coverage thresholds"). The body comes from this file, links #28 and #34, and names both SHAs.
    - **CI checks on the PR:**
      - `build` is green on macOS, Linux and Windows with zero warnings (AC-34).
      - net481 tests pass and net481 coverage meets thresholds on Windows (AC-1, AC-11). A net481 gap follows the decision-3 process by hand.
      - The `coverage-<os>` artefacts each have HTML for 5 projects (AC-24).
      - The job summary has one section per project per TFM (AC-8) and lists no passed tests (AC-24).
    - **FR-14:** five CI scores at or above the T01 baseline; add the scores and the mutants removed by suppressions (count per target, and file) to the PR body. If `mutations-core` hangs (#46), rerun it; after two hung reruns, use the like-for-like local fallback (both SHAs) and comment on #46 (AC-32).
    - **NFR-4:** compare the PR's `build` durations with the T02 medians, and record times and cause if any exceeds 120% (AC-37).
    - **Moving merge base:** re-take the FR-14 baseline if the merge base moves.
    - **FR-2:** re-check which Dependabot PRs for FR-2 rows are open at merge time.
    - **Approval:** Ian's approval before merge (C-5).
    - **After merge:**
      - #28: the 1a80392b entry and the f35bc00d Specs entry link the merged PR.
      - #6: comment that coverlet was removed (it stays closed).
      - PR #19: close it unmerged. The comment says the failing-gate fix is carried and the per-TFM HTML fix deliberately is not.
      - Close each superseded Dependabot PR (for example #39 and #41) with a link.
      - #14: close it with the FR-15 table if the runs were identical; otherwise update it with the differing project, TFM, metric and values.
      - Optionally, advance `polly-upstream` per `.agent_instructions/upstream_sync.md` Step 5.
  - **Test file**: none — verified by RALPH-VERIFY
  - **Test should verify**:
    - All required headings are present.
    - The checklist mentions every housekeeping item and gate.
    - The checklist has at least 10 unchecked items.
  - **Implementation files**:
    - `specs/001-port-xunit-v3/pr-notes.md` - append the checklist
  - **RALPH-VERIFY**: `n=specs/001-port-xunit-v3/pr-notes.md; ( for h in '## FR-2 ' '## FR-6 ' '## FR-8 ' '## FR-9 ' '## FR-14 ' '## FR-15 ' '## NFR-4 ' '## Open items' '## Post-merge checklist'; do grep -q "^$h" "$n" || exit 1; done ) && s=$(awk '/^## Post-merge checklist/{f=1;next}/^## /{f=0}f' "$n") && ( for k in '#28' '#34' '#6' '#19' '#14' '#46' 'Dependabot' 'net481' 'NFR-4' 'FR-14' 'AC-39' 'AC-28' '1a80392b' 'f35bc00d'; do echo "$s" | grep -qF -- "$k" || exit 1; done ) && [ "$(echo "$s" | grep -c '^- \[ \]')" -ge 10 ]`
  - **Squash group**: E
  - **References**: ralph-tasks.md § Spec Context; requirements FR-13 (all bullets), FR-14, FR-15, NFR-4, AC-29, AC-30, AC-31, AC-32, AC-34, AC-37, AC-39; ADR "Implementation Approach" steps 7 and 8; `.agent_instructions/upstream_sync.md` (Steps 4 and 5)

## Notes for the human

**Assumptions**
- **Spec files and squash groups.** Every loop commit stages `ralph-tasks.md`, so `specs/001-port-xunit-v3/**` is treated as bookkeeping outside groups A–E (Spec Context convention 15). Tasks that touch only `specs/` (T01–T03, T11, T32–T35, T42–T45) carry their position's letter. T42–T45 are labelled E only to keep "E comes last" in task order. They put nothing into the E commit, which AC-28 limits to three files.
- **FR-15 needs the final commit,** so T42 runs after the E port (T41). Because nothing in code changes after T41, T42–T45 still describe the final code commit.
- **TheoryData casts are in T19, not earlier.** IDE0004 is `warning` in `eng/analyzers/Stylecop.globalconfig`, which makes it an error under `-warnaserror`. Under xunit v2 the upstream casts are probably redundant, and under v3 they are required. T19 is the one deliberately indivisible task, and its Behavior says why.
- **The coverage gate (T30) sits before B.** Its verify checks for all 15 files explicitly, so it doesn't depend on divergence 1. Any porting-omission fix it makes belongs to A.
- **Verbatim-port verifies rely on a rebrand filter.** At the merge base, `sed 's/Polly/Paramore.Fences/g'` plus restoring `App-vNext/Polly` URLs reproduces Fences' `cake.cs`, `eng/Test.targets` and the FR-7/FR-8 files byte for byte. I checked this against the 1a80392b parent. `eng/stryker-config.json` matches upstream with no filter. `mutation-tests.yml` differs only by `SKIP_FENCES_ANALYZERS`. Most verifies are therefore a full-file `diff` against rebranded upstream, which also covers AC-39.
- **`RALPH_STOP` is used in more places than decision 3.** It also covers the polyfill fallback (T10), a version conflict between FR-2 versions (T19), Cake ignoring `--exclusive` (T24), an inconclusive local Stryker run (T35), and a failing FR-15 run (T42). On a stop, the sub-agent lists no files. That keeps the main agent's scoped `git checkout --` from choking on untracked reports.
- **Unauthorised changes I made.**
  - T39 also changes `.agent_instructions/testing.md:62` ("re-run … with `--filter`"). That line isn't a `dotnet test` invocation, but VSTest `--filter` doesn't work under MTP.
  - T33 uses 0.99996 rather than 0.99995 for the rounding experiment, because 0.99995 is not exact in binary and can format as `99.99`.
- **The NFR-4 baseline** uses `build` runs at or before the merge base ("before the merge base" is ambiguous). T02 records which runs it used.

**Before starting the loop**
- Commit or stash the current working-tree changes: `docs/adr/index.md`, the untracked ADR and the `specs/001-port-xunit-v3/` files. T18 and T44 check `git diff --quiet -- '*.md' ':!specs'`. `.design-approved` and `.adr-list` already exist.
- PowerShell isn't installed, so every `./build.ps1` step uses `dotnet tool restore && dotnet cake.cs -- --target=… --configuration=Release`.
- The full Cake runs (T30, T32, twice-plus in T42) and Stryker (T35) exceed the 10-minute foreground Bash limit. The tasks tell the sub-agent to background them, but expect long iterations: roughly 3+ hours of build time across those tasks.

**Verify commands I could not confirm from here**
- **`--exclusive` on Cake.Sdk 6.3.0** (ADR claim), and `--dryrun` output listing tasks in execution order (T24, T26, T31–T34). T24 stops if `--exclusive` isn't honoured.
- **MTP `dotnet test` forms:** `--project`, `--framework`, `--filter-method "*.X"`, `--filter-class "*X*"`, and a bare `dotnet test` at the root over `Fences.slnx` (T37 runs it). Also that `cake.cs`, which imports `eng/Test.targets`, still compiles after the switch (T19 checks with a dry run).
- **`-p:CollectCoverage=false`** overriding `eng/Test.targets` for the pre-switch VSTest verifies, and the exact VSTest summary format the verifies grep (`Passed!  - Failed: 0, Passed: N`).
- **Package name in Cobertura.** That Microsoft CodeCoverage names each Cobertura `package` after the assembly with the attribute pattern the perl edits assume (T25, T33). That Cake honours `NO_COLOR`.
- **Per-TFM test counts.** Whether the default MTP output gives test counts per project per TFM (FR-15). T42 records per-project totals and flags them if it doesn't.
- **xunit v3 analysers.** Any new xunit v3 analyser warnings (for example xUnit1051) under `-warnaserror`. `eng/analyzers/Test.globalconfig` matches `polly-upstream` and doesn't configure xUnit1051. Upstream builds clean, so none are expected.

**Things the loop may hit that need you**
- **A coverage gap** (T30): read `specs/001-port-xunit-v3/coverage-gap-report.md`. A Fences-specific fix would be group D, which needs approval and an NFR-7 entry.
- **The polyfill fallback** (divergence 2), **an FR-2 version conflict**, **a #46-style Stryker hang** (T35), or **Cake ignoring `--exclusive`**: the ADR's fallback is a local uncommitted edit that the loop must not make.
- **Windows (net481)** is untested locally. CI after your push is its first run, including net481 coverage against `100` and `96,95,93`.
- **Moving merge base:** if you update the branch from `main`, re-run T01's procedure.
