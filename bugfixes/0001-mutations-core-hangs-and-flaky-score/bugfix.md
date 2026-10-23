# Bugfix: mutations-core hangs intermittently and its mutation score is non-deterministic

**Linked Issue**: #46
**Status**: Confirmed

## Symptom

**Observed (issue #46, filed about 2026-09-28, before PR #51):**

- The `mutations-core` job sometimes stalled until the 60-minute job timeout
  (`.github/workflows/mutation-tests.yml:27`). A normal run takes 7–8 minutes. There were 13 hangs in
  77 runs from 2026-08-26, about 1 in 6. Only `core` ever hung. A re-run cleared it.
- Every hung job stopped before any mutant was tested:
  - 8 jobs stopped after `Number of tests found: 698/700 … Initial test run started.`
  - 5 jobs stopped after `Capture mutant coverage using 'CoverageBasedTest' mode.`

  So Stryker's runner sometimes never finished the *un-mutated* Core suite. A plain local
  `dotnet test` of Core also hung once in VSTest's `testhost` (2026-09-26).
- **A second, separate symptom:** the score changes between runs on the same code. `main` failed on
  run 36323895894 (2026-09-27) and run 37957870797 (2026-10-09), both with
  `final mutation score is 99.72 %` against a 100% break-at. 99.72% is 356 of 357, so one survivor:
  a *Negate expression* mutant at `src/Paramore.Fences.Core/Utils/Pipeline/ExecutionTrackingComponent.cs:54`.
  Other runs of the same code scored 100%. Run 36323895894 also reported 22 `Timeout` mutants.

**Expected:** `mutations-core` finishes in about 7–8 minutes and scores exactly 100% every time on
the same code.

**Since PR #51 (merged 2026-10-18, commit 13b523c0):** that PR moved the tests to xunit v3 on MTP v2,
and Stryker from 4.16.0 (VSTest runner) to 5.0.0 (`"test-runner": "mtp"`). Since then, 18 runs on
post-#51 code, plus 3 on pre-#51 code on 2026-10-16, all passed first time with no hangs. The 10 Core
jobs inspected took 6–8 minutes and scored 100.00%, with 702 tests and 357 mutants.

- At the old hang rate, 18 clean runs in a row would happen by chance with p ≈ 3.8%.
- The score failure was rarer (2 in about 80 runs), so the clean runs say almost nothing about it.

**Reproduction:** none known for either symptom. Both are intermittent and have only been seen in
CI, apart from the one local `testhost` hang.

## Suspected Location

### Mutation set-up

- `.github/workflows/mutation-tests.yml:27`: `timeout-minutes: 60`, the only limit on a hang.
  The `core` matrix entry is at lines 37-38.
- `cake.cs:384-388` (`MutationTestsCore`) and `cake.cs:536-591` (`RunMutationTests`): Stryker runs
  with `--break-at` set from the `.csproj` `MutationScore` (`cake.cs:538-539`, `cake.cs:579`). No blame
  or hang option is passed. A non-zero exit throws at `cake.cs:588-590`.
- `eng/stryker-config.json`: `"concurrency": 4` (line 22), `"target-framework": "net10.0"`
  (line 25), and `"test-runner": "mtp"` (line 26, added by 13b523c0; before #51 it used the VSTest
  runner). Thresholds are 100/100 (lines 27-30). There is no `coverage-analysis` setting, so the
  default per-test coverage applies: that is the `CoverageBasedTest` phase seen in the hung logs.
- `.config/dotnet-tools.json:26-27`: `dotnet-stryker` 5.0.0, previously 4.16.0.
- `eng/Test.targets:14-23`: `xunit.v3.mtp-v2` and `Microsoft.Testing.Platform`. Before #51 these
  were xunit v2, `coverlet.msbuild` and `JunitXml.TestLogger` on VSTest.

### Score symptom (H2)

- `src/Paramore.Fences.Core/Utils/Pipeline/ExecutionTrackingComponent.cs:39-61` (`DisposeAsync`):
  - The wait loop is at lines 45-58, with `Task.Delay(SleepDelay, _timeProvider)` at line 48.
  - Line 53 is `// stryker disable once equality : no means to test this`. It only disables
    *equality* mutations.
  - Line 54 is `if (_timeProvider.GetElapsedTime(start) > Timeout)`, the line of the survivor.
- `test/Paramore.Fences.Core.Tests/Utils/Pipeline/ExecutionTrackingComponentTests.cs`:
  - `DisposeAsync_PendingOperations_Delayed` (lines 10-41) should kill the mutant with
    `inner.Disposed.ShouldBeFalse()` at line 31, straight after `_timeProvider.Advance(SleepDelay)` at
    line 30. Under the mutant, the first 1s wake-up breaks out of the loop and disposes the inner
    component.
  - `DisposeAsync_Timeout_Ok` (lines 69-99) is the second chance, at line 91, after advancing 29s at
    line 90.
  - `DisposeAsync_WhenRunningMultipleTasks_Ok` (lines 101-149) uses real `TimeProvider.System`
    (line 115), so it cannot kill the mutant: its release loop almost certainly finishes in well
    under 1s. It also blocks 10 thread-pool threads on `ManualResetEvent.WaitOne()` (line 111) and
    polls `while (tasks.Count != 10)` with no limit (lines 125-128).
  - Both killing assertions rely on the `Task.Delay` continuation running **inline** inside
    `FakeTimeProvider.Advance` on the test thread. If it runs on another thread instead, the
    assertion races the continuation and can pass.

**Hang symptom (H1): candidate Core tests with unbounded waits.** Confidence is low for all of them;
none has been shown to hang.

- `test/Paramore.Fences.Core.Tests/Hedging/HedgingTimeProvider.cs:13-21` and `:29-43`: this
  hand-written fake clock is not thread-safe. `_utcNow` is a plain field, and `Advance` only completes
  entries already in the queue, so a timer created by another thread *after* an `Advance` is never
  completed. It is used with `InfiniteTimeSpan` waits in
  `test/Paramore.Fences.Core.Tests/Hedging/Controller/HedgingExecutionContextTests.cs:155-163`,
  `:488` and `:534`, and in `HedgingControllerTests.cs:33`. A missed wake-up here would hang forever.
- `test/Paramore.Fences.Core.Tests/CircuitBreaker/Controller/ScheduledTaskExecutorTests.cs`:
  unbounded `WaitOne()` at lines 57, 61, 86 and 90, which need a thread-pool thread to run
  `ScheduledTaskExecutor.StartProcessingAsync`
  (`src/Paramore.Fences.Core/CircuitBreaker/Controller/ScheduledTaskExecutor.cs:54-78`).
  `ScheduleTask_InlineContinuationDoesNotDeadlock` (line 133) suggests a deadlock was fixed here
  before.
- `ExecutionTrackingComponentTests.cs`: unbounded `WaitOne()` at lines 21, 27, 54, 60, 80, 86 and
  111, plus the poll at lines 125-128.
- `test/Paramore.Fences.Core.Tests/Retry/RetryResilienceStrategyTests.cs:559-569` (`ExecuteAndAdvance`):
  a tight `while (!executing.IsCompleted) Advance(...)` loop with no limit.
- **Not a candidate:** `test/Paramore.Fences.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs:46-49`,
  a similar loop, is bounded by a real 20s `CancellationTokenSource` (line 38).

## Root-Cause Hypothesis

The two symptoms are treated as separate.

**H1 (hang): the hang came from VSTest, its `testhost` or Stryker 4's VSTest runner, and #51 removed it
by replacing that stack. It was not a deadlock in a Core test that still exists.**

- For: every hang was in Stryker's initial run or coverage-capture phase; the one local hang was in
  VSTest's `testhost`, which MTP does not use; 21 clean runs since, 18 of them post-#51.
- Against: p ≈ 3.8% is suggestive, not conclusive. Only Core hung, and Core has the most
  concurrency-heavy tests, so a test-level race is a credible alternative.

**H1-alt:** a Core test can block forever under thread-pool pressure or an unlucky interleaving. The
leading candidate is a missed wake-up in the non-thread-safe `HedgingTimeProvider` followed by an
`InfiniteTimeSpan` wait; second is an unbounded `WaitOne()` in `ScheduledTaskExecutorTests` or
`ExecutionTrackingComponentTests`. If this is right, the hang still exists on MTP and is only rarer
or hidden.

To decide it in /bugfix:confirm:

- **Refutes H1:** any `mutations-core` hang, or any local Core hang under MTP, on post-#51 code.
- **Supports H1:** many repeated post-#51 runs (Stryker, or `dotnet test` of Core on net10.0, ideally
  under thread-pool stress) with no hang. About 50 more clean CI runs would push the chance of a
  1-in-6 hang still being present below 0.01%.
