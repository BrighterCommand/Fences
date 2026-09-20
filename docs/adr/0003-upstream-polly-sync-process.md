---
id: 0003-upstream-polly-sync-process
title: "Upstream Polly Sync Process"
status: Accepted
author:
  - "iancooper"
created: 2026-09-20
summary: "Every upstream sync from App-vNext/Polly runs a mandatory licence-text gate before any diff, triage or port step; ported commits rebrand to Fences namespaces while preserving Polly's public API shape, with any unavoidable incompatibility called out at the point it occurs and in the tracking issue; a read-only upstream remote and polly-upstream tracking branch bound each sync's diff to what changed since the last one, and dependency-bump commits are skipped by default."
tags:
  - "meta"
  - "licensing"
  - "governance"
---

# 3. Upstream Polly Sync Process

Date: 2026-09-20

## Status

Accepted

## Context

ADR 0002 forked `App-vNext/Polly` at commit `47e3b41` (Polly 8.7.0). A fork taken at one commit is
a snapshot, not a subscription: 34 commits and a full release, 8.8.0, had already landed upstream
by the time anyone checked, discovered only by hand while writing issue #28.
`fork-migration-plan.md`'s risk R2 already commits us to catching up — a read-only `upstream`
remote and a `polly-upstream` tracking branch — but that commitment was never executed, said
nothing about cadence or what to skip, and said nothing about what happens if the licence that
makes pulling from upstream safe ever changes.

Fences intends to keep pulling from upstream for as long as its licence permits it. That makes
syncing recurring work, and the ad hoc rediscovery that produced issue #28 is not a repeatable
process: nothing forced the licence check to happen before the reading and porting, and nothing
carried the triage judgement from one sync to the next.

## Decision

**Every upstream sync runs a mandatory licence gate before any diff, triage or port step, and the
remaining steps are structured to make each sync cheaper than the last.**

Porting rebrands; it does not redesign. ADR 0002 promised a consumer that upgrading from Polly to
Fences is a `using` find/replace, because the public API is otherwise identical. Every later sync
has to keep that promise, not just the original fork — so a ported commit changes namespaces, not
shapes, and any upstream change that cannot honour that is surfaced, not silently absorbed.

The gate and the steps:

| # | Decision |
| --- | --- |
| D1 | Step 0 of every sync fetches `LICENSE` at the tip of `App-vNext/Polly`'s `main` and compares it, byte for byte, against the copy recorded in this repository at fork time. Any difference halts the sync before it reads a single commit. |
| D2 | A licence-gate failure is not a routine finding. It opens or updates a tracking issue asking whether Fences may still pull from upstream, and no later sync step runs until a maintainer resolves it. |
| D3 | The gate checks the licence text itself, not the Open Source Maintainers Fee (OSMF). The OSMF governs App vNext's own binary distribution and does not alter the BSD-3-Clause grant Fences relies on (`README.md`, "The Open Source Maintainers Fee"). An OSMF policy change alone does not trip the gate. |
| D4 | A read-only `upstream` remote (`https://github.com/App-vNext/Polly.git`) and a `polly-upstream` branch track the last commit successfully synced. Each sync diffs from that pointer, not from the original fork point, so the diff shrinks to only what changed since the last sync. |
| D5 | A commit whose message matches Dependabot's `Bump <dep> from <x> to <y>` convention defaults to "skip" without individual review, because Fences tracks its own dependencies independently through its own Dependabot configuration. Every other commit needs a person to bucket it. |
| D6 | Findings land in one recurring tracking issue, updated at each sync, rather than a new issue written from scratch: the licence-gate result, the commits since the last sync, and the skip/port/needs-review buckets. |
| D7 | A ported commit rebrands namespaces, assemblies and package IDs to `Paramore.Fences.*` per ADR 0002's D1, but otherwise keeps Polly's public types, members and signatures unchanged, so the ADR 0002 find/replace promise holds for every sync, not only the original fork. Where an upstream commit cannot be ported without breaking that compatibility, the port does not quietly diverge to fit: the divergence is recorded as a code comment at the point it occurs and flagged in the tracking issue for a maintainer to decide. |
| D8 | A ported commit also references the upstream SHA in its commit message and adds a `CHANGELOG.md` entry in Fences' own voice. `CHANGELOG.md` itself is never rewritten. |
| D9 | `polly-upstream` advances to the SHA just triaged at the end of each sync, so D4's diff starts there next time. |
| D10 | The runbook for D1 to D9 lives in `.agent_instructions/upstream_sync.md`, not in this ADR or in session state, so a contributor or agent can run a sync without re-deriving the process. |

### Where the pieces live

