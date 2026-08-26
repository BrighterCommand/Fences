---
id: 0001-record-architecture-decisions
title: "Record architecture decisions"
status: Accepted
author:
  - "Brighter Command"
created: 2026-08-22
summary: "Fences will use Architecture Decision Records (as described by Michael Nygard) to record the architectural decisions made on the project, matching the convention already used by Brighter."
tags:
  - "meta"
---

# 1. Record architecture decisions

Date: 2026-08-22

## Status

Accepted

## Context

We need to record the architectural decisions made on this project.

Fences is a fork of Polly, and the fork itself is the first such decision. The reasoning behind
it — why we forked, how far the rename goes, what we kept — is currently held in a migration
plan that will be retired once the migration completes. That reasoning needs somewhere durable
to live.

Brighter already records its decisions this way, in `docs/adr/`. Using the same mechanism here
means a contributor moving between the two repositories finds what they expect.

## Decision

We will use Architecture Decision Records, as [described by Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions).

ADRs live in `docs/adr/`, numbered sequentially, and carry the same YAML front matter Brighter
uses: `id`, `title`, `status`, `author`, `created`, `summary`, `tags`.

An ADR is immutable once accepted. To change a decision, write a new ADR that supersedes it and
mark the old one `Superseded by NNNN`.

## Consequences

See Michael Nygard's article, linked above. For a lightweight ADR toolset, see Nat Pryce's
[adr-tools](https://github.com/npryce/adr-tools).

The agent workflows added in this repository expect `docs/adr/` to exist and treat accepted ADRs
as binding context.
