# Fences — Fork Migration Plan

> Status: **draft for review**. Nothing in Phases 2+ has been executed yet.
> Owner: BrighterCommand. Created 2026-08-22.

## 1. Why this document exists

`BrighterCommand/Fences` is a detached (non-GitHub-fork) copy of `App-vNext/Polly` at
upstream commit `47e3b41` (~Polly 8.7.x). Brighter depends on Polly; Polly's adoption of the
Open Source Maintainers Fee (OSMF) creates a risk that Brighter's users are billed for Polly
binaries. To remove that risk we build and publish our own binaries, and to remove trademark
and confusion risk we rebrand to **Fences**.

This plan takes the repository from "a copy of Polly with a partially edited README" to
"an independent, buildable, publishable project with its own identity, governance and agent
tooling" — while stopping short of actually pushing to NuGet, in case negotiations with the
Polly maintainers make the fork unnecessary.

## 2. What we inherited — assessment

### 2.1 Repository shape

| Thing | Value |
|---|---|
| Tracked files | 1,028 (797 `.cs`, 70 `.md`, 32 `.yml`, 21 `.csproj`) |
| Commits / tags | 2,939 commits, 64 upstream tags (`v1.0.0` … `8.7.0`) |
| Git remote | `git@github.com:BrighterCommand/Fences.git` — **`fork: false`, `parent: null`** on GitHub |
| Solution | `Polly.slnx` (slnx format) |
| Shipping projects | `Polly.Core`, `Polly`, `Polly.Extensions`, `Polly.RateLimiting`, `Polly.Testing` |
| Test projects | `Polly.Core.Tests`, `Polly.Specs`, `Polly.Extensions.Tests`, `Polly.RateLimiting.Tests`, `Polly.Testing.Tests`, `Polly.TestUtils`, `Polly.AotTest` |
| Other | `bench/` (2 projects), `samples/` (7 projects incl. one VB), `src/Snippets` (docs snippets) |
| Build | Cake (`cake.cs`, `build.ps1`, `Cake.Sdk` 6.2.0), MinVer versioning, CPM via `Directory.Packages.props` |
| Analysis | StyleCop, SonarAnalyzer, BannedApiAnalyzers, PublicApiAnalyzers, Stryker mutation testing (100% threshold) |
| Tests | xUnit 2.9, Shouldly, NSubstitute, FsCheck.Xunit |
| Docs | docfx site in `docs/` (35 md pages) + `mdsnippets` pulling compiled snippets from `src/Snippets` |
| Workflows | 15 GitHub Actions workflows |

### 2.2 Branding surface

