# Review: requirements — 001-port-xunit-v3

**Date**: 2026-10-11
**Threshold**: 60
**Verdict**: NEEDS WORK

8 findings at or above threshold 60. Address these before approving.

## Findings

### 1. #6 is already closed, so FR-13 and AC-29 ask for something that cannot happen (Score: 72)

The requirements treat #6 as an open item to be closed after merge. #6 is a Dependabot pull request, not an issue, and it was closed on 2026-09-20. Neither "close #6" nor "#6 is closed with the reason" can be done or checked as written. AC-29 would pass with no action taken, or the implementer would have to invent an action. The "Related" line also describes #6 loosely, as "(coverlet 10 bump)".

**Evidence**: FR-13 says "#6 is closed with the recorded reason 'coverlet removed by #<PR>'." AC-29 says "#6 is closed with the reason 'coverlet removed' and a link to the PR." `gh pr view 6` shows `"author":"app/dependabot"`, `"state":"CLOSED"`, `"closedAt":"2026-09-20T16:38:36Z"`. The last maintainer comment is "Holding this pending #14", and after that Dependabot posts "OK, I won't notify you again about this release".

**Recommendation**: Say that #6 is an already-closed Dependabot PR. Replace the close action with "post a comment on #6 linking the merged PR and recording that coverlet was removed". Rewrite AC-29 to check for that comment.

---

### 2. The #46 exception in FR-14 needs an intermittent hang reproduced on demand (Score: 68)

The FR-14 boundary and AC-31 only allow a `mutations-core` timeout if "the same hang is reproduced on the merge base". #46 records that the hang is intermittent: about one run in six, always cleared by `rerun --failed`. You can't reproduce it on request. This AC could need an open-ended number of 60-minute CI runs, or be impossible to meet. Two implementers would read it differently: one keeps rerunning the merge base until it hangs, the other cites the 13 hangs already logged on #46. The FR also never says whether a simple rerun that passes on the PR is acceptable.

**Evidence**: FR-14 says "(a) the same hang is reproduced on the merge base, and recorded on #46". #46's body says "13 hangs in 77 workflow runs since 2026-08-26, about one run in six… `gh run rerun <id> --failed` has cleared it every time". The hangs it lists include runs on `main`.

**Recommendation**: Allow a successful rerun of `mutations-core` on the PR's final commit as the primary pass condition. Make the local `MutationTestsCore` score the fallback. Treat #46's existing evidence as enough baseline proof, rather than requiring a fresh reproduction.

---

### 3. PR #19 closes as superseded, but no FR delivers PR #19's second fix (Score: 65)

PR #19 fixes two things: (1) a coverage report for each TFM, and (2) a report that is still produced when the coverage gate fails. FR-4 and FR-10 cover (1). No FR or AC requires (2): coverage reports generated, and uploaded as the `coverage-<os>` artefact, when the threshold check or a test fails. Upstream's Cake wiring happens to run `__GenerateCoverageReports` before `__VerifyCoverageThresholds`, but nothing makes that order binding. An implementation could fail the threshold check first and produce no report, and PR #19 would still be closed as "superseded".

**Evidence**: PR #19's body says "a failing gate produced no report at all — omitting from the artefact exactly the project needing inspection". FR-13 says "PR #19 is closed unmerged with a comment linking the merged PR". No AC covers report generation on a failing build. `build.yml:103-109` uploads with `if: always()`.

**Recommendation**: Add an FR clause and an AC: "When the threshold check fails, coverage reports for every coverage file are still written to `artifacts/coverage-reports/` and uploaded." Optionally say what happens when a test fails.

---

### 4. FR-11 lists the doc edits too narrowly: positional `dotnet test <path>` commands break under the MTP runner (Score: 63)

For CONTRIBUTING.md, FR-11 names only "the 'One test, by filter' command at about line 337". With `"test": {"runner": "Microsoft.Testing.Platform"}` in `global.json`, which FR-1 requires, the .NET 10 SDK expects the project to be passed as `--project`. The requirements' own FR-1 example uses `--project`. That means every positional form shown in the docs needs changing, not just the filter command. A developer who follows the list will leave broken commands behind. One who follows AC-25 will fix more. The two disagree, and it's unclear whether AC-25 also covers the inline prose command at CONTRIBUTING.md:48.

