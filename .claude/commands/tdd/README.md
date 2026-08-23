# Test-Driven Development (TDD) Commands

This directory contains Claude Code commands that enforce Test-Driven Development workflows for the Fences project.

## Commands

### `/test-first <behavior description>`

Guides you through the Red-Green-Refactor TDD cycle with a **mandatory approval gate** before implementation.

**Purpose**: Ensures you write and approve tests before writing implementation code, preventing scope creep and promoting better design.

**Usage:**

```bash
/test-first the retry strategy widens its delay when the backoff curve is configured
```

**Workflow:**

1. **🔴 RED Phase**: Claude writes a failing test following Fences's testing conventions
   - Naming: `Member_Scenario_Outcome` — **not** Brighter's `When_X_Then_Y`
   - Tests grouped by the type under test, in `[TypeUnderTest]Tests.cs`
   - Arrange/Act/Assert structure
   - Tests public exports only
   - Uses `FakeTimeProvider` to control time, and the shared fakes in `test/Paramore.Fences.TestUtils`

2. **✅ APPROVAL GATE**: Claude asks for your explicit approval
   - You must approve the test before implementation begins
   - You can request modifications to the test
   - Implementation only proceeds after approval

3. **🟢 GREEN Phase**: Claude implements minimum code to pass the test
   - Only writes what's needed for this specific test
   - No speculative code
   - Follows Fences's code style and documentation standards

4. **🔵 REFACTOR Phase**: Claude suggests design improvements (optional)
   - Structural changes only (no behavior changes)
   - Tests remain green throughout

**Why Use This?**

From [.agent_instructions/testing.md](../../../.agent_instructions/testing.md):
> It has a mandatory approval gate: you write the test, you stop, the user reviews it, and only
> then do you implement. Do not write a test by hand and carry straight on into the implementation.

This command enforces that requirement automatically, ensuring:

- Tests correctly specify desired behavior before implementation
- Scope control - only code required by tests is written
- Better design - thinking about behavior first
- No speculative code
- Incremental progress through small, focused tests

**Related Guidelines:**

- [Testing Guidelines](../../../.agent_instructions/testing.md)
- [Code Style](../../../.agent_instructions/code_style.md)
- [Documentation Standards](../../../.agent_instructions/documentation.md)

## Integration with Spec Workflow

The `/test-first` command can be used standalone or as part of the [specification workflow](../spec/README.md).

- **Standalone**: Use anytime you want to add behavior with TDD
- **With /spec:implement**: The spec implement command uses the same TDD workflow with approval gates

## Example Session

```text
$ /test-first the retry strategy widens its delay when the backoff curve is configured

🔴 RED: Adding ExecuteAsync_BackoffCurveConfigured_DelaysWiden to
test/Paramore.Fences.Core.Tests/Retry/RetryResilienceStrategyTests.cs
[Claude shows test code]
Test fails: ✓ "RetryStrategyOptions does not contain a definition for BackoffCurve"

✅ Should I proceed to implement the code to make this test pass?
> Yes

🟢 GREEN: Adding the BackoffCurve property to RetryStrategyOptions...
[Claude shows implementation]
✓ Test passes
✓ All tests pass
✓ Public API changed — appended to
  src/Paramore.Fences.Core/.PublicAPI/PublicAPI.Unshipped.txt

🔵 REFACTOR: Extracting the curve evaluation...
[Claude shows refactoring]
✓ All tests still pass

Suggested commit: feat: add a configurable backoff curve to the retry strategy

Should I commit these changes?
```

## Best Practices

1. **Start small**: Write the simplest test that moves you toward your goal
2. **One behavior at a time**: Use `/test-first` multiple times to build up functionality
3. **Review the test carefully**: The test is your specification - make sure it's correct before approving
4. **Trust the process**: Don't skip ahead to implementation - the approval gate is there for a reason
5. **Refactor regularly**: Take advantage of the refactor phase to improve design while tests are green

## Fences gates

`/test-first` is not finished when the test is green. Before the cycle counts as done:

- The build is **analyser-clean** — CI treats warnings as errors.
- If a `public` or `protected` member in `src/` changed, the entry is in that project's
  `.PublicAPI/PublicAPI.Unshipped.txt`. See
  [Public API Changes](../../../.agent_instructions/public_api.md).
- If a project under `src/` was touched, its Stryker mutation score has not regressed.
- No dependency was added or updated.
