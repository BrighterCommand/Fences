# Claude Code Skills for Fences Development

This directory contains Claude Code skills (slash commands) that enforce Fences's engineering practices and streamline common development workflows.

## Quick Start

Skills are invoked using slash commands in Claude Code:

```bash
/test-first <behavior description>    # TDD with mandatory approval
/adr <title>                          # Create Architecture Decision Record
/tidy-first <change description>      # Separate structural from behavioral changes
```

## Available Skills

### 1. Test-Driven Development

**Command**: `/test-first <behavior description>`

**Purpose**: Enforces TDD workflow with mandatory user approval before implementation.

**When to use**:

- Adding new behavior or functionality
- Fixing bugs with test-first approach
- Want to ensure tests are correct before writing implementation

**Workflow**:

1. 🔴 **RED**: Claude writes a failing test following Fences conventions
2. ✅ **APPROVAL**: You must approve the test before implementation
3. 🟢 **GREEN**: Claude implements minimum code to pass the test
4. 🔵 **REFACTOR**: Claude suggests design improvements (optional)

**Example**:

```bash
/test-first the retry strategy widens its delay when the backoff curve is configured
```

**Why it matters**: the approval step is mandatory per [.agent_instructions/testing.md](../../.agent_instructions/testing.md). This command enforces it, preventing implementation before you have validated the test as a specification.

📖 **Documentation**: [.claude/commands/tdd/README.md](tdd/README.md)

---

### 2. Architecture Decision Records

**Command**: `/adr <title>`

**Purpose**: Automates creation of properly formatted and numbered ADRs.

**When to use**:

- Making significant architectural decisions
- Need to document WHY a design choice was made
- Want to capture alternatives considered
- Required for new capabilities per CONTRIBUTING.md

**What it does**:

1. Scans `docs/adr/` to find next sequence number
2. Checks for current spec and links if applicable
3. Prompts for key ADR content (context, decision, alternatives, consequences)
4. Creates properly named file: `docs/adr/[NNNN]-[title].md`
5. Updates spec's `.adr-list` if part of spec workflow

**Example**:

```bash
/adr backoff curve selection
```

**Output**: Creates `docs/adr/0037-hedging-result-selection.md` with proper structure, Status: Proposed.

**Why it matters**: ADRs capture the WHY behind decisions, not just the WHAT. This skill ensures they're created consistently and tracked properly.

📖 **Documentation**: [.claude/commands/adr/README.md](adr/README.md)

---

### 3. Tidy First - Separate Structural from Behavioral Changes

**Command**: `/tidy-first <change description>`

**Purpose**: Enforces Beck's "Tidy First" methodology by separating refactoring from functionality changes into distinct commits.

**When to use**:

- Need to refactor code AND add/change functionality
- Existing code is messy and needs cleanup before modification
- Want cleaner git history and easier code reviews
- Large methods need breaking down before adding features

**Workflow**:

1. **Analysis**: Categorizes changes into structural (refactoring) vs behavioral (functionality)
2. **Plan**: Gets your approval of categorization
3. **Structural Phase**: Makes refactoring changes only
4. **Validate**: Runs tests - all must pass (behavior unchanged)
5. **Commit**: Creates `refactor:` commit
6. **Behavioral Phase**: Makes functionality changes
7. **Validate**: Runs tests with new behavior
8. **Commit**: Creates `feat:`/`fix:`/`perf:` commit

**Example**:

```bash
/tidy-first optimize the message processing in RetryResilienceStrategy
```

**Output**: Two separate commits:

1. `refactor: simplify message processing structure in RetryResilienceStrategy`
2. `feat: add caching and exponential backoff to message processing`

**Why it matters**: Separating structural from behavioral changes makes code reviews easier, git history clearer, and reduces bugs. Required per code_style.md.

📖 **Documentation**: [.claude/commands/refactor/README.md](refactor/README.md)

---

### 4. Bugfix - Diagnosis-First Bug Workflow

**Commands**: `/bugfix:triage`, `/bugfix:confirm`, `/bugfix:test`, `/bugfix:fix`, `/bugfix:verify` (plus `/bugfix:status`, `/bugfix:switch`)

