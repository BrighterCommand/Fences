---
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, AskUserQuestion
description: TDD workflow with mandatory approval before implementation
argument-hint: <behavior description>
---

# Test-First Development (TDD with Approval Gate)

You are guiding the user through a Test-Driven Development workflow with a **mandatory approval gate** before implementation.

## The Behavior to Test

$ARGUMENTS

## Workflow Phases

### 🔴 RED Phase - Write Failing Test

**Your task:** Write a test that specifies the desired behavior, following the guidelines in [.agent_instructions/testing.md](../../../.agent_instructions/testing.md).

**Test Requirements:**

1. **Naming Convention**: Method names use `Member_Scenario_Outcome` — for example `Ctor_EnsureDefaults`, `ExecuteAsync_CanceledBeforeExecution_EnsureNotExecuted`. Class names are the type under test plus `Tests`, e.g. `RetryResilienceStrategyTests`. Do **not** use Fences's `When_X_Then_Y` style.
2. **File Structure**: Group tests by the type under test, in `[TypeUnderTest]Tests.cs`. Fences does not use one-test-per-file — `SA1402` is switched off and the existing suites hold many tests per class.
3. **Test Structure**: Use Arrange/Act/Assert with explicit comments
4. **Evident Data**: Highlight only the data that impacts the test outcome
5. **Test Exports Only**: Test public methods on public classes only
6. **Developer Test Style**: Do NOT use mocks for isolation - we test behaviors, not classes
7. **Time Substitution**: Use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` and advance it. **Never** put a real `Task.Delay` in a test to wait for a strategy — the strategies take a `TimeProvider` precisely so tests can control the clock.

**Steps:**

1. Identify the exact behavior being tested
2. Determine which public class/method will provide this behavior
3. Write the test following the xUnit BDD-style naming
4. Run the test to verify it fails for the right reason
5. Show the test code and failure message to the user

**After writing the test, proceed to the Approval Gate.**

---

### ✅ APPROVAL GATE - User Must Approve Test

**CRITICAL: You MUST get explicit user approval before proceeding to implementation.**

Use the AskUserQuestion tool to ask:

```text
Question: "Should I proceed to implement the code to make this test pass?"
Options:
1. "Yes, implement the code" - Proceed to GREEN phase
2. "Modify the test first" - User will explain changes needed
3. "Cancel" - Stop the workflow
```

**If user requests modifications:**

- Make the requested changes to the test
- Run it again to verify it still fails appropriately
- Ask for approval again

**Do NOT proceed to implementation without explicit approval.**

---

### 🟢 GREEN Phase - Make Test Pass

**Only execute this phase after receiving approval.**

**Your task:** Write the **minimum code** necessary to make the test pass.

**Code Requirements** (from [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md)):

1. Follow .NET C# naming conventions; constants are `PascalCase`, **not** `ALL_CAPS`
2. Expression-bodied members are **required** at severity `error` for methods, constructors, operators, properties, indexers and accessors
3. `using` directives go outside the namespace, `System.*` first; file-scoped namespaces
4. Static private/internal fields are `PascalCase` with no prefix
5. Use `readonly` for fields that do not change after construction
6. Use `TimeProvider` — `DateTime.Now` and friends are banned by an analyser
7. **Do not add a licence header.** This repository has none
8. Keep methods small and focused on a single responsibility
9. Do not change the public API unless asked; if you do, update `src/<Project>/.PublicAPI/PublicAPI.Unshipped.txt` (see [.agent_instructions/public_api.md](../../../.agent_instructions/public_api.md))

**Documentation Requirements** (from [.agent_instructions/documentation.md](../../../.agent_instructions/documentation.md)):

1. Add XML documentation comments (`///`) for all public members
2. Use `<summary>`, `<param>`, `<returns>`, `<exception>` tags
3. Add `<remarks>` for complex implementation details

**Steps:**

1. Write ONLY the code needed to make this specific test pass
2. Do NOT write speculative code for future requirements
3. Run the test to verify it passes
4. Run all related tests to ensure no regressions
5. Show the implementation and test results to the user

---

### 🔵 REFACTOR Phase - Improve Design (Optional)

**Your task:** Review the implementation for potential improvements while keeping tests green.

**Refactoring Principles:**

1. Keep methods small (avoid more than one level of indentation)
2. Extract methods to express intent
3. Replace primitives with expressive types where appropriate
4. Ensure clear responsibility assignment
5. Simplify conditional logic

**IMPORTANT - Tidy First Approach** (from [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md)):

- Only make STRUCTURAL changes (renaming, extracting methods, moving code)
- Do NOT change behavior during refactoring
- Run all tests after each refactoring to verify behavior unchanged

**Steps:**

1. Identify potential design improvements
2. Make one refactoring at a time
3. Run all tests after each change
4. Show the refactored code with explanation

---

## Completion

After completing all phases:

1. **Suggest a commit message** following conventional commits format:
   - `test: add test for [behavior]` (if only test was committed before approval)
   - `feat: [description]` (for new feature implementation)
   - `fix: [description]` (for bug fix implementation)
   - `refactor: [description]` (for refactoring changes)

2. **Ask the user:**
   - "Should I commit these changes?"
   - "Do you want to write another test for the next behavior?"

---

## Key Principles

**From testing.md:**

- The approval step is **MANDATORY** when working with an AI coding assistant
- Tests should specify behavior, not implementation details
- Only write code required by tests - no speculative code
- Tests should use the Arrange/Act/Assert structure
- Use developer tests (not unit tests) - tests implicate the most recent edit

**From code_style.md:**

- Follow Responsibility Driven Design
- Maximize abstraction - think in terms of roles and responsibilities
- Distribute behavior - make objects smart
- Preserve flexibility - interior details should be readily changeable
- Avoid primitive obsession

**Scope Control:**

- Write the most obvious test to move towards the requirement
- Only implement what's needed for the current test
- Each test should be a small, focused step forward

---

## Example Session

```text
User: /test-first the retry strategy widens its delay when the backoff curve is configured

🔴 RED: Writing test ExecuteAsync_CanceledBeforeExecution_EnsureNotExecuted

[Shows test code]
Test file: test/Paramore.Fences.Core.Tests/ExecuteAsync_CanceledBeforeExecution_EnsureNotExecuted.cs

Running test...
Test fails: ✓ "Method SendToDeadLetterQueue not found on RetryResilienceStrategy"

✅ APPROVAL REQUIRED
Should I proceed to implement the code to make this test pass?

User: Yes

🟢 GREEN: Implementing SendToDeadLetterQueue method

[Shows implementation code with XML docs]

Running tests...
✓ ExecuteAsync_CanceledBeforeExecution_EnsureNotExecuted PASSED
✓ All 47 tests in Paramore.Fences.Core.Tests PASSED

🔵 REFACTOR: Extracting validation logic into IsValidMessage method

[Shows refactoring]

Running tests...
✓ All 47 tests PASSED

Suggested commit message:
feat: add a configurable backoff curve to the retry strategy

Should I commit these changes?
```

---

## Notes

- This workflow enforces the TDD approval requirement from .agent_instructions/testing.md
- The approval gate ensures the test correctly specifies desired behavior before implementation
- Following this workflow provides scope control and better design
- You may run `/test-first` multiple times to build up functionality incrementally
