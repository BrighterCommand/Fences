---
id: 0002-fork-polly-as-fences
title: "Fork Polly as Fences"
status: Accepted
author:
  - "Brighter Command"
created: 2026-08-22
summary: "Brighter depends on Polly, and Polly's adoption of the Open Source Maintainers Fee creates a risk that Brighter's users are billed for binaries they obtain through us. We fork Polly 8.7.0 as Paramore.Fences, publish our own binaries, and rebrand fully to remove trademark and confusion risk."
tags:
  - "meta"
  - "licensing"
  - "packaging"
---

# 2. Fork Polly as Fences

Date: 2026-08-22

## Status

Accepted

## Context

Brighter uses Polly for resilience. Polly, maintained by App vNext, adopted the Open Source
Maintainers Fee (OSMF). Under that model a commercial consumer of the binaries may owe a fee.

Brighter's users do not choose Polly — they acquire it transitively, because Brighter references
it. That is the problem. A dependency our users cannot see and did not select should not be able
to create a payment obligation for them. Whether or not any particular user would in fact be
billed, we cannot make the guarantee on their behalf, and a transitive dependency that carries
an unquantified commercial obligation is not one we can responsibly ship.

The code is BSD 3-Clause licensed. The licence permits redistribution, with or without
modification, provided the copyright notice and disclaimer are retained. Building and publishing
our own binaries from that source is squarely within the grant. The OSMF applies to the
maintainers' own distribution, not to the licence terms of the source.

Options considered:

1. **Drop the resilience dependency.** Brighter would have to grow its own retry, circuit breaker
   and timeout implementations. Large, and a strictly worse outcome for users than a proven
   library.
2. **Vendor Polly's source into Brighter as internal types.** Removes the package dependency, but
   loses the ability for a user to configure resilience with types they can name, and makes every
   upstream fix a manual port with no shared benefit.
3. **Republish Polly's binaries unchanged under a new package ID.** Removes the fee exposure but
   keeps `Polly.*` namespaces. Any application referencing both `Polly` and our package gets the
   same fully-qualified type from two assemblies — an unresolvable ambiguity, and precisely the
   position a user mid-migration is in.
4. **Fork and fully rebrand.** Chosen.

A separate negotiation with the Polly maintainers may yet make the fork unnecessary. The work
proceeds so that the option exists; publishing to NuGet is deliberately held until that
conversation resolves.

## Decision

We fork `App-vNext/Polly` at commit `47e3b412e8c3b7e6db1629acd98f3e3b6b529d6c` (Polly 8.7.0,
17 August 2026) as **Fences**, a BrighterCommand project, and rebrand it fully.

The decisions that follow from that:

| # | Decision |
|---|---|
| D1 | Full rename: `Polly.*` → `Paramore.Fences.*` across namespaces, assemblies, package IDs, project and folder names, and the solution file. Type-forwarding is not available — `[TypeForwardedTo]` requires the same fully-qualified name and so cannot bridge a namespace change. |
| D2 | Package ID = assembly name = root namespace = `Paramore.Fences.*`, the convention `Paramore.Brighter.*` and `Paramore.Darker.*` already follow. `Fences.slnx` and `Fences.snk` stay unprefixed: they name the repository, not a package. |
| D3 | Versioning starts at `9.0.0`. It continues the lineage — `Fences 9.x` reads as "Polly 8.7 plus our changes" — and a major bump is the honest signal for a breaking namespace change. Inherited tags are kept as provenance. |
| D4 | Telemetry is renamed: `Meter`/`ActivitySource` `Polly` → `Paramore.Fences`; metrics `resilience.polly.*` → `resilience.fences.*`. Keeping `Polly` would make a process using both libraries emit indistinguishable metrics from two sources. This is the one change a consumer cannot fix by find/replace on their own source, and it is called out prominently in the migration guide. |
| D5 | Strong naming is kept, with a brand-new `Fences.snk`. Not Brighter's key: a rotation on one product should not churn the others. `Polly.snk` is deleted, never reused. |
| D6 | Authenticode signing is dropped, matching Brighter and Darker, neither of which signs. SBOM generation and build-provenance attestation are kept. Packages that were Authenticode signed under Polly will not be under Fences; the README says so rather than letting migrators discover it. |
| D7 | Contributions are gated by Brighter's CLA — which is a **licence grant, not an assignment**: the contributor retains copyright and grants an irrevocable licence — plus the Contributor Covenant code of conduct. Not the .NET Foundation CLA. |
| D8 | Documentation is docfx published to GitHub Pages at `https://brightercommand.github.io/Fences/`. This diverges from Brighter and Darker, which use GitBook, because GitBook cannot generate API reference from source and Fences inherits a working docfx pipeline that can. |
| D9 | We lead with the lineage — "Fences is a community fork of Polly" — as nominative use, inside the guardrails below. |
| D10 | The icon is a fence palisade in Brighter's palette, so Fences reads as part of the family. No Polly imagery is retained. |
| D11 | Ownership is the same four maintainers as Brighter; security reports go to GitHub Security Advisories on `BrighterCommand/Fences`. |

