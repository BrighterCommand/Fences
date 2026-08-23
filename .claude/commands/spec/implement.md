---
allowed-tools: Bash(cat:*), Bash(test:*), Bash(ls:*), Bash(echo:*), Bash(dotnet:*), Bash(git:*), Read, Write, Edit, Glob, Grep, AskUserQuestion
description: Start TDD implementation from approved tasks
argument-hint: [task-number]
---

# Start TDD implementation from approved tasks

## Context

Current spec directory: specs/

**Workflow**: Issue → Requirements → ADR(s) → Tasks → **Tests → Code**

**TDD Cycle**: 🔴 Red → ✅ User Approval → 🟢 Green → 🔵 Refactor

> **Recommended model: `sonnet`.** Unlike the other `/spec:*` commands, `/spec:implement`
> does its work in the **main agent** (the interactive approval gate must reach the user),
> so there is no sub-agent to assign a model to — the session model is what runs. This is
> implementation work, which the model policy puts on **sonnet**. Step 0 below actively
> prompts you to switch if the session is on another model. See
> `.claude/commands/spec/README.md` → "Sub-agents & model policy".

## Critical Guidelines

**ALWAYS follow these instructions when writing code:**

- **Testing**: [.agent_instructions/testing.md](../../../.agent_instructions/testing.md)
- **Code Style**: [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md)

## Your Task

### Step 0: Confirm the Session Model

`/spec:implement` is interactive implementation work and the model policy puts it on
**sonnet**. Check the session's current model:

- **If already on sonnet**: continue silently to Step 1.
- **If on any other model** (e.g. opus, haiku): use `AskUserQuestion` to ask whether to
  switch to sonnet before starting — e.g. "This session is on {model}. `/spec:implement` is
  recommended on sonnet. Switch to sonnet first?" with options to **switch** (tell the user to
  run `/model sonnet`, since a command can't change the session model itself) or **continue on
  the current model**. Respect the choice; do not switch on their behalf, and do not block if
  they decline.

This is a one-time check at the start of the command.

### Step 1: Gather Context

1. Read `specs/.current-spec` to determine the active specification directory
2. Verify `.tasks-approved` exists in that directory
3. Read `specs/{current-spec}/tasks.md` to see task list
4. Read `specs/{current-spec}/.adr-list` to see all ADRs
5. Read ADRs from `docs/adr/` to understand design decisions
6. If task number provided in $ARGUMENTS, focus on that task only

### Step 2: Verify Prerequisites

Check that all phases are approved:

- Requirements: `.requirements-approved` exists
- Design: `.design-approved` exists and all ADRs have Status "Accepted"
- Tasks: `.tasks-approved` exists

If not all approved, inform user and exit.

### Step 3: Select Task

Display current incomplete tasks from tasks.md.

If task number provided, work on that specific task.
Otherwise, suggest the next logical task to work on.

### Step 4: TDD Implementation Cycle

For each task, follow this strict workflow:

#### 🔴 RED: Write a Failing Test

1. **Read Testing Guidelines**: Review [.agent_instructions/testing.md](../../../.agent_instructions/testing.md)

2. **Understand the Behavior**: Identify the specific behavior this task requires
   - What is the expected behavior?
   - What is the simplest test that demonstrates this behavior?

3. **Write the Test** following these rules from testing.md:
   - **Test naming**: `Member_Scenario_Outcome`
   - **File naming**: Group tests by the type under test, in `[TypeUnderTest]Tests.cs`
   - **Structure**: Use Arrange/Act/Assert with explicit comments
   - **Evident Data**: Highlight the state that impacts the test outcome
   - **Test behavior, not implementation**: Test public exports only
   - **No mocks for isolation**: Use developer tests that implicate the most recent edit
   - **Control time, never wait**: use `FakeTimeProvider` and advance it; never put a real `Task.Delay` in a test
   - **Prefer existing helpers**: `test/Paramore.Fences.TestUtils` holds `TestResilienceStrategy`, `FakeTelemetryListener`, `FakeLogger` and friends
   - **Only test public exports**: Don't test private or internal methods

4. **Create/Update Test File**: Use Write or Edit tool to create the test

