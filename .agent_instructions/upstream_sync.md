# Upstream Polly Sync

This is the runbook for keeping Fences current with `App-vNext/Polly`, per
[ADR 0003](../docs/adr/0003-upstream-polly-sync-process.md). Read that ADR for the *why*; this file
is the *how* — the exact commands.

## The rule

**Step 0, the licence gate, runs first, every time, with no exceptions.** Do not run the diff or
triage steps before it has passed. If it fails, stop entirely and follow *If the licence gate
fails* below — do not continue the sync "just to see what changed".

## One-time setup

A read-only remote and a tracking branch pinned at the last commit successfully synced. Run this
once per clone:

```sh
git remote add upstream https://github.com/App-vNext/Polly.git
git fetch upstream
git branch polly-upstream 47e3b412e8c3b7e6db1629acd98f3e3b6b529d6c   # the ADR 0002 fork point, first time only
```

On later syncs, `polly-upstream` already exists and points at the last synced commit — skip the
`git branch` line.

## Step 0 — Licence gate

Compare the licence text recorded in this repository at fork time against upstream's licence text
today:

```sh
git show 47e3b412e8c3b7e6db1629acd98f3e3b6b529d6c:LICENSE > /tmp/polly-license-at-fork.txt
curl -s https://raw.githubusercontent.com/App-vNext/Polly/main/LICENSE > /tmp/polly-license-now.txt
diff /tmp/polly-license-at-fork.txt /tmp/polly-license-now.txt
```

**Empty diff — proceed to Step 1.** Any output at all — stop and follow *If the licence gate
fails*, below.

This checks the licence text, not the Open Source Maintainers Fee. The OSMF governs App vNext's
own binary distribution and does not change the BSD-3-Clause grant this fork relies on
(`README.md`, "The Open Source Maintainers Fee") — do not treat an OSMF pricing change alone as a
gate failure.

### If the licence gate fails

1. Do not run Step 1 or later. A commit read under a licence that may no longer permit reading it
   is a mistake that cannot be undone by discarding the diff afterwards.
2. Open (or update) the recurring tracking issue with only: the gate failed, the diff output, and
   a request for a maintainer to review whether Fences may still pull from upstream. Do not add a
   commit triage to that issue yet.
3. Wait for that review to close before resuming any sync.

## Step 1 — Diff since the last sync

```sh
gh api repos/App-vNext/Polly/compare/$(git rev-parse polly-upstream)...upstream/main \
  --jq '.ahead_by, .behind_by'
gh api repos/App-vNext/Polly/compare/$(git rev-parse polly-upstream)...upstream/main \
  --jq '.commits[] | .sha[0:8] + " " + (.commit.message | split("\n")[0])'
```

`ahead_by` is the number of new upstream commits to triage. `behind_by` should be `0` — if it
isn't, `polly-upstream` points somewhere upstream's history no longer contains, which needs
investigating before anything else (most likely `polly-upstream` was moved by hand; fix it before
continuing rather than triaging against a wrong base).

## Step 2 — Auto-triage

Bucket each commit from Step 1:

- **Skip, automatically.** The message matches `^Bump .+ from .+ to .+` (Dependabot's format) —
  covers `dotnet`, `xunit`, `github-codeql-action`, `SonarAnalyzer.CSharp`, `MinVer`, `Cake.Sdk`,
  and similar tooling bumps. Fences tracks these independently via its own
  `.github/dependabot.yml`; porting the upstream bump would just be a second, redundant path to
  the same version.
- **Needs a person.** Everything else: behavioural changes, new strategies, bug fixes, test
  changes, platform-support changes (e.g. a new target framework). Read the commit, decide
  port / decide-later / skip-with-reason, and record the decision — do not leave a commit
  untriaged in the tracking issue.

## Step 3 — Report

Update the recurring tracking issue with: the licence-gate result (pass, with the date checked),
the commit list from Step 1, and the Step 2 buckets. Reuse the same issue across syncs rather than
opening a new one — the point is that the next sync reads what was decided last time instead of
re-deriving it.

## Step 4 — Port what's kept

For each commit triaged "port":

1. Apply the change by hand (a straight `git cherry-pick` will conflict on nearly every file it
   touches — see *Why this isn't a `git cherry-pick`* below).
2. Apply the ADR 0002 rename to anything the change touches: `Polly.*` → `Paramore.Fences.*`
   namespaces, `Polly.PollyServiceCollectionExtensions` → `FencesServiceCollectionExtensions` if
   relevant, and any `Polly`-branded string constants.
3. **Check API compatibility (ADR 0003, D7).** Rebrand, don't redesign: once the rename fixups
   from step 2 are applied, the ported change's public types, members and signatures should match
   what upstream shipped. This is what keeps the ADR 0002 promise true — upgrading Fences stays a
   `using Polly;` → `using Paramore.Fences;` find/replace — for every commit synced after the fork,
   not just the ones that were already there. If a commit genuinely can't be ported without
   breaking that — upstream redesigned a public signature, removed something Fences already
   shipped, or introduced an overload that collides with a Fences-only addition — do not resolve
   the tension unilaterally:
   - Add a short comment in the code at the point of divergence stating what upstream did and why
     Fences doesn't (or doesn't yet) match it, referencing the tracking issue number. This is a
     legitimate "why" comment under `CLAUDE.md`'s comment policy — the divergence from upstream is
     exactly the kind of non-obvious constraint worth recording.
   - Flag the same divergence in the tracking issue (Step 3) instead of the "port" bucket, so a
     maintainer decides whether to accept the drift, find another shape that satisfies both, or
     defer the port.
4. Reference the upstream SHA in the commit message, e.g.
   `Port App-vNext/Polly@482bdf82: return null from FaultGenerator when no fault is generated`.
5. Append a `CHANGELOG.md` entry in Fences' own voice. Never rewrite the existing history in that
   file — Polly's entries for 8.7.0 and below stay exactly as they are.
6. Follow the normal verification gates in `CLAUDE.md` before merging: analyser-clean build, tests
   passing, a `.PublicAPI/` entry if the change is public, mutation score not regressed.

### Why this isn't a `git cherry-pick`

Phase 2 of the migration renamed the namespace, assembly names, package IDs, the strong-name key
(`Fences.snk`, not `Polly.snk`), and the one `Polly`-named public type. Nearly every file upstream
touches now has a different name, a different namespace declaration, or both, in this repository.
Treat each ported commit as a manual port guided by the upstream diff, not as something `git`
resolves for you.

## Step 5 — Advance the pointer

```sh
git branch -f polly-upstream <last-triaged-sha>
```

This is what makes the *next* sync's Step 1 diff small. Do this even on a sync where nothing was
ported — "triaged and skipped" still means the sync as a whole covered that commit.

## What this does not decide

Cadence — a scheduled job that runs Steps 0 to 3 automatically versus a person running this
runbook by hand on a reminder — is deliberately left open by
[ADR 0003](../docs/adr/0003-upstream-polly-sync-process.md). Change this runbook directly if the
cadence or the automation changes; that does not need a new ADR unless it changes the licence-gate
rule itself.
