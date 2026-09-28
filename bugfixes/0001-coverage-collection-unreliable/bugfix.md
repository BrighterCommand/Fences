# Bugfix: Coverage collection is unreliable, and blocks the coverlet 10 upgrade

**Linked Issue**: #14
**Status**: Verified

## Symptom

### Symptom 1 — coverlet 6.0.4 loses hit data non-deterministically

Tests pass, then coverlet reports a coverage figure that is wrong for one target framework while
the other TFMs report the correct figure for the same tests, the same commit, and — critically —
**the same IL**. `Paramore.Fences.Core` targets `net8.0;net6.0;netstandard2.0;net472;net462`
(`src/Paramore.Fences.Core/Paramore.Fences.Core.csproj:4`), so the `net8.0`, `net9.0` and `net10.0`
test runs of `Core.Tests` all instrument the *same* `net8.0` build of Core. Divergent numbers across
TFMs are therefore impossible as a matter of code, and can only be a collection defect.

Verified in CI logs:

- Run `33262613610`, attempt 1, job `99127041539` (ubuntu, `Core.Tests`) — 777 tests passed on all
  three TFMs; net10.0 and net8.0 reported `100% / 100% / 100%`; net9.0 reported
  `99.95% / 99.81% / 100%`, failing at
  `coverlet.msbuild.targets(72,5): error : The minimum line coverage is below the specified 100
  [...::TargetFramework=net9.0]`.
- Run `32841065953`, attempt 1, job `97780460065` (ubuntu, `Specs`) — net10.0 and net8.0 reported
  `94.74 / 95.06 / 91.98`; net9.0 reported `0% / 0% / 0%`.
- Run `32841065953`, attempt 1, job `97780460279` (windows, `Core.Tests`) — net9.0 and net8.0 both
  failed with, verbatim:

```
error : Unable to read beyond the end of the stream.
error :    at System.IO.BinaryReader.ReadInt32()
error :    at Coverlet.Core.Coverage.CalculateCoverage() in /_/src/coverlet.core/Coverage.cs:line 420
error :    at Coverlet.Core.Coverage.GetCoverageResult() in /_/src/coverlet.core/Coverage.cs:line 160
error :    at Coverlet.MSbuild.Tasks.CoverageResultTask.Execute() in /_/src/coverlet.msbuild.tasks/CoverageResultTask.cs:line 87
```

`Coverage.CalculateCoverage()` is where coverlet opens each module's **hits file** with a
`BinaryReader`. So the three visible shapes — `0%`, `99.95%`, and the stream exception — are the
same defect at three depths: hits file never written, hits file partially written, hits file
truncated before the first `Int32`.

**A finding that contradicts the issue's leading suspicion:** in that Windows job the three TFMs ran
strictly **sequentially** — net10.0 finished at `11:17:58.42`, net9.0 started at `11:17:59.88`,
net8.0 at `11:18:13.22` — because `Core.Tests` sets `TestTfmsInParallel=false` on Windows
(`test/Paramore.Fences.Core.Tests/Paramore.Fences.Core.Tests.csproj:5`). Two of the three sequential
runs still lost their hits file. Concurrency is therefore **not necessary** to reproduce the failure.

On Linux the TFMs *do* run concurrently (same job, `Core.Tests` hosts started `16:24:05.59`,
`16:24:06.38`, `16:24:06.40`), so concurrency is present in some failures and absent in others.

Reproduction: none deterministic. Every occurrence cleared on `gh run rerun --failed`.

### Symptom 2 — coverlet 10.0.1 counts more branches; the gate then fails

Deterministic, and broader than the issue records. PR #6 (`Directory.Packages.props` only,
6.0.4 → 10.0.1), run `33154073408`, failed on **all three OS**. Job `98792578745` (ubuntu) shows
`Paramore.Fences.Specs` failing on **all three TFMs** with identical figures:

```
| Paramore.Fences | 94.74% | 93.99% | 91.98% |
error : The minimum branch coverage is below the specified 94
   at Coverlet.MSbuild.Tasks.CoverageResultTask.Execute() in /_/src/legacy/coverlet.msbuild.tasks/CoverageResultTask.cs:line 216
```

Line coverage (`94.74%`) and method coverage (`91.98%`) are **byte-identical** to 6.0.4; only branch
coverage moved, `95.06% → 93.99%`. Under 6.0.4 the Specs run counts `branches-valid="1499"`,
`branches-covered="1425"` (`test/Paramore.Fences.Specs/coverage.net10.0.cobertura.xml:2`). A drop to
93.99% with line/method unchanged is on the order of 15–20 branch points appearing or losing
coverage. The build aborts at Specs, so the `Core.Tests` figure quoted in the issue comes from a
later phase of the same job set.

Also note the stack frame path: coverlet 10 has moved the MSBuild integration to `src/legacy/`, and
the targets now resolve from `coverlet.msbuild/10.0.1/buildMultiTargeting/`. Upstream has demoted
the integration this repo depends on.

## Suspected Location

### Symptom 1

- `eng/Test.targets:26-30` — the only place coverage is switched on. `CollectCoverage=true` selects
  coverlet's **in-process MSBuild collector**; `CoverletOutputFormat=cobertura`; `ExcludeByAttribute`
  set. No `CoverletOutput`, no `ThresholdType`, no `ThresholdStat`, no `MergeWith` anywhere in the
  repo (grep for `CoverletOutput`/`ThresholdType`/`MergeWith` across `*.props`/`*.targets`/`*.csproj`
  /`*.yml` returns only `eng/Test.targets:28`).
- Therefore `CoverletOutput` falls back to the package default,
  `$([MSBuild]::EnsureTrailingSlash('$(MSBuildProjectDirectory)'))`
  (`~/.nuget/packages/coverlet.msbuild/6.0.4/build/coverlet.msbuild.props:15`). **Answering the crux
  question directly: the output *directory* is shared across TFMs, but the *filename* is not** —
  `GenerateCoverageResult` passes `CoverletMultiTargetFrameworksCurrentTFM`
  (`~/.nuget/packages/coverlet.msbuild/6.0.4/build/coverlet.msbuild.targets:70,79`), which makes
  coverlet emit `coverage.<tfm>.cobertura.xml`. Confirmed on disk:
  `test/Paramore.Fences.Core.Tests/coverage.net8.0.cobertura.xml`, `coverage.net9.0.cobertura.xml`,
  `coverage.net10.0.cobertura.xml`. **Coverlet's own reports do not collide.**