- **Identifies a hanging test (H1-alt):** a hang dump or blame output naming the test still running
  when the process stalls, on 13b523c0^ and on current `main`; or hammering the candidate tests in a
  loop.

**H2 (non-deterministic score): the mutant at `ExecutionTrackingComponent.cs:54` sometimes survives
because whether it is killed depends on Stryker's per-test coverage attribution or on thread
scheduling, not on what the tests check.**

- On paper, `DisposeAsync_PendingOperations_Delayed` (line 31) and `DisposeAsync_Timeout_Ok` (line 91)
  kill it deterministically. So a "Survived" result most likely means one of:
  - **(a)** in the failing run, Stryker's `CoverageBasedTest` phase linked line 54 only to tests that
    cannot kill it, most obviously `DisposeAsync_WhenRunningMultipleTasks_Ok`, where line 54 runs on
    a real-timer thread-pool thread; or
  - **(b)** the continuation did not run inline, so the assertions at lines 31 and 91 raced it.
- The 22 `Timeout` mutants show timing sensitivity under mutation, but they count as detected, so
  they do not lower the score.

To decide it in /bugfix:confirm:

- **Confirms (a):** in the `mutation-report-core` JSON artifacts for runs 36323895894 and 37957870797,
  the line-54 mutant's `coveredBy`/`killedBy` lack the two killing tests in the failing runs, and
  include them (with a kill) in 100% runs.