5. **Run the Test**: Use Bash to run: `dotnet test [test-project] --framework net10.0 --filter "FullyQualifiedName~[TestName]"`
   - Verify the test FAILS (Red)
   - The failure should be for the expected reason (behavior doesn't exist yet)

6. **Show Test to User**:
   - Display the test code
   - Explain what behavior it tests
   - Show the test failure output
   - Explain why this is the next logical step

#### ✅ USER APPROVAL: Get Approval for Test

**CRITICAL**: Before writing any implementation code, you MUST:

1. Use AskUserQuestion tool to ask: "I've written a failing test for [behavior]. The test verifies that [expected behavior]. Should I proceed to make this test pass?"

2. Wait for user approval

3. If user requests changes to the test:
   - Make the requested changes
   - Re-run the test to verify it still fails correctly
   - Ask for approval again

**DO NOT proceed to implementation without explicit user approval of the test.**

#### 🟢 GREEN: Make the Test Pass

1. **Read Code Style Guidelines**: Review [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md)

2. **Write Minimum Code** to make the test pass:
   - Only write code necessary for the test to pass
   - No speculative code
   - "Commit any sins necessary to move fast" - don't worry about perfect design yet
   - That comes in the Refactor step

3. **Follow Code Style** from code_style.md:
   - Use .NET C# naming conventions (PascalCase for public, camelCase for private)
   - Constants are `PascalCase` — **not** `ALL_CAPS`
   - Expression-bodied members are **required** at severity `error`, not merely preferred
   - Use `readonly` for fields that don't change after construction
   - `Nullable` is enabled everywhere except `src/Paramore.Fences`, which opts out on purpose
   - Use `TimeProvider`; `DateTime.Now` and friends are banned
   - **Do not add a licence header**

4. **Create/Update Implementation Files**: Use Write or Edit tool

5. **Run the Test Again**: `dotnet test [test-project] --framework net10.0 --filter "FullyQualifiedName~[TestName]"`
   - Verify the test PASSES (Green)

6. **Run All Tests**: `dotnet test` to ensure no regressions

7. **Fences gates** — check each of these before calling the cycle done:
   - The build is **analyser-clean**. StyleCop, Sonar and BannedApiAnalyzers run during every build
     and CI treats warnings as errors.
   - If a `public` or `protected` member in `src/` changed, the entry is in that project's
     `.PublicAPI/PublicAPI.Unshipped.txt` — see `.agent_instructions/public_api.md`. If the task did
     not ask for an API change, stop and reconsider rather than adding the entry.
   - If anything under `src/Snippets` changed, `dotnet mdsnippets` has been run and the regenerated
     Markdown is committed alongside the C#.
   - If a project under `src/` was touched, its Stryker mutation score has not regressed
     (`./build.ps1 -Target MutationTests<Project>`).
   - No new dependency was added. If the task needs one, stop and ask.

8. **Show Results to User**:
   - Show what code was added/changed
   - Show the test now passes
   - Show all tests still pass

#### 🔵 REFACTOR: Improve the Design

1. **Review the Code** for design improvements:
   - Is it tidy and simple?
   - Can complexity be reduced?
   - Are there any code smells?
   - Does it follow Responsibility Driven Design?
   - Does it avoid primitive obsession?
   - Are methods small and focused?
   - Is there duplicated knowledge?
   - Is intention revealed clearly?

2. **Apply "Tidy First" Principles**:
   - Separate structural changes from behavioral changes
   - Make structural improvements (renaming, extracting methods, moving code)
   - Don't change behavior during refactoring

3. **Make Refactoring Changes**: Use Edit tool to improve the design
   - Keep methods small and focused on single responsibility
   - Extract methods if more than one level of indentation
   - Use expressive types instead of primitives
   - Distribute behavior appropriately

4. **Run All Tests After Each Refactoring**: Verify no behavioral changes
   - Tests should still pass
   - If a test breaks, the refactoring changed behavior (rollback)

5. **Show Refactoring to User**:
   - Explain what was refactored and why
   - Show the improved design
   - Confirm all tests still pass

### Step 5: Commit the Change

After completing Red-Green-Refactor for a behavior:

1. **Stage Changes**:

   ```bash
   git add [test-file] [implementation-files]
   ```

2. **Commit with Descriptive Message**:

   ```bash
   git commit -m "feat: [behavior description]

   - Test: Member_Scenario_Outcome
   - Implementation: [brief description]

   Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
   ```

3. **Update Tasks**: Use Edit tool to check off completed task in `specs/{current-spec}/tasks.md`

### Step 6: Continue to Next Behavior

Ask user: "This behavior is complete. Should I continue to the next test, or would you like to review?"

- If continue: Return to Step 4 (Red-Green-Refactor cycle)
- If review: Show current progress and wait for next instruction

## Important Reminders

### Test-First Requirements

- **NEVER write implementation before writing a failing test**
- **ALWAYS get user approval of the test before implementing**
- Each test should represent the smallest possible behavioral step
- The next test should be the most obvious step toward implementing the requirement

### Code Quality Requirements

- Follow ALL guidelines in .agent_instructions/testing.md
- Follow ALL guidelines in .agent_instructions/code_style.md
- Keep changes small and incremental
- Each Red-Green-Refactor cycle should take minutes, not hours
- Commit frequently (after each successful cycle)

### Test Scope

- Only test public exports from assemblies
- Don't test private or internal implementation details
- Control time with `FakeTimeProvider`; never wait on a real `Task.Delay`
- Tests should be coupled to behavior, not implementation

### Design Principles

See `.agent_instructions/design_principles.md`. The invariants that matter most here:

- A strategy is either **reactive** or **proactive** — never both, never neither
- Every strategy has a matching `*Options` class, and the options class is the public surface
- Handled outcomes are declared through `PredicateBuilder`, not loose predicate parameters
- `ResilienceContext` is **pooled** — never hold a reference beyond its execution
- `src/Paramore.Fences` is frozen; new work goes in `src/Paramore.Fences.Core`
- Keep methods small: single responsibility, minimal indentation

## Example Session

```text
🔴 RED Phase:
Adding test ExecuteAsync_BackoffCurveConfigured_DelaysWiden
to test/Paramore.Fences.Core.Tests/Retry/RetryResilienceStrategyTests.cs
[Shows test code]
Test fails with: "RetryStrategyOptions does not contain a definition for BackoffCurve"

✅ USER APPROVAL:
Asking: Should I proceed to make this test pass?
User: Yes, proceed

🟢 GREEN Phase:
Adding BackoffCurve property to RetryStrategyOptions
[Shows implementation]
Test now passes ✓
All tests pass ✓

🔵 REFACTOR Phase:
Extracting the curve evaluation into a private method
Renaming a local for clarity
[Shows refactored code]
All tests still pass ✓

🔒 Fences gates:
Build is analyser-clean ✓
Public API changed — appended to
  src/Paramore.Fences.Core/.PublicAPI/PublicAPI.Unshipped.txt ✓
src/Snippets untouched, no mdsnippets run needed ✓
MutationTestsCore: 100% (unchanged) ✓

✓ Committed: feat: add a configurable backoff curve to the retry strategy
✓ Updated tasks.md

Ready for next behavior!
```

Use Read, Write, Edit, Bash, and AskUserQuestion tools throughout the implementation process.