- `eng/Test.targets:36` — `ReportGeneratorTargetDirectory` is
  `artifacts/coverage-reports/$(MSBuildProjectName)`, keyed by project only. `eng/Test.targets:73`
  invokes ReportGenerator into it from every inner build, distinguished only by
  `Tag="$(TargetFramework)"`. **This directory *is* shared and *is* clobbered.**
- `eng/Test.targets:42-70,83` — a hand-rolled `WriteLinesToFileWithRetry` task that retries
  `File.AppendAllLines` three times on `IOException` when appending to `$(GITHUB_STEP_SUMMARY)`.
  This is pre-existing evidence that concurrent inner builds contending for a shared file was
  already observed, and papered over rather than removed.
- `Microsoft.TestPlatform.CrossTargeting.targets:43,53` (in the installed SDK,
  `10.0.400/Current/Microsoft.Common.CrossTargeting.targets/ImportAfter/`) —
  `_TestTfmsInParallel = ValueOrDefault($(TestTfmsInParallel), $(BuildInParallel))`, fed to
  `MSBuild ... BuildInParallel=`. `BuildInParallel` defaults to `true`
  (`Microsoft.Common.CurrentVersion.targets:378`), so **TFM-parallel test execution is on by
  default** for every test project.
- `test/Paramore.Fences.Core.Tests/Paramore.Fences.Core.Tests.csproj:5` — the single, Windows-only,
  one-project opt-out of that default. Inherited verbatim from Polly (`git log -S TestTfmsInParallel`
  → `520ee38c "Update to .NET 9 SDK (#2003)"`). No other test project sets it; no OS other than
  Windows sets it.
- `cake.cs:151-162` — `__RunTests` iterates test projects sequentially and calls
  `DotNetTest(..., NoBuild = true)`. `NoBuild` selects coverlet's `InstrumentModulesNoBuild` path
  (`coverlet.msbuild.targets:58-61`). No `--framework`, no explicit `-m`.
- `.github/workflows/build.yml:99` — CI entry point is `./build.ps1`; `build.yml:103-109` uploads
  `./artifacts/coverage-reports` (the clobbered directory) as the `coverage-<os>` artifact.

### Symptom 2

- `Directory.Packages.props:5` — `<PackageVersion Include="coverlet.msbuild" Version="6.0.4" />`;
  PR #6 changes only this line. `coverlet.collector` is referenced nowhere in the repo.
- Threshold declarations, all with `ThresholdType` unset and therefore defaulting to
  `line,branch,method` (`coverlet.msbuild.props:17`):
  - `test/Paramore.Fences.Core.Tests/Paramore.Fences.Core.Tests.csproj:8` — `<Threshold>100</Threshold>`
  - `test/Paramore.Fences.Extensions.Tests/Paramore.Fences.Extensions.Tests.csproj:7` — `<Threshold>100</Threshold>`
  - `test/Paramore.Fences.RateLimiting.Tests/Paramore.Fences.RateLimiting.Tests.csproj:7` — `<Threshold>100</Threshold>`
  - `test/Paramore.Fences.Testing.Tests/Paramore.Fences.Testing.Tests.csproj:7` — `<Threshold>100</Threshold>`
  - `test/Paramore.Fences.Specs/Paramore.Fences.Specs.csproj:7` — `<Threshold>94,94,91</Threshold>`
- Constructs in `Paramore.Fences.Core` matching the coverlet 10 branch-counting fixes named in the
  issue:
  - **Pattern combinators** — `src/Paramore.Fences.Core/Utils/DefaultPredicates.cs:6`:
    `args.Outcome.Exception is { } and not OperationCanceledException`. A recursive-pattern
    `and`/`not` combinator, exactly the `is`/`and` case.
  - **`finally` in `async` methods** — `src/Paramore.Fences.Core/ResiliencePipeline.Async.cs:90,99,119`
    (`public async ValueTask ExecuteAsync<TState>`, `try` at 99, `finally` at 119); also
    `ResiliencePipeline.Async.cs:161`, `ResiliencePipeline.AsyncT.cs:149,190`,
    `Hedging/HedgingResilienceStrategy.cs:80`, `Utils/Pipeline/ExecutionTrackingComponent.cs:33`,
    `CircuitBreaker/CircuitBreakerManualControl.cs:84,114`. Sixteen `finally` blocks in Core, most on
    async paths.
  - **`IAsyncEnumerable` / `await foreach` / `await using`** — grep returns **zero** hits in
    `Paramore.Fences.Core` and `Paramore.Fences`. That coverlet 10 fix cannot be in play here.
  - Switch expressions with discard arms (`Retry/RetryHelper.cs:122,131`,
    `CircuitBreaker/Controller/CircuitStateController.cs:144,326`, `PredicateBuilder.TResult.cs:126`,
    `Hedging/Controller/HedgingExecutionContext.cs:54,172`) are a further candidate.
- Provenance of the gate: `<Threshold>100</Threshold>` was introduced by Polly upstream in
  `a57742fa6` (`src/Polly.Core.Specs/Polly.Core.Specs.csproj`, 2023-03-13); the Specs `94,94,91` by
  `3fb089717` (2025-02-21) — i.e. already ratcheted once to whatever the tool happened to report.
  **No Fences document records a coverage threshold at all**: `.agent_instructions/testing.md:22`
  says only "Coverage | coverlet | Collected on every non-.NET-Framework run, reported by
  ReportGenerator", and `CONTRIBUTING.md:236-238` says only that coverage is published as an artefact
  with "no external coverage service". The documented 100% threshold
  (`.agent_instructions/testing.md:96`, `CONTRIBUTING.md:222`) is **Stryker mutation score**, a
  different gate. The coverage gate is undocumented inherited configuration.

## Root-Cause Hypothesis

### Symptom 1

**H1 (primary) — the instrumented test host does not reliably produce a complete hits file, and
coverlet.msbuild reads it unguarded.**