- **Points to (b):** both tests are listed as covering the mutant in the failing run, yet it survived.
- **Refutes both:** negate line 54 by hand, run the two tests in a loop, and they fail every time.
  That would show the tests are deterministic and put the cause in Stryker's mutant-to-test selection.

**Suggested directions from the issue:**

- Use blame-hang-timeout, or the MTP hang-dump equivalent, to find the hanging test.
  **UNVERIFIED — to be proven or refuted in /bugfix:confirm**
- Lower `timeout-minutes` (`.github/workflows/mutation-tests.yml:27`) so a hang fails fast.
  **UNVERIFIED — to be proven or refuted in /bugfix:confirm**
- Exclude the wait-loop condition at `ExecutionTrackingComponent.cs:54` from mutation, or test it
  deterministically. **UNVERIFIED — to be proven or refuted in /bugfix:confirm**
- #34 (xunit v3 / MTP v2) and Stryker 4.16 → 5.0 may affect the hang. The post-#51 evidence fits this
  but does not prove it. **UNVERIFIED — to be proven or refuted in /bugfix:confirm**

## Confirmed Root Cause

**Verdict:** H2 **CONFIRMED** (mechanism proven). H1 **SUPPORTED, NOT PROVEN**. H2(a), a difference in
coverage attribution, is **refuted**.

**H2, the non-deterministic score: xunit v2's `AsyncTestSyncContext` made the line-54 kill a race,
and PR #51 removed it.**

- xunit v2 installs `Xunit.Sdk.AsyncTestSyncContext` on the test thread for the synchronous part of
  every test method, before the first `await`. xunit v3 installs none. Shown directly by a probe test
  (evidence 4).
- `DisposeAsync_PendingOperations_Delayed` and `DisposeAsync_Timeout_Ok` call
  `_timeProvider.Advance` (test lines 30 and 90) and then check `inner.Disposed` (lines 31 and 91)
  before their first `await`. `Advance` completes the `Task.Delay(SleepDelay, _timeProvider)` from
  `ExecutionTrackingComponent.cs:48` on the test thread.
- An await continuation runs inline only when the current `SynchronizationContext` is null or the
  base type (BCL `AwaitTaskContinuation.IsValidLocationForInlining`; inferred from BCL behaviour, not
  read from source here). `ConfigureAwait(false)` does not change that check.
- So under xunit v2, `DisposeAsync`'s continuation was queued to the thread pool, and the assertion
  raced it. Under the mutant at `ExecutionTrackingComponent.cs:54`, line 31 always won the race
  (never killed), and line 91 usually lost it (killed). Sometimes both won, and the mutant survived:
  the 99.72% runs. Under xunit v3 the continuation runs inline, and both assertions kill the mutant
  every time.
- **Not pinned down:** why line 31 always wins while line 91 usually loses. This is speculative
  (thread-pool warmth, test order, coverlet overhead). The Pre-#51 `Timeout` outcomes for this mutant
  are not explained either; they count as detected.

**H1, the hang: the VSTest / Stryker 4 stack is the most likely cause, but this is not proven.** There
have been no hangs in 18 post-#51 runs (p ≈ 3.8% by chance at the old rate), and none in 40
local runs. No test-level hang has been shown on either stack. The H1-alt candidates still exist on
the current stack (`HedgingTimeProvider` is not thread-safe, and there are unbounded `WaitOne()`
calls), but none has been seen to hang.

## Evidence

- [x] **Stryker JSON reports**, `mutation-report-core` artifacts from 30 CI runs, line-54
  *Negate expression* mutant:

  | Stack | Runs | Killed | Timeout | Survived | Timeout mutants per run |
  | --- | --- | --- | --- | --- | --- |
  | Pre-#51 (xunit 2.9.3, VSTest, coverlet, Stryker 4.16) | 17 | 7 | 8 | 2 | 22–39, varies |
  | Post-#51 (xunit v3 4.0.2, MTP v2, Stryker 5.0 `mtp`) | 13 | 13 | 0 | 0 | 9, every run |

  The two Survived runs were 36323895894 and 37957870797. Of the pre-#51 runs inspected in detail
  (36323895894, 37957870797 and 36324157998), the mutant was covered by the same 3 tests in the
  passing run and in both failing runs, which refutes H2(a). Post-#51, it was always killed by
  `DisposeAsync_Timeout_Ok` and `DisposeAsync_PendingOperations_Delayed`.