### The attribution boundary

BSD 3-Clause clause 3 forbids using the copyright holder's or contributors' names *to endorse or
promote* derived products. Clauses 1 and 2 **require** retaining the copyright notice. Naming
what you forked is a factual statement, not an endorsement — but only if executed as nominative
use. The guardrails:

* **G1** — wording stays descriptive: "Fences is a community fork of Polly". Never
  "Polly-approved", "the official successor to Polly", "Polly by BrighterCommand", or "the new
  Polly".
* **G2** — `Authors` and `Company` are Brighter Command only, never App vNext or Michael
  Wolfenden. Those fields assert authorship of *this* package.
* **G3** — every prominent lineage mention sits next to a non-affiliation line. This is what
  converts an assertive claim into a safe one; it is not dropped from the package description to
  save characters.
* **G4** — no Polly logo, wordmark, colours or visual identity anywhere. Trade dress is separate
  from the copyright licence.
* **G5** — `Polly` and `Simmy` stay out of `PackageTags`. A bare keyword carries no descriptive
  context, and nuget.org indexes the description anyway.
* **G6** — no domains, social handles or repository names containing "Polly". Naming *inside*
  your product is nominative use; naming your *channel* after theirs is not.

`LICENSE` keeps `Copyright (c) 2015-2025, App vNext` verbatim, with our own line added alongside
for new work. `NOTICE.md` records the provenance and the non-affiliation. `ACKNOWLEDGEMENTS.md`
credits App vNext, Michael Wolfenden and the Polly contributor community.

## Consequences

**For consumers.** Migration is `using Polly;` → `using Paramore.Fences;`. No public *type* name
contains "Polly" — the surface is `ResiliencePipeline`, `RetryStrategyOptions`,
`ResilienceContext` — so the rename touches namespaces only, and a consumer's change is a
find/replace on `using` directives. The exceptions a find/replace will not catch: telemetry
names (D4), the absence of Authenticode signatures (D6), and dashboards or OTel collector filters
keyed on `resilience.polly.*`.

**For history.** `CHANGELOG.md` keeps Polly's entries in full, headed by a note that 8.7.0 and
below are Polly's releases, not ours. The rename lands as one scripted, reviewable commit whose
SHA goes into `.git-blame-ignore-revs`, so `git blame` still reaches the real author of each
line.

**For us.** We take on maintenance of a substantial codebase and lose automatic upstream fixes;
merging them becomes a manual, per-fix decision. We also take on the obligation to be a good
downstream: to be accurate about what Fences is, to send fixes upstream where that is welcome,
and to say plainly that Polly is actively maintained and remains the right choice for many users.
Fences is a divergence, not a criticism.

**A limit on what the fork achieves.** `Microsoft.Extensions.Http.Resilience` — Microsoft's
official HttpClient resilience package, and a common dependency in ASP.NET Core applications — has
a hard NuGet dependency on `Polly.Extensions` and `Polly.RateLimiting` via
`Microsoft.Extensions.Resilience`. Its `AddResilienceHandler` callback receives a
`Polly.ResiliencePipelineBuilder<HttpResponseMessage>`, which no Fences strategy can attach to.

Fences therefore removes *Brighter's* Polly dependency, but it cannot remove Polly from an
application that calls `AddStandardResilienceHandler`. Those users keep Polly binaries in their
graph regardless of what Brighter does, and whatever OSMF exposure follows from that is unchanged
by this fork. This does not undermine the decision — Brighter's own transitive imposition on its
users is the thing we control and the thing we remove — but it does bound the claim, and the
migration guide must say so plainly rather than implying the fork makes an application
Polly-free. The two libraries coexist safely; that is the point of the full rename (R5).

Closing that gap would mean shipping our own `AddResilienceHandler` equivalent, a
`Paramore.Fences.Http` package. That is a candidate for future work, not part of the fork.

**Held.** Nothing is published to nuget.org pending the negotiation with the Polly maintainers.
Package IDs cannot be deleted once published, only unlisted.