**Purpose**: A lightweight, diagnosis-first workflow for fixing bugs. It is `/test-first` wrapped with an explicit **Confirm** gate up front — because a bug's root cause is a hypothesis until proven.

**When to use**:

- A defect whose root cause is not yet proven
- An issue that arrived with a suggested fix (including agent-authored) you should verify before trusting
- Anywhere `/test-first` alone would jump to a test for an *assumed* cause

**Workflow**:

1. **Triage** (`/bugfix:triage [issue|description]`) - Restate the symptom, locate the code, form a root-cause hypothesis (any suggested fix is UNVERIFIED)
2. ✋ **Confirm** (`/bugfix:confirm`) - Prove the hypothesis by code-trace and/or red repro before any fix; surfaces scope changes / extra defects
3. ✋ **Test-first** (`/bugfix:test`) - Delegates to `/test-first` for the failing regression test
4. **Fix** (`/bugfix:fix`) - Minimal change to green, scoped to the confirmed cause
5. **Verify** (`/bugfix:verify`) - Run the suite; capture the root cause and `Fixes #N` in the commit/PR

**Example**:

```bash
/bugfix:triage 4054     # ASB SessionId case-sensitivity
/bugfix:confirm         # proves CamelCase round-trip cause; finds a 2nd defect
/bugfix:test            # red regression test (via /test-first)
/bugfix:fix             # minimal fix scoped to the confirmed cause
/bugfix:verify          # suite green; fix: commit with Fixes #4054
```

**Why it matters**: The Confirm gate stops you fixing a symptom or trusting a wrong suggested fix — and frequently changes the scope of the fix. It deliberately omits the ADR/requirements/review rounds that `/spec` mandates.

📖 **Documentation**: [.claude/commands/bugfix/README.md](bugfix/README.md)

---

## Skill Categories

### Development Workflow Skills

- **`/test-first`** - TDD with approval gate
- **`/tidy-first`** - Safe refactoring workflow
- **`/bugfix:*`** - Diagnosis-first bug workflow (Triage → Confirm → Test-first → Fix → Verify)

### Documentation Skills

- **`/adr`** - Architecture Decision Records

### Specification Workflow Skills (Pre-existing)

- **`/spec:requirements`** - Capture requirements
- **`/spec:design`** - Create design ADRs
- **`/spec:tasks`** - Break down implementation
- **`/spec:implement`** - TDD implementation
- **`/spec:status`** - Show spec status
- **`/spec:approve`** - Approve phases
- **`/spec:review`** - Review phases

📖 **Documentation**: [.claude/commands/spec/README.md](spec/README.md)

---

## When to Use Which Skill

### Decision Tree

```text
Do you need to document an architectural decision?
├─ Yes → /adr <title>
└─ No ↓

Are you fixing a bug?
├─ Yes ↓
│   └─ Is the root cause already proven/obvious?
│       ├─ No  → /bugfix:triage  (Triage → Confirm gate → Test-first → Fix → Verify)
│       └─ Yes → /test-first <behavior>  (cause is clear; just need the test)
└─ No ↓

Are you adding new behavior?
├─ Yes ↓
│   └─ Does existing code need refactoring first?
│       ├─ Yes → /tidy-first <description>
│       └─ No → /test-first <behavior>
└─ No ↓

Are you just refactoring with no behavior changes?
├─ Yes → /tidy-first <description> (will create single refactor commit)
└─ No → Use standard workflow
```

### Common Scenarios

#### Scenario 1: Adding a new feature

```bash
# If code is clean, use test-first
/test-first when message fails validation it should log detailed error

# If code needs cleanup first, use tidy-first
/tidy-first add validation logging with error details
```

#### Scenario 2: Implementing from a specification

```bash
# Part of spec workflow
/spec:requirements 123
/spec:design message-validation-strategy  # Uses /adr internally
/spec:tasks
/spec:implement  # Uses /test-first approach
```

#### Scenario 3: Making architectural decision

```bash
# Standalone or part of spec
/adr outcome-classification-for-retry
```

#### Scenario 4: Optimizing existing code

```bash
# Refactor structure, then add optimizations
/tidy-first optimize message batch processing for better throughput
```

---

## Integration with Fences Practices