- [x] **Red reproduction**: line 54 negated by hand, Release, net10.0, macOS arm64,
  `ExecutionTrackingComponentTests` only:
  - post-#51 tree: **20/20** runs fail at `ExecutionTrackingComponentTests.cs:31` and `:91`;
  - pre-#51 tree (13b523c0^), coverlet on: 19/20 fail at `:91` only, **1/20 pass everything**
    (the Survived outcome), and `:31` never fails;
  - pre-#51 tree, coverlet off, `--blame-hang-timeout 60s`: 10/10 fail at `:91` only, and none hangs.
- [x] **Code-trace**: `DisposeAsync` (called at test lines 29 and 88) registers a timer through
  `Task.Delay(…).ConfigureAwait(false)` at `ExecutionTrackingComponent.cs:48`. `Advance` (test `:30`
  and `:90`) fires it on the test thread. The mutated `:54` breaks on the first wake-up, and `:60`
  disposes the inner component, setting `Inner.Disposed`. Test `:31` and `:91` read `Disposed`
  synchronously, so the result is deterministic only if `:54-60` ran inline.
  `DisposeAsync_WhenRunningMultipleTasks_Ok` uses `TimeProvider.System` (`:115`) and cannot kill
  the mutant, which matches `killedBy`.
- [x] **Probe**: `SynchronizationContext.Current`, read in a test before and after its first
  `await`:
  - pre-#51: before `Xunit.Sdk.AsyncTestSyncContext`, after `null`;
  - post-#51: before `null`, after `null`.

  An earlier probe that only looked after an `await` wrongly suggested there was no context on
  either stack.

## Scope Notes

- **No source defect.** `ExecutionTrackingComponent.cs` is correct. The flaky score came from the
  test framework. Do **not** exclude the line-54 mutant: it is a real, now-deterministic kill.
- **Latent fragility (optional hardening):** `ExecutionTrackingComponentTests.cs:30-31` and `:90-91`
  still depend on continuations running inline. A future `SynchronizationContext` or
  `RunContinuationsAsynchronously` change would bring the flake back. Hardening, if wanted, belongs
  in the tests, not in `src`.
- **Issue's suggested fixes:**
  - (i) blame-hang / hang dump: **PARTIAL**, insurance only. On MTP it needs
    `Microsoft.Testing.Extensions.HangDump`, which would be a new dependency. It is unclear whether
    Stryker's MTP runner passes it through.
  - (ii) lower `timeout-minutes` (`.github/workflows/mutation-tests.yml:27`): **CONFIRMED** as a cheap
    mitigation. Runs take 6–8 minutes, so 20–25 would make any recurrence fail fast.
  - (iii) exclude or harden line 54: exclusion is **WRONG**; hardening is **UNNECESSARY** on the
    current stack, but optional (see above).
  - (iv) "#34/#51 may affect it": **CONFIRMED** for H2; consistent with, but not proof of, H1.
- **Separate concern, not #46:** one `RuntimeError` mutant appears on every post-#51 run:
  `src/Paramore.Fences.Core/Registry/ConfigureBuilderContext.cs:45`, a Logical mutation (`||` →
  `&&`). It re-adds already-cancelled reload tokens, so `ReloadableComponent.Reload`
  (`ReloadableComponent.cs:49-51`, `:70`) calls itself again until the stack overflows and the test host
  crashes. Stryker treats `RuntimeError` as invalid and leaves it out of the score, so it is no threat
  to the 100% threshold. The mutant is effectively untested.
- **H1 watch:** about 50 more clean CI runs would push the chance of an undetected 1-in-6 hang below
  0.01%.

## Regression Test

None, by decision at the Confirm gate (Ian, 2026-10-22). No defect remains in source or test code:
the H2 race was removed by #51's move off xunit v2, and the line-54 mutant is now killed
deterministically (20/20 locally, 13/13 in CI). So there is no behaviour for a failing test to pin
down. Hardening `ExecutionTrackingComponentTests` against non-inline continuations was offered and
not chosen.

## Fix

- `.github/workflows/mutation-tests.yml`: `timeout-minutes` 60 → 20 for every mutation job. The
  slowest successful jobs over about 56 runs were `core` and `legacy`, at 11.1 minutes, so this
  leaves about 1.8× headroom. A recurrence of H1 now fails in 20 minutes instead of 60. This is a
  mitigation, not a cause fix.
- Close #46 with this evidence. Reopen it if a post-#51 hang or a sub-100% score on unchanged code
  is seen.
- Not done here (separate concern): the `RuntimeError` mutant at `ConfigureBuilderContext.cs:45`.
