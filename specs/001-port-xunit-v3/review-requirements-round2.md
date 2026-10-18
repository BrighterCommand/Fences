# Review: requirements — 001-port-xunit-v3 (round 2)

**Date**: 2026-10-11
**Threshold**: 60
**Verdict**: NEEDS WORK

3 findings at or above threshold 60. Address these before approving.

Round 1's review is preserved in `review-requirements-round1.md`.

## Round 1 resolution

| # | Round-1 finding | Status (Resolved / Partly / Not resolved) | Note |
|---|---|---|---|
| 1 | #6 already closed | Resolved | Related line, FR-13, AC-29 and Additional Context now ask for a comment, not a close. `gh` confirms #6 is a closed Dependabot PR. |
| 2 | #46 hang needs an on-demand reproduction | Resolved | FR-14 now has a rerun as the primary path and a local run as the fallback, and treats #46's evidence as enough. AC-31 matches. The "13 hangs in 77 runs" figure matches #46. |
| 3 | PR #19's failing-gate fix not carried | Resolved | FR-4's "Reports survive a failing threshold check" bullet and AC-9a cover it. Upstream's Cake order (`__GenerateCoverageReports` before `__VerifyCoverageThresholds`) gives that result. The *other* PR #19 fix, per-TFM reports, is not delivered by upstream's code. See new Finding 1. |
| 4 | Positional `dotnet test` commands | Resolved | Scope is now every invocation. Each cited line (CONTRIBUTING.md:48/331/334/337/340, build_and_development.md:59/62/65/69, testing.md:122) was checked and is correct. AC-25 adds a grep for positional paths. |
| 5 | Open Dependabot PRs not reconciled | Resolved | FR-2 now has an open-PR rule, plus FR-13 and AC-29. #39 and #41 are confirmed open. |
| 6 | AC-16 grep count | Resolved | The pattern now counts added lines only, under `src test`. Upstream 1a80392b adds no `ExcludeFromCodeCoverage` lines, so the count starts at zero. |
| 7 | AC-23 throwaway CI run | Resolved | AC-23 now checks the configuration and the final green run. The same problem has come back in AC-9a's CI clause. See Finding 4. |
| 8 | FR-9 local MinVer ambiguity | Resolved | The workaround is documentation only, `cake.cs` is untouched (AC-21), it is in FR-11 and NFR-7 item 4, and AC-22 uses `MutationTestsCore`. |
| 9 | `CreateContext()` misdescribed | Resolved | FR-12 and Additional Context now say f35bc00d adds the helper. Checked against `git show f35bc00d`. |
| 10 | `#if !NETFRAMEWORK` guards | Resolved | Added to FR-7 and AC-17. Lines 1–2 and 316 are correct. |
| 11 | "as they do now" | Resolved | The wording is fixed. However, the replacement claim that the artefact now holds per-TFM content is false under upstream's code (Finding 1). |
| 12 | `dependency_management.md` stale | Resolved | It is added to FR-11 and AC-25. The new reason given for keeping the `.github/wordlist.txt` entry is factually wrong (Finding 7). |
| 13 | Rounding boundary | Resolved | Upstream's format is explicitly accepted (decision 6). AC-12 covers 99.995. |
| 14 | AC-14 tested non-test projects | Resolved | Now temporarily removes `<Threshold>` from `Paramore.Fences.RateLimiting.Tests`. Upstream's `GetCoverageThresholds` returns empty, so that project is skipped. |
| 15 | net472 / A-3 | Resolved | `net472` is added. A-3 is verified: every `GetOrAdd(` call in Core is 2-argument, and `<LegacySupport>` is set only in Core, Extensions and RateLimiting. |
| 16 | FR-12 garble, review-fix terms | Resolved | "Review fix" is in the glossary and used consistently in FR-13 and AC-28. FR-12 now says it touches only these three files. |
| 17 | NFR-2 self-contradiction | Resolved | Now reads "empty, except for D7-divergence entries in Unshipped". |
| 18 | AC-11 OS-dependent count | Resolved | Limited to macOS/Linux. |