| Surface | Count / detail |
|---|---|
| Tracked paths containing `polly` | 790 of 1,028 |
| Occurrences of the string `Polly` in content | ~6,840 across 883 files |
| Occurrences of `App-vNext` | 634 — **573 of them in `CHANGELOG.md`** (upstream PR links) |
| `pollydocs.org` references | 40 |
| Namespaces | `Polly`, `Polly.Retry`, `Polly.CircuitBreaker`, `Polly.Telemetry`, `Polly.Simmy`, `Polly.Utils`, … (file-scoped, e.g. `namespace Polly;`) |
| Logo/brand assets | `Polly-Logo.{jpg,png,svg,ai}`, `logos/` (7 Polly variants + aws/github/microsoft), `package-icon.png`, `docs/icon.png` |
| Strong-name key | `Polly.snk` (596 bytes — a **full 1024-bit key pair**, i.e. Polly's private key) |

### 2.3 The single most important finding

> **CORRECTED 2026-08-22.** This section originally read *"No public type or member name contains
> the string `Polly`"*, citing
> `git grep -ohE "\b[A-Za-z_]*Polly[A-Za-z_]*\b" -- "src/*.cs"` returning nothing. **That was a
> false negative with two independent bugs**: `\b` is a GNU extension unsupported by this
> platform's POSIX ERE, so the pattern matches nothing at all
> (`git grep -cE "\bPolly\b"` on a file with six hits exits 1); and the pathspec `src/*.cs`
> matches only files directly in `src/`, of which there are none. **Do not use `\b` with
> `git grep -E` in this repo.**
>
> The corrected check runs against the P0.3 baseline rather than the source:
> ```sh
> cat baseline/publicapi/*.txt \
>   | grep -ohE "[A-Za-z0-9_]*Polly[A-Za-z0-9_]+|[A-Za-z0-9_]+Polly[A-Za-z0-9_]*" | sort -u
> ```

**Exactly one public type name contains the string `Polly`: `Polly.PollyServiceCollectionExtensions`
(`src/Polly.Extensions/DependencyInjection/`), carrying 8 public members.** Everything else in the
public surface is brand-neutral — `ResiliencePipeline`, `RetryStrategyOptions`,
`ResilienceContext`.

That one type is an extension-method holder, so consumers write
`services.AddResiliencePipeline(...)` and never name the class; the practical migration cost is
still nil. But it is a public API change and needs a `.PublicAPI/PublicAPI.Unshipped.txt` entry,
and it must be handled in **Pass 1**, or `Polly.PollyServiceCollectionExtensions` becomes
`Paramore.Fences.Paramore.FencesServiceCollectionExtensions`.

Other embedded `Polly*` identifiers exist and were missed for the same reason — `PollyVersion`
(54 hits), `PollyConfig` (11), `EnablePollyMetering` (6), `ListenPollyMetrics` (3),
`PollyDiagnosticSource` (2) — but all are in `bench/`, `test/` or docs prose. Internal, and
mechanical.

Consequences:

* The rename is a **namespace / assembly / package-ID** change plus **one type rename**. No API
  redesign, no doc-comment rewrites of member names.
* A consumer migrating from Polly 8.7 to Fences changes `using Polly…;` to `using Fences…;`
  and their package references. Nothing else.
* The mechanical rename is therefore scriptable with high confidence.

The only `Polly` string *values* in `src/` are:

```
src/Polly.Core/Telemetry/TelemetryUtil.cs:5          internal const string PollyDiagnosticSource = "Polly";
src/Polly.Extensions/Telemetry/TelemetrySource.cs:7  internal const string Name = "Polly";
src/Polly.Extensions/Telemetry/TelemetryListenerImpl.cs  "resilience.polly.strategy.events"
                                                          "resilience.polly.strategy.attempt.duration"
                                                          "resilience.polly.pipeline.duration"
src/Polly.Core/Retry/RetryHelper.cs                  link to Polly-Contrib/Polly.Contrib.WaitAndRetry
```

These are the `Meter` / `ActivitySource` names and metric names — see decision **D4**.

### 2.4 Things that are currently broken or will break

| # | Problem | Impact |
|---|---|---|
| B1 | `global.json` pins SDK `10.0.400`; the dev machine has `10.0.101`/`10.0.102`. `dotnet build` fails with *"A compatible .NET SDK was not found"*. | Cannot build locally today. |
| B2 | `Polly.snk` is Polly's private signing key. Shipping Fences binaries signed with it is wrong (identity theft of the assembly identity) and it would collide with real Polly assemblies. | Blocks publish. |
| B3 | `eng/Library.targets` sets `PackageValidationBaselineVersion` = `8.5.2` — package validation will try to download `Polly.*` 8.5.2 and compare against `Paramore.Fences.*`. | Build failure after rename. |
| B4 | `build.yml` signing job needs Azure Key Vault secrets (`SIGN_CLI_*`), Codecov needs `CODECOV_TOKEN`, publish needs `NUGET_USER`, `POLLY_UPDATER_BOT_APP_ID/KEY`. None exist in BrighterCommand. | CI red on day one. |
| B5 | `.github/FUNDING.yml` points at `martincostello`; `updater-approve.yml` gates on `polly-updater-bot[bot]`; `update-dotnet-sdk.yml` calls a reusable workflow from `martincostello/update-dotnet-sdk`. | Wrong-project automation. |
| B6 | `CONTRIBUTING.md` / `CODE_OF_CONDUCT.md` bind contributors to the **.NET Foundation CLA and CoC**. Fences is not a .NET Foundation project. | Legally wrong; must change before accepting contributions. |
| B7 | `SECURITY.md` routes vulnerability reports to `App-vNext/Polly` security advisories and the Polly Slack. | Reports go to the wrong project. |
| B8 | `.PublicAPI/PublicAPI.Shipped.txt` (1,725 lines total) lists every public member with its `Polly.` namespace prefix. | Thousands of RS0016/RS0017 errors after rename unless regenerated. |
| B9 | `README.md` has already been partly rebranded and contains broken/empty markdown links (`[built-in strategies])`, `[![NuGet]()`) and a typo'd URL `BrigherCommand`. | Cosmetic but public-facing. |

### 2.5 Good news

* GitHub already reports `fork: false` / `parent: null`, so the `github.event.repository.fork == false`
  guards throughout the workflows evaluate **true** and those jobs will run.
* All five target NuGet IDs are unregistered (checked 2026-08-22): `Paramore.Fences`,
  `Paramore.Fences.Core`, `Paramore.Fences.Extensions`, `Paramore.Fences.RateLimiting`,
  `Paramore.Fences.Testing` — all HTTP 404 on the nuget.org flat container.
* BrighterCommand owns **120 of the 121** `Paramore.*` packages on nuget.org (15.7M downloads),
  which makes a `Paramore.*` prefix reservation a strong application — see P8.1.
* The repo already has `.git-blame-ignore-revs`, so the big rename commit can be made
  blame-transparent.
* Upstream `AGENTS.md` already exists and is decent — it becomes the seed for our agent instructions.

---

## 3. Decisions

| # | Decision | Status |
|---|---|---|
| D1 | Full rename `Polly.*` → `Paramore.Fences.*` (namespaces, assemblies, packages) | **CONFIRMED 2026-08-22** |
| D2 | Package/assembly/namespace all `Paramore.Fences.*`, matching Brighter and Darker | **CONFIRMED 2026-08-22** |
| D3 | Start at version `9.0.0` | **CONFIRMED 2026-08-22** |
| D4 | Telemetry: scope `Paramore.Fences`, metrics `resilience.fences.*` | **CONFIRMED 2026-08-22** |
| D5 | Keep strong naming, generate a new `Fences.snk` | **CONFIRMED 2026-08-22** |
| D6 | Drop Authenticode signing, matching Brighter | **CONFIRMED 2026-08-22** |
| D7 | CLA (Brighter's licence-grant ICLA) on CLA Assistant + Contributor Covenant CoC | **CONFIRMED 2026-08-22** |
| D8 | docfx to GitHub Pages at `brightercommand.github.io/Fences` | **CONFIRMED 2026-08-22** |
| D9 | Lead with the Polly lineage, inside the guardrails in D9 | **CONFIRMED 2026-08-22** |
| D10 | Icon: fence palisade in Brighter's palette | **CONFIRMED 2026-08-22 — draft accepted as starting point** |
| D11 | Ownership: same four maintainers as Brighter | **CONFIRMED 2026-08-22** |
| D12 | Rename `PollyServiceCollectionExtensions` → `ResilienceServiceCollectionExtensions` | **CONFIRMED 2026-08-22** |

All decisions are settled. Phases 0–8 are cleared to execute.

### D1 — How far does the rename go? — **CONFIRMED: full rename to `Paramore.Fences`**

Namespaces `Polly.*` → `Paramore.Fences.*`, plus assemblies, package IDs, project/folder names
and the solution file.

Rejected alternatives:

* *Keep `Polly.*` namespaces, rename packages only.* Any app that references both `Polly` and
  Fences gets duplicate type definitions in the same namespace — an unresolvable ambiguity for
  users mid-migration, and exactly the scenario Brighter's users will be in.
* *Type-forwarding shim package.* `[TypeForwardedTo]` requires the same fully-qualified name;
  it cannot bridge a namespace change. Not available.

Because no public *type* names change (§2.3), the migration cost to a consumer is a
find/replace on `using` directives.

### D2 — Package, assembly and namespace names — **CONFIRMED: `Paramore.Fences.*` throughout**

| Upstream package | Fences package = assembly | Root namespace |
|---|---|---|
| `Polly.Core` | `Paramore.Fences.Core` | `Paramore.Fences` |
| `Polly` (pre-v8 legacy API) | `Paramore.Fences` | `Paramore.Fences` |
| `Polly.Extensions` | `Paramore.Fences.Extensions` | `Paramore.Fences` |
| `Polly.RateLimiting` | `Paramore.Fences.RateLimiting` | `Paramore.Fences` |
| `Polly.Testing` | `Paramore.Fences.Testing` | `Paramore.Fences` |

Package ID, assembly name and namespace all agree — exactly the convention `Paramore.Brighter.*`
and `Paramore.Darker.*` already follow (Darker's namespaces are `Paramore.Darker`,
`Paramore.Darker.Builder`, …). Sub-namespaces follow through: `Polly.Retry` →
`Paramore.Fences.Retry`, `Polly.CircuitBreaker` → `Paramore.Fences.CircuitBreaker`, and so on.

Consumer migration is therefore `using Polly;` → `using Paramore.Fences;`.

**Not** prefixed, because they name the repo rather than a package: `Fences.slnx`, `Fences.snk`
(matching `Brighter.slnx` / `Brighter.snk`).

The `Paramore.` prefix also solves the nuget.org reservation problem — see P8.1.

### D3 — Version number — **CONFIRMED: `9.0.0`**

* Continues the lineage so `Fences 9.x` reads as "Polly 8.7 plus our changes".
* A major bump is the semantically honest signal for the breaking namespace change.
* Set `MinVerMinimumMajorMinor` to `9.0` in `Directory.Build.props` (currently `8.7`). With the
  inherited tags still present, MinVer resolves untagged builds to `9.0.0-alpha.0.N`, which is
  what we want.
* Keep the inherited tags (they are provenance and cost nothing). Tag Fences releases bare, e.g.
  `9.0.0`, matching upstream's convention — do **not** introduce a `v` prefix, because the old
  `v5.x` tags would then be picked up by MinVer.
* Clear `PackageValidationBaselineVersion` for the first release (no prior `Paramore.Fences.*` package
  exists); reintroduce it pointing at `9.0.0` once shipped. (Fixes **B3**.)

### D4 — Telemetry names — **CONFIRMED: rename**

| Kind | Polly | Fences |
|---|---|---|
| `Meter` / `ActivitySource` name | `Polly` | `Paramore.Fences` |
| Metric | `resilience.polly.pipeline.duration` | `resilience.fences.pipeline.duration` |
| Metric | `resilience.polly.strategy.events` | `resilience.fences.strategy.events` |
| Metric | `resilience.polly.strategy.attempt.duration` | `resilience.fences.strategy.attempt.duration` |

The instrumentation-scope name takes the full `Paramore.Fences` (convention is that it matches the
assembly). Metric names keep the short `fences` token — they are lowercase dotted identifiers and
should not carry the org prefix. Note that the bulk rename script would turn `resilience.polly.*`
into `resilience.paramore.fences.*`; Pass 2 in §2b deliberately maps lowercase `polly` → `fences`
to avoid that.

Call this out prominently in the migration guide — it is the one change a consumer cannot fix
with a find/replace on their own source.

Rationale: keeping `"Polly"` as the meter name means a process using both libraries emits
indistinguishable metrics from two sources, and it leaves Polly's brand in our telemetry output.
Renaming breaks existing dashboards and OTel collector filters — a one-time, well-signposted
break is better than carrying the wrong name indefinitely.

Affected tests assert these literals; they change in the same commit.

### D5 — Strong naming — **CONFIRMED: keep it, with a brand-new key**

Brighter strong-names its core assemblies with `Brighter.snk` (a handful of optional transport
packages set `SignAssembly=false` where their dependencies are not strong-named); the shipped
`Paramore.Brighter` 10.7.0 assembly carries a public-key blob. Fences gets its **own** key rather
than sharing Brighter's, so a future rotation on one product does not churn the others.

* Generate `Fences.snk` (cross-platform, no Windows `sn.exe` needed):
  ```sh
  openssl genrsa -out fences.pem 2048
  openssl rsa -in fences.pem -outform MSBLOB -out Fences.snk
  rm fences.pem      # keep the .snk only; it is committed, as Polly's was
  ```
* Delete `Polly.snk`.
* Replace the `PollyStrongNamePublicKey` property in `Directory.Build.props` with
  `FencesStrongNamePublicKey` holding the **new** public key token blob (extract from the built
  assembly, or `sn -p` / `ildasm` equivalent). This feeds the `InternalsVisibleTo` generation in
  `eng/Library.targets`.
* Leave the `DynamicProxyGenAssembly2` `InternalsVisibleTo` entry untouched — that is Castle
  DynamicProxy's own key, unrelated to Polly.

Note: committing a strong-name private key is normal practice (strong naming is an identity
mechanism, not a security boundary) and is what upstream does.

### D6 — Authenticode / package signing

**CONFIRMED: drop it.** Brighter and Darker do no Authenticode signing at all (no
`azure-key-vault` or `SIGN_CLI` usage anywhere in Brighter's workflows), and BrighterCommand has
no code-signing certificate in Azure Key Vault, so this matches the rest of the org.

Remove the `sign` and `validate-signed-packages` jobs from `build.yml`. Keep the SBOM generation
and the `actions/attest` build-provenance step — neither needs a secret beyond `id-token: write` —
and nuget.org applies its own repository signature on publish.

Say so in the README rather than letting migrators discover it: packages that were Authenticode
signed under Polly will not be under Fences. Azure Trusted Signing (~$10/month, no HSM or
certificate procurement) is the cheap route back if that turns out to matter to consumers.

### D7 — Contributor agreement

Brighter requires a CLA (clahub, `iancooper/Paramore`). Fences needs *some* equivalent, and it
must not be the .NET Foundation CLA (**B6**).

**CONFIRMED: CLA**, using Brighter's existing agreement text, plus the **Contributor Covenant**
code of conduct.

**Brighter's `CLA.txt` is already the right instrument — it is a licence grant, not an
assignment.** Section 2.1(a) reads *"You retain ownership of the Copyright in Your Contribution"*,
and 2.1(b) grants "a perpetual, worldwide, non-exclusive, transferable, royalty-free, irrevocable
license". That is precisely the model we want. Copy it verbatim into Fences (Darker already has
its own copy).

**But the prose in `CONTRIBUTING.md` contradicts the agreement.** Brighter's says:

> "The goal is to let you keep your copyright, but to **assign** it to the project so that it can
> use it in perpetuity."

That describes an assignment. `CLA.txt` grants a licence. The prose is wrong and should be fixed
in all three repos to say the contributor **retains** copyright and grants an irrevocable licence.

**Mechanism: replace clahub with CLA Assistant across Brighter, Darker and Fences.** clahub has
been defunct for years and works erratically, so contributions are almost certainly not being
gated today. Two variants:

| Option | How it works | Notes |
|---|---|---|
| `cla-assistant.io` (hosted) | OAuth app, signatures stored by the service | Least setup; third-party holds the signature record |
| `contributor-assistant/github-action` (self-hosted) | Runs in Actions, stores signatures as JSON in a repo or gist you own | **Recommended** — BrighterCommand keeps custody of the signature record, and it is three near-identical workflow files |

Scope note: this is a **three-repo change** (Brighter, Darker, Fences) and Fences is the smallest
and least risky place to prove the workflow before rolling it to the other two. Suggested order:
Fences → Darker → Brighter.

Migration question to settle: existing clahub signatures. If they cannot be exported, the
pragmatic route is to seed the new signature file with the current contributor list as
grandfathered, and gate only new contributions.

### D10 — Icon and visual identity — **CONFIRMED: riff on Brighter's cannon**

Fences adopts the BrighterCommand visual language so the two projects read as a family:

* flat vector, thick dark-navy outline (`#3D4A5C`-ish), slate-grey fills with a light-to-mid
  vertical gradient, single warm-orange accent (`#F5A623`-ish);
* wordmark below the mark in Brandon Grotesque Bold, dark navy, with **one letter in orange**
  (Brighter accents the `I`);
* source assets live in `assets/logo/` (Brighter's convention), exports in `images/`.

Motif: a fence/palisade reads directly and carries the right meaning for a resilience library —
a barrier that contains failure. Keeping Brighter's cannon silhouette *behind* or *beside* a
fence panel is the obvious family cue; the alternative is a fence-only mark in the same style.

Required exports (Brighter ships these sizes, match them):

| File | Size | Purpose |
|---|---|---|
| `package-icon.png` | 128×128 | NuGet embedded icon (upstream ships 120×120) |
| `docs/icon.png` | 128×128 | docfx `_appLogoPath` / `_appFaviconPath` |
| `images/fences-264.png` | 264×264 | README |
| `images/fences-nuget.png` | 64×64 | small mark |
| `assets/logo/Fences.svg` | vector | source of truth |

Brighter's `assets/logo/brandon_grotesque_bold.ttf` is present in that repo — check its licence
before reusing it for Fences exports.

**Draft in the tree (uncommitted):** `assets/logo/Fences-Mark-DRAFT.svg` and
`Fences-Lockup-DRAFT.svg` — a three-picket palisade with a pointed cap, one picket in the orange
accent, two slate rails, drawn in Brighter's exact palette. It is a starting point for a designer,
not final art. Known issues: the rail overhangs go muddy below ~48 px, and the wordmark uses a
fallback font because Brandon Grotesque is not installed here (final exports must convert text to
paths, as Brighter's sticker SVG does).

### D8 — Documentation site

`pollydocs.org` is gone; we have a docfx site and 40 links to the old domain.

**CONFIRMED: docfx to GitHub Pages** at `https://brightercommand.github.io/Fences/`. The
`gh-pages` workflow and branch already exist, so this is mostly rewriting the 40 `pollydocs.org`
links.

This deliberately diverges from Brighter and Darker, whose documentation lives in the
`BrighterCommand/Docs` repo and is published via GitBook
(`brightercommand.gitbook.io/paramore-brighter-documentation`). The reason: GitBook cannot
generate API reference from source, and Fences inherits a working docfx pipeline that does —
including the `mdsnippets` mechanism that compiles `src/Snippets` and injects the results into the
prose. Throwing that away to gain family consistency is a bad trade.

Cross-link both ways: a Fences entry in the GitBook pointing out to the docfx site, and a link
home to BrighterCommand from the Fences site. Buying a domain is a later, optional step.

### D9 — Attribution boundary (see also Phase 1)

BSD-3-Clause clause 3 forbids using the copyright holder's or contributors' names *to endorse or
promote* derived products. It does **not** forbid — in fact clauses 1 and 2 **require** —
retaining the copyright notice.

**CONFIRMED: lead with the lineage.** The README and package descriptions open with "a community
fork of Polly", maximising discoverability for people actively looking for a Polly alternative
because of the OSMF change.

This is the most assertive of the options considered, and it is defensible — but only if executed
as *nominative* use: a factual statement of what the software is, not a claim of association.
Clause 3 bars using the names to **endorse or promote**; accurately naming what you forked is not
endorsement. The guardrails that keep it on the right side of that line:

| # | Rule | Why |
|---|---|---|
| G1 | Wording stays descriptive: "Fences is a community fork of Polly". **Never** "Polly-approved", "the official successor to Polly", "Polly by BrighterCommand", or "the new Polly". | The first states a fact; the rest imply association or succession. |
| G2 | `Authors` and `Company` are **Brighter Command** only — never App vNext or Michael Wolfenden. | Those fields assert authorship of *this* package. |
| G3 | Every prominent lineage mention sits next to a non-affiliation line: *"Not affiliated with, endorsed by, or supported by App vNext or the Polly maintainers."* README lead, package description, and `NOTICE.md`. | This is what converts an assertive claim into a safe one. Do not skip it in the package description just because it costs characters. |
| G4 | No Polly logo, wordmark, colours or visual identity anywhere. | P1.6 deletes the assets; do not reintroduce them. Trade dress is separate from the copyright licence. |
| G5 | Keep `Polly` and `Simmy` out of `PackageTags`. | A bare keyword carries no descriptive context, which is the weakest position to defend — and nuget.org indexes the description anyway, so the discoverability gain is ~nil. **Recommended, marginal call.** |
| G6 | No domains, social handles or repo names containing "Polly". | Naming *inside* your product is nominative use; naming your *channel* after theirs is not. |

Unchanged either way, because the licence requires it:

* **Keep** `LICENSE` verbatim, including `Copyright (c) 2015-2025, App vNext`. Add our own
  copyright line for new work; do not remove theirs.
* Add `ACKNOWLEDGEMENTS.md` crediting App vNext and the Polly contributors for the work Fences
  is built on.

### D11 — Ownership and security contact — **CONFIRMED: same four as Brighter**

`CODEOWNERS`: `* @iancooper @holytshirt @DevJonny @preardon`, matching Brighter exactly.

Brighter has **no** `SECURITY.md`; Fences inherits Polly's, so we keep and rewrite it rather than
delete it (P4.3) — reporting goes to GitHub Security Advisories on `BrighterCommand/Fences`, with
the four maintainers as the fallback contact.

Courtesy check before P4.6 lands: the other three are being signed up for Fences review load on a
project that may be abandoned if the Polly talks succeed. Worth a heads-up rather than a surprise
review request.

### D12 — The one public type rename — **CONFIRMED: `ResilienceServiceCollectionExtensions`**

§2.3's correction turned up exactly one public type carrying the brand:
`Polly.PollyServiceCollectionExtensions`, with 8 public members. It has to change — shipping a
type called `Polly*` from a package called `Paramore.Fences` is the branding-not-nominative-use
that G4 rules out.

**Chosen: `ResilienceServiceCollectionExtensions`** — drop the brand token, describe what the class
does. It matches how the rest of the surface is already named: `ResiliencePipeline`,
`ResilienceContext`, `ResiliencePipelineBuilder`, none of which carry a brand. `Polly*` was the
odd one out upstream, and the fork is the moment to fix it rather than carry it forward as
`Fences*`.

Rejected: `FencesServiceCollectionExtensions` (mechanically simpler, but re-introduces a brand
token into a surface that is otherwise brand-free).

Consumer impact is nil in practice — it is an extension-method holder, so callers write
`services.AddResiliencePipeline(...)` and never name the class. It is still a public API change
and needs a `.PublicAPI/PublicAPI.Unshipped.txt` entry.

Handle it in **Pass 1**, before the dotted replacement, or it becomes
`Paramore.Fences.Paramore.FencesServiceCollectionExtensions`.

---

## 4. Phases

Phases 0–3 are sequential. Phases 4–7 are independent of each other once Phase 2 lands and can
be parallelised. Phase 8 is deliberately parked.

### Phase 0 — Baseline and safety net

Goal: a green local build *before* changing anything, so the rename can be validated by
comparison.

- [x] **P0.1** Fix the SDK mismatch (**B1**) — **done by installing, not by downgrading the pin.**
      `10.0.400` is the current latest .NET 10 SDK (channel `10.0`, `latest-sdk: 10.0.400`), so
      `global.json` is correct and should stay as-is. Installed side-by-side with:
      ```sh
      curl -fsSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh && chmod +x dotnet-install.sh
      ./dotnet-install.sh --version 10.0.400 --install-dir /usr/local/share/dotnet --no-path
      ```
      (No `sudo` needed on this machine — `/usr/local/share/dotnet` is user-owned.)
      **Also add `"rollForward": "latestFeature"`** so contributors on 10.0.1xx/2xx/3xx are not
      blocked the way this machine was. This matters more now that D-level decisions dropped
      `update-dotnet-sdk.yml` (Phase 6) — nothing bumps the pin automatically any more.
- [x] **P0.2** Run the full local build and record the result as the baseline:
      `./build.ps1` (clean → restore → build → test → pack). Capture test counts per project.
      **Done 2026-08-22 — green: 0 warnings, 0 errors, 3044 passed / 1 skipped per TFM across
      three TFMs, all five packages packed.** Recorded in `baseline/README.md` (gitignored), with
      the full logs beside it. `build.ps1` needs PowerShell, which is not installed here; the
      equivalent is `(cd tools && dotnet tool restore)` then
      `dotnet cake.cs -- --target=Default --configuration=Release`.
- [x] **P0.3** Record the baseline public API: the five `.PublicAPI/PublicAPI.Shipped.txt` files
      are the contract. Copy them aside; after the rename the *only* diff must be the
      `Polly.` → `Paramore.Fences.` prefix. **Done — `baseline/publicapi/` (gitignored), Shipped
      *and* Unshipped, 1725 lines total. The P2.2 diff command is in `baseline/README.md`.**
- [x] **P0.4** Create the working branch `fork/rebrand-to-fences`. Do not commit directly to `main`.
- [x] **P0.5** Decide and record D1–D9 at the top of this file.

**Exit criteria:** `./build.ps1` succeeds locally; baseline artefacts captured.

### Phase 1 — Legal, attribution and provenance

Do this *before* the rename so the record of what we forked and why is committed while the tree
still visibly matches upstream.

- [x] **P1.1** `LICENSE` — retain upstream BSD-3-Clause text and the App vNext copyright line
      unchanged. Append a second copyright line:
      `Copyright (c) 2026, Brighter Command` (new work only). Do not alter the licence terms —
      BSD-3-Clause stays.
- [x] **P1.2** Add `NOTICE.md`:
      * what Fences is, and that it derives from `App-vNext/Polly` at commit `47e3b41` (Polly 8.7.x);
      * the retained BSD-3-Clause notice;
      * an explicit statement that Fences is **not** affiliated with, endorsed by, or supported by
        App vNext or the Polly maintainers.
- [x] **P1.3** Add `ACKNOWLEDGEMENTS.md` — credit App vNext, Michael Wolfenden, and the Polly
      contributor community; link to the upstream repo. This is where "acknowledging past
      contribution" lives.
- [x] **P1.4** `CHANGELOG.md` — keep the 92 KB of upstream history intact (it is the honest record
      of where the code came from, and the 573 `App-vNext` links are historical citations, not
      branding). Prepend a header block: *"Entries below 9.0.0 are the changelog of Polly, from
      which Fences is derived."* Start Fences entries above it. **Exclude `CHANGELOG.md` from the
      rename script.**
- [x] **P1.5** Add `docs/adr/0001-record-architecture-decisions.md` and
      `docs/adr/0002-fork-polly-as-fences.md` capturing the OSMF rationale, the rename decision,
      and D1–D9. This also bootstraps the ADR directory that the agent workflows (Phase 5) expect.
- [x] **P1.6** Delete brand assets that are Polly's: `Polly-Logo.jpg`, `Polly-Logo.png`,
      `logos/Polly-Logo*.{ai,svg,png,jpg}`. Keep `logos/{aws,github,microsoft}.png` only if the
      docs pages that use them survive. Replace `package-icon.png` (currently 120×120) and
      `docs/icon.png` with the Fences mark per **D10** — **this is a hard blocker for packing**,
      since `eng/Library.targets` declares `<PackageIcon>package-icon.png</PackageIcon>` and packs
      it unconditionally. Ship a placeholder in this phase if the final art is not ready; do not
      let it block Phases 2–3.
- [x] **P1.7** `.github/FUNDING.yml` — repoint to BrighterCommand's funding or delete (**B5**).

**Exit criteria:** provenance and attribution committed; no Polly-owned imagery remains; icon
replacement in place (even if a placeholder).

#### Execution notes (2026-08-22)

* **P1.6 deleted all of `logos/`, including `aws.png`, `github.png` and `microsoft.png`.** Nothing
  in `docs/`, `README.md` or `package-readme.md` referenced them — they were assets for Polly's
  sponsor/adopter section, which does not survive. They are also third-party trademarks we have no
  reason to carry.
* **P1.7 deleted `.github/FUNDING.yml` rather than repointing it.** It pointed at
  `github: [martincostello]`, a Polly maintainer; neither Brighter nor Darker has a `FUNDING.yml`,
  so deleting matches the org.
* **The Polly logo references in `package-readme.md` and `docs/index.md` were images only** —
  swapped for the Fences mark. The surrounding *prose* in both files is still Polly's and is left
  for Phases 2–3. `docs/index.md` still asserts *"Polly is part of the .NET Foundation"*, which is
  false of Fences: **P3.x must remove it**, not merely rename it.
* **`README.md`'s fenced code blocks are generated by `mdsnippets` from `src/Snippets/Docs/*.cs`,
  and the cake build rewrites them.** A hand edit inside a `<!-- snippet: -->` region is reverted
  on the next build — the P0.2 run silently restored a `pollydocs.org` link over a manual `TBD`.
  The 26 files containing `pollydocs.org` include the snippet sources; fix them there (D8/Phase 7),
  never in the generated Markdown.
* **`test/Polly.Core.Tests` `HedgingResiliencePipelineBuilderExtensionsTests.AddHedging_AttemptNumbers_Are_Incremented`
  is load-flaky.** It failed once on `net9.0` in the first P0.2 run (expected attempt 4, got 3)
  while passing on `net8.0` and `net10.0`, and passed 3/3 in isolation in 91 ms against 4 s under
  the three-TFM parallel run. Treat a single failure of this test after the rename as noise, not a
  regression — re-run it alone before investigating.

### Phase 2 — The rename

Goal: one reviewable, reproducible, blame-transparent commit.

**Principle:** script it, commit the script under `eng/`, and add the resulting SHA to
`.git-blame-ignore-revs` so `git blame` still reaches the real authors of each line.

#### 2a. Path renames (use `git mv` so rename detection survives)

```
Polly.slnx                    -> Fences.slnx                                  (repo, not package)
Polly.snk                     -> (deleted; Fences.snk generated in P3.1)      (repo, not package)
src/Polly                     -> src/Paramore.Fences
src/Polly.Core                -> src/Paramore.Fences.Core
src/Polly.Extensions          -> src/Paramore.Fences.Extensions
src/Polly.RateLimiting        -> src/Paramore.Fences.RateLimiting
src/Polly.Testing             -> src/Paramore.Fences.Testing
test/Polly.Core.Tests         -> test/Paramore.Fences.Core.Tests
test/Polly.Specs              -> test/Paramore.Fences.Specs
test/Polly.Extensions.Tests   -> test/Paramore.Fences.Extensions.Tests
test/Polly.RateLimiting.Tests -> test/Paramore.Fences.RateLimiting.Tests
test/Polly.Testing.Tests      -> test/Paramore.Fences.Testing.Tests
test/Polly.TestUtils          -> test/Paramore.Fences.TestUtils
test/Polly.AotTest            -> test/Paramore.Fences.AotTest
bench/Polly.Benchmarks        -> bench/Paramore.Fences.Benchmarks
bench/Polly.Core.Benchmarks   -> bench/Paramore.Fences.Core.Benchmarks
```
…plus the matching `*.csproj` filenames inside each.

#### 2b. Content replacement

> ⚠️ **Order matters, and getting it wrong is the most likely way to break this commit.**
> Because the replacement is now `Polly` → `Paramore.Fences` (a *dotted* string), any place where
> `Polly` is embedded inside a single identifier must be rewritten **first**, or you get garbage
> like `Paramore.FencesStrongNamePublicKey` and `IncludeParamore.FencesUsings`.

**Pass 1 — embedded identifiers (must run first):**

> **Revised 2026-08-22.** The original Pass-1 table had four entries. The §2.3 correction found
> six more — they were missed by the same broken `\b` grep. The full list, verified with
> `git grep -ohE "[A-Za-z0-9_]*Polly[A-Za-z0-9_]+|[A-Za-z0-9_]+Polly[A-Za-z0-9_]*" | sort -u`:

| Pattern | Replacement | Where |
|---|---|---|
| `PollyServiceCollectionExtension` | `ResilienceServiceCollectionExtension` | **public API** — D12. The un-suffixed pattern deliberately catches both `…Extensions` (the type) and `…ExtensionTests` (its test class) |
| `PollyStrongNamePublicKey` | `FencesStrongNamePublicKey` | `Directory.Build.props`, `eng/Library.targets` |
| `IncludePollyUsings` | `IncludeFencesUsings` | `Directory.Build.targets`, `eng/Benchmark.targets` |
| `PollyDiagnosticSource` | `FencesDiagnosticSource` | `src/Polly.Core/Telemetry/TelemetryUtil.cs` |
| `SKIP_POLLY_ANALYZERS` | `SKIP_FENCES_ANALYZERS` | `eng/Analyzers.targets` |
| `PollyVersion` | `FencesVersion` | `bench/` — 54 hits, the most common of the lot |
| `PollyConfig` | `FencesConfig` | `bench/Polly.Benchmarks` |
| `EnablePollyMetering` | `EnableFencesMetering` | `test/Polly.TestUtils`, `test/Polly.Extensions.Tests` |
| `ListenPollyMetrics` | `ListenFencesMetrics` | `bench/Polly.Core.Benchmarks` |

All but the first are internal, in `bench/` or `test/`.

**Pass 2 — the bulk (word-boundary-aware):**

| Pattern | Replacement | Note |
|---|---|---|
| `Polly` | `Paramore.Fences` | namespaces, assembly names, project names, package IDs |
| `polly` | `fences` | metric names (`resilience.polly.*`), lowercase URLs — **not** `paramore.fences` |

Both Pass-2 replacements must be **case-sensitive** (`sed s///g`, not `s///gI`). That is what
keeps them independent of each other: `Polly` → `Paramore.Fences` cannot touch lowercase
`resilience.polly.*`, so the two rules may run in either order. Make either one
case-insensitive and `resilience.polly.*` becomes `resilience.paramore.fences.*` — the exact
outcome D4 rules out.

**Pass 3 — manual, after the script:** `Polly.slnx` → `Fences.slnx` and `Polly.snk` →
`Fences.snk` are repo-level names and must **not** pick up the `Paramore.` prefix (Pass 2 would
give `Paramore.Fences.slnx`). Same for the meter name — see D4.

Add a guard to the script: after both passes, `git grep -n "Paramore\.Fences[A-Za-z]"` must return
nothing. Any hit is a Pass-1 case that was missed.

**Exclusion list — do not rewrite:**

* `CHANGELOG.md` (historical record — P1.4)
* `LICENSE`, `NOTICE.md`, `ACKNOWLEDGEMENTS.md` (attribution — must keep "Polly"/"App vNext")
* `docs/adr/0002-fork-polly-as-fences.md` — including its *filename*. The ADR records what we
  forked and why; rewriting "Polly" inside it would make it unreadable and destroy the record.
  ADR 0001 has no "Polly" in it, so it is unaffected either way.
* **All `*.md`** — see "Why prose is out of scope" below.
* `fork-migration-plan.md`, `CLAUDE.md`, `PROMPT.md` — working documents *about* the migration.
* `.git-blame-ignore-revs`
* Any URL that must continue to resolve to upstream: the `Polly-Contrib/Polly.Contrib.WaitAndRetry`
  link in `src/Polly.Core/Retry/RetryHelper.cs`, and upstream issue links in doc prose. Handle
  these as a manual pass after the bulk script.
* The `DynamicProxyGenAssembly2` public key in `eng/Library.targets`.

#### 2b-bis. Why prose is out of scope for this commit

**Decided 2026-08-22.** The original 2c listed "`docs/toc.yml` and 35 doc pages" as part of the
rename script. Renaming Markdown *prose* mechanically does not merely look clumsy — **it
manufactures falsehoods**, and a few minutes in the docs makes that concrete:

* `docs/community/resources.md` is 33 hits of **third-party article and podcast titles**:
  "NuGet Package of the Week: Polly wanna fluently express…", ".NET Rocks — *Polly V8 with Joel
  Hulen and Martin Costello*". Rewriting them misquotes Scott Hanselman and Carl Franklin.
* `docs/community/polly-contrib.md`, `http-client-integrations.md` and
  `libraries-and-contributions.md` describe **Polly's ecosystem** — `Polly-Contrib`, `Simmy`,
  third-party packages. A mechanical pass invents a `Fences-Contrib` organisation that does not
  exist.
* `README.md` links `App-vNext/Polly-Samples` and `dotnet/eShop`. Renaming the link text
  misdescribes real repositories; renaming inside the URL breaks them.
* `docs/migration-v8.md` is Polly's own v7 → v8 guide. "Migrating from Fences v7" is false —
  there was no Fences v7.

So: **the rename script skips every `*.md`.** Documentation is Phase 7's job (D8 already owns the
40 `pollydocs.org` links), and it is editorial work — decide what to keep, delete, rewrite or cite
as external — not a substitution.

The corollary for **P2.3**: `git grep -in polly` will still return every `*.md` after this commit.
That is expected, not a miss. Triage the *non-Markdown* hits.

Structural, non-prose docs files **are** in scope, because the build depends on them:
`docs/docfx.json`, `docs/toc.yml`, `mdsnippets.json`.

#### 2c. Non-obvious files that must be touched

These will not be caught by a casual eye and each will break the build or a check if missed:

- [x] `src/*/.PublicAPI/PublicAPI.Shipped.txt` × 5 (1,725 lines) — namespace prefix (**B8**)
- [x] `mdsnippets.json` — `ExcludeSnippetDirectories` lists `src/Polly*` paths
- [x] `eng/signing/filelist.txt` — contains `**/Polly*`
- [x] `eng/stryker-config.json` (paths are passed from `cake.cs`, verify)
- [x] `cake.cs` — `var projectName = "Polly"`, the five packable project paths, the AOT test path,
      the `if (moduleName == "Polly.Testing")` special case, all mutation-test targets
- [x] `docs/docfx.json` — `_appName`, `_appTitle`, and the `Polly/**` metadata exclusion
- [x] `docs/toc.yml` — done. **The 35 doc *pages* are NOT done**: prose is Phase 7, see 2b-bis
- [x] `.github/wordlist.txt` (spellcheck dictionary — `pollydocs` entry) and `.github/spellcheck.yml`
- [x] `exclusion.dic` (VS spell checker)
- [x] `Directory.Build.targets` — the eleven `Polly.*` implicit `Using` entries
- [x] `eng/Library.targets` — `Company`, `Copyright`, `Authors`, `PackageProjectUrl`,
      `PackageReleaseNotes`, `PackageValidationBaselineVersion` (see D9, D3)
- [x] `samples/*` — **8**, not 7: the plan missed `Intro.FSharp` (`.fsproj` + `Program.fs`)
      alongside `Intro.VisualBasic`. All switched from `PackageReference` to `ProjectReference`
- [x] `.vscode/` settings and `.config/dotnet-tools.json` (verify)
- [ ] `AGENTS.md` — **deferred**: it is Markdown, so 2b-bis excludes it from the script. It is
      superseded in Phase 5 anyway, so renaming it now would be churn on a file about to be
      rewritten

#### 2d. Validate

- [x] **P2.1** `./build.ps1` green (same test counts as the P0.2 baseline). **Done — exit 0,
      0 errors, 0 warnings; counts identical to baseline on all three TFMs; all five packages
      packed.**
- [x] **P2.2** **Done — the only diff across 1,725 lines is D12.** Four of the five projects are
      byte-identical modulo the prefix. `diff` the regenerated `PublicAPI.Shipped.txt` against the P0.3 baseline — the only
      change must be the namespace prefix. Any other diff is a bug in the rename.
- [x] **P2.3** **Done — every non-Markdown hit triaged against the survivor table above; no
      unexpected hits.** All `*.md` still match, by design (2b-bis).
- [x] **P2.4** **Done — `ad904b6e`.** Note that **git does not use this file unless told to**:
      contributors need `git config blame.ignoreRevsFile .git-blame-ignore-revs` locally.
      GitHub's blame UI honours it automatically. Say so in `CONTRIBUTING.md` (Phase 4).

**Exit criteria:** build green, public API diff is prefix-only, `git grep polly` returns only
deliberate survivors.

#### Execution notes (2026-08-22)

The script is `eng/rename-to-fences.py` (Python, not PowerShell — `pwsh` is not installed on the
dev machine). Four things came out of running it that the plan did not anticipate:

* **`Polly.Contrib.WaitAndRetry` is a third-party package, and the bulk pass renamed it to
  `Paramore.Fences.Contrib.WaitAndRetry`** — a package that does not exist, so `restore` failed
  outright. The script now masks `Polly.Contrib.*` the same way it masks URLs. **Any future
  `Polly*` package ID that is somebody else's must go in `PROTECTED_RE`, not in the exclusion
  list** — the exclusion list works per file, and this appears in three.
* **P3.1 (the strong-name key) had to be pulled forward too.** PASS 2 rewrote
  `AssemblyOriginatorKeyFile` to `Paramore.Fences.snk`, but the key is a *repo-level* name like
  `Fences.slnx`, so it is `Fences.snk` — that is what PASS 3 in the script now fixes, rather than
  being a manual step as originally planned. And the file has to *exist*: with `Polly.snk` deleted,
  every signed assembly fails `CS7027` and Phase 2 cannot be green. So `Fences.snk` is generated
  here (D5's `openssl` recipe, but **2048-bit**, where Polly's was 1024), and
  `FencesStrongNamePublicKey` now holds **our** public key rather than Polly's — otherwise
  `InternalsVisibleTo` would still be granting access to assemblies signed with Polly's key.
  **`sn.exe`/`ildasm` do not exist on macOS**; extract the blob from a built assembly by finding
  the `00 24 00 00 04 80 00 00` header and reading the little-endian length that follows. Do not
  derive it from the `.snk` directly — openssl emits a `CALG_RSA_KEYX` algid (`00a40000`) where the
  compiler normalises to `CALG_RSA_SIGN` (`00240000`), so the two blobs differ by four bytes and
  `InternalsVisibleTo` silently stops matching.
* **`PackageValidationBaselineVersion` had to be pulled forward from P3.2 into this commit.** It
  pointed at `8.5.2`, and package validation downloads that baseline from nuget.org as
  `Paramore.Fences.Core` 8.5.2 — which does not exist. Restore fails, so Phase 2 cannot be green
  without it. It is now commented out with a pointer to reinstate at `9.0.0`. **B3 is closed.**
* **The samples were referencing published `Polly.*` packages, and there is no published
  `Paramore.Fences.*` to switch them to** (Phase 8 is held, possibly forever). `cake.cs` restores
  every `**/*.slnx`, `samples/Samples.slnx` included, so this is a build break, not a cosmetic
  one. All 8 sample projects — including the F# and VB ones — now use `ProjectReference` into
  `src/`, and the corresponding `PackageVersion` entries are gone from `Directory.Packages.props`.
  **Confirmed as the preferred shape, not a stopgap** — project references for samples are what we
  want here permanently, so do not switch them back to `PackageReference` when Phase 8 publishes.
* **The fork cannot remove Polly from an app that uses `Microsoft.Extensions.Http.Resilience`** —
  discovered because it broke the build, and much the most important thing to come out of Phase 2.
  Microsoft's package has a **hard NuGet dependency** on Polly:

  ```
  Microsoft.Extensions.Http.Resilience 10.8.0
    -> Microsoft.Extensions.Resilience
         -> Polly.Extensions 8.4.2
         -> Polly.RateLimiting 8.4.2
  ```

  `AddResilienceHandler` hands its callback a `Polly.ResiliencePipelineBuilder<HttpResponseMessage>`,
  so no Fences strategy can be attached to it, and no rename changes that. **This qualifies the
  OSMF rationale in ADR 0002**: the fork removes *Brighter's* Polly dependency, but any consumer
  calling `AddStandardResilienceHandler` — very common in ASP.NET Core — still has Polly binaries
  in their graph via Microsoft. Say so in the migration guide rather than letting people discover
  it; overclaiming here would be worse than the limitation itself.

  **Decided: document the interop rather than delete it.** `Polly.Core` is now an explicit
  `PackageReference` of `src/Snippets` and `samples/Chaos`, and those files use Polly's API
  deliberately, with a comment saying why. Nothing is deleted; Phase 7 writes the prose. The
  project-wide `<Using Include="Paramore.Fences" />` had to go from `Snippets.csproj` — both
  libraries export `ResiliencePipelineBuilder`, `PredicateResult`, `DelayBackoffType` and friends,
  and a *global* using cannot be opted out of per file, so it made every interop snippet
  ambiguous. 23 snippet files gained an explicit `using Paramore.Fences;` instead.
  `Pattern_CircuitPerEndpoint` moved from `CircuitBreaker.cs` to `HttpClientIntegrations.cs`,
  which is transparent to the docs because mdsnippets resolves regions by name across the project,
  not by file.

  A **`Paramore.Fences.Http`** package providing our own `AddResilienceHandler` is the way to
  close this properly. It is real work and out of scope here — record it as a candidate phase.

  Trap while doing this: inserting a `using` line **before a file's byte-order mark** leaves the
  BOM stranded mid-file, which surfaces as `IDE0055`/`SA1208` on the *following* line and reads
  like an ordering problem rather than an encoding one. Strip every BOM and re-emit a single
  leading one. Ordering is `System.*` first, then the rest alphabetically. Note that an
  *incremental* `dotnet build` of `src/Snippets` can report success while the analyzers are stale —
  use `--no-incremental` before believing it.
* **`test/Shared/StrongNameTests.cs` hard-codes the expected public key *and* the public key
  token**, so generating `Fences.snk` made it fail deterministically on all three TFMs — the only
  genuine test regression in the whole rename. Both literals are now ours. The token is not
  derivable by eye: it is the **last 8 bytes of `SHA1(publicKeyBlob)`, reversed**
  (`6998A40D28482B6D`). If `Fences.snk` is ever rotated, this file must change with it, and it is
  the *only* place the key is duplicated outside `eng/Common.props`.
* **One line of prose inside a `.cs` file went false.** `RetryHelper.cs` read *"Per discussion in
  Polly issue https://…/Polly/issues/530"*; the URL was protected but the words were not, giving
  *"Per discussion in **Paramore.Fences** issue https://…/**Polly**/issues/530"*. Fixed by hand.
  It was the only instance — `git grep -In "Paramore\.Fences" -- '*.cs' | grep -iE "App-vNext|Polly-Contrib|pollydocs"`
  finds this class of error and now returns nothing. **Re-run that grep if the script is ever
  re-run.**

**Deliberate survivors after this commit**, i.e. the expected output of P2.3:

| What | Where | Owner |
|---|---|---|
| Every `*.md` | 76 files | Phase 7 — see 2b-bis |
| `App-vNext/Polly/issues/NNN` citations | ~20 code comments | permanent; they cite real upstream issues |
| `Polly-Contrib/Polly.Contrib.WaitAndRetry` | `RetryHelper.cs`, `RetryStrategyOptions.TResult.cs`, `Directory.Packages.props` | permanent; third-party |
| `pollydocs.org` links | `src/Snippets/Docs/*.cs`, `.github/ISSUE_TEMPLATE/*` | Phase 7 (D8) |
| `PackageProjectUrl`, `PackageReleaseNotes` | `eng/Library.targets` | Phase 3 (P3.3) |
| `https://www.nuget.org/profiles/Polly` | `.github/workflows/build.yml` | Phase 6 / 8 |
| `App-vNext/Polly` link | `docs/template/public/main.js` | Phase 7 |
| **`POLLY_UPDATER_BOT_*` / `POLLY_REVIEWER_BOT_*` secrets** | 6 workflow files | **Phase 6** |

Two things the script renamed *correctly but not finally*, both for Phase 3/7:

* `.github/wordlist.txt` now has `fencesdocs` where it had `pollydocs`. But `pollydocs.org` still
  appears throughout `docs/`, so **the spellcheck workflow will fail on `pollydocs` until Phase 7
  rewrites those links**. It runs in CI, not in the local build, so it is invisible here. Fix the
  links and the wordlist entry together.
* `docs/docfx.json` has `_appName` and `_appTitle` as `Paramore.Fences`. That is the *package*
  name; the site should be titled **Fences**. P3.3/P7 — the same distinction as `Fences.slnx`.

That last row is a genuine miss in the original plan and survives only because the Pass-2 rules are
case-sensitive: **uppercase `POLLY_`** never matched. They name GitHub App secrets that do not
exist in the BrighterCommand org, so those workflow steps cannot work regardless of naming. P6
must rename *and* re-provision them, or delete the steps.

### Phase 3 — New identity

- [x] **P3.1** Generate `Fences.snk` and wire up `FencesStrongNamePublicKey` (D5, **B2**).
      Verify `InternalsVisibleTo` still works — i.e. the test projects still see internals.
      *Done in Phase 2; token `6998A40D28482B6D`, asserted in `test/Shared/StrongNameTests.cs`.*
- [x] **P3.2** `MinVerMinimumMajorMinor` → `9.0`. **`PackageValidationBaselineVersion` is already
      done** — it had to be pulled forward into Phase 2 to get a green build; see the Phase 2
      execution notes. **B3 is closed.**
- [x] **P3.2a** Decide what `PublicAPI.Shipped.txt` should contain. The rename script rewrote the
      five `Shipped.txt` files in place, so the analyser now treats the whole `Paramore.Fences.*`
      surface as *shipped* — but nothing has shipped under that name, and D3 starts us at `9.0.0`.
      Strictly, `Shipped.txt` should be empty and everything should sit in `Unshipped.txt` until
      `9.0.0` is released. Carrying the baseline forward is the pragmatic choice and is what a
      rebranded fork usually does; it also keeps the P2.2 diff meaningful. **Confirm the carry-
      forward deliberately rather than by accident** — the alternative is a 1,725-line churn and
      loses the oracle.

      **CONFIRMED 2026-08-22 (session 3): carry the baseline forward.** The `Shipped.txt` files
      keep the renamed surface and are treated as if it had shipped under the Fences names. Two
      consequences to hold on to: the D12 rename lives in `Shipped.txt` rather than
      `Unshipped.txt`, which is the one place this repo's "every public API change goes in
      Unshipped" rule is deliberately not followed; and the `baseline/publicapi/` oracle stays
      comparable, so the P2.2 diff continues to read "D12 and nothing else".
- [x] **P3.3** `eng/Library.targets` package metadata:
      `Company` → `Brighter Command`; `Authors` → `Brighter Command`;
      `Copyright` → `Copyright (c) 2026-$(year), Brighter Command`;
      `PackageProjectUrl` → `https://github.com/BrighterCommand/Fences`;
      `PackageReleaseNotes` → the Fences changelog URL. (D9)

      **Deviation, deliberate:** `Copyright` retains App vNext's notice *alongside* ours rather
      than replacing it — `Copyright (c) 2015-2025, App vNext. Copyright (c) 2026-$(year),
      Brighter Command.` G2 constrains `Authors` and `Company`, which assert authorship of this
      package; `Copyright` states who holds copyright in a derivative work, and BSD-3-Clause
      clause 1 requires the upstream notice be retained. This matches `LICENSE`, which carries
      both lines and is packed into every nupkg.
- [x] **P3.4** Package descriptions and tags: describe Fences, drop `Polly`/`Simmy` from
      `PackageTags`, keep a factual "fork of Polly" sentence in the description. (D9)
      Every description now opens with the G1 lineage sentence immediately followed by the G3
      non-affiliation line. `PackageTags` lead with `Fences Paramore`; the dotted
      `Paramore.Fences` token is gone (it was a rename artefact — a dotted string is a poor tag).
- [x] **P3.5** Telemetry rename per D4, plus a `docs/migration-from-polly.md` section listing the
      old → new meter/metric names. *The code side of D4 landed in Phase 2; Phase 3 adds the
      guide (`docs/migration-from-polly.md`, linked from `docs/toc.yml`) and bumps
      `MinVerMinimumMajorMinor` to `9.0` in `eng/Common.props`.*
- [x] **P3.6** Fix `README.md` (**B9**): the broken link syntax, the empty NuGet badges, and the
      `BrigherCommand` typo. Add the fork-provenance note and a "Migrating from Polly" section
      (it is a two-line change for consumers — say so, it is our best adoption argument).
- [x] **P3.7** ~~Rewrite the 40 `pollydocs.org` links per D8.~~ **Moved to Phase 7 (P7.3).**
      The plan contradicted itself: §2b's survivors table already assigned these to Phase 7. Phase
      7 is right, because `.github/wordlist.txt` says `fencesdocs` and the spellcheck workflow
      fails until the links and the wordlist change in the same commit — and Phase 7 rewrites
      those same files' prose anyway. `README.md`'s own prose links were fixed under P3.6; the
      `pollydocs.org` URLs still visible in `README.md` are inside generated `<!-- snippet: -->`
      regions and belong to `src/Snippets/Docs/*.cs`, which is Phase 7's.

Also done in Phase 3, from the Phase 1–2 outstanding list:

- [x] `docs/index.md` — deleted the false *"Polly is part of the .NET Foundation"* claim and
      rebranded the page. The `eShopOnContainers` bullet went with it: it is a Polly resource, not
      a Fences one, and the link was already stale.
- [x] `package-readme.md` — was still wholly Polly's. Rewritten. **The NuGet version badge is
      removed, not repointed**, because nothing is published; Phase 8 puts it back.
- [x] `docs/docfx.json` — `_appName`/`_appTitle` are now `Fences`, the site name, not
      `Paramore.Fences`, the package name.
- [x] `.github/wordlist.txt` — added `paramore`, `brighter`, `brightercommand`, which the new
      prose needs and aspell does not know.

**Exit criteria:** packages build with Fences identity, correct strong name, correct metadata.

### Phase 4 — Governance and contributing guidelines

Align with Brighter, adapted to this repo's very different engineering conventions.

- [x] **P4.1** Rewrite `CONTRIBUTING.md` using Brighter's structure (`BrighterCommand/Brighter/CONTRIBUTING.md`),
      keeping these sections: *First Time Contributing?*, *Architecture Decision Records*,
      *Code Style*, *Testing*, *Documentation*, *Dependency Management*, *Making Changes*
      (Build & Test / Commit Messages / Branching / Submitting Changes), *Contributor Agreement*,
      *Support for Agentic Coding*, *Code of Conduct*, *Project Structure*.

      **Adapt, do not copy, the technical content** — Fences and Brighter differ materially:

      | Topic | Brighter | Fences (keep this) |
      |---|---|---|
      | Default branch | `master` | `main` |
      | Test dir | `tests/` | `test/` |
      | Build entry | `dotnet build Brighter.slnx` | `./build.ps1` (Cake) |
      | Assertions | FakeItEasy | Shouldly + NSubstitute + FsCheck |
      | Const naming | `ALL_CAPS` | PascalCase (StyleCop-enforced) |
      | Licence header | `#region Licence` in every file | none — do not introduce one |
      | Public API | free-form | `.PublicAPI/PublicAPI.Unshipped.txt` must be updated |
      | Mutation testing | none | Stryker, 100% threshold |
      | Test infra | Docker Compose | none needed — all tests are in-process |

- [x] **P4.2** Replace `CODE_OF_CONDUCT.md` — remove the .NET Foundation reference (**B6**), adopt
      Contributor Covenant, matching Brighter.
- [x] **P4.3** Rewrite `SECURITY.md` (**B7**) — point at GitHub Security Advisories on
      `BrighterCommand/Fences`, remove the Polly Slack contact, name the D11 maintainers as
      fallback. Update `.github/IRP.md` likewise. Note Brighter has no `SECURITY.md`, so this is
      the org's first — consider porting it back to Brighter and Darker afterwards.
- [x] **P4.4** Contributor agreement per D7 — copy Brighter's `CLA.txt` into Fences and wire up
      CLA Assistant. See D7 for the cross-repo work and the prose bug that must be fixed.
- [x] **P4.5** `.github/ISSUE_TEMPLATE/*` and `pull_request_template.md` — remove Polly-specific
      wording and links; add a "which Polly version does this correspond to?" field to the bug
      template while we are still tracking upstream closely.
- [x] **P4.6** Add `CODEOWNERS` per D11: `* @iancooper @holytshirt @DevJonny @preardon`.

**Exit criteria:** no contributor-facing document binds anyone to the .NET Foundation or routes
them to Polly.

**COMPLETE.** Every contributor-facing document is now Fences'. No .NET Foundation obligation
survives; no reader is routed to Polly's Slack, tracker or documentation site. Nothing under `src/`,
`test/`, `eng/` or `cake.cs` changed, so the build cannot have regressed.

Deliberate deviations from the wording above, and decisions taken while doing the work:

* **`CLA.txt` is not byte-for-byte Brighter's.** Sections 1 to 6 — the whole operative agreement,
  Harmony HA-CLA-I 1.0 — are verbatim. Three things changed, because copying them literally would
  have produced a defective document: the title and opening sentence now name `Paramore.Fences`
  rather than `Paramore.Brighter`; the execution instruction points at the CLA assistant instead of
  the defunct `clahub.com`; and the scraped GitHub Pages chrome (`Paramore` /
  `View the Project on GitHub BrighterCommand/Brighter` header, `Theme by orderedlist` footer) is
  gone. The dangling cross-reference the Harmony template leaves in section 1 and 3(d)
  ("follow the instructions in .") is **retained as-is** — it is in the legal text and fixing it
  means inventing terms.
* **CLA Assistant stores signatures on a `cla-signatures` branch of this repository**, not in a
  separate repository or gist as D7's table suggested. Same custody outcome — BrighterCommand holds
  the signature record — but it runs on `GITHUB_TOKEN` alone, so no personal access token has to be
  provisioned before the workflow works. `.github/workflows/cla.yml` is gated on
  `github.event.repository.fork == false` like the repo's other `pull_request_target` workflows, and
  carries the same `# zizmor: ignore[dangerous-triggers]` justification as `dependabot-approve.yml`.
* **`CONTRIBUTING.md` gained two sections Brighter does not have.** *Public API Changes* is its own
  top-level section rather than a bullet under Code Style, because it is the discipline contributors
  and agents get wrong most often. *Configure Git Blame* carries the
  `git config blame.ignoreRevsFile .git-blame-ignore-revs` note.
* **`.github/ISSUE_TEMPLATE.md` deleted.** The legacy single-file template is superseded by the
  `.github/ISSUE_TEMPLATE/` forms, duplicated `10_bug_report.yml`, and was unreferenced.
* **`.github/wordlist.txt` gained 36 entries.** `aspell` is not installed here, so the CI spellcheck
  could not be run against the new prose. The additions are belt-and-braces for terms the dictionary
  plausibly does not know (`Shouldly`, `NSubstitute`, `Stryker`, `docfx`, `PowerShell`, the
  maintainer handles, and the British spellings `licence`, `behavioural`, `organisation`,
  `labelled`, `honours`). Extra entries are harmless; a missing one fails the build.
* **`markdownlint-cli2` was run locally** against `.markdownlint.json` over every changed Markdown
  file: 0 issues. All changed YAML parses.

**New finding, for Phase 6 or 7:** `.github/workflows/gh-pages.yml` still sets
`cname: www.fencesdocs.org` — the rename script's mechanical output from `www.pollydocs.org`. D8
puts the site at `https://brightercommand.github.io/Fences/` and explicitly defers buying a domain,
so the `cname:` line contradicts the decision and would break the Pages deployment. The Phase 4
documents all link to the `github.io` URL, per D8.

**Second new finding — CI Markdown lint is already broken, on Phase 1 and 3 output.** Running the
exact tool the `gh-pages` workflow pins (`markdownlint-cli2` **0.23.2**, confirmed by reading
`package.json` at the pinned action SHA) over the same glob it uses gives errors on four *tracked*
files, none of them touched by Phase 4:

| File | Errors | Rules |
| --- | --- | --- |
| `fork-migration-plan.md` | 105 | `MD004` (86, dash vs asterisk), `MD060` (90 → 45 sites), `MD037`, `MD046`, `MD040`, `MD031`, `MD024` |
| `docs/adr/0002-fork-polly-as-fences.md` | 5 | `MD025` (front matter `title` counts as the first h1), `MD060` |
| `docs/adr/0001-record-architecture-decisions.md` | 1 | `MD025` |
| `NOTICE.md` | 1 | `MD040` (fence with no language) |

`PROMPT.md` and `baseline/README.md` also fail but are gitignored, so CI never sees them. The
Phase 3 files (`README.md`, `package-readme.md`, `docs/index.md`) are clean — that check was real,
it just did not cover the Phase 1 files or the plan itself.

The three small files are a few minutes' work. `fork-migration-plan.md` is the awkward one: it is a
working document that will be retired, and `MD004` wants its 86 `-` bullets turned into `*`, which
is pure churn. The cheaper answer for it is to add `!fork-migration-plan.md` to the `globs:` list in
`.github/workflows/gh-pages.yml`, alongside the existing `BenchmarkDotNet.Artifacts` exclusion.
Deferred to Phase 7 unless done sooner as a standalone chore.

### Phase 5 — Agentic workflow support

Goal: parity with Brighter's `/spec:*` and `/bugfix:*` flows, but with **Fences's own** coding
conventions in `.agent_instructions/` — inferred from this repo, not imported from Brighter.

#### 5a. `.agent_instructions/` (new directory)

Content is derived from `.editorconfig`, `eng/analyzers/*`, `eng/*.targets` and the existing
`AGENTS.md`, which already contains a good architecture summary worth preserving.

- [ ] **P5.1** `build_and_development.md` — Cake targets, `./build.ps1`, `dotnet test` per project,
      `--framework` selection, the five mutation-test targets, `SKIP_FENCES_ANALYZERS` escape hatch.
- [ ] **P5.2** `project_structure.md` — `src/Paramore.Fences.Core` (the v8 engine), `src/Paramore.Fences` (pre-v8
      legacy API), `Paramore.Fences.Extensions` (DI + telemetry), `Paramore.Fences.RateLimiting`,
      `Paramore.Fences.Testing`,
      `src/LegacySupport` (source-injected polyfills), `src/Shared`, `src/Snippets` (docs snippets),
      and the reactive/proactive strategy taxonomy from the existing `AGENTS.md`.
- [ ] **P5.3** `code_style.md` — **inferred from this repo**, explicitly:
      * file-scoped namespaces (`namespace Fences;`)
      * 4-space indent, LF, final newline; 2-space for XML/JSON/YAML project files
      * expression-bodied methods, constructors, operators, properties, indexers and accessors are
        **required** (`csharp_style_expression_bodied_* = true:error`)
      * private/internal static fields are PascalCase with no prefix
      * no `var` preference (IDE0007/IDE0008 disabled) — match surrounding code
      * usings **outside** the namespace, `System.*` first (StyleCop `orderingRules`)
      * XML docs required on exposed elements, not on internals (StyleCop `documentationRules`)
      * SA1402 (one type per file) is **off** — multiple types per file are allowed
      * banned APIs: `DateTime.Now`, `DateTime.Today`, `DateTimeOffset.Now`, `DateTimeOffset.Today`,
        implicit `DateTime`→`DateTimeOffset` — use `TimeProvider`
      * `Nullable` is enabled everywhere **except** `src/Paramore.Fences` (the legacy package), which
        deliberately opts out — do not "fix" this
      * `ImplicitUsings` enabled; implicit usings are declared centrally in `Directory.Build.targets`
- [ ] **P5.4** `public_api.md` — the discipline that has no Brighter equivalent and that agents get
      wrong constantly: every public API addition must be appended to
      `src/<Project>/.PublicAPI/PublicAPI.Unshipped.txt`, and the default posture is
      *"do not change the public API unless specifically asked"*. Explain the per-TFM subdirectory
      layout under `.PublicAPI/`.
- [ ] **P5.5** `testing.md` — xUnit 2.9, Shouldly assertions, NSubstitute, FsCheck for
      property-based tests, `Paramore.Fences.TestUtils` shared helpers, `InternalsVisibleToProject` in the
      csproj, `Paramore.Fences.AotTest` for trimming/AOT validation, coverage via coverlet, and the Stryker
      100% mutation threshold on `src/`. Rule: **a bug fix must include a test that fails without
      the fix** (inherited from the existing `AGENTS.md`; keep it).
- [ ] **P5.6** `documentation.md` — docfx site layout, `toc.yml`, and the `mdsnippets` convention
      (snippets live in `src/Snippets`, are compiled, and are injected into `docs/*.md` between
      `<!-- snippet: name -->` markers; run `mdsnippets` after changing them, never hand-edit the
      generated block). Note: unlike Brighter, **no licence header region** in source files.
- [ ] **P5.7** `dependency_management.md` — CPM via `Directory.Packages.props`,
      `NuGetAuditMode=direct`, multi-targeting matrix (`net8.0;net6.0;netstandard2.0;net472;net462`
      for Core), and the standing rule: do not add or update dependencies unless asked.
- [ ] **P5.8** `release_and_versioning.md` — MinVer from tags, `MinVerMinimumMajorMinor`,
      `eng/bump-version.ps1`, `eng/update-changelog.ps1`, `eng/update-baselines.ps1`, and the
      package-validation baseline workflow.
- [ ] **P5.9** `design_principles.md` — capture the architecture invariants worth defending:
      strategies are either reactive or proactive; every strategy has an `*Options` class;
      predicates go through `PredicateBuilder`; `ResilienceContext` is pooled; the legacy `Fences`
      package is frozen for compatibility and must not gain features.

#### 5b. Entry points

- [ ] **P5.10** Rewrite `AGENTS.md` as a thin index into `.agent_instructions/`, mirroring
      Brighter's `AGENTS.md` shape. Preserve the architecture summary by moving it into
      `project_structure.md` and `design_principles.md` rather than deleting it.
- [ ] **P5.11** Add `CLAUDE.md` mirroring Brighter's — TDD mandate, spec workflow summary, change-scope
      rule, adversarial-review strictness, skills index, and a pointer to `.agent_instructions/`.
- [ ] **P5.12** Add `.github/copilot-instructions.md` mirroring the above (Brighter does this).

#### 5c. `.claude/commands/` — port the workflows

Port from `BrighterCommand/Brighter/.claude/commands/`, adapting paths and gates. The `spec` and
`bugfix` flows both depend on `tdd/test-first` and `refactor/tidy-first`, so those come too.

- [ ] **P5.13** `tdd/test-first.md` + `README.md`
- [ ] **P5.14** `refactor/tidy-first.md` + `README.md`
- [ ] **P5.15** `adr/*` (5 files) — points at `docs/adr/` created in P1.5
- [ ] **P5.16** `spec/*` (11 files: `new`, `requirements`, `design`, `approve`, `review`, `tasks`,
      `implement`, `status`, `switch`, `ralph-tasks`, `ralph-implement`) + `README.md`
- [ ] **P5.17** `bugfix/*` (7 files: `triage`, `confirm`, `test`, `fix`, `verify`, `status`,
      `switch`) + `README.md`
- [ ] **P5.18** `.claude/commands/README.md` — the skills index
- [ ] **P5.19** Create `specs/` and `bugfixes/` directories with `.gitkeep`, matching Brighter's
      state model (`specs/.current-spec`, `bugfixes/.current-bug`, `.confirm-approved` markers).

**Fences-specific adaptations to make while porting — these are the parts that will silently rot
if copied verbatim:**

| Change | Where |
|---|---|
| `master` → `main` | branch references throughout |
| `tests/` → `test/` | all test paths |
| `dotnet build Brighter.slnx` → `./build.ps1` / `dotnet build Fences.slnx` | build commands |
| Docker Compose test infra → **not applicable** | `/spec:implement`, `/bugfix:verify` |
| Add: update `.PublicAPI/PublicAPI.Unshipped.txt` | `/spec:implement`, `/bugfix:fix` gates |
| Add: build must be analyzer-clean (StyleCop/Sonar/BannedApi) | verify steps |
| Add: mutation score must not regress on touched `src/` projects | `/spec:review`, `/bugfix:verify` |
| Add: run `mdsnippets` if `src/Snippets` changed | verify steps |
| Brighter's `ALL_CAPS` const rule → **remove** | any code-style references inside the commands |
| Brighter's `#region Licence` header → **remove** | any code-style references inside the commands |
| SlopWatch hook | out of scope for now; note as a follow-up |

- [ ] **P5.20** `.claude/settings.json` — port Brighter's permissions/hooks, minus SlopWatch, plus
      allowlisting the Cake/dotnet commands this repo actually uses.
- [ ] **P5.21** Add the *Support for Agentic Coding* section to `CONTRIBUTING.md` (P4.1) describing
      all of the above.

**Exit criteria:** an agent can run `/spec:new`, `/bugfix:triage` and `/test-first` in this repo
and every path, command and gate it is told about is real.

### Phase 6 — GitHub Actions build

The repo already has 15 workflows. The work is **subtraction and re-pointing**, not authoring from
scratch — upstream's `build.yml` is genuinely good (3-OS matrix, package validation, SBOM,
attestation) and most of it survives.

#### 6a. Triage of existing workflows

| Workflow | Action |
|---|---|
| `build.yml` | **Keep**, heavily edited — see 6b |
| `code-ql.yml` | Keep as-is |
| `dependency-review.yml` | Keep as-is |
| `lint.yml` | Keep (actionlint / zizmor / PSScriptAnalyzer) |
| `ossf-scorecard.yml` | Keep |
| `stale.yml` | Keep |
| `dependabot-approve.yml` + `dependabot.yml` | Keep; verify auto-approve policy suits BrighterCommand |
| `gh-pages.yml` | Keep, re-point to the Fences Pages URL (D8) |
| `on-push-do-docs.yml` | Keep (regenerates docs from `src/Snippets`) |
| `mutation-tests.yml` | Keep — the 100% Stryker threshold is a real quality asset |
| `release.yml` | Keep (draft-release creator, no Polly coupling) |
| `after-release.yml` | Review — check for Polly-specific announcement targets |
| `updater-approve.yml` | **Delete** — gates on `polly-updater-bot[bot]` (**B5**) |
| `nuget-packages-published.yml` | **Delete** — driven by the `repository_dispatch` we are removing |
| `update-dotnet-sdk.yml` | **Delete** — it calls a reusable workflow from `martincostello/update-dotnet-sdk`, a Polly maintainer's personal repo. Dropping it removes an external dependency and one more app token to provision. Dependabot still covers NuGet packages; `global.json` gets bumped by hand. |

#### 6b. `build.yml` changes

- [ ] **P6.1** Keep the macOS / Ubuntu / Windows matrix, `./build.ps1`, artifact upload, and the
      `validate-packages` job. These need no secrets.
- [ ] **P6.2** Remove the `sign` job and the Authenticode half of `validate-signed-packages` (D6, **B4**).
      Keep the SBOM step (rename `polly.spdx.json` → `fences.spdx.json`) and the
      `actions/attest` provenance step.
- [ ] **P6.3** Codecov: either add `CODECOV_TOKEN` to BrighterCommand and keep the two upload steps,
      or remove them and rely on the coverage artefact + the `GenerateCoverageReports` step-summary
      that `eng/Test.targets` already emits. Recommend **keep the artefact + step summary now,
      add Codecov later** — one fewer secret on day one.
- [ ] **P6.4** Rewire `publish-nuget` (see Phase 8) — do **not** delete it, gate it.
- [ ] **P6.5** Remove the `POLLY_UPDATER_BOT_*` app-token step and the `repository-dispatch` step.
- [ ] **P6.6** Update the `environment.url` (currently `nuget.org/profiles/Polly`).
- [ ] **P6.7** Verify the `github.event.repository.fork == false` guards — confirmed **true** for
      `BrighterCommand/Fences`, so no change needed, but re-check if the repo is ever recreated
      as a GitHub fork.
- [ ] **P6.8** Confirm the `10.0.400` SDK pin (P0.1) is consistent between `global.json` and the
      workflow's `setup-dotnet` steps.

**Exit criteria:** a push to `fork/rebrand-to-fences` produces a fully green Actions run with no
missing secrets, and `packages-*` artefacts containing `Paramore.Fences.*.nupkg`.

### Phase 7 — Documentation site

- [ ] **P7.1** Rebuild the docfx site under the new branding; verify `docs/api` metadata generation
      still resolves after the project renames.
- [x] **P7.2** ~~Add `docs/migration-from-polly.md`~~ — **delivered early, in Phase 3 (P3.5)**:
      the `using` change, the package-ID map, the D12 type rename, the telemetry rename (D4), the
      strong-name change, and the bounded `Microsoft.Extensions.Http.Resilience` claim.
- [ ] **P7.3** Publish to GitHub Pages per D8; update all doc links. **This now also owns the ~40
      `pollydocs.org` links** (moved here from P3.7) — in `docs/**/*.md`,
      `src/Snippets/Docs/*.cs` and `.github/ISSUE_TEMPLATE/*`. Change
      `.github/wordlist.txt`'s `fencesdocs` entry in the *same* commit or CI spellcheck breaks.
- [ ] **P7.4** Prune `docs/community/*` — it lists Polly-ecosystem resources, Slack, and
      contributor pages that no longer apply.

### Phase 8 — NuGet publishing (**HELD**)

Everything is built and staged; the trigger is not pulled.

- [ ] **P8.1** Reserve the **`Paramore.*`** ID prefix on nuget.org. Moving to `Paramore.Fences.*`
      (D2) turns this from a likely rejection into a strong application, and it benefits all three
      projects at once.

      **Process:** email `account@nuget.org` with the owner display name (`BrighterCommand`) and
      the prefix requested (`Paramore.*`). No form, no publish required, free, days-to-weeks.

      **Why this application is strong** — measured against nuget.org's three stated criteria:

      | Criterion | Evidence |
      |---|---|
      | Does the prefix clearly identify the owner? | **120 of the 121** `Paramore.*` packages on nuget.org are owned by `BrighterCommand`, with **15.7M** downloads between them |
      | Is it a common/generic word (avoid, esp. under 4 chars)? | "Paramore" is a project name, not a dictionary word — unlike "Fences", which is exactly the kind of generic term likely to be refused |
      | Would *not* reserving cause confusion or harm? | There is already one third-party package under the prefix — `Paramore.Brighter.CommandStore.RavenDb`, owned by `12downsk`, not by BrighterCommand |

      That last row is the argument that usually carries these applications: someone else is
      already publishing under your prefix.

      Note `Paramore.Brighter` currently shows `verified: false` — the prefix has **never** been
      reserved, so Brighter and Darker gain the verified-owner tick from this too.

      **Reservation ≠ owning the ID.** A granted reservation blocks *future* submissions by others;
      it does not stop someone taking `Paramore.Fences.Core` in the window before it is granted.
      All five IDs are currently free (checked 2026-08-22). If we want a hard lock before the Polly
      negotiation concludes, push `0.0.1` placeholders and immediately unlist them — claims the IDs,
      keeps them out of search, ships no usable binary. Trade-off: nuget.org IDs can never be
      deleted, only unlisted.

      **Recommend:** send the `Paramore.*` reservation mail now (it stands on its own merits
      regardless of whether Fences ever ships); hold the placeholder publish unless squatting risk
      looks material.

- [ ] **P8.2** Gate `publish-nuget` behind **all** of: a tag push, a protected GitHub Environment
      (`NuGet.org`) with required reviewers, **and** a repository variable such as
      `FENCES_PUBLISH_ENABLED == 'true'`. Belt, braces and a third thing — the explicit cost of an
      accidental publish is an unretractable package ID.
- [ ] **P8.3** Configure NuGet trusted publishing (the workflow already uses `NuGet/login` with
      OIDC — needs `NUGET_USER` and a nuget.org trusted-publisher policy).
- [ ] **P8.4** Dry-run the whole path against a private feed (GitHub Packages) to prove the
      pipeline end to end without touching nuget.org.
- [ ] **P8.5** Prepare, but do not publish: release notes, the migration guide, and the Brighter-side
      PR that switches Brighter's `Polly` reference to `Fences`.

**Do not proceed past P8.4 without an explicit decision that the Polly negotiations have failed.**

---

## 5. Risks

| # | Risk | Likelihood | Mitigation |
|---|---|---|---|
| R1 | The rename script corrupts something subtle (a string literal, a serialised name, a resource key). | Medium | P2.2's public-API diff and the P0.2 test baseline catch nearly all of it. Review the diff by file *category*, not line by line. |
| R2 | Divergence from upstream makes future cherry-picks of Polly security fixes hard. | High | Keep a read-only `upstream` remote and a `polly-upstream` tracking branch. Cherry-picks will need path/namespace fixups — that is the accepted cost of D1. Record this in ADR 0002. |
| R3 | Polly negotiations succeed and the fork is abandoned. | Unknown | Phases 0–7 are cheap relative to Phase 8, and the agentic tooling (Phase 5) is reusable regardless. Phase 8 is the only irreversible step; it is explicitly held. |
| R4 | Trademark or goodwill dispute over the fork. | Low | D9's attribution boundary — retain the notice, drop the promotional use, state non-affiliation explicitly in `NOTICE.md`. |
| R5 | Consumers end up with both `Polly` and `Fences` in one app (e.g. Brighter uses Fences, another library uses Polly). | High — this is the *expected* steady state | Harmless by construction: different assembly names, different strong-name keys, different namespaces. This is precisely why D1 chose a full rename. Do resolve D4 so their telemetry is also distinguishable. |
| R6 | 100% Stryker mutation threshold blocks the first Fences PRs. | Medium | Verify it still passes post-rename in P2.1. If the CI cost is too high, make `mutation-tests.yml` non-blocking rather than lowering the threshold. |
| R7 | No code-signing certificate weakens the trust story versus Polly's signed packages. | Medium | **Accepted** (D6) — it matches Brighter and Darker, which have never Authenticode-signed. Build-provenance attestation plus nuget.org repository signing is the interim position. Flag it in the README so migrators are not surprised, and treat Azure Trusted Signing (~$10/mo) as the cheap route back. |

| R8 | "Lead with the lineage" (D9) draws a trademark or goodwill objection from App vNext. | Low–Medium | The G1–G6 guardrails in D9 are the mitigation, and G3 (a non-affiliation line beside every prominent mention) is the load-bearing one. If an objection does arrive, the fallback is the "factual and low-key" wording — a metadata and README edit, not a code change, so the cost of reversing is hours not days. |

## 6. Suggested sequencing

```
Phase 0  ──►  Phase 1  ──►  Phase 2  ──►  Phase 3  ──┬──►  Phase 4  ──┐
 baseline     legal        rename        identity    │    governance  │
                                                     ├──►  Phase 5  ──┤──►  Phase 8
                                                     │    agents      │     (HELD)
                                                     ├──►  Phase 6  ──┤
                                                     │    CI          │
                                                     └──►  Phase 7  ──┘
                                                          docs
```

Phase 5 (agent tooling) is the one phase that pays for itself immediately: once
`/spec:*` and `/bugfix:*` work in this repo, Phases 6 and 7 can be driven through them.

## 7. Decision log

All eleven decisions are settled as of 2026-08-22 — see the table in §3. Recorded here for
traceability:

| # | Decision | Outcome |
|---|---|---|
| D1 | Rename scope | Full rename, `Polly.*` → `Paramore.Fences.*` |
| D2 | Naming | Package = assembly = namespace = `Paramore.Fences.*` |
| D3 | Version | Start at `9.0.0` |
| D4 | Telemetry | Scope `Paramore.Fences`, metrics `resilience.fences.*` |
| D5 | Strong naming | Keep, with a new `Fences.snk` |
| D6 | Authenticode | Drop, matching Brighter and Darker |
| D7 | Contributor agreement | Brighter's licence-grant ICLA, on CLA Assistant; roll to Darker and Brighter after |
| D8 | Docs | docfx to GitHub Pages at `brightercommand.github.io/Fences` |
| D9 | Attribution | Lead with the Polly lineage, inside guardrails G1–G6 |
| D10 | Icon | Fence palisade in Brighter's palette; draft accepted as a starting point |
| D11 | Ownership | `@iancooper @holytshirt @DevJonny @preardon`; security via GitHub Advisories |
| D12 | The one public type rename | `PollyServiceCollectionExtensions` → `ResilienceServiceCollectionExtensions` |
| P3.2a | `PublicAPI.Shipped.txt` | Carry the renamed Polly baseline forward; keep the oracle |
| P8.1 | NuGet IDs | `Paramore.*` prefix reservation email only — no placeholder publish |
| — | SDK | Install `10.0.400`, keep the pin, add `rollForward: latestFeature` |
| — | `update-dotnet-sdk.yml` | Delete |

Carried forward as tasks rather than open questions:

* Can existing clahub signatures be exported, or do we grandfather the current contributor list
  and gate only new contributions? (D7)
* Does the icon draft go to a designer for finishing, and is Brighter's
  `brandon_grotesque_bold.ttf` licensed for our use? (D10)
* Give @holytshirt, @DevJonny and @preardon a heads-up before P4.6 assigns them Fences review load. (D11)
