<!-- GENERATED FILE - DO NOT EDIT BY HAND.
     Regenerated from ADR frontmatter by /adr and /spec:approve
     (see .agent_instructions/adr_frontmatter.md). Edit the ADRs, not this file. -->

# Architecture Decision Records — Index

Derived index of every ADR in `docs/adr/`, generated from each file's YAML frontmatter.
It is a regenerable cache: **do not hand-edit**. To refresh it after adding or changing an
ADR, rerun the generator documented in
[`.agent_instructions/adr_frontmatter.md`](../../.agent_instructions/adr_frontmatter.md).

Identity is the `id` (filename stem), not the leading number — numbers may repeat if two branches allocate concurrently.

| id | Title | Status | Tags | Summary |
| --- | --- | --- | --- | --- |
| `0001-record-architecture-decisions` | Record architecture decisions | Accepted | meta | Fences will use Architecture Decision Records (as described by Michael Nygard) to record the architectural decisions made on the project, matching the convention already used by Brighter. |
| `0002-fork-polly-as-fences` | Fork Polly as Fences | Accepted | meta, licensing, packaging | Brighter depends on Polly, and Polly's adoption of the Open Source Maintainers Fee creates a risk that Brighter's users are billed for binaries they obtain through us. We fork Polly 8.7.0 as Paramore.Fences, publish our own binaries, and rebrand fully to remove trademark and confusion risk. |

_2 ADRs indexed._