Mechanism: with `CollectCoverage=true` and no data collector, coverlet rewrites the assemblies in the
test output directory so each sequence point increments an in-memory array, and registers a
process-exit handler that serialises that array to a hits file. `CoverageResultTask` then reads that
file back with a `BinaryReader` after `dotnet test` returns. If the VSTest test host is torn down
before the handler completes, the file is absent, short, or truncated. Coverlet does not distinguish
"no hits recorded" from "hits file lost": an absent/empty array yields `0%`, a partially flushed
array yields a plausible-but-wrong figure such as `99.95%`, and a file truncated before its length
prefix yields `Unable to read beyond the end of the stream` at `BinaryReader.ReadInt32()`. All three
observed shapes are the same failure at different truncation points. Predicts: failures cluster at
process shutdown, are independent of test outcome, and are unaffected by serialising TFMs.

**H2 (contributory, refuted as sole cause) — TFM inner builds race.** The issue suggests "the three
target frameworks are racing over shared coverage output paths". Partially **refuted already**:
coverlet's per-TFM report filenames do not collide, and the Windows `Core.Tests` failure occurred
with TFMs strictly sequential. What *is* genuinely shared is `ReportGeneratorTargetDirectory`
(`eng/Test.targets:36`) and `$(GITHUB_STEP_SUMMARY)` (`eng/Test.targets:83`) — so the **uploaded
`coverage-<os>` artefact and the job step summary are unreliable**, but that is a separate (real)
defect that cannot move coverlet's threshold verdict. Concurrency may still raise the *rate* of H1 by
adding shutdown-time CPU pressure.

**Restated from the issue, UNVERIFIED — to be proven or refuted in `/bugfix:confirm`:**

- *"Switching to the `coverlet.collector` datacollector (`--collect:"XPlat Code Coverage"`) may make
  symptom 1 disappear regardless of version."* Plausible under H1, because the datacollector is
  flushed by the test platform through the datacollector protocol before the host is allowed to exit,
  rather than by a best-effort process-exit hook. **UNVERIFIED.**
- *"Whether the three target frameworks are racing over shared coverage output paths."* **Partially
  refuted above; the residual shared-path defect at `eng/Test.targets:36,83` is real but is not the
  cause of the wrong numbers. UNVERIFIED as a contributory factor to failure rate.**

Evidence that would settle it in `/bugfix:confirm`:

- **Proves H1:** run `Core.Tests` in a loop with coverlet 6.0.4 and MSBuild diagnostic logging; on a
  failing iteration, locate the hits file coverlet is about to read and show it is zero-length or
  shorter than its declared length. Equivalently, show that coverlet's `Coverage.cs:420` read path
  has no guard for a short file.
- **Refutes H1:** a failing iteration in which the hits file is complete and well-formed, yet the
  reported percentage is still wrong.
- **Discriminates H1 vs H2:** set `TestTfmsInParallel=false` for *all* test projects on *all* OSes
  (or pass `--framework` per TFM from `cake.cs`) and loop. If the failure rate drops to zero,
  concurrency is causal. Windows job `97780460279` already predicts it will *not* drop to zero.
- **Tests the collector remedy:** replace `coverlet.msbuild` with `coverlet.collector` in
  `eng/Test.targets:15` and loop the same number of iterations. Zero failures across ≥50 iterations
  would support it.
- **Proves the shared-directory defect independently (cheap, deterministic):** inspect
  `artifacts/coverage-reports/<Project>/` after a normal run — there is exactly one `Cobertura.xml`
  and one `index.html` for three TFMs. Downloading the `coverage-linux` artefact from run
  `33262613610` attempt 1 will show which TFM won the race.

### Symptom 2

**This is a policy question wearing a defect's clothes, and should be recorded as such.** The
evidence supports it plainly: line and method coverage are bit-identical between 6.0.4 and 10.0.1
(`94.74%` / `91.98%` on Specs, all three TFMs); only branch coverage moves, and it moves in the same
direction by the same amount on every TFM and every OS. That is a deterministic change in what the
tool *counts*, not a change in what the tests *cover*. No test lost coverage.

**H3 — coverlet 10 emits branch points for C# constructs 6.x silently omitted, and
`Paramore.Fences.Core`/`Paramore.Fences` contain those constructs.** The strongest candidates present
in the source are the `and`/`not` pattern combinator at
`src/Paramore.Fences.Core/Utils/DefaultPredicates.cs:6` and the sixteen `finally` blocks on async
paths (`ResiliencePipeline.Async.cs:119`, `ResiliencePipeline.AsyncT.cs:149,190`, and others listed
above). The `IAsyncEnumerable` fix is definitively **not** in play — the construct does not occur in
either module.

**H4 — a hard `100` on `line,branch,method` is the wrong instrument.** Because `ThresholdType` is
unset, `<Threshold>100</Threshold>` gates all three metrics at 100 for four projects. At 100 there is
no headroom: any collection wobble (H1) or any tool-counting change (H3) is a red build, and the only
cheap remedy is to lower the number — which is how `94,94,91` came to exist upstream in the first
place (`3fb089717`). The gate is also undocumented: `.agent_instructions/testing.md` and
`CONTRIBUTING.md` record only the Stryker 100% mutation threshold, never a coverage threshold.

Evidence that would settle it in `/bugfix:confirm`:

- **Proves H3 and identifies the constructs:** run `Core.Tests` and `Specs` under coverlet 10.0.1
  locally, then diff the resulting `coverage.<tfm>.cobertura.xml` against the 6.0.4 files already on
  disk (`test/Paramore.Fences.Specs/coverage.net10.0.cobertura.xml`,
  `test/Paramore.Fences.Core.Tests/coverage.net10.0.cobertura.xml`). Compare
  `branches-valid`/`branches-covered` at the `<coverage>` element, then walk down to the
  `<class>`/`<line branch="true">` entries that differ, and map them to source lines. The prediction
  is a specific, enumerable list of newly-counted branches — not a diffuse shift.
- **Refutes H3:** the branch delta lands on lines that are unremarkable (plain `if`/`&&`), indicating
  a general counting change rather than the specific fixes named in the coverlet 10 notes.
