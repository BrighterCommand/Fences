# Review: requirements — 001-port-xunit-v3 (round 3)

**Date**: 2026-10-11
**Threshold**: 60
**Verdict**: PASS

No findings at or above threshold 60. Consider addressing lower-scored items.

Earlier rounds are preserved in `review-requirements-round1.md` and `review-requirements-round2.md`.

## Round 2 resolution

| # | Round-2 finding | Status (Resolved / Partly / Not resolved) | Note |
|---|---|---|---|
| 1 | Upstream overwrites each TFM's HTML report | Resolved | The glossary ("Coverage report"), Proposed Solution 1–2, FR-4's shared-directory boundary, AC-8 ("no per-TFM subdirectory"), AC-10, FR-13/AC-30 (PR #19 note says the per-TFM HTML fix is *not* carried), Out of Scope and Additional Context all now say this consistently. A grep for "tagged" and "per-TFM" turns up no leftover per-TFM HTML claim. Checked against `git show 1a80392b -- cake.cs`: `targetDirectory` is outside the per-file loop. |
| 2 | AC-9 could not pass (reportgenerator exits 0 on empty files) | Resolved | AC-9 is now checked by inspection. The empty-file case moved to AC-15 and FR-5's "unreadable coverage file" boundary, which matches upstream's bare `XDocument.Load` (decision 10). FR-4's error case and Additional Context agree. |
| 3 | ADR delegations undermined NFR-7's closed list | Resolved | The "may choose another" wording in FR-3, the "unless the ADR moves" wording in FR-10, the net481 open question and the "where a requirement does not explicitly say otherwise" escape are all gone. net481 is collected and checked (FR-1, FR-3, FR-5, FR-6, AC-11), which matches upstream's unconditional `--coverage` and the unfiltered `coverage.*.xml` glob. NFR-7 says "No FR leaves a choice to the ADR that would diverge". The remaining ADR choices (the `--filter` syntax and recording divergences) are docs and records, not divergences. |
| 4 | Throwaway failing CI run in AC-9a | Resolved | AC-10 now checks `if: always()` and the `./artifacts/coverage-reports` path in `build.yml`. Both are confirmed at `build.yml:104–108`. |
| 5 | FR-13 PR-body list incomplete | Resolved | FR-13 and AC-29 now include the major-version open item and the NFR-4 timings as conditional items. |
| 6 | FR-14 baseline tied to a moving merge base | Resolved | Baseline source (CI `mutation-tests` at the merge-base SHA), the re-take on a moving merge base, and like-for-like comparison are all stated. AC-31 matches. `mutation-tests` runs on every push to `main` with no path filter, and c41497ac has a successful run. |
| 7 | Wrong reason for keeping `coverlet` in the wordlist | Resolved | The reason is now this spec and the ADR. `.github/spellcheck.yml`'s `**/*.md` source covers `specs/**` and `docs/adr/**`, and `fork-migration-plan.md` is excluded. |
| 8 | Renaming upstream Specs methods causes drift | Resolved | FR-7 keeps upstream names verbatim (decision 9). Out of Scope adds "or renaming test methods ported from upstream". FR-12 relies on the matching names. The old NFR-7 naming item is removed, leaving four items. Upstream's two Core.Tests names were checked as `Member_Scenario_Outcome`. |
| 9 | AC-9a before AC-9 | Resolved | ACs now run AC-1..AC-39 in order. Every prose cross-reference (FR-10's "AC-24", AC-9's "AC-15") and every traceability row points at the right AC. |

Round-1 spot-checks show no regressions. The #6 comment-not-close wording, the AC-15 `<Threshold>`-removal clause on RateLimiting, the AC-12 limit to macOS and Linux, NFR-2's Unshipped exception, the `#if !NETFRAMEWORK` guards and the `CreateContext()` helper are all still correct. #39, #41 and #19 are still open, and #6 is still closed.

## Findings

### 1. AC-13, AC-14 and AC-15 rely on edited coverage files but don't say how the check is run on its own (Score: 45)

AC-14 deletes `coverage.net9.0.xml` "after the test run", AC-15 replaces a file with an empty one "after the test run", and AC-13 needs files where only `net8.0` falls short. Each then says "when the threshold check runs". The check can't be reached that way through the documented entry point. Under upstream's Cake graph, `__VerifyCoverageThresholds` `IsDependentOn("__RunTests")`, and `Build` also depends on `__Clean`, so any `./build.ps1` run re-runs the tests and rewrites the files you edited. `build.ps1` has a fixed `Param` block (Script, Target, Configuration, Verbosity, WhatIf…) and doesn't pass through a `--exclusive` flag. A verifier could run `dotnet cake.cs --target=__VerifyCoverageThresholds --exclusive`, or use a test harness, but the ACs don't say so, so two people may verify these ACs differently, or not be able to run them at all.

**Evidence**: upstream `cake.cs`: `Task("__VerifyCoverageThresholds").IsDependentOn("__RunTests")`. Fences `cake.cs:58–71` cleans `artifactsDir`. `build.ps1` Param block.

**Recommendation**: Add one sentence, to AC-13 or to a shared note, naming how the threshold check is run in isolation against edited files (for example, Cake's exclusive-target option). Alternatively, leave it to tasks.md explicitly.

---

### 2. FR-3's `[ExcludeFromCodeCoverage]` requirement has no direct AC (Score: 40)

FR-3 says "Code marked `[ExcludeFromCodeCoverage]` is excluded from the coverage figures". No AC checks this directly. It is covered indirectly: `src/` has about 20 uses (LegacySupport, `Guard.cs`, `ResiliencePipelineContext`, `DelegatingComponent`, `ValidationHelper`), so the 100% thresholds in AC-11 would very likely fail if the exclusion didn't work. AC-39 also requires matching upstream, which relies on Microsoft CodeCoverage's default attribute exclusion. So this is low risk, but it isn't stated.

**Evidence**: `git grep -c ExcludeFromCodeCoverage -- src`. No AC mentions the exclusion.

**Recommendation**: Optionally add a clause to AC-6 or AC-11, for example "a member marked `[ExcludeFromCodeCoverage]` (such as in `Guard.cs`) is absent from the covered assembly's `package` in the Cobertura file". Or note that AC-11 covers it.

---

### 3. FR-9 and NFR-7 disagree on whether the `MinVerSkip` doc note is a divergence (Score: 35)

FR-9 says the local `MinVerSkip` note "is part of FR-11, and is listed as an NFR-7 divergence". NFR-7 lists it as item 3, but also says "FR-11's documentation changes are Fences-owned files, not divergences". AC-39 accepts either label. That makes the outcome harmless, but the closed list contains an item that the same paragraph says isn't a divergence.

**Evidence**: FR-9's "Affected" bullet. NFR-7 item 3 and its closing sentence.

**Recommendation**: Pick one. Either drop item 3 (it's an FR-11 doc change), or say "FR-11's documentation changes, other than item 3, are not divergences".

---

### 4. The example PR title says "with per-framework coverage", but the HTML report is deliberately not per framework (Score: 25)

FR-13's example title, "Move tests to xunit v3 and Microsoft Testing Platform v2, with per-framework coverage", becomes the CHANGELOG entry (C-2, ADR 0003 D8). It is accurate for the coverage files, the threshold check and the job summary. But readers could take it to mean per-TFM HTML reports, which Out of Scope and the PR #19 note explicitly drop.

**Evidence**: FR-13's title bullet, C-2, Out of Scope's "Per-TFM HTML coverage reports".

**Recommendation**: Reword the example, for example "…with per-framework coverage thresholds".

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 0 |
| 50-69 (Medium) | 0 |
| 0-49 (Low) | 4 |

**Total findings**: 4
**Findings at or above threshold (60)**: 0