These skills enforce practices documented in `.agent_instructions/`:

| Skill | Enforces | Reference |
| --- | --- | --- |
| `/test-first` | TDD approval workflow; a bug fix must come with a failing test | [testing.md](../../.agent_instructions/testing.md) |
| `/adr` | ADR structure, frontmatter and readability | [documentation.md](../../.agent_instructions/documentation.md), [adr_frontmatter.md](../../.agent_instructions/adr_frontmatter.md) |
| `/tidy-first` | Structural/behavioural separation | [code_style.md](../../.agent_instructions/code_style.md) |
| `/spec:*`, `/bugfix:*` | Public API baseline, analyser cleanliness, mutation score | [public_api.md](../../.agent_instructions/public_api.md), [testing.md](../../.agent_instructions/testing.md) |

They make **mandatory workflows enforceable** rather than just documented.

The gates that are specific to this repository, and that every one of these commands checks:

- **Public API** — any change to a `public` or `protected` member in `src/` needs an entry in that
  project's `.PublicAPI/PublicAPI.Unshipped.txt`, and the default posture is not to change the
  surface at all. See [public_api.md](../../.agent_instructions/public_api.md).
- **Analyser cleanliness** — StyleCop, Sonar and BannedApiAnalyzers run in every build and CI
  treats warnings as errors.
- **Mutation score** — Stryker's threshold on `src/` is 100%. Touching a project under `src/` means
  its score must not regress.
- **Generated Markdown** — if `src/Snippets` changed, run `dotnet mdsnippets` and commit the
  regenerated blocks. Never hand-edit inside a snippet region.
- **No new dependencies** unless the issue asked for one.

---

## Benefits

### For Individual Developers

- **Faster workflows**: One command does multi-step processes correctly
- **Less to remember**: Skills encode the practices, you just invoke them
- **Built-in guardrails**: Mandatory approval gates prevent mistakes
- **Better habits**: Using skills reinforces best practices

### For Code Reviews

- **Clearer commits**: Structural and behavioral changes are separated
- **Better context**: ADRs explain WHY decisions were made
- **Correct tests**: Approval gate ensures tests are reviewed before implementation
- **Easier review**: Small, focused commits are easier to review

### For Project Quality

- **Consistent ADRs**: All architectural decisions are documented the same way
- **Better git history**: Commits have single purpose (structure OR behavior)
- **Test coverage**: TDD approach ensures tests are written first
- **Design quality**: Approval and refactoring phases promote better design

---

## Skill Implementation Details

Skills are implemented as Markdown files with frontmatter defining:

```yaml
---
allowed-tools: Read, Write, Edit, Bash, AskUserQuestion, ...
description: Brief description shown in skill list
argument-hint: <what user should provide>
---
```

The body contains instructions for Claude Code to follow when the skill is invoked.

**Location**: `.claude/commands/[category]/[skill-name].md`

---

## Examples of Skills in Action

### Example 1: Test-First Development

```text
Developer: /test-first the retry strategy widens its delay when the backoff curve is configured

Claude:
🔴 RED: Adding test ExecuteAsync_BackoffCurveConfigured_DelaysWiden
to test/Paramore.Fences.Core.Tests/Retry/RetryResilienceStrategyTests.cs

[Shows test code: Member_Scenario_Outcome naming, Arrange/Act/Assert,
 FakeTimeProvider advanced rather than a real delay]

Test fails: ✓ "RetryStrategyOptions does not contain a definition for BackoffCurve"

✅ APPROVAL REQUIRED: Should I proceed to implement the code to make this test pass?

Developer: Yes

Claude:
🟢 GREEN: Adding the BackoffCurve property to RetryStrategyOptions
[Shows implementation with XML docs]
✓ Test passes
✓ All 777 tests in Paramore.Fences.Core.Tests pass
✓ Public API changed — appended to
  src/Paramore.Fences.Core/.PublicAPI/PublicAPI.Unshipped.txt

🔵 REFACTOR: Extracting the curve evaluation into a private method
[Shows refactoring]
✓ All tests still pass

Suggested commit: feat: add a configurable backoff curve to the retry strategy
```

### Example 2: Architecture Decision Record