## Findings

### 1. Upstream's report step overwrites each TFM's HTML report, so FR-4, AC-8, AC-9a, AC-23 and the PR #19 closure contradict NFR-7's "match upstream" (Score: 90)

Upstream's `__GenerateCoverageReports` sends every TFM of a project to the same `-targetdir` (`coverage-reports/<project>`), running `reportgenerator` once per coverage file. ReportGenerator overwrites `index.html` and `Cobertura.xml` on each run, and `-tag` does not accumulate anything without a history directory. The result is one HTML report per project that holds only the last file processed. Files are processed in `OrderBy(FullPath)` order (`net10.0`, `net481`, `net8.0`, `net9.0`), so that is `net9.0`. This is the same defect PR #19 was opened to fix ("the three TFM inner builds overwrote each other's report"). Only the job-step-summary half works per TFM, because each `SummaryGithub.md` is appended to the summary before the next run overwrites it.

The document requires the opposite, in several places:
- FR-4's example: "`artifacts/coverage-reports/Paramore.Fences.Specs/` contains an HTML report with tags `net10.0`, `net9.0` and `net8.0`".
- AC-8: "an HTML report exists with an entry tagged for each mandatory TFM".
- AC-9a and AC-23: reports "covering each mandatory TFM".
- Proposed Solution 2: "Per-TFM content is new".
- FR-13 and AC-29: PR #19 is closed "noting that both its fixes are carried by FR-4".
- Additional Context: "FR-4 carries both, so closing PR #19 loses nothing."

An implementer who follows NFR-7 and the glossary's "Upstream reference" ports upstream's loop and fails AC-8. An implementer who meets AC-8 has to change the target directory, which is a divergence that NFR-7's closed list does not permit. Two developers will build different things, and one of them breaks a stated requirement.

**Evidence**: `git show 1a80392b -- cake.cs`: `var targetDirectory = System.IO.Path.Combine(coverageReportsDir, projectName);` sits outside the per-file loop, and `-targetdir:\"{targetDirectory}\" ... -tag:{tag}` is inside it. ReportGenerator 5.5.11 (`~/.nuget/packages/reportgenerator/5.5.11/tools/net10.0`) was run three times into one target directory with tags `net10.0`, `net8.0` and `net9.0`. The resulting `index.html` contains only `net9.0`, and `Cobertura.xml` holds only the last file's rates.

**Recommendation**: Choose one of these.
(a) Accept upstream's behaviour. Per-TFM coverage is then guaranteed for coverage files, the threshold check and the job summary, but not for the HTML artefact. Rewrite FR-4's example, AC-8, AC-9a, AC-23 and Proposed Solution 2 to match, and change the PR #19 closing note to say that its per-TFM HTML fix is *not* carried. That means either keeping PR #19 open, or recording the loss on it.
(b) Require per-TFM report directories, for example `coverage-reports/<project>/<tfm>/`, and add this as a sixth divergence in NFR-7, recorded in the ADR.
Decision 6 (minimise drift) points to (a) unless Ian wants the HTML fix kept.

---

### 2. AC-9 cannot pass: ReportGenerator succeeds on an empty or malformed coverage file (Score: 72)

AC-9 expects an empty `coverage.net9.0.xml` to fail the report step "with a message naming the test project and `net9.0`". ReportGenerator exits 0 on an empty file and on a malformed one, and writes an empty report. Upstream's `result != 0` check therefore never fires. The build still fails later, because `__VerifyCoverageThresholds` calls `XDocument.Load`, which throws `XmlException` ("Root element is missing"). That message names neither the project nor the TFM. So AC-9 fails if the implementation follows upstream, and passing it requires validating input before or after report generation, or wrapping the load. Either is a divergence that NFR-7 does not list. FR-4's error case ("if report generation fails…") is upstream behaviour, but AC-9's example does not trigger it.

