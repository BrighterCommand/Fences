# AGENTS.md

This file provides guidance to coding agents working in this repository.

## How to Use This File

This file contains instructions for coding agents. It is not intended to be modified by
contributors. Human contributors should follow [CONTRIBUTING.md](CONTRIBUTING.md), from which these
guidelines derive. Where the two disagree, `CONTRIBUTING.md` is right.

## What this repository is

Fences is a .NET resilience library — a community fork of
[Polly](https://github.com/App-vNext/Polly), maintained by
[Brighter Command](https://github.com/BrighterCommand). It is not affiliated with, endorsed by, or
supported by App vNext or the Polly maintainers.

**Fences shares an organisation with [Brighter](https://github.com/BrighterCommand/Brighter) and
[Darker](https://github.com/BrighterCommand/Darker), but not their engineering conventions.** Fences
inherits Polly's, deliberately. Several rules are the exact opposite of Brighter's — constants,
licence headers, test directory, test naming, `InternalsVisibleTo`. Read
[Code Style](.agent_instructions/code_style.md) before applying a habit from there.

## Standing rules

- **Do not change the public API unless you were asked to.** If you do change it, the entry goes in
  `src/<Project>/.PublicAPI/PublicAPI.Unshipped.txt`. This is the thing agents get wrong most often
  here — see [Public API Changes](.agent_instructions/public_api.md).
- **A bug fix must include a test that fails without the fix.**
- **Do not add or update dependencies unless you were asked to.**
- Do not change defaults or make changes beyond what was asked for.
- Do not use APIs marked `[Obsolete]`.
- Ensure the code compiles with no warnings and the tests pass before pushing. CI treats warnings
  as errors.

## Detailed Instructions

Read these as needed:

- [Build and Development](.agent_instructions/build_and_development.md) — Cake targets, `build.ps1`,
  test commands, mutation targets, the analyser escape hatch
- [Project Structure](.agent_instructions/project_structure.md) — the projects, the target framework
  matrix, the core abstractions and the strategy taxonomy
- [Code Style](.agent_instructions/code_style.md) — the conventions, and how they differ from
  Brighter's
- [Public API Changes](.agent_instructions/public_api.md) — the `.PublicAPI/` discipline
- [Testing](.agent_instructions/testing.md) — xUnit, Shouldly, NSubstitute, FsCheck,
  `FakeTimeProvider`, the 100% mutation threshold
- [Documentation](.agent_instructions/documentation.md) — XML docs, docfx, the generated snippets,
  and how to write an ADR
- [ADR Frontmatter](.agent_instructions/adr_frontmatter.md) — the frontmatter schema and tag
  vocabulary
- [Design Principles](.agent_instructions/design_principles.md) — the architecture invariants worth
  defending
- [Dependency Management](.agent_instructions/dependency_management.md) — central package
  management, and the multi-targeting constraint
- [Release and Versioning](.agent_instructions/release_and_versioning.md) — MinVer, the release
  scripts, and what they own

## Workflows

`.claude/commands/` holds commands that walk an agent through the preferred workflow —
`/test-first`, `/tidy-first`, `/adr`, `/spec:*` and `/bugfix:*`. See
[.claude/commands/README.md](.claude/commands/README.md). They are written for Claude Code, but the
gates they describe apply whatever agent you are.
