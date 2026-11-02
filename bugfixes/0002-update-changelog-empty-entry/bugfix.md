# Bugfix: `update-changelog.ps1` writes an empty entry when the release body starts with a comment

**Linked Issue**: none (found in bot PR #67)
**Status**: Verified (failing loudly on 0 bullets kept out of scope)

## Symptom

**Observed.** When the 9.0.1 release was published, `.github/workflows/after-release.yml` ran
`eng/update-changelog.ps1`. The script inserted an entry after `<!-- next-release -->` in `CHANGELOG.md`
that held only the heading `## 9.0.1` and no bullets. That empty entry went into bot PR #67, and the
maintainer later filled it by hand in commit `808efe2a` on branch `changelog-9.0.1`.

**Expected.** The entry lists every `*` line under `## What's Changed` in the release body, up to the
next `##` heading (`## New Contributors`), with each `@login` turned into a profile link. For the 9.0.1
body that is 19 bullets, from PR #20 through PR #65.

**The 9.0.1 release body** (from `gh api repos/BrighterCommand/Fences/releases/tags/9.0.1 -q .body`).
Its lines end in CRLF (`\r\n`):

1. `<!-- Release notes generated using configuration in .github/release.yml at main -->`
2. (blank, `\r` only)
3. `## What's Changed`
4. 19 `* ... by @iancooper in https://.../pull/N` lines
5. blank, then `## New Contributors`, then `* @github-actions[bot] made their first contribution ...`
6. blank, then `**Full Changelog**: ...`

**Reproduction** (use a throwaway copy; never run against the tracked `CHANGELOG.md`):

1. Copy `eng/update-changelog.ps1` to `<tmp>/eng/` and `CHANGELOG.md` to `<tmp>/`. The script finds the
   changelog through `$PSScriptRoot/..`.
2. Run `pwsh -NoProfile -File <tmp>/eng/update-changelog.ps1 9.0.1 (Get-Content -Raw body901.txt)
   https://github.com`. Pass the body as one string, the same way the workflow passes `${env:RELEASE_NOTES}`.
3. Look at `<tmp>/CHANGELOG.md`. Under `<!-- next-release -->` there is `## 9.0.1` and no bullets.

Running the script's loop (lines 28-44) in memory with `pwsh` 7.6 on the saved body stops at
`## What's Changed` and keeps 0 bullets.

## Suspected Location

- `eng/update-changelog.ps1:28`: `` $ReleaseNotesText.Split("`n") | Select-Object -Skip 1 `` splits on LF
  only, so each line keeps its trailing `\r`. It also always drops line 1 of the body, assuming line 1 is
  the `## What's Changed` heading.
- `eng/update-changelog.ps1:30-32`: the `-eq ""` blank-line skip. The blank line `"\r"` is not equal to
  `""`, so this check never matches a CRLF blank line.
- `eng/update-changelog.ps1:33-35`: `StartsWith("##")` then `break`. This is meant to stop at the *next*
  section. Because line 1 was skipped instead of the heading, it fires on the first section's own heading.
- `eng/update-changelog.ps1:36-38`: the `StartsWith("* ")` filter. It drops any line that is not a bullet.
- `eng/update-changelog.ps1:41`: the `@login` → `[@login](server/login)` replace. The trailing `\r` stays
  on the line.
- `eng/update-changelog.ps1:43`, `:46-49`: the entry is added, inserted after `<!-- next-release -->`
  (`CHANGELOG.md:12`), and written back with `Set-Content`.
- `.github/workflows/after-release.yml:11`: `RELEASE_NOTES: ${{ github.event.release.body }}`.
- `.github/workflows/after-release.yml:38-40`: the call site
  `./eng/update-changelog.ps1 ${env:RELEASE_VERSION} ${env:RELEASE_NOTES} ${env:GITHUB_SERVER_URL}`.
- Background: `.github/workflows/release.yml:49` and `:56` create the draft release with
  `generate_release_notes: true`. On this repository that produces the leading HTML comment.

The script has not been changed since upstream Polly (`git log`: `32064ed55`, `20cf89205`).

## Root-Cause Hypothesis

**H1 (primary).** `update-changelog.ps1:28` assumes line 1 of the release body is the `## What's Changed`
heading and drops it with `Select-Object -Skip 1`. Then `:33-35` stops at the first `##` it sees.
Upstream Polly's release bodies (e.g. 8.9.0) start with `## What's Changed`, so the assumption holds
there. Fences' generated notes start with the
`<!-- Release notes generated using configuration in .github/release.yml at main -->` comment and a blank
line. So:

1. `Skip 1` drops the comment instead of the heading.
2. The `\r` blank line falls through to the `*` filter and is skipped.
3. `## What's Changed` reaches the `##` check and the loop breaks before any bullet.

**Falsifiable prediction.** Remove the comment and blank line, so the body starts with
`## What's Changed`, and leave everything else unchanged. The loop should then emit exactly 19 bullets
and stop at `## New Contributors`. An in-memory run did exactly that, which supports H1. Upstream's 8.9.0
body has a blank line between the heading and the bullets. That line is harmless, because it is skipped
either by `-eq ""` (LF) or by the `*` filter (CR). The diagnosis in the maintainer notes matches H1 but is
**UNVERIFIED — to be proven or refuted in /bugfix:confirm**.

**(a) The CR problem does not cause the empty entry.**

- No bullet is lost to it. `StartsWith("##")` and `StartsWith("* ")` look at the start of the line, so a
  trailing `\r` does not affect them.
- A `\r` blank line fails `-eq ""`, but it still falls through to the `*` filter and is skipped. The
  control flow ends up the same.
- What CR does change is the output. Every copied bullet keeps a trailing `\r` (in memory, 19 of 19 had
  it), and the `@login` replace at `:41` leaves it there. `Set-Content` then writes `\r` plus the
  platform newline (LF on `ubuntu-24.04`), so the new entry has CRLF endings while the rest of the file
  does not.
- The `-eq ""` check at `:30` is also dead code for CRLF bodies.

CR is therefore a secondary defect that touches only the written output. A fix for H1 alone would
produce 19 bullets, each ending in `\r`. Confirm should check whether those mixed line endings get past
`markdownlint` and `.gitattributes` normalisation.

**(b) It is an assumption about where the body comes from.** "Line 1 is the heading" is true for
upstream's release bodies and false for Fences' generated ones. 9.0.0-alpha001, created by the bot, has
the same comment-first shape, so any future bot-drafted release that is published unedited will hit the
same failure. The script relies on the position of the heading line, not its text.

**(c) A 9.0.0-shaped body (prose, then `*` bullets, no `##` heading) loses some content, not all.**

- Prose lines are dropped by the `*` filter. That looks intentional, since the script only copies
  bullets.
- With no `##` there is no early break, so the bullets are kept. In memory, a prose-first body kept
  both bullets.
- But `Skip 1` drops line 1 whatever it contains. If such a body *starts* with a bullet, that first
  bullet is silently lost (in memory, 2 bullets in gave 1 out).
- Any later `##` section would also cut the bullets short.

**Secondary observations** (outside the reported symptom; for confirm to scope):

- The `@login` regex at `:41` would turn `@github-actions[bot]` into `[@github-actions](...)[bot]`. This
  only matters if `## New Contributors` lines were ever copied, and today they are excluded by the break.
- The regex would also rewrite any `@` that appears inside a bullet's text.
- `$lines.IndexOf("<!-- next-release -->")` at `:46` returns -1 if the marker is missing. The entry
  would then be inserted at the top of the file. This is not involved here, because the marker exists at
  `CHANGELOG.md:12`.

## Confirmed Root Cause

**Verdict: CONFIRMED.** H1 holds.

`eng/update-changelog.ps1:28` decides where the content starts by *position*: it always drops line 1 of the
body, assuming line 1 is the section heading. `:33-35` then stops at the first line that starts with `##`.

Fences' generated release notes have a different line 1:
`<!-- Release notes generated using configuration in .github/release.yml at main -->`, then a blank line,
then `## What's Changed`. So `Skip 1` removes the comment, the blank line (`"\r"`) falls through to the `*`
filter at `:36-38` and is skipped, and `## What's Changed` reaches `:33`. The loop breaks with 0 bullets.

The trigger is the heading not being on line 1, not the comment's text. Removing only the comment and keeping
the blank line still gives 0 bullets (run E below). CR does not cause the empty entry (run D).

## Evidence

- [x] Code-trace: as above, `eng/update-changelog.ps1:28` → `:30-32` → `:36-38` → `:33-35`.
- [x] Red reproduction: under `## 9.0.1` in the written `CHANGELOG.md`, the entry should contain 19 `*` lines (PR #20
  to PR #65). Today it contains 0.

**Body bytes.** The 9.0.1 body has CRLF endings: 26 `\r\n`, 27 lines, 20 `*` lines (19 under
`## What's Changed`, 1 under `## New Contributors`). `od -c` of the start:
`... -->\r\n\r\n## What's Changed\r\n* build(deps) ...`.

**Runs.** Each run used a throwaway layout, `runX/eng/update-changelog.ps1` plus `runX/CHANGELOG.md`, and called
the script the same way as `after-release.yml:40`:

| Run | Body | `*` lines in the new entry |
| --- | --- | --- |
| A | Real 9.0.1 body (CRLF) | 0, red |
| B | Same body with lines 1-2 (comment and blank) removed | 19, PR #20 to PR #65, stops before `## New Contributors` |
| C | 9.0.0 body (LF, prose then bullets, then `## The API is unchanged`) | 9, matches bot commit `b3c97717` |
| D | Real 9.0.1 body converted to LF only | 0, so CR is not the cause |
| E | Comment removed, leading blank line kept | 0, so the cause is position, not the comment |

Runs A, B and C were reproduced independently by the main agent with the same results.

In run A the inserted block is `""`, `## 9.0.1`, `""`. That matches the empty entry the bot wrote on PR #67
(`git show 808efe2a^:CHANGELOG.md`).

**The environment variable keeps the carriage returns.** Locally, `$env:RELEASE_NOTES` held all 26 of them, and the call is
in-process (`pwsh` calling a `.ps1`), so no native-argument quoting step rewrites the string. For production this
is inferred, not observed: `after-release.yml:11` passes the release event's `release.body` through unchanged.

**Bytes written in run B.** The 19 bullets (file lines 16 to 34) end in `\r\n`. Every other line ends in `\n`.
`git ls-files --eol` reports `w/mixed`.

**Suggested fix: PARTIAL.** "Copy the `*` lines under `## What's Changed` until the next `##`" fixes 9.0.1. As
stated, it regresses bodies the script already handles:

- **9.0.0** is hand-written, with no `## What's Changed`. Today it gives 9 bullets (run C). A fix that only
  anchors on the heading would write an empty entry for it: the same symptom, on a different body shape.
- **Upstream 8.7.0 and 8.8.0** start with `## Highlights`. Anchoring on the heading text would copy their
  `## What's Changed` section instead of the highlights, which changes today's behaviour.

The "plus handle CRLF" part is CONFIRMED as needed (see Scope Notes).

**Behaviour the fix must preserve:**

1. Ignore leading lines that are not bullets: the HTML comment, blank lines, prose, and a leading `##` heading.
   The `##` break must not fire before the first bullet has been copied.
2. Copy `*` lines, then stop at the first `##` heading that comes after copied content.
3. Never drop line 1 because of its position. Today a body whose first line is a bullet loses that bullet.
4. Keep these results: 9.0.1 gives 19, 9.0.0 gives 9, upstream 8.9.0 gives its `## What's Changed` bullets,
   and the comment-first 9.0.0-alpha001 gives its bullets.

Anchoring on `## What's Changed` when it is present, with rule 1-3 as the fallback, is also acceptable. The
fallback is not optional.

## Scope Notes

**In scope.**

- **H1: positional skip and early break** (`eng/update-changelog.ps1:28`, `:33-35`). This is the cause.
- **CRLF splitting** (`eng/update-changelog.ps1:28`, `:30`). Split on `\r?\n`, or trim `\r`.
  - `.gitattributes` has `* text=auto`, so `git add .` at `after-release.yml:75` normalises the stray carriage returns to LF.
    They would not reach the repository.
  - It is still in scope: the `-eq ""` check at `:30` never matches a CRLF blank line; any whole-line comparison,
    such as an anchor `-eq "## What's Changed"`, fails silently on `"## What's Changed\r"`; and the regression
    test reads the working-tree file before git normalises it. It is a cheap change on the same line.
- **Regression test coverage:** the real CRLF 9.0.1 body; a comment-first body; the 9.0.0 body with no heading
  (must still give 9); a body whose first line is a bullet.

**Out of scope** (possible follow-ups):

- **`@login` regex** (`:41`). It would turn `@github-actions[bot]` into
  `[@github-actions](https://github.com/github-actions)[bot]`, and it rewrites any `@` in bullet text. It cannot
  fire today, because copying stops before `## New Contributors`.
- **Missing marker** (`:46-47`). `IndexOf("<!-- next-release -->")` returns -1 when the marker is missing, so the
  entry goes in at index 0. The marker exists at `CHANGELOG.md:12`, so this did not contribute.
- **Empty entries are not caught.** With no bullets, the script still writes `"", "## X", ""`, and the workflow
  commits it because `after-release.yml:53-57` only checks that something changed. Failing loudly on 0 bullets
  would have caught this bug, but it changes behaviour.
- **`eng/update-baselines.ps1`** (`after-release.yml:44`) never reads the release body. No other consumer of
  `release.body` or `RELEASE_NOTES` exists in `.github` or `eng`.

**Where the release bodies come from.** 9.0.0-alpha001 (bot, `release.yml:49-56`, `generate_release_notes: true`)
and 9.0.1 (created by a person, probably with "Generate release notes" in the web UI) both start with the comment.
Upstream's 8.5.0 to 8.9.0 releases are bot-authored and none has it; 8.7.0 and 8.8.0 start with `## Highlights`,
which GitHub never generates, so upstream edits at least some drafts before publishing. That fits the comment
being removed during editing, but does not prove it. The fix must not depend on the comment being present or
absent.

**Testability with Pester.**

- The script finds `CHANGELOG.md` through `Join-Path $PSScriptRoot ".."` (`:16-17`). A test has to copy the
  script into a temporary layout, `<root>/eng/update-changelog.ps1` plus `<root>/CHANGELOG.md` (for example under
  `TestDrive:`), and pass all three mandatory parameters.
- CRLF fixtures: `.gitattributes` `text=auto` would normalise a committed `.txt` fixture to LF. A fixture needs
  `-text` or `eol=crlf` in `.gitattributes`, or the test builds the CRLF endings in code.
- Pester is not installed locally, the repo has no `*.Tests.ps1` files, and CI has no Pester step.

## Regression Test

`eng/update-changelog.Tests.ps1` (Pester 6.2.0). Each test copies the script into a `TestDrive:` layout with a
minimal `CHANGELOG.md`, and builds its release notes in code so `.gitattributes` cannot rewrite the CRLF endings.

| Test | Before the fix |
| --- | --- |
| `UpdateChangelog_GeneratedNotesWithLeadingComment_CopiesWhatsChangedBullets` | Red: the entry is empty |
| `UpdateChangelog_NotesWithCrlfLineEndings_WritesNoCarriageReturns` | Red: `\r` is written |
| `UpdateChangelog_NotesStartingWithABullet_CopiesTheFirstBullet` | Red: the first bullet is lost |
| `UpdateChangelog_HandWrittenNotesWithoutAHeading_CopiesBulletsUntilTheNextHeading` | Green guard (9.0.0 shape) |
| `UpdateChangelog_NotesStartingWithWhatsChangedHeading_CopiesBulletsUntilTheNextHeading` | Green guard (upstream shape) |

Approved 2026-10-31.

## Fix

- `eng/update-changelog.ps1`: split the body on `\r?\n` instead of `` `n ``, drop `Select-Object -Skip 1`, and skip
  `##` headings until the first bullet has been copied. After that, the next `##` ends the entry as before.
- `.github/workflows/lint.yml`: a `Test PowerShell scripts` step installs Pester at the pinned `PESTER_VERSION`
  (`6.2.0`) and runs every `*.Tests.ps1` in the repository.

All 5 regression tests pass. The real release notes now give 19 bullets for 9.0.1 (none ending in `\r`) and 9
for 9.0.0, the same as before. PSScriptAnalyzer 1.24.0, with CI's settings, reports no issues.