**Evidence**: The positional forms are at `CONTRIBUTING.md:48` ("`dotnet test ./test/Paramore.Fences.Core.Tests`"), `:334` and `:340`; `.agent_instructions/build_and_development.md:62` and `:69`; and `.agent_instructions/testing.md:122`. FR-1's example is `dotnet test --project ./test/Paramore.Fences.Core.Tests --framework net9.0`.

**Recommendation**: List every affected line, or define the scope as "every `dotnet test` invocation in those files, inline or in code blocks". Say that project-path arguments move to `--project`.

---

### 5. Open Dependabot PRs for FR-2's packages are not reconciled (Score: 62)

FR-2's reconciliation rule only covers versions already merged to `main`. Two open Dependabot PRs touch FR-2 rows. #41 bumps `Microsoft.NET.Test.Sdk` from 18.9.0 to 18.10.1, which is higher than upstream's 18.10.0. #39 bumps `dotnet-stryker` from 4.16.0 to 5.0.0. Nothing says whether they are merged first, closed as superseded, or left open, where they will conflict with this PR. User decision 1 ("reconciled with Dependabot") is only half covered.

**Evidence**: `gh pr list` shows #41 "Bump Microsoft.NET.Test.Sdk from 18.9.0 to 18.10.1" and #39 "Bump dotnet-stryker from 4.16.0 to 5.0.0", both open. FR-2 says "if `main` already carries a version at or above the current version… keep `main`'s version."

**Recommendation**: Add a rule for open Dependabot PRs that touch FR-2 rows, for example "closed as superseded with a comment linking this PR after merge", and add a matching clause to AC-29.

---

### 6. AC-16's grep count is wrong by construction (Score: 62)

AC-16 equates `git diff <merge-base> | grep -c ExcludeFromCodeCoverage` with the number of listed exclusions. `grep -c` on a raw diff counts removed lines and context lines as well as added ones, and the "(added lines)" note doesn't change what the command does. FR-3 requires removing `<ExcludeByAttribute>System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute</ExcludeByAttribute>` from `eng/Test.targets`, so that removed line alone makes the count at least 1 even with zero new exclusions. The command also misses coverage-settings exclusions, which FR-6 allows.

**Evidence**: The line is at `eng/Test.targets:29`. AC-16 says "`git diff <merge-base> | grep -c ExcludeFromCodeCoverage` (added lines) equals the number of attribute entries listed."

**Recommendation**: Use a check that counts added lines only, such as `git diff <merge-base> -- src test | grep -cE '^\+.*\[ExcludeFromCodeCoverage'`. Handle settings-file exclusions separately.

---

### 7. AC-23 needs a throwaway CI run with a deliberately failing test (Score: 60)

The second half of AC-23 can only be checked by pushing a deliberately failing test and running CI. Nobody is named to do this, it isn't part of the deliverable, and its result isn't recorded anywhere the PR review can see. FR-13's PR-body contents don't include it, so the AC can't be verified after the fact.

**Evidence**: AC-23 says "Given a deliberately failing test in a throwaway CI run, then the job summary names the failing test, project and TFM, and lists no passed tests."

**Recommendation**: Either require that run's link in the PR body, or verify the configuration instead: check that `--report-github` and `--report-github-summary-include-passed false` are passed on CI. You can also check from the final green run's summary that no passed tests are listed.

---

### 8. FR-9's local mutation-run clause is ambiguous and may conflict with NFR-7 (Score: 60)

FR-9 places the MinVer workaround in the workflow, as upstream does. It then requires local mutation runs to work "with no manual environment setup beyond what `CONTRIBUTING.md` documents". If stryker-net#3094 also affects local runs, one developer will set `MinVerSkip` in `cake.cs`, which diverges from upstream and so touches NFR-7. Another will document an environment variable in CONTRIBUTING.md. FR-11's list of CONTRIBUTING.md edits doesn't mention the Mutation Testing section. AC-22 only exercises `MutationTestsTesting`, not the target that matters most.