**Evidence**: ReportGenerator 5.5.11 exited 0 on a zero-byte `coverage.empty.xml`, and also on a file containing `not xml <`. Upstream's code is `if (result != 0) { throw new InvalidOperationException($"Failed to generate the coverage report for '{projectName}' ({tag})."); }`, and `GetAssemblyCoverage` calls `XDocument.Load(coberturaPath)` without a try/catch.

**Recommendation**: Drop AC-9's example, or replace it with one that does make `reportgenerator` exit non-zero, if one exists. Alternatively, verify FR-4's error case by inspecting the code (a non-zero exit throws with project and TFM). If Ian wants unreadable coverage files reported by name, add that as a requirement and as an NFR-7 divergence.

---

### 3. NFR-7's "closed" divergence list is undermined by ADR delegations in FR-3, FR-5 and FR-10, and by NFR-7's own wording (Score: 70)

NFR-7 says "The permitted list is closed… Any divergence not on this list needs the requirements amended first". It also says "Where a requirement does not explicitly say otherwise, … match the upstream reference", which lets any FR create a divergence. Three FRs then give the ADR room to diverge without amending the requirements:
- FR-3: "The `artifacts/coverage/` root is upstream's reference location. The ADR may choose another".
- FR-10: "…unless the ADR moves the report location".
- FR-5 `net481` boundary and Open Question 1: "the ADR decides whether they are checked". Upstream has already decided this. Its new `Test.targets` drops the `.NETFramework` condition, so `--coverage` runs on every TFM, and `__VerifyCoverageThresholds` checks every `coverage.*.xml` it finds. Upstream therefore checks `net481` on Windows. If the ADR picks "not checked", that is a divergence missing from the closed list. If it picks "checked", the open question was never open.

The result is that two designers can make different ADR choices and both claim to satisfy the requirements. This contradicts decision 6.

**Evidence**: `git show 1a80392b -- eng/Test.targets` removes `<PropertyGroup Condition="...GetTargetFrameworkIdentifier... != '.NETFramework'">` and adds an unconditional `--coverage`. The verify step uses `GetFiles(... "coverage.*.xml")` with no TFM filter. Test projects add `net481` on Windows (`test/*/*.csproj:4`).

**Recommendation**: Remove the "where a requirement does not explicitly say otherwise" escape, or make it refer only to the five listed items. Fix the coverage and report locations to upstream's. Resolve the `net481` question as upstream does (collected and checked on Windows), and delete Open Question 1. If Ian wants `net481` excluded, add it to NFR-7 as a sixth divergence.

---

### 4. AC-9a's CI clause brings back the throwaway failing CI run that round 1 removed (Score: 58)

AC-9a ends: "On CI, a failing threshold check still uploads a `coverage-<os>` artefact containing those reports." The only way to observe this is a CI run with a deliberately failing threshold. That is the same unrecorded throwaway run that round-1 Finding 7 objected to in AC-23, which the revision fixed there by checking configuration instead. FR-13's PR-body list does not ask for evidence of such a run.

**Evidence**: AC-9a's last sentence. FR-10 already gives the mechanism: "the upload step's existing `if: always()`" (`build.yml:104`).

**Recommendation**: Replace the CI clause with a configuration check: "the Upload Coverage Reports step keeps `if: always()` and reads the FR-4 report directory". Keep the local experiment as the behavioural check.

---

### 5. FR-13's PR-body contents list misses items other requirements send to the PR body (Score: 52)

FR-13, which AC-28 copies, lists the PR-body items as FR-2, FR-6, FR-8, FR-9, FR-14 and FR-15. Two other requirements also ask for PR-body content:
- FR-2's major-version rule: "record it as an open item in the PR body".
- NFR-4: "If it is longer, the PR body records the measured times and the cause." AC-36 checks this, but AC-28 does not.

