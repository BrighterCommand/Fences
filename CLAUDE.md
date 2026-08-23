# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this
repository.

## How to Use This File

This file contains instructions for Claude Code. It is not intended to be modified by contributors.
Human contributors should follow [CONTRIBUTING.md](CONTRIBUTING.md), from which these guidelines
derive. Where the two disagree, `CONTRIBUTING.md` is right.

[AGENTS.md](AGENTS.md) carries the agent-neutral version of the same material. This file adds the
workflow rules that are specific to Claude Code.

## ⚠️ Fences is not Brighter

Fences shares an organisation with Brighter and Darker but **not their engineering conventions**.
It inherits Polly's, deliberately. Several rules are the exact opposite:

| Rule | Fences | Brighter |
| --- | --- | --- |
| Constants | `PascalCase` | `ALL_CAPS` |
| Licence header | **None.** Do not add one | `#region Licence` on every file |
| Test directory | `test/` | `tests/` |
| Test naming | `Member_Scenario_Outcome` | `When_X_Then_Y` |
| Test doubles | NSubstitute | FakeItEasy |
| Expression-bodied members | Required, at `error` | Preferred |
| `InternalsVisibleTo` | The established mechanism | Banned |
| Public API baseline | Every change updates `.PublicAPI/` | No equivalent |

If you have Brighter context in this session, check it against
[.agent_instructions/code_style.md](.agent_instructions/code_style.md) before applying it.

## ⛔ TDD Workflow (MANDATORY — NOT OPTIONAL)

When working on implementation tasks in `specs/*/tasks.md`:

- **ALWAYS use `/test-first <behavior>`** for TEST tasks
- **NEVER write tests manually and proceed to implementation**
- **STOP and ASK FOR APPROVAL** after writing each test
- The user will review the test in their IDE before you implement
- Each TEST task in `tasks.md` specifies the exact `/test-first` command to use
- The command enforces the approval gate automatically — you cannot bypass it

**Why this is mandatory:**

1. Tests correctly specify desired behaviour before implementation
2. Scope control — only code required by tests is written
3. No speculative code
4. The user reviews the test in their IDE, not in CLI output

**If a task says `/test-first when ...`** — YOU MUST USE THAT COMMAND. Do not write the test file
by hand.

Independently of the spec workflow: **a bug fix must include a test that fails without the fix.**

## ⛔ Public API discipline

This is the rule most often got wrong in this repository.

- **The default posture is: do not change the public API.** If the task did not ask for an API
  change and you find yourself writing `public`, stop and reconsider.
- If you do change it, append the entry to `src/<Project>/.PublicAPI/PublicAPI.Unshipped.txt` —
  never to `Shipped.txt`. A removal is a `*REMOVED*` line.
- The analyser fails the build on any difference, so this is not optional bookkeeping.

See [.agent_instructions/public_api.md](.agent_instructions/public_api.md).

## Spec Workflow

Follow the structured specification workflow: Requirements → ADR Design → Adversarial Review
(multiple rounds) → Task Breakdown → Implementation. Never skip review rounds or assume approval —
wait for explicit user approval before proceeding to the next phase.

## Change Scope

Do NOT change defaults or make changes beyond what was explicitly requested. When fixing or
modifying code, restrict changes to exactly what the user asked for — no additional "improvements"
and no default value changes. The same applies to dependencies: **do not add or update one unless
asked**.

## Adversarial Reviews

When conducting adversarial reviews, apply strict judgment criteria. A clear violation should result
in FAIL, not NEEDS_ATTENTION. Err on the side of strictness rather than leniency when evaluating
against guardrails and principles.

## Claude Code Commands

Commands automate common workflows and enforce mandatory engineering practices. **Use them
proactively** rather than manually following the documented procedure.

- **[Command index](.claude/commands/README.md)** — full documentation for all of them

### Core development

- `/test-first <behavior>` — TDD with a mandatory approval gate
  ([docs](.claude/commands/tdd/README.md))
- `/tidy-first <change>` — separate structural from behavioural changes
  ([docs](.claude/commands/refactor/README.md))
- `/adr <title>` — create an Architecture Decision Record
  ([docs](.claude/commands/adr/README.md))
- `/bugfix:*` — diagnosis-first bug workflow: Triage → Confirm (✋ gate) → Test-first → Fix →
  Verify. Use it for a bug whose root cause is not yet proven, or one that arrived with a suggested
  fix you should verify first ([docs](.claude/commands/bugfix/README.md))

### Specification workflow

- `/spec:requirements`, `/spec:design`, `/spec:tasks`, `/spec:implement`, `/spec:review`,
  `/spec:status` ([docs](.claude/commands/spec/README.md))

**When to use which:**

- `/test-first` when adding behaviour or fixing a bug
- `/tidy-first` when code needs refactoring before or during feature work
- `/adr` when documenting an architectural decision
- `/spec:*` for full feature development from requirements to implementation

## Verification gates

Before calling any change done, check all of these. They are what CI checks.

1. The build is **analyser-clean** — StyleCop, Sonar and BannedApiAnalyzers run in every build and
   CI treats warnings as errors.
2. Tests pass. Name one framework while iterating: test projects multi-target `net10.0;net9.0;net8.0`.
3. If a public member changed, `.PublicAPI/PublicAPI.Unshipped.txt` was updated.
4. If `src/Snippets` changed, `dotnet mdsnippets` was run and the regenerated Markdown committed.
5. If a project under `src/` was touched, its Stryker mutation score has not regressed.
6. No dependency was added or updated.

There is **no external infrastructure** — no Docker, no database, no broker. Every test in this
repository runs in process.

## Context Management

When asked to remember learnings or update guidance:

- **Prefer project-owned files** (`.agent_instructions/`, `CLAUDE.md`, `PROMPT.md`) over ephemeral
  Claude memory. Project-owned files are shared, version-controlled and authoritative.
- Update `.agent_instructions/code_style.md` for coding conventions,
  `.agent_instructions/testing.md` for test practices, and so on.
- Use `PROMPT.md`, if it exists, for temporary state that should persist across conversations.
- Only use Claude memory (`MEMORY.md`) for user-specific preferences that do not belong in the
  project.

## Migration status

Fences is mid-migration from its Polly fork. While that is true:

- **Do not publish to NuGet.** Published package IDs can never be deleted, only unlisted.
- **Do not commit to `main`.** Work on the migration branch.
- **Do not rewrite `CHANGELOG.md`.** It is the historical record and still describes Polly
  releases up to the fork. That is deliberate.
- Most files under `docs/` still say Polly. That is expected, not a miss — the prose rewrite is a
  later phase.
- `fork-migration-plan.md` is the authoritative plan; `PROMPT.md`, if present, is the current
  session state. Read `PROMPT.md` first when picking up migration work.

## Detailed Instructions

See [AGENTS.md](AGENTS.md) for the full index into
[`.agent_instructions/`](.agent_instructions/).