**Evidence**: FR-9 says "Running a mutation target locally (`./build.ps1 -Target MutationTestsCore`) must also work, with no manual environment setup beyond what `CONTRIBUTING.md` documents." `CONTRIBUTING.md:219-233` has no MinVer note. Upstream sets `MinVerSkip` only in `mutation-tests.yml`.

**Recommendation**: Decide which it is. Either state that local runs are unaffected and verify it, or say where the local workaround lives and add that location to FR-11 and NFR-7's divergence list. Make AC-22 use `MutationTestsCore`, or all five targets.

---

### 9. The `CreateContext()` helper is misdescribed: f35bc00d adds it (Score: 55)

The Additional Context says the f35bc00d edit "relies on a private `CreateContext()` helper defined in that same file (line 270 upstream)". At 1a80392b, which is what FR-7 ports, the file has no `CreateContext`. f35bc00d itself adds `private static Context CreateContext() => new("operation-key");`. FR-12's wording, "replaced by a private `CreateContext()` helper", doesn't say to add it. Someone looking for an existing helper won't find one.

**Evidence**: `git show 1a80392b:test/Polly.Specs/Timeout/TimeoutOverloadSmokeSpecs.cs | grep CreateContext` finds nothing. `git show f35bc00d` shows `+    private static Context CreateContext() => new("operation-key");`.

**Recommendation**: Correct the Additional Context. Make FR-12 say "adds a private static `CreateContext()` helper and replaces every…".

---

### 10. FR-7 leaves out upstream's `#if !NETFRAMEWORK` guards in `RetryHelperTests` (Score: 55)

Upstream's `RetryHelperTests.cs` change does two things. It adds a test, and it wraps the `FsCheck` usings and the `Arbitraries` class in `#if !NETFRAMEWORK`, which the move to `FsCheck.Xunit.v3` needs for `net481`. FR-7's table only says "Gain upstream's added tests". Fences' file has `using FsCheck;` and `Arbitraries` unguarded today. The Windows `net481` build is where this would surface, and FR-1 requires that build to pass.

**Evidence**: The upstream diff adds `#if !NETFRAMEWORK` around `using FsCheck; using FsCheck.Fluent;` and around `public static class Arbitraries`. Fences' `test/Paramore.Fences.Core.Tests/Retry/RetryHelperTests.cs:1-2` has unguarded usings, and `Arbitraries` at line 316 is outside the line-266 `#if`.

**Recommendation**: Add the guards to FR-7's "Required outcome" for `RetryHelperTests.cs`.

---

### 11. Proposed Solution 2's "as they do now" contradicts PR #19's finding (Score: 52)

Proposed Solution 2 says the artefact shows coverage per project per TFM "as they do now". PR #19 found that today's TFM inner builds overwrite each other's report, so the artefact holds "one arbitrary framework's data". The baseline claim is wrong, and it hides that this is new behaviour.

**Evidence**: PR #19's body says "`ReportGeneratorTargetDirectory` was keyed by project only, so the three TFM inner builds overwrote each other's report". `eng/Test.targets:35` keys the target directory by `$(MSBuildProjectName)` only.

**Recommendation**: Drop "as they do now". State that per-TFM report contents are new.

---

### 12. Stale coverlet guidance in `.agent_instructions/dependency_management.md` is not in scope (Score: 52)

FR-11 limits documentation updates to three files, and AC-25 searches only those three for `coverlet`. `dependency_management.md` also describes the test stack as including coverlet, so it will be stale after the port.

**Evidence**: `.agent_instructions/dependency_management.md:42` says "test stack from `eng/Test.targets`. Do not add xUnit, Shouldly, NSubstitute or coverlet to a test…"

**Recommendation**: Add `dependency_management.md` to FR-11 and to AC-25's search. Also consider `.github/wordlist.txt:62`.

---

### 13. FR-5's rounding rule produces contradictory messages at the boundary (Score: 50)