- **Feeds the policy decision (H4):** for each newly-counted uncovered branch, classify as (a) a
  genuine untested path deserving a test, (b) a compiler-generated artefact deserving
  `ExcludeByAttribute` extension at `eng/Test.targets:29`, or (c) unreachable. Only (a) justifies
  keeping `100`; (b) and (c) argue for an exclusion, not a lowered number.
- The acceptance criterion "No threshold is lowered merely to accommodate a collection defect" is not
  satisfiable until H1 is settled first — symptom 1 must be confirmed and fixed before any number in
  a `.csproj` is touched, or the two will be confused.

## Confirmed Root Cause

**Verdicts: H1 CONFIRMED (with a correction). H2 CONFIRMED and proven empirically. H3 REFUTED.
H4 CONFIRMED.**

### Symptom 1 — the hits file is written by a process-exit hook that vstest does not wait for

With `CollectCoverage=true` and no data collector (`eng/Test.targets:26-30`), the only thing that
persists coverage data is a hook injected into the instrumented assembly:

- `ModuleTrackerTemplate.cs:42-46` (coverlet v6.0.4) — `RegisterUnloadEvents()` subscribes
  `UnloadModule` to `AppDomain.CurrentDomain.ProcessExit` and `DomainUnload`. On .NET Core
  `DomainUnload` never fires, so **`ProcessExit` is the sole flush**.
- `ModuleTrackerTemplate.cs:105-111` — `using var fs = new FileStream(HitsFilePath, FileMode.CreateNew);`
  then `bw.Write(hitsArray.Length)` and a loop of `bw.Write(hitCount)`. **In-place, non-atomic, no
  temp-file-plus-rename**, and the two-argument `FileStream` ctor defaults to `FileShare.Read`, so a
  reader may open the file mid-write.

vstest does not wait for that hook:

- `ProxyOperationManager.Close()` (vstest v18.0.0,
  `src/Microsoft.TestPlatform.CrossPlatEngine/Client/ProxyOperationManager.cs:317-365`) — after
  `RequestSender.EndSession()` the comment reads verbatim *"We want to give test host a chance to
  safely close. The upper bound for wait should be 100ms."* The timeout is
  `VSTEST_TESTHOST_SHUTDOWN_TIMEOUT` or **100 ms** by default; on expiry it logs *"Timed out waiting
  for test host to exit. Will terminate process."*
- `DotnetTestHostManager.CleanTestHostAsync`
  (`src/Microsoft.TestPlatform.TestHostProvider/Hosting/DotnetTestHostManager.cs:763-770`) then calls
  `_processHelper.TerminateProcess(_testHostProcess)` unconditionally.

Upstream measured coverlet's `UnloadModule` at **988 ms for a single assembly** (coverlet issue
#1983's diagnostic timeline). The flush routinely needs an order of magnitude more time than the
grace period it is given; on a loaded CI runner the testhost is killed mid-flush.

The reader does nothing to defend itself:

- `Coverage.cs:379-387` — if the file is absent, coverlet logs at **verbose** and `continue`s,
  emitting a report in which every line has zero hits → the **`0%`** shape. Coverlet's own comment
  names the cause: *"Hits file could be missed mainly for two reason / 1) Issue during module
  Unload()"*.
- `Coverage.cs:416-420` — `NewFileStream(result.HitsFilePath, FileMode.Open, FileAccess.Read)` /
  `new BinaryReader(fs)` / `int hitCandidatesCount = br.ReadInt32();`. **This is exactly
  `Coverage.cs:line 420` in the CI stack trace.** Line 422 carries coverlet's own
  `// TODO: hitCandidatesCount should be verified against result.HitCandidates.Count`. A short file →
  the **`EndOfStreamException`** shape.

### Correction to triage: the "three shapes, one defect" framing is REFUTED

`0%` and `Unable to read beyond the end of the stream` are this defect. **`99.95%` is not** — a
truncated hits file cannot produce it:

1. The hits file is a length-prefixed `int32` array read in a tight loop (`Coverage.cs:420,428`). Any
   short read throws. There is **no** code path that yields a valid-but-partial hit array.
2. The CI log proves it. Job `99127041539` at `16:24:16.866` logs
   `Generating report '…/coverage.net9.0.cobertura.xml'` — coverlet read the file successfully, no
   exception, no warning — and *then* printed `99.95% / 99.81% / 100%`.
3. Arithmetic: `2425/2426 = 99.958%`, `552/553 = 99.819%`. **Exactly one line and one branch were
   lost** — not a truncation, a single missing code path.

A plausible sub-mechanism remains inside the same design — `ModuleTrackerTemplate.cs:97` does
`Interlocked.Exchange(ref HitsArray, new int[HitsArray.Length])` and then spends the whole write
window with hits landing in the discarded array, so any thread-pool continuation still running at
shutdown is silently dropped. But that is **a different sub-mechanism, unproven either way**, and it
may equally be a genuinely flaky path in `Core.Tests`. **It must not be assumed fixed by a symptom-1
fix.**

### Symptom 2 — coverlet 10 does not find more branches; 6.0.4 was over-crediting the ones it found

