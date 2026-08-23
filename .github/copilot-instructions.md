---
applyTo: '**'
---

# Copilot Instructions

## How to Use This File

This file contains instructions for GitHub Copilot. It is not intended to be modified by
contributors. Human contributors should follow [CONTRIBUTING.md](../CONTRIBUTING.md), from which
these guidelines derive.

## What this repository is

Fences is a .NET resilience library — a community fork of
[Polly](https://github.com/App-vNext/Polly), maintained by
[Brighter Command](https://github.com/BrighterCommand). It shares an organisation with Brighter and
Darker but **not their engineering conventions**; it inherits Polly's. Constants are `PascalCase`,
there is no licence header, tests live in `test/`, and every public API change is recorded in a
`.PublicAPI/` baseline.

## Standing rules

- **Do not change the public API unless asked.** Any change you do make goes in
  `src/<Project>/.PublicAPI/PublicAPI.Unshipped.txt`.
- **A bug fix must include a test that fails without the fix.**
- **Do not add or update dependencies unless asked.**
- Do not change defaults or go beyond what was asked for.
- Use `TimeProvider` — `DateTime.Now` and friends are banned by an analyser.
- The build treats warnings as errors.

## Detailed Instructions

- [Build and Development](../.agent_instructions/build_and_development.md) — build scripts, test
  commands, mutation targets
- [Project Structure](../.agent_instructions/project_structure.md) — the projects, target
  frameworks, and the strategy taxonomy
- [Code Style](../.agent_instructions/code_style.md) — C# conventions, and how they differ from
  Brighter's
- [Public API Changes](../.agent_instructions/public_api.md) — the `.PublicAPI/` discipline
- [Testing](../.agent_instructions/testing.md) — TDD practice, test structure, mutation threshold
- [Documentation](../.agent_instructions/documentation.md) — XML documentation, docfx, generated
  snippets, ADRs
- [ADR Frontmatter](../.agent_instructions/adr_frontmatter.md) — the ADR metadata schema
- [Design Principles](../.agent_instructions/design_principles.md) — the architecture invariants
- [Dependency Management](../.agent_instructions/dependency_management.md) — central package
  management
- [Release and Versioning](../.agent_instructions/release_and_versioning.md) — MinVer and the
  release scripts