```text
Developer: /adr retry outcome classification

Claude:
Next ADR number: 0037
Found current spec: specs/0001-adaptive-retry-backoff/

What is the architectural problem or decision to be made?
Developer: The retry strategy treats every exception the same. Callers want transient
faults retried and permanent ones surfaced immediately...

What is your proposed solution?
Developer: Classify the outcome through PredicateBuilder, so the decision stays on the
options class and composes with the existing ShouldHandle...

What alternatives did you consider?
Developer: A separate strategy - rejected because it would need the retry strategy's
attempt count, which is not part of its contract...

✓ Created: docs/adr/0037-retry-outcome-classification.md
✓ Added to: specs/0001-adaptive-retry-backoff/.adr-list

Status: Proposed

Next: Review and commit: git commit -m "docs: add ADR for retry outcome classification"
```

### Example 3: Tidy First Refactoring

```text
Developer: /tidy-first simplify the delay calculation in RetryResilienceStrategy

Claude:
Analyzing src/Paramore.Fences.Core/Retry/RetryResilienceStrategy.cs...

STRUCTURAL changes (refactoring):
- Extract CalculateDelay() from ExecuteCore
- Rename 'd' to 'delay'
- Reduce nesting in the attempt loop
- Move the jitter ceiling to a PascalCase constant

BEHAVIORAL changes (new functionality):
- Add a configurable backoff curve
- Skip the delay entirely on the final attempt

Proceed with structural changes first?
Developer: Yes

Claude:
🔧 STRUCTURAL: Making refactoring changes...
[Shows refactoring]

Running tests... ✓ 777 tests pass (no behaviour changed)
Public API unchanged ✓

✓ Committed: refactor: extract the delay calculation in RetryResilienceStrategy

Proceed with behavioral changes?
Developer: Yes

Claude:
✨ BEHAVIORAL: Adding the backoff curve...
[Shows new features]

Running tests... ✓ 777 tests pass
Public API changed — appended to
  src/Paramore.Fences.Core/.PublicAPI/PublicAPI.Unshipped.txt ✓

✓ Committed: feat: add a configurable backoff curve to the retry strategy

Complete! Two commits created for easier review.
```

---

## Tips for Using Skills

### Best Practices

1. **Use skills proactively**: Don't wait until you're stuck - use them from the start
2. **Trust the process**: The approval gates and validations are there for good reasons
3. **Combine skills**: Use `/adr` to document, `/test-first` to implement
4. **Review skill output**: Always review what the skill produces before accepting
5. **Iterate**: Skills support iteration - if categorization is wrong, adjust and continue

### Common Patterns

#### Pattern 1: Feature Development

```bash
/adr <design decision>        # Document the approach
/test-first <behavior>        # Implement with TDD
# Repeat test-first for each behavior
```

#### Pattern 2: Refactoring + Feature

```bash
/tidy-first <optimization>    # Clean up + add feature
# Results in two commits: refactor + feat
```

#### Pattern 3: Specification-Driven

```bash
/spec:requirements <issue>    # Capture requirements
/spec:design <focus>          # Uses /adr internally
/spec:tasks                   # Break down work
/spec:implement               # Uses /test-first approach
```

---

## Getting Help

- **Skill documentation**: Each skill has a README.md in its directory
- **Fences guidelines**: See `.agent_instructions/` for full practices
- **Issues**: Report skill issues at <https://github.com/anthropics/claude-code/issues>
- **Contributing guidelines**: See [CONTRIBUTING.md](../../CONTRIBUTING.md)

---

## Summary

These skills enforce Fences's mandatory engineering practices:

| Skill | Enforces | Creates |
| --- | --- | --- |
| `/test-first` | TDD with approval | Tests → Implementation → Refactoring |
| `/adr` | Documented decisions | Numbered ADR files |
| `/tidy-first` | Structural/behavioral separation | Two commits: refactor + feat |
| `/bugfix:*` | Confirm root cause before fixing | Bug record + regression test + scoped `fix:` commit |

**Key insight**: These skills make the **correct approach the easy path** by automating multi-step workflows and enforcing approval gates.

**Try them**: Start with `/test-first` for your next feature or `/tidy-first` for your next optimization.
