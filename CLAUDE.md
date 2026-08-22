# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this
repository.

> **Interim file.** Fences is mid-migration from its Polly fork. This is a stub so that a fresh
> session finds the right context; Phase 5 of `fork-migration-plan.md` (P5.11) replaces it with
> the full version, mirroring Brighter's.

## Read these first, in this order

1. **`PROMPT.md`** (untracked, local) — current session state: what is done, what is next, and the
   established facts not to re-derive. **Always read this before starting work.**
2. **`fork-migration-plan.md`** — the authoritative migration plan. Decisions in §3, phases in §4,
   decision log in §7.
3. **`AGENTS.md`** — build commands and architecture (still written in upstream Polly terms until
   Phase 2 renames it).

## Context management

Prefer **project-owned files** over ephemeral Claude memory:

- `PROMPT.md` for temporary state that should persist across conversations.
- `fork-migration-plan.md` for anything that is a decision or a plan.
- `.agent_instructions/` for durable conventions (created in Phase 5).

Only use Claude memory (`MEMORY.md`) for user-specific preferences that do not belong in the
project.

## Standing constraints during the migration

- **Do not publish to NuGet.** Phase 8 is held pending the Polly negotiation; published IDs can
  never be deleted, only unlisted.
- **Do not commit to `main`.** Work on `fork/rebrand-to-fences`.
- **Do not change the public API** unless explicitly asked. Every public API change requires an
  update to the relevant `src/*/.PublicAPI/PublicAPI.Unshipped.txt`.
- **Do not add or update dependencies** unless explicitly asked.
- Bug fixes must include a test that fails without the fix.
- This repo's conventions are **not** Brighter's — see `PROMPT.md` item 7 before applying any
  Brighter habit here.