- `.agent_instructions/upstream_sync.md` — the executable runbook: the licence-diff command, the
  `git remote`/branch setup, the compare query, and the triage rules.
- The `upstream` remote and `polly-upstream` branch — local git state, not committed, re-creatable
  from the runbook.
- The recurring tracking issue — the record of what has and has not been ported.

## Consequences

### Positive

- The one check that actually matters — whether the source is still under a licence that permits
  this — runs first and cannot be skipped by accident, because it blocks every later step rather
  than being one finding among many.
- Each sync costs less than the last: the diff is bounded to what changed since the previous sync,
  and dependency-bump commits are triaged automatically.
- The triage rules and fixup checklist survive between sessions and between contributors, in a
  durable file rather than a chat transcript.
- The ADR 0002 promise — a Fences upgrade is a `using` find/replace — keeps holding as upstream
  moves, because D7 makes preserving it part of every port rather than something checked once at
  the fork and never again.

### Negative

- A licence-gate failure stops all syncing, including any commit that would otherwise be an easy,
  wanted fix, until a maintainer reviews it. That is deliberate, but it means good fixes wait
  behind a legal question they have nothing to do with.
- The `polly-upstream` pointer is local git state. If the branch is deleted or the clone is lost,
  the next sync must reconstruct the last-synced SHA from the tracking issue rather than reading it
  directly.
- Skipping Dependabot-pattern commits by default can miss a dependency bump that carries a security
  fix. D5 accepts that risk because Fences already runs Dependabot against its own manifest
  independently.
- D7's compatibility check adds work to every port: a commit cannot be applied and forgotten, it
  must be checked against Polly's public surface first. An upstream commit that redesigns an API
  can sit flagged in the tracking issue, unported, for as long as the divergence is unresolved.

### Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| A sync runs manually and the operator skips the licence check because "it was fine last time". | D1 makes the licence check step 0 in the runbook, before any command produces something worth looking at — there is nothing to triage until it passes. |
| Upstream's licence changes in a way that is easy to miss, such as a new file added alongside `LICENSE` rather than `LICENSE` itself changing. | D1 is a starting point, not a complete legal review. D2 routes any detected change to a maintainer instead of letting the sync process decide on its own whether new terms are acceptable. |
| The `polly-upstream` tracking branch drifts from what was actually ported, because a sync's D9 step is forgotten. | D6's recurring issue records the last-ported SHA independently of the branch, so the branch can be re-pointed from the issue if it drifts. |
| Nobody runs a sync for months and the backlog becomes too large to triage in one sitting. | Out of scope for this ADR. Cadence — scheduled automation versus a manual runbook — is an implementation choice left to `.agent_instructions/upstream_sync.md`, which can be revised without a new ADR. |
| A ported commit reshapes Polly's public API and the port is applied without anyone noticing the ADR 0002 find/replace promise no longer holds for that surface. | D7 makes the comment-at-divergence step part of the port itself, not a separate audit, and requires the tracking issue to carry the flag until a maintainer decides. |

## Alternatives Considered

**1. Sync on demand, with no recorded process.** What issue #28 already did once. Rejected because
it reconstructs the triage rules and the licence question from scratch each time, and no step
guarantees the licence is checked before commits are read and ported.

**2. Mirror upstream automatically with no gate — a scheduled job that opens a pull request for
every new commit.** Rejected because it treats a licence change the same as a dependency bump:
something a maintainer approves after the fact, rather than something that stops the pipeline
before it acts. A fork whose entire justification is "the source licence permits this" cannot keep
pulling from a source once that licence has changed, even briefly.

**3. Track upstream releases only, by tag, not `main`.** Simpler, and 8.8.0 is a real signal that
upstream has moved. Rejected as the sole trigger because an unreleased fix on `main`, such as the
`FaultGenerator` change in #3220, can be worth porting before the next tagged release, and because
a licence change is not guaranteed to coincide with a release.

**4. Port upstream API changes as upstream wrote them, accepting drift from Polly's shape where
Fences' own design has since diverged.** Rejected because it breaks the ADR 0002 promise that a
Fences upgrade is a `using` find/replace: a consumer who checked that promise once, at the fork,
should not have to re-check it after every later sync. D7 pays that cost per commit instead.

## References

- [ADR 0002: Fork Polly as Fences](0002-fork-polly-as-fences.md) — the decision this ADR's sync
  process maintains.
- `fork-migration-plan.md` §5, risk R2 — the original, unexecuted mitigation this ADR fulfils.
- `.agent_instructions/upstream_sync.md` — the runbook these decisions require.
- Issue #28 — the first sync this process was designed for; a historical anchor, not a source of
  rationale.