**Evidence**: FR-2 Rules bullet 2, NFR-4, the FR-13 PR-body bullet, and AC-28.

**Recommendation**: Add both, as conditional items, to FR-13's list and AC-28's section list.

---

### 6. The FR-14 baseline is tied to a "merge base" that moves (Score: 50)

The glossary defines Merge base as the commit the branch "was created from, or was last updated from". FR-14 records the baseline "before any implementation change… at the merge base", and AC-30 compares against "baseline mutation scores recorded at the merge base". If the branch is updated from `main` during the work, the merge base moves, but the baseline was recorded at the old one. The document also doesn't say whether the baseline comes from CI's `mutation-tests` runs on `main` or from local runs, even though FR-14's fallback compares against a local run.

**Evidence**: Glossary "Merge base", FR-14's first sentence, AC-30.

**Recommendation**: Say that the baseline is re-recorded whenever the merge base changes, or pin it to a named SHA recorded in the PR body. State where the baseline is measured (CI on `main`, or local), and say that a local-fallback result is compared against a baseline measured the same way.

---

### 7. FR-11's reason for keeping `coverlet` in the wordlist is false (Score: 45)

FR-11 says "`.github/wordlist.txt`'s `coverlet` entry stays, because `fork-migration-plan.md` still uses the word and the spell check reads it." The spell check explicitly excludes that file. After FR-11's edits, no spell-checked Markdown file in the repository uses "coverlet", although this spec and the new ADR will once they are committed.

**Evidence**: `.github/spellcheck.yml` sources: `'!CHANGELOG.md|!fork-migration-plan.md|…|**/*.md'`. `git grep -il coverlet -- '*.md'` finds only the three FR-11 files and `fork-migration-plan.md`.

**Recommendation**: Keep the entry, but give the correct reason: the ADR and the spec files for this work mention coverlet. Alternatively, leave the decision to the ADR.

---

### 8. Renaming upstream's new Specs methods creates drift that FR-12 runs into straight away (Score: 45)

NFR-7 item 2 and FR-7 require `Member_Scenario_Outcome` names for all 12 new `Paramore.Fences.Specs` methods. Upstream's names, such as `Generic_dictionary_members_should_delegate_to_the_wrapped_dictionary`, match the Specs project's existing legacy style, which the Out of Scope section leaves alone. The cost shows up at once: f35bc00d edits `Generic_dictionary_members_should_delegate_…` and `Async_policy_execute_overloads_…_should_delegate_correctly`, which FR-7 will already have renamed. Every future upstream change to these five files will need the same name mapping. This is a listed divergence, and the binding decisions accept the list, so it scores low. It is the largest recurring source of drift in the spec and is worth a deliberate confirmation from Ian.

**Evidence**: `git show 1a80392b -- test/` adds 12 Specs `[Fact]` methods with snake_case names. `git show f35bc00d` hunks fall inside two of them. `.agent_instructions/testing.md:33` sets the naming rule.

**Recommendation**: Either confirm the rename explicitly, noting the cost for future ports, or exempt methods ported verbatim from upstream into `Paramore.Fences.Specs` and drop NFR-7 item 2 for Specs.

---

### 9. AC numbering: AC-9a comes before AC-9 (Score: 30)

AC-9a was inserted ahead of AC-9, and the traceability table lists "AC-8, AC-9, AC-9a". This is cosmetic, but it is an odd identifier for the AC that carries a whole FR-4 rule.

**Evidence**: The Acceptance Criteria section and the Traceability rows for FR-4 and FR-10.

**Recommendation**: Renumber in order (for example, make AC-9a into AC-10 and shift the rest), or at least put AC-9a after AC-9.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 1 |
| 70-89 (High) | 2 |
| 50-69 (Medium) | 3 |
| 0-49 (Low) | 3 |

**Total findings**: 9
**Findings at or above threshold (60)**: 3
