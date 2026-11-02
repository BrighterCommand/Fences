# Bugfix: `update-baselines.ps1` never moves Unshipped public API entries to Shipped

**Linked Issue**: none (found while preparing 9.0.2)
**Status**: Verified (approved 2026-11-02; failing on 0 files, `*REMOVED*` and blank-line handling kept out of scope)

## Symptom

`eng/update-baselines.ps1` runs from `.github/workflows/after-release.yml` (step "Update public API baselines").
It should move every entry from each `src/<Project>/.PublicAPI/PublicAPI.Unshipped.txt` into the matching
`PublicAPI.Shipped.txt`, sort both, and keep the `#nullable enable` header
(`.agent_instructions/release_and_versioning.md:70-72`).

What actually happens: the script exits with code 0 and changes nothing.

- The after-release pull requests for 9.0.0 (#13) and 9.0.1 (#67) each changed only `CHANGELOG.md`
  (`gh pr view 13 --json files`, `gh pr view 67 --json files`).
- `Paramore.Fences.DependencyInjection.AddResiliencePipelineContext<TKey>.EnableReloadsWithMonitor<TOptions>(...)`
  has shipped since 9.0.0. It is still the only entry in
  `src/Paramore.Fences.Extensions/.PublicAPI/PublicAPI.Unshipped.txt`, and the last commit to touch that file is
  the rename commit `ad904b6ef`.
- Upstream Polly has the same problem. Its release commits after the move (`f55d6185`, `e3b73ae5`,
  "Update CHANGELOG") changed only `CHANGELOG.md` and `Directory.Packages.props`, and the same entry (added by
  `f2a07e687`) is still in `src/Polly.Extensions/.PublicAPI/PublicAPI.Unshipped.txt`. Before the move it worked:
  upstream `afd4df1a` (2025-06-10, "Update CHANGELOG") moved two lines from `src/Polly.Core/PublicAPI.Unshipped.txt`
  to `src/Polly.Core/PublicAPI.Shipped.txt`.

Reproduction (on a throwaway copy, `pwsh` 7.6 on macOS):

1. Copy `eng/update-baselines.ps1` to `<tmp>/eng/`, and copy each `src/*/.PublicAPI/*.txt` to the same relative
   path under `<tmp>/src/`. Run `git init` and commit, so changes are easy to see.
2. Run `pwsh -NoProfile -File eng/update-baselines.ps1` in `<tmp>`. It exits with 0 and `git status --porcelain`
   is empty.
3. In `<tmp>`, `(Get-ChildItem -Path src -Filter PublicAPI.Unshipped.txt -Recurse).Count` returns `0`. Adding
   `-Force` returns `5`. `(Get-Item -Force src/Paramore.Fences/.PublicAPI).Attributes` returns `Hidden, Directory`.

Expected: after step 2, the `EnableReloadsWithMonitor` line is in
`src/Paramore.Fences.Extensions/.PublicAPI/PublicAPI.Shipped.txt`, and the Unshipped file holds only
`#nullable enable`.

## Suspected Location

- `eng/update-baselines.ps1:12`: `$files = Get-ChildItem -Path $src -Filter $unshippedName -Recurse` has no
  `-Force`. On Linux and macOS, `pwsh` treats directories whose names start with a dot as hidden, so it skips
  `.PublicAPI/`.
- `eng/update-baselines.ps1:27-62`: the `foreach` loop does all the work. If `$files` is empty, nothing runs and
  nothing reports an error.
- `eng/update-baselines.ps1:28-30`: the Shipped and Unshipped paths come from the directory of each file found.
- `eng/update-baselines.ps1:40-48`: skips the `#nullable enable` header and moves the remaining lines.
- `eng/update-baselines.ps1:15-25` and `:50-52`: the comparer, then sorting `$additions` and `$baseline`.
- `eng/update-baselines.ps1:54-58`: rewrites Unshipped, either as the header alone or with `$null | Set-Content`.
- `eng/update-baselines.ps1:60`: rewrites Shipped.
- `.github/workflows/after-release.yml:42-44`: the call site (`run: ./eng/update-baselines.ps1`, `shell: pwsh`).
  The job runs on `ubuntu-24.04` (`after-release.yml:18`).
- `.github/workflows/after-release.yml:52-57`: the "No changes to commit." guard.
- `eng/Library.targets:68-81`: wires `.PublicAPI\PublicAPI.*.txt`, and optional per-target-framework folders
  `.PublicAPI\<tfm>\`, into the analyzers. None of those folders exist today.

## Root-Cause Hypothesis

**Hypothesis (UNVERIFIED — to be proven or refuted in /bugfix:confirm):** on Linux and macOS,
`Get-ChildItem -Recurse` without `-Force` skips the hidden `.PublicAPI/` directories. So `$files` at
`eng/update-baselines.ps1:12` is empty, the loop never runs, and the script finishes without error.

The defect started with upstream `dbe4eb7d` ("Move Public API baselines (#3016)", 2026-04-03). That commit moved
every `PublicAPI.*.txt` from `src/<Project>/` into `src/<Project>/.PublicAPI/` and added the
`eng/Library.targets` wiring. It did not change the script, whose last commits (`20cf89205`, 2024-04-25, and
`da315ba9f`, 2025-03-18) both came earlier. Fences inherited the move, so no Fences release has ever run the
script against the real files.

Falsification test: if the copy is run with `-Force` added at line 12 and still changes nothing, or if CI
(`ubuntu-24.04`) does not treat dot-directories as hidden, the hypothesis is wrong.

The suggested fix, adding `-Force` to the `Get-ChildItem` call at line 12, is
**UNVERIFIED — to be proven or refuted in /bugfix:confirm.** Points for confirm to check:

1. **Unwanted matches with `-Force`.** In the working tree only the 10 files under `src/*/.PublicAPI/` exist.
   Build output goes to `artifacts/` at the repository root, outside `$src`. `-Force` also enters other hidden
   folders and returns hidden files. The after-release job does not build, so this is probably harmless, but it is
   not proven.
2. **BOM and header check (lines 35 and 42).** Every Unshipped file starts with the `UTF-8` BOM `EF BB BF`. In
   `pwsh` 7.6, `Get-Content` strips it: `$a[0] -eq "#nullable enable"` returned `True`, and `$a[0].Length` was
   `16`. Confirm this on `ubuntu-24.04` too.
3. **Header position in Shipped after sorting (line 52).** `$baseline.Sort($comparer)` sorts the
   `#nullable enable` line with the API lines rather than handling it separately. It stays first only because
   `#` (`0x23`) sorts before every first character in use: letters, `~`, and `*` (as in `*REMOVED*`).
4. **Possible second defect: blank lines.** `src/Paramore.Fences/.PublicAPI/PublicAPI.Shipped.txt` has no
   `#nullable enable` header and ends with an empty line 1067. An empty string sorts before everything, so the
   next time that project has Unshipped entries the blank line would move to the top of Shipped. Today its
   Unshipped file is BOM-only, so `$edited` stays `$false` and the sort is not reached. Blank lines in Unshipped
   would also be copied into Shipped.
5. **Possible second defect: `*REMOVED*` entries.** Lines 43-48 copy every Unshipped line into Shipped as is. A
   `*REMOVED*<api>` line would be added to Shipped as text, when the shipped `<api>` line should be deleted. No
   `*REMOVED*` lines exist today, so this is latent.
6. **The `$null | Set-Content` branch (line 57).** It runs when the Unshipped file had no header. Confirm that it
   writes a file the analyzers accept, and that the header-only rewrite at line 55 writes `#nullable enable`
   followed by a newline.
7. **Line endings and duplicates.** Line 60 writes platform line endings (LF on Ubuntu), which matches the
   current files. Duplicate lines are not removed. Neither looks like a problem now.

**Would the workflow guard have caught this?** No. `after-release.yml:53-57` throws only when
`git status --porcelain` is empty. `update-changelog.ps1` always changes `CHANGELOG.md`, so the guard passes
whether or not the script found any baseline files. The script never reports how many files it processed, so the
job stays green.

## Confirmed Root Cause

**Verdict: CONFIRMED.**

`$files = Get-ChildItem -Path $src -Filter $unshippedName -Recurse` (`eng/update-baselines.ps1:12`) does not pass `-Force`.
`pwsh` reads `FileSystemInfo.Attributes`, and on Unix (Linux and macOS) .NET sets `FileAttributes.Hidden` on any name that
starts with `.`. Without `-Force`, `Get-ChildItem -Recurse` neither returns hidden items nor goes into hidden folders.
Upstream `dbe4eb7d` moved every baseline into `src/<Project>/.PublicAPI/`, so `$files` is empty, the loop at
`eng/update-baselines.ps1:27-62` runs zero times, and the script exits 0 with no output. The after-release job runs on
`ubuntu-24.04` (`.github/workflows/after-release.yml:18`), which takes the same Unix path. `CHANGELOG.md` always changes,
so the guard at `after-release.yml:53-57` never fires.

## Evidence

- [x] Code-trace: `after-release.yml:42-44` runs `./eng/update-baselines.ps1` under `pwsh` on `ubuntu-24.04`;
  `eng/update-baselines.ps1:10-11` sets `$src` to `<repo>/src`; line 12 searches without `-Force`, so the hidden
  `.PublicAPI` folders are skipped (`0` files found; `5` with `-Force`; `(Get-Item -Force src/Paramore.Fences/.PublicAPI)`
  reports `Hidden, Directory`); line 27 loops over nothing. A `.PublicAPI` folder created at run time with `New-Item` is
  hidden in the same way (`0` without `-Force`, `1` with it).
- [x] Red reproduction, on a throwaway copy of the script and all ten `src/*/.PublicAPI/*.txt` files: after running the
  script, `src/Paramore.Fences.Extensions/.PublicAPI/PublicAPI.Shipped.txt` should contain the `EnableReloadsWithMonitor`
  line, and the matching Unshipped file should be exactly the BOM plus `#nullable enable` and a newline. Actually: exit code
  `0`, no file changed, and the entry is still in Unshipped. Reproduced independently by the main agent.

**The same copy with only `-Force` added at line 12.** Exit code `0`, and only the Extensions pair changes:

- **Extensions Shipped:** one BOM, `#nullable enable` on line 1, a single trailing LF, no carriage returns, and the entry
  sorted in after `...EnableReloads<TOptions>(...)`. Two existing lines also move, because the file was already out of the
  order the comparer produces: `Reg` sorts before `Res` under `OrdinalIgnoreCase`, and that order came from upstream. That is a
  small, correct re-sort.
- **Extensions Unshipped:** exactly `EF BB BF` + `#nullable enable` + newline (20 bytes), the same as the other projects'
  Unshipped files.
- **Projects with nothing Unshipped** (`Core`, `RateLimiting`, `Testing`, and the BOM-only `Paramore.Fences`): not
  rewritten, because `$edited` stays `$false`.

The resulting files have the layout the analyzers accept today. No build was run against them.

**Suggested fix: CONFIRMED.** Adding `-Force` at `eng/update-baselines.ps1:12` is enough, and correct, for this symptom.

**The triage points:**

1. **Unwanted matches with `-Force`: not a defect.** `Directory.Build.props:5` sets `UseArtifactsOutput`, so `bin` and
   `obj` go to `artifacts/` outside `src`; the only hidden folders under `src` are the `.PublicAPI` folders; the
   after-release job never builds; `-Filter` still limits matches to `PublicAPI.Unshipped.txt`. `-Force` also reaches the
   optional per-target-framework folders (`eng/Library.targets:76-83`), each of which has its own pair, so they would work.
2. **BOM and header check: not a defect.** `Get-Content` strips the BOM, the header is detected, and no BOM is duplicated.
3. **Header position after sorting: latent.** Only a blank line sorts above `#`. No Shipped file with a header has blank
   lines; a hand-added blank line in Unshipped would trigger it.
4. **Blank line in `src/Paramore.Fences/.PublicAPI/PublicAPI.Shipped.txt`: latent and cosmetic.** That file has no header,
   is also out of order (10 lines), and ends with a blank line. The first release that adds an Unshipped entry for that
   project would move the blank line to the top and re-sort the 10 lines.
5. **`*REMOVED*` entries: a real logic gap, but latent.** A `*REMOVED*` line would be copied into Shipped as text, and the
   original line kept. The Roslyn convention is to delete the Shipped line and drop the `*REMOVED*` line. No `*REMOVED*`
   lines exist today, and upstream has the same gap.
6. **The `$null | Set-Content` branch: not a defect.** For an Unshipped file with no header it writes a BOM-only file, which
   matches today's state.
7. **Line endings and duplicates: not a defect.** LF on Unix, matching the files; no duplicates in any Shipped file.

## Scope Notes

**In scope.**

- `eng/update-baselines.ps1:12`: add `-Force`. The proven cause and the minimal fix.
- A new `eng/update-baselines.Tests.ps1` regression test. Like `eng/update-changelog.Tests.ps1`, it copies the script into
  `TestDrive:` and creates `src/<Project>/.PublicAPI/` with a Shipped and an Unshipped file. It asserts that the entry
  moves to Shipped, that Unshipped is left as `#nullable enable` plus a newline, and that a project with nothing unshipped is
  not rewritten. A `.PublicAPI` folder created there is hidden too, so the test is red today on macOS and Linux. The
  `lint.yml` Pester step runs it automatically.

**Out of scope** (possible follow-ups):

- **Failing when no files are found** (`eng/update-baselines.ps1:12-27`, `after-release.yml:53-57`). It would have made this
  bug visible, but the same kind of item was kept out of scope for bugfix 0002.
- **`*REMOVED*` handling** (`eng/update-baselines.ps1:43-48`). Real but latent, and a behaviour change beyond this bug.
- **Blank-line handling and sorting the header separately** (`eng/update-baselines.ps1:42-52`). Latent and cosmetic.
- **Re-sorting existing Shipped files by hand.** The script corrects Extensions' two lines at the next release, and hand
  edits to baselines are not allowed.

## Regression Test

`eng/update-baselines.Tests.ps1` (Pester 6.2.0). Each test copies the script into a `TestDrive:` root and writes a
project's baselines into `src/Library/.PublicAPI/`, which is hidden in the same way as the real folders.

| Test | Before the fix |
| --- | --- |
| `UpdateBaselines_UnshippedEntryInPublicApiFolder_MovesEntryToShipped` | Red: the entry is not moved to Shipped |
| `UpdateBaselines_NothingUnshipped_LeavesBaselinesUnchanged` | Green guard: both files stay byte-for-byte the same |

Approved 2026-11-02.

## Fix

- `eng/update-baselines.ps1:13`: pass `-Force` to `Get-ChildItem`, with a comment saying why, so the search enters the
  hidden `.PublicAPI` folders.

Both regression tests pass, as do the five `update-changelog.ps1` tests. PSScriptAnalyzer 1.24.0, with the settings CI
uses, reports no issues across the repository. Run on a copy of the real baselines, the fixed script moves the
`EnableReloadsWithMonitor` entry into Extensions' Shipped file and leaves the other four projects untouched.