The glossary says the comparison is on the unrounded value, "so 99.995 against 100 fails". FR-5 says the message prints the actual figure to 2 decimal places. For 99.995, the message would read "line coverage of 100% … is below the required threshold of 100%". Upstream's `{actual:0.##}` format has the same problem. The FR doesn't say how this case is reported.

**Evidence**: Glossary "Threshold check". FR-5 says "actual percentage (2 decimal places)". Upstream `cake.cs` formats with `{actual:0.##}%`.

**Recommendation**: Specify that the shortfall must be visible, for example by truncating rather than rounding, or by printing more decimal places when the rounded value equals the required one. Or accept upstream's behaviour and say so explicitly.

---

### 14. AC-14's "no `<Threshold>`" clause tests nothing and contradicts the glossary (Score: 50)

AC-14 uses `Paramore.Fences.AotTest` and `Paramore.Fences.TestUtils` as examples of projects without `<Threshold>`. Neither matches the `test/**/*{Tests,Specs}.csproj` glob, so the check never looks at them. The glossary also says they "are not test projects". Every real test project declares `<Threshold>`, so FR-5's "no `<Threshold>`" boundary has no meaningful AC.

**Evidence**: The glossary's "Test project" entry. `grep Threshold test/*/*.csproj` lists exactly the 5 test projects.

**Recommendation**: Have the AC temporarily remove `<Threshold>` from a real test project in an uncommitted experiment, or drop the clause.

---

### 15. FR-8's legacy-TFM list is incomplete, and A-3 could have been verified (Score: 42)

FR-8's error case names "`netstandard2.0` or `net462`". The three LegacySupport projects also target `net472`. A-3 says binding to the polyfill "is not verified". A grep settles it: every `GetOrAdd(` call site uses a 2-argument form. A-3 also cites `src/Paramore.Fences/Registry/PolicyRegistry.cs`, but that project doesn't set `<LegacySupport>`, so the polyfill isn't compiled into it.

**Evidence**: `src/Paramore.Fences.Core.csproj:4` has `net8.0;net6.0;netstandard2.0;net472;net462`. `git grep "GetOrAdd(" -- src` finds no 3-argument calls. `<LegacySupport>` is set only in Core, Extensions and RateLimiting.

**Recommendation**: Add `net472`. Record A-3 as verified, and remove the `PolicyRegistry` reference.

---

### 16. FR-12 has a garbled clause, and FR-13 and AC-28 use different terms (Score: 40)

FR-12 says the commit "touches no other file, unless a test method name was changed under FR-7, in which case only the matching Fences name is used". It mixes file scope with naming, and f35bc00d's edits don't touch method names at all. FR-13 allows "review fixes" after the f35bc00d commit, while AC-28 says "no non-fixup commits".

**Evidence**: The FR-12 last bullet, FR-13 bullet 1 and AC-28.

**Recommendation**: Reduce FR-12 to "touches only these three files". Use one term, "review fixes" or "fixup", in both FR-13 and AC-28.

---

### 17. NFR-2 contradicts itself (Score: 40)

NFR-2 first states that the `.PublicAPI` diff "is empty", then allows a `PublicAPI.Unshipped.txt` addition. AC-34 takes the permissive reading.

**Evidence**: NFR-2 says "`git diff <merge-base> -- 'src/**/.PublicAPI/*'` is empty… If any port change does need one… it is added to `PublicAPI.Unshipped.txt`".

**Recommendation**: Rephrase as "is empty, except for D7-divergence entries in `PublicAPI.Unshipped.txt`".

---

### 18. AC-11's expected count depends on the OS and the ADR (Score: 40)

AC-11 expects exactly 9 violations. On Windows, if the ADR chooses to check `net481` (FR-5 boundary), it would be 12. The AC doesn't name an OS.

**Evidence**: AC-11 says "one violation per metric per mandatory TFM (9 in total)". FR-5 leaves `net481` to the ADR.

**Recommendation**: Name the OS (macOS or Linux), or express the count as 3 × the number of checked TFMs.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 1 |
| 50-69 (Medium) | 13 |
| 0-49 (Low) | 4 |

**Total findings**: 18
**Findings at or above threshold (60)**: 8