`branches-valid` is **unchanged** in every comparison. Coverlet 6.0.4 marked the "taken" leg of a
two-way branch as covered whenever the branch instruction was reached, regardless of which leg ran;
coverlet 10 instruments both legs (coverlet PR #1865, *"Investigate and fix branch coverage"*, which
per #1983 *"uses trampolines appended at end of method bodies for taken-branch paths"*).

**The repo's current baselines are inflated.** The true figures are 93.99% (Specs), 99.09% (Core),
96.52% (Extensions) branch coverage.

## Evidence

- [x] **Code-trace** — the `file:line` chain above, cross-checked against three CI job logs and
  corroborated by upstream.
- [x] **Red repro** — deterministic, run locally, for both H2 and H3.

### H1 — upstream corroboration

Coverlet's issue tracker carries this exact defect:

| # | State | Title |
| --- | --- | --- |
| 1983 | closed | **[BUG] Race condition between ProcessExit hit-file write and out-of-proc coverage read causes EndOfStreamException** |
| 1981 | closed | [BUG] Unable to read beyond the end of the stream |
| 1957, 1086, 996, 972, 894, 865, 718, 491, 210 | closed | same exception — "random" / "intermittent" / "spurious in CI" |
| 1085 | closed | *Code coverage showing 0% and sometimes higher than 0%* — reporter's binlog shows the identical `Hits file:'…' not found for module` line |

Issue #1983 is a full RCA with captured `--diag` timelines. Its Step 3 identifies
`new FileStream(HitsFilePath, FileMode.CreateNew)` and the `FileShare.Read` default as the defect;
its Fix 2 proposes precisely temp-file + `File.Move`. Maintainer `Bertk` replied: *"it's possible
that the same issue also exists in the 6.0.x release line."* It does — the v6.0.4
`ModuleTrackerTemplate.cs:105` read during this confirmation is that same line.

**Alternative explanations ruled out:**

- **`net481` on Windows** — ruled out. `eng/Test.targets:26` gates `CollectCoverage` on
  `GetTargetFrameworkIdentifier(...) != '.NETFramework'`, so the `net481` inner build writes no hits
  file and runs no ReportGenerator.
- **Stale hits files / `NoBuild=true`** — ruled out. `Coverage.cs:79` sets
  `Identifier = Guid.NewGuid().ToString()` fresh per `InstrumentationTask`; `Instrumenter.cs:136-139`
  builds the path as `Path.GetTempPath() + <module>_<guid>`; `Coverage.cs:464` calls
  `DeleteHitsFile(...)` after reading. A prior run's file cannot be picked up.
- **Multiple testhosts on one file** — not applicable (one test assembly per `dotnet test`), and
  coverlet guards it with a named mutex (`ModuleTrackerTemplate.cs:86`) plus a read-modify-write
  merge path (`:119-147`).
- **CI I/O pressure** — this is the *trigger*, acting through the 100 ms budget above.

**Live repro attempt (capped):** 8 consecutive `Core.Tests -f net9.0` runs, all identical
(`100% / 99.09% / 100%`). No wobble locally in ~8 minutes. The non-determinism is CI-load-bound; the
trace stands on its own.

### H2 — the shared ReportGenerator directory is proven clobbered

Ruled out as collision sites: hits files (distinct GUIDs, above); module outputs
(`Directory.Build.props:5` sets `UseArtifactsOutput=true`, so each TFM builds into
`artifacts/bin/<project>/release_<tfm>/`); coverlet reports (`coverlet.msbuild.targets:68-81` passes
`CoverletMultiTargetFrameworksCurrentTFM`, giving `coverage.<tfm>.cobertura.xml`).

**Proven as a collision site — `eng/Test.targets:36,73`.** A full 3-TFM `RateLimiting.Tests` build
produced three per-TFM coverlet reports but exactly **one** `Cobertura.xml` and **one** `index.html`
in `artifacts/coverage-reports/Paramore.Fences.RateLimiting.Tests/`, the survivor tagged `net8.0`.
Two of three TFM reports were destroyed. There is no merge and no history: `Tag` feeds only
ReportGenerator's displayed tag and history-file naming, and no `HistoryDirectory` is configured
anywhere.

Visible in the existing on-disk artefact without running anything:
`artifacts/coverage-reports/Paramore.Fences.Core.Tests/Cobertura.xml` has `timestamp="1788018910"`,
matching `coverage.net9.0.cobertura.xml` (`1788018910`) and not net10.0/net8.0 (`1788018907`).

### H3 — reproduced, and the hypothesis is wrong

A scratchpad copy at 6.0.4 reproduced CI exactly (`94.74% / 95.06% / 91.98%`). Flipping only
`Directory.Packages.props:5` to `10.0.1` in the copy reproduced the failure exactly
(`94.74% / 93.99% / 91.98%`, `error : The minimum branch coverage is below the specified 94`).

| | Specs 6.0.4 | Specs 10.0.1 | Core.Tests 6.0.4 | Core.Tests 10.0.1 |
| --- | --- | --- | --- | --- |
| lines-valid / covered | 4204 / 3983 | 4204 / 3983 | 2426 / 2426 | 2426 / 2426 |
| **branches-valid** | **1499** | **1499** | **553** | **553** |
| branches-covered | 1425 | **1409** | 553 | **548** |

`branches-valid` is unchanged, and the per-condition IL offsets are unchanged too (e.g.
`AdvancedCircuitBreakerTResultSyntax.cs:33` reports jump offsets `12` and `43` in *both* versions;
only their coverage moves `100% → 50%`). This is a **hit-attribution** change, not a discovery
change.

**6.0.4 was demonstrably wrong.** `src/Paramore.Fences.Core/Utils/Pipeline/DelegatingComponent.cs:64`
has `hits="1"` in both reports — the line executed **once** — yet 6.0.4 reports its two-way branch as
`100% (2/2)`. One execution cannot take both legs. Coverlet 10's `50% (1/2)` is correct.

**Every line whose branch count changed.**

`Paramore.Fences.Specs` → `src/Paramore.Fences/` (16 newly-uncovered branches):

| file:line | 6.0.4 → 10.0.1 | construct | class |
| --- | --- | --- | --- |
| `CircuitBreaker/AdvancedCircuitBreakerTResultSyntax.cs:33` | 4/4 → 2/4 | 2× cached method-group/lambda null-check | (b) |
| `CircuitBreaker/AsyncAdvancedCircuitBreakerTResultSyntax.cs:33` | 4/4 → 2/4 | same | (b) |
| `CircuitBreaker/RollingHealthMetrics.cs:79` | 4/4 → 3/4 | real `while (_windows.Count > 0 && …)` | **(c)** |
| `PolicyBuilder.OrSyntax.cs:179` | 2/2 → 1/2 | cached lambda `ex => ex is TException` | (b) |
| `Retry/AsyncRetryTResultSyntax.cs:182` | 2/2 → 1/2 | cached `static _ => { }` | (b) |
| `Retry/AsyncRetryTResultSyntax.cs:363` | 2/2 → 1/2 | cached `EmptyAction` method group | (b) |
| `Retry/AsyncRetryTResultSyntax.cs:730` | 2/2 → 1/2 | cached `EmptyAction` | (b) |
| `Retry/AsyncRetryTResultSyntax.cs:912` | 2/2 → 1/2 | cached `static (_,_,_) => { }` | (b) |
| `Retry/RetryTResultSyntax.cs:206` | 2/2 → 1/2 | cached `EmptyHandlerWithContext` | (b) |
| `Retry/RetryTResultSyntax.cs:547` | 2/2 → 1/2 | cached `static (_,_,_) => { }` | (b) |
| `Timeout/AsyncTimeoutTResultSyntax.cs:223` | 2/2 → 1/2 | cached `EmptyHandlerOfT<TResult>` | (b) |
| `Timeout/AsyncTimeoutTResultSyntax.cs:241` | 2/2 → 1/2 | same | (b) |
| `Timeout/AsyncTimeoutTResultSyntax.cs:334` | 2/2 → 1/2 | same | (b) |
| `Timeout/AsyncTimeoutTResultSyntax.cs:345` | 2/2 → 1/2 | same | (b) |

`Paramore.Fences.Core.Tests` → `src/Paramore.Fences.Core/` (5 newly-uncovered branches):

| file:line | 6.0.4 → 10.0.1 | construct | class |
| --- | --- | --- | --- |
| `Hedging/HedgingStrategyOptions.TResult.cs:63` | 2/2 → 1/2 | cached lambda in `Task.Run(...)` | (b) |
| `Simmy/Fault/FaultGenerator.cs:65` | 2/2 → 1/2 | cached lambda, generic `TException` | (b) |
| `Simmy/Outcomes/OutcomeGenerator.cs:65` | 2/2 → 1/2 | same | (b) |
| `Simmy/Utils/GeneratorHelper.cs:24` | 2/2 → 1/2 | cached `_ => null` | (b) |
| `Utils/Pipeline/DelegatingComponent.cs:64` | 2/2 → 1/2 | cached `static (context, wrapper) => …` | (b) |

**Classification: 18 of 19 are (b) — compiler-generated artefacts.** They are the C# compiler's
delegate/method-group cache check (`<>c.<>9__x ?? (<>c.<>9__x = new …)`). The uncovered leg is the
*cache-hit* path, reachable only on a second invocation within the same generic instantiation.
`GeneratorHelper.cs:24` and `FaultGenerator.cs:65` both show `hits="2"`, but each hit is a *different*
generic instantiation with its own static cache field, so both are cache misses — coverlet 10's `1/2`
is right. Covering these would mean writing tests whose only purpose is to call a method twice; zero
test value.

**One is (c)/(a): `src/Paramore.Fences/CircuitBreaker/RollingHealthMetrics.cs:79`**, a genuine
`while (a && b)` short-circuit leg. Line 78 already carries
`// stryker disable once all : no means to test this` — the team has already judged this path
untestable.

Determinism: 8/8 identical runs. This is not H1 noise.

Coverlet 10 also breaks `Extensions.Tests` (`100% / 96.52% / 100%` against `<Threshold>100</Threshold>`).
`Testing.Tests` still passes at `100 / 100 / 100`. **The issue's claim that the failure is confined to
`Core.Tests` on `net10.0` understates the blast radius**; PR #6's abort at `Specs` hid it.

### H4 — confirmed

`coverlet.msbuild.props:17` defaults `ThresholdType` to `line,branch,method`, and no file in the repo
sets it. Four projects gate all three metrics at `100` with zero headroom. Every one of the four
symptom-1 incidents and every symptom-2 delta lands as a red build.

## Suggested-Fix Assessment

**The issue's suggested remedy — switch to `coverlet.collector` / `--collect:"XPlat Code Coverage"` —
is WRONG as stated; PARTIAL at best.**

The stated rationale ("the datacollector is flushed by the test platform through the datacollector
protocol before the host is allowed to exit") is contradicted by the primary evidence:

- Coverlet issue **#1983 reproduces the identical `EndOfStreamException` at
  `Coverage.CalculateCoverage()` using `coverlet.collector` 10.0.1 with
  `--collect:"XPlat Code Coverage"`** on a multi-targeted net8/9/10 project. Its stack trace goes
  through `CoverletCoverageDataCollector.OnSessionEnd`. **The datacollector is not immune; it is where
  the bug was reported.**
- #1983 §"A note on history" states the writer is unchanged: *"`CoverletInProcDataCollector.cs`,
  `ModuleTrackerTemplate.cs`, and the entire data-collection hot path are byte-for-byte identical
  between v8.0.1 and v10.0.1."* The datacollector does not replace the `ProcessExit` flush; it adds an
  in-proc collector that *tries* to flush first.
- Worse, `SendTestSessionEnd` is fire-and-forget (no `ReceiveMessage()`), and the collector adds an
  **out-of-process reader** that can open the file while `ProcessExit` is writing it — a race that
  does not exist in the msbuild path, where the reader is an MSBuild node running strictly after the
  `VSTest` task.
- The in-proc collector's own failure paths (`GetInstrumentationClass()` swallowing
  `ReflectionTypeLoadException`; `unloadModule.Invoke()` failing without clearing `FlushHitFile`) are
  silent unless `COVERLET_DATACOLLECTOR_INPROC_EXCEPTIONLOG_ENABLED` is set.

What *is* true: the in-proc collector usually flushes at `TestSessionEnd`, i.e. before shutdown, which
narrows the window materially. Switching is a genuine mitigation — but it is not the fix the issue
believes it is, and **it will not eliminate symptom 1**.

Two levers worth more than the version swap, both missed by triage:

1. **`VSTEST_TESTHOST_SHUTDOWN_TIMEOUT`** — raising it directly widens the 100 ms budget the flush is
   losing (`ProxyOperationManager.cs:328-332`). Cheap, no code change, testable in CI.
2. **Neither symptom-1 shape is visible today.** `Coverage.cs:385`'s `Hits file … not found` is
   `LogVerbose`, invisible at default MSBuild verbosity — which is why the `0%` job log
   (`97780460065`) contains no diagnostic at all.

## Scope Notes

- **`eng/Test.targets:73` — when the coverage gate fails, no coverage report is produced at all.**
  `GenerateCoverageReports` is `AfterTargets="GenerateCoverageResultAfterTest"`; when
  `CoverageResultTask` errors on threshold, MSBuild stops and ReportGenerator never runs. **Proven:**
  after `Extensions.Tests` failed its gate and `Testing.Tests` passed, `artifacts/coverage-reports/`
  contained only `Paramore.Fences.Testing.Tests`. Combined with `.github/workflows/build.yml:104`
  (`if: always()`) and `:109` (`if-no-files-found: ignore`), the `coverage-<os>` artefact **silently
  omits exactly the project you need to inspect**. Any fix must decouple report generation from the
  gate.
- **`eng/Test.targets:36`** — `artifacts/coverage-reports/<project>/` holds one arbitrary TFM's
  report (proven above). The uploaded `coverage-<os>` artefact (`build.yml:107-108`) and everything
  downstream of it are unreliable, **independent of coverlet version or threshold**. Key by
  `$(TargetFramework)` too, or merge with ReportGenerator's multi-report input.
- **`eng/Test.targets:42-70,83`** — `WriteLinesToFileWithRetry` appends to `$(GITHUB_STEP_SUMMARY)`,
  shared by every concurrent inner build *and* every project in the run. Three retries with
  `ContinueOnError="WarnAndContinue"` means summary sections are silently dropped on contention.
- **The `99.95%` failure mode is unexplained and must not be assumed closed** by a symptom-1 fix. It
  is not truncation (proved above). Investigate whether one line/branch in `Paramore.Fences.Core` is
  reached only by a thread-pool continuation racing shutdown — `ModuleTrackerTemplate.cs:97`'s
  `Interlocked.Exchange` discards every hit recorded after the swap — or whether a `Core.Tests` case
  is genuinely flaky.
- **Stryker is unaffected.** `eng/stryker-config.json` pins `"target-framework": "net10.0"` (single
  TFM, no inner-build race) and uses its own instrumentation, not coverlet. Its
  `"thresholds": {"high": 100, "low": 100}` is the mutation-score gate documented at
  `.agent_instructions/testing.md:96` and `CONTRIBUTING.md:222` — a different gate from the coverage
  one.
- **`net481` is not implicated** (`eng/Test.targets:26`), so the Windows 4-TFM matrix adds no coverlet
  participants.
- **`cake.cs:151-162` does not swallow coverage failures** — `DotNetTest` throws on non-zero exit and
  there is no `ContinueOnError` in the build. But `NoBuild = true` (`cake.cs:159`) routes through
  `InstrumentModulesNoBuild` (`coverlet.msbuild.targets:58-61`), which re-instruments built assemblies
  **in place**; if a run is killed between instrumentation and `RestoreOriginalModules`, the next
  `dotnet test` instruments an already-instrumented module. Not observed, but a live hazard of the
  msbuild path that the collector path avoids.
- **The issue text is factually wrong on symptom 2 and should be corrected.** *"coverlet 10 identifies
  branches the 6.x line did not"* — it does not (`branches-valid` 1499→1499, 553→553). It identifies
  the same branches and stops over-crediting them. Framed correctly, this is not "deciding what our
  branch-coverage numbers should be"; it is discovering that the current numbers are **inflated**.
- **The coverage gate remains undocumented.** `CONTRIBUTING.md:237` describes the `coverage-<os>`
  artefact and states there is no external coverage service, but no Fences document records a coverage
  *threshold*. The `100` values are inherited Polly configuration (`a57742fa6`), and `94,94,91`
  (`3fb089717`).

### Agreed scoping (2026-09-04)

The work splits into **two changes**, agreed with the user after the Confirm gate. They are separated
because only one of them can be proven green.

**Change A — the collection/reporting defects and the coverlet 10 uptake.** Testable, deterministic,
one coherent change:

1. `eng/Test.targets:36` — key `ReportGeneratorTargetDirectory` by `$(TargetFramework)` as well as
   project (or merge the per-TFM reports). Red test today: build across three TFMs, assert three
   reports survive.
2. `eng/Test.targets:73` — decouple `GenerateCoverageReports` from the threshold gate, so a failing
   gate still produces the report you need to diagnose it. Red test today: fail a threshold, assert a
   report exists.
3. Take PR #6 (coverlet 10.0.1) and correct the thresholds to the **true** figures. Per H3 this is
   *not* lowering a threshold to accommodate a collection defect — the 6.0.4 baselines were inflated
   by a branch over-reporting bug. 18 of the 19 changed sites are compiler-generated delegate-cache
   checks with no test value; the 19th (`RollingHealthMetrics.cs:79`) is already marked untestable for
   Stryker. Note `Extensions.Tests` is affected too (96.52%), not just `Core.Tests` and `Specs`.

**Change B — the H1 mitigation.** Cannot be proven green by a regression test: the defect is a race
against vstest's 100 ms shutdown budget under CI load, and a test that fails only under load is itself
flaky. Scope is therefore mitigation plus **visibility**:

1. Raise `VSTEST_TESTHOST_SHUTDOWN_TIMEOUT` to widen the budget the flush is losing
   (`ProxyOperationManager.cs:328-332`).
2. Make the silent failure loud. `Coverage.cs:385`'s `Hits file … not found` is `LogVerbose` — which is
   why the `0%` job log (`97780460065`) carried no diagnostic at all. A `0%` result must not be able to
   pass unremarked.
3. Switching to `coverlet.collector` is a *mitigation only* and is explicitly **not** the fix the issue
   believes it is (see Suggested-Fix Assessment).

**Still open, and not closed by either change:** the `99.95%` failure mode (see Scope Notes above).

## Regression Test

**File**: `cake.cs` (both tests; +42 and +40 lines, wired into the `Build` pipeline after `__RunTests`)

**Why not xUnit.** Both Change A defects are in MSBuild (`eng/Test.targets:36` and `:73`). No
in-process xUnit test can pin them — proving either one requires a real multi-TFM build, and shelling
out to one from a test project would break the repo's "every test runs in process" rule, take minutes,
and sit inside a coverage-collected, 100%-gated project while itself invoking a coverage build. The
instrument chosen instead is a **cake build task**, with precedent at `__ValidateAot`
(`cake.cs:120-135`): red today, green after the fix, run on every CI build. Agreed with the user at the
`/bugfix:test` gate. `.agent_instructions/testing.md:4` requires this deviation be stated rather than
skipped quietly.

### Test 1 — `__ValidateCoverageReports` (pins `eng/Test.targets:36`)

Walks the same `./test/**/*{Tests,Specs}.csproj` glob `__RunTests` uses, so the two cannot drift, and
asserts `artifacts/coverage-reports/<project>/<tfm>/Cobertura.xml` exists for every coverage-collected
target framework. The helper `CoverageCollectedFrameworks` reads `TargetFrameworks` via `XmlPeek`
(matching `RunMutationTests`' idiom) and filters `net4*`, mirroring `eng/Test.targets:26` — so `net481`
on Windows is correctly not expected to produce a report.

**Red today, verified:** fails with `Every target framework must keep its own coverage report, but 15
are missing` — 5 projects × 3 TFMs, i.e. *none* of the per-TFM reports exist, because all three inner
builds collapse into one directory per project.

**Note:** this test asserts a layout that does not exist yet (per-TFM subdirectories). It therefore
defines the shape of the fix, not merely the absence of files.

### Test 2 — `__ValidateCoverageReportOnThresholdFailure` (pins `eng/Test.targets:73`)

Runs one test project (`Paramore.Fences.Testing.Tests`, `net10.0`) with `-p:Threshold=101`, asserts the
build fails, then asserts a report was still written. `Threshold=101` is drift-proof: a global MSBuild
property overrides the csproj value, and coverlet accepts it, calculating coverage in full before
failing at `CoverageResultTask.cs:251` — i.e. *after* the point at which the report should already
exist. `ReportGeneratorTargetDirectory` is redirected to `artifacts/coverage-threshold-check`
(gitignored) so the deliberately-failing run cannot overwrite a real report, and the assertion globs
`**/Cobertura.xml` beneath it so it stays valid whether or not the fix nests reports per TFM.

**Red today, verified:** reaches the second assertion — `A failing coverage threshold must still
produce a coverage report, but none was written to …` — proving the provocation worked (tests ran,
coverage was calculated) and the report was still never produced.

**Accepted trade-off:** this prints red coverlet errors during a green build. An `Information()` banner
precedes it saying the errors are expected.

### Not covered by any test

- **H1 (the hits-file race)** — Change B. Not testable: it is a race against vstest's 100 ms shutdown
  budget under CI load, and a test that fails only under load is itself flaky. Mitigation plus
  visibility only.
- **The `99.95%` failure mode** — still unexplained (see Scope Notes); not closed by either change.

## Fix

**File**: `eng/Test.targets` (both defects). No source, no public API, no dependency change.

### Fix 1 — per-TFM report directory (`eng/Test.targets:36`)

`ReportGeneratorTargetDirectory` now appends `$(TargetFramework)`. Verified: a 3-TFM
`RateLimiting.Tests` run produces three `Cobertura.xml` and three `index.html`, where before it
produced one of each.

### Fix 2 — a failing threshold still produces a report (`eng/Test.targets:77-86`)

Two things had to change, and the first attempt at each was wrong — recorded here because the dead
ends are the useful part:

1. **Hooking around coverlet's target cannot work.** `GenerateCoverageResult` runs as a
   *DependsOnTargets* of `GenerateCoverageResultAfterTest`, so its failure aborts the build before
   any `BeforeTargets`/`AfterTargets` hook on that target is reached. Proved with `-v:d`: the log
   shows `Done building target "GenerateCoverageResult" … -- FAILED` and our target never appears.
   The fix therefore **overrides** `GenerateCoverageResultAfterTest`, which is legitimate here because
   `eng/Test.targets` is imported at preprocessed line 11567, after coverlet's at 11448 — last
   definition wins. It drops `DependsOnTargets` and instead calls the target with
   `<CallTarget ContinueOnError="ErrorAndContinue" />`, so the threshold failure is still logged as an
   error and still fails the build, but report generation runs first.
2. **`@(CoverletReport)` is unusable on exactly the runs that matter.** It is an *output* of the task
   that enforces the threshold, so it is empty whenever the gate fails. ReportGenerator ran with no
   inputs and silently produced nothing. The target now reads
   `$(CoverletOutput)coverage.$(TargetFramework).cobertura.xml` from disk — coverlet writes that file
   before it checks the threshold (confirmed: the file survives a failing run).

**Regression caught during the fix.** The override initially also fired in the outer cross-targeting
build, where coverlet's targets are not imported, producing `MSB4057` (target does not exist) and
`MSB4036` (ReportGenerator task not found). `CollectCoverage` is true there because
`GetTargetFrameworkIdentifier('')` is not `.NETFramework` (`eng/Test.targets:26`). Guarded with
`AND '$(TargetFramework)' != ''`. A clean 3-TFM run now reports zero errors.

**Coverlet 10 compatibility, checked by inspection.** The override depends on coverlet's target names,
so it was verified against 10.0.1 before recommending the uptake: `GenerateCoverageResultAfterTest`
(`AfterTargets="VSTest"`, `DependsOnTargets="GenerateCoverageResult"`) is unchanged, and
`build/<tfm>/coverlet.msbuild.targets` simply imports `buildMultiTargeting/`, so inner builds still
see the targets under both versions.

**Visible consequence:** the `coverage-<os>` artefact (`.github/workflows/build.yml:107-108`) gains a
TFM level — `coverage-reports/<project>/<tfm>/` rather than `coverage-reports/<project>/`. That is the
point of the fix, but anything consuming that path shape changes with it.

### Verification

| | |
| --- | --- |
| `__ValidateCoverageReportOnThresholdFailure` | **green** — build still fails on the threshold, report produced |
| Normal 3-TFM run | **green** — zero errors, three per-TFM reports |
| `__ValidateCoverageReports` | green for `RateLimiting.Tests`; full 5-project run deferred to `/bugfix:verify` |

### Not done — the coverlet 10 uptake and threshold corrections

Deliberately left out of this change. It has no red test (it is not a defect), and setting the
thresholds needs measured coverlet-10 figures for all five projects — `RateLimiting.Tests` has never
been measured under 10.0.1, and the scratchpad copy used at Confirm is no longer intact.

The approach that avoids touching `Directory.Packages.props` at all: correct each threshold to a value
that passes under **both** versions (e.g. Specs `94,94,91` → `94,93,91`; `Core.Tests` `100` →
`100,99,100`; `Extensions.Tests` `100` → `100,96,100`), after which #6 merges green unmodified and
Dependabot still owns the version bump. `Testing.Tests` needs no change; `RateLimiting.Tests` is
unmeasured.
