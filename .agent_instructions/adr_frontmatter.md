# ADR Frontmatter Conventions

This document defines the YAML frontmatter that every Architecture Decision Record in `docs/adr/`
carries. It is the source of truth referenced by the `read_adr_metadata` and `write_adr_metadata`
commands, by `/adr` and by `/spec:design`.

## Why frontmatter

An agent needs to find the prior ADRs relevant to a new design without reading every ADR in full.
Grepping whole bodies is expensive and pollutes context, especially with superseded decisions. A
small structured block at the top of each record lets a reader scan `title`, `summary` and `tags`
cheaply and decide which records are worth opening.

## Schema

Frontmatter is a YAML block delimited by `---` fences, at the very top of the file, before the
`# N. Title` heading:

```yaml
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
```

Note that `.markdownlint.json` sets `"MD025": { "front_matter_title": "" }` precisely so that the
`title:` field and the `# N. Title` heading do not both count as a level-one heading. Do not remove
that setting — without it every ADR fails the Markdown lint.

## Field reference

| Field | Required | Type | Notes |
| --- | --- | --- | --- |
| `id` | yes | string | **The filename stem** — the name without `.md`, for example `0002-fork-polly-as-fences`. This is the unique key. |
| `title` | yes | string, quoted | The title text from the H1, without the leading number. |
| `status` | yes | enum | One of `Proposed`, `Accepted`, `Deprecated`, `Superseded`. Mirrors the body `## Status`. |
| `author` | yes | list of strings | Always a list, even for one author. `- "Brighter Command"` where no individual is recorded. |
| `created` | yes | date | ISO 8601, `YYYY-MM-DD`. Matches the body `Date:` line. |
| `summary` | yes | string, quoted | One or two sentences saying **what was decided**, not just the topic. This is what an agent reads to decide whether to open the record. |
| `tags` | yes | list of strings | One to four tags from the vocabulary below. |

## Identity and numbering

The identity is `id`, the filename stem. The number is an ordering hint.

A new ADR takes the next number — `max + 1` across `docs/adr/[0-9]*.md`. If two branches allocate
the same number concurrently, that is acceptable and not a defect to fix, because the slug
distinguishes them. Do **not** renumber existing records; inbound links and anchors break.

When referring to a record in prose, prefer the slug over the bare number.

## Status

- **Proposed** — drafted, not yet approved. New records start here.
- **Accepted** — approved and in force. `/spec:approve design` flips `Proposed` to `Accepted` in
  both the frontmatter and the body.
- **Deprecated** — no longer in force and *not* replaced by a specific record.
- **Superseded** — replaced by a specific later record. Set the older record's `status` to
  `Superseded` and add a `Superseded by` reference to the newer one.

`read_adr_metadata` skips `Deprecated` and `Superseded` by default when surfacing prior art.

Partial supersession — where a later record replaces only a section of an older one — does **not**
change the older record's status. It stays `Accepted`; say so in prose.

Do not guess. Only assign `Superseded` when a specific replacing record exists.

## Tag vocabulary

Tags are lowercase kebab-case, quoted, one to four per record, drawn from this list. Prefer the most
specific applicable tags. If nothing fits, propose an addition to this list rather than inventing an
ad-hoc tag.

**Resilience strategies**
`retry`, `circuit-breaker`, `fallback`, `hedging`, `timeout`, `rate-limiting`, `chaos`

**Engine and API**
`architecture`, `pipeline`, `strategy`, `options`, `predicates`, `context`, `api-design`,
`extensibility`, `di`, `configuration`

**Runtime concerns**
`performance`, `memory`, `allocation`, `concurrency`, `async`, `cancellation`, `time-provider`

**Compatibility**
`legacy-api`, `multi-targeting`, `aot`, `trimming`, `strong-naming`, `versioning`,
`public-api`, `polly-compatibility`

**Observability**
`observability`, `otel`, `metrics`, `logging`, `tracing`

**Process and tooling**
`meta`, `testing`, `mutation-testing`, `build`, `packaging`, `documentation`, `licensing`,
`governance`

## Commands

- **`read_adr_metadata`** — extracts frontmatter blocks cheaply and returns matching records
  (`id`, `title`, `summary`, path) filtered by tag and status. Use it from `/spec:design` to find
  prior art before drafting. Skips `Superseded` and `Deprecated` by default.
- **`write_adr_metadata`** — adds or updates a file's frontmatter and enforces the status rules.
  Idempotent.

## The derived index

`docs/adr/index.md` is a table generated **from** the frontmatter. It is a cache that lets a reader
scan every record in a single file read. It must never be hand-edited.

```bash
awk -f .claude/commands/adr/generate_adr_index.awk docs/adr/[0-9]*.md > docs/adr/index.md
```

Regenerate it after adding a record or changing any frontmatter. `/adr` and `/spec:approve` do this
for you. The generator reads only the frontmatter, keys rows off `id` and orders by filename, so it
is deterministic: a dirty `index.md` after regenerating means a record's frontmatter really changed.
