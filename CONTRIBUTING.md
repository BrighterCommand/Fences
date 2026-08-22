# Contributing

## How to Use This File

Use this file to follow our coding guidelines when submitting to Fences.

Fences is a community fork of [Polly](https://github.com/App-vNext/Polly), maintained by
[Brighter Command](https://github.com/BrighterCommand). It is not affiliated with, endorsed by, or
supported by App vNext or the Polly maintainers. Please do not raise Fences issues on the Polly
tracker, or Polly issues here.

> Fences shares an organisation with [Brighter](https://github.com/BrighterCommand/Brighter) and
> [Darker](https://github.com/BrighterCommand/Darker), but **not** their engineering conventions.
> Fences inherits Polly's, and we have kept them deliberately. If you contribute to both, read
> [Code Style](#code-style) before assuming a Brighter habit applies here.

## Table of Contents

- [How to Use This File](#how-to-use-this-file)
- [First Time Contributing?](#first-time-contributing)
- [Architecture Decision Records](#architecture-decision-records)
- [Code Style](#code-style)
- [Public API Changes](#public-api-changes)
- [Testing](#testing)
- [Documentation](#documentation)
- [Dependency Management](#dependency-management)
- [Making Changes](#making-changes)
  - [Build and Test](#build-and-test)
  - [Commit Messages](#commit-messages)
  - [Repository Branching Strategy](#repository-branching-strategy)
  - [Submitting Changes](#submitting-changes)
  - [Contributor License Agreement](#contributor-license-agreement)
- [Support for Agentic Coding](#support-for-agentic-coding)
- [Contributor Code of Conduct](#contributor-code-of-conduct)
- [Project Structure](#project-structure)

---

## First Time Contributing?

Welcome! Here's how to get started.

### Quick Setup

1. Fork and clone the repository
2. Ensure you have the .NET SDK pinned in `global.json` or later installed: `dotnet --version`
3. Build and test everything: `./build.ps1`
4. Or, for a faster inner loop: `dotnet build` then `dotnet test ./test/Paramore.Fences.Core.Tests`

There are no external services to stand up. Every test in this repository runs in process, so a
clone and an SDK are all you need.

### Configure Git Blame

One commit in the history renamed every file from Polly to Fences and would otherwise dominate
`git blame`. Tell your local Git to skip it:

```bash
git config blame.ignoreRevsFile .git-blame-ignore-revs
```

GitHub's blame view honours `.git-blame-ignore-revs` automatically; your local checkout does not
until you run the above.

### Your First Contribution

1. Look for issues labelled ["good first issue"](https://github.com/BrighterCommand/Fences/issues?q=is%3Aissue+is%3Aopen+label%3A%22good+first+issue%22)
2. Comment on the issue to let others know you're working on it
3. Read the relevant sections below:
   - [Code Style](#code-style) — our conventions, which are Polly's and not Brighter's
   - [Public API Changes](#public-api-changes) — the one thing contributors most often miss
   - [Testing](#testing) — how we test, and the mutation threshold
   - [Documentation](#documentation) — XML docs, and why you must not hand-edit generated Markdown
4. Make your changes following our guidelines
5. Submit a pull request targeting the `main` branch

### Key Guidelines at a Glance

- Constants use `PascalCase`, **not** `ALL_CAPS`
- Do **not** add a licence header region to source files — this repository has none
- Expression-bodied members are required, not preferred, and are enforced as errors
- Any public API change must be reflected in `src/<Project>/.PublicAPI/PublicAPI.Unshipped.txt`
- Do not change the public API unless the issue specifically asks for it
- A bug fix must include a test that fails without the fix
- Do not add or update dependencies unless the issue specifically asks for it
- Use `TimeProvider`; `DateTime.Now` and friends are banned by an analyser
- Use Conventional Commits for commit messages

---

## Architecture Decision Records

If you are adding a new capability, or changing how an existing one works at the design level,
write an Architecture Decision Record.

- The record should focus on the *why* of your decision, over implementation details, which are
  better found in the code.
- Follow the format defined in [ADR 0001](docs/adr/0001-record-architecture-decisions.md).
- Use the record to agree what you want to do, before you do it.
- Use the record to signal to others, including future maintainers, why Fences is built the way it is.

You can write the record as the first step on a new branch. Your first commit then includes the
record describing the change, which lets you open a draft pull request early and get feedback on
the design before you have written the code. If you change the design as you learn, add another
record that supersedes the old one.

Records live in `docs/adr/`.

## Code Style

Our conventions come from Polly and are enforced by analysers, `.editorconfig` and
`eng/analyzers/`. Where a rule is enforced, the build fails; where it is not, follow the
surrounding code.

### Naming

- Follow [Microsoft's C# naming conventions for identifiers](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names).
- Constants use `PascalCase`. This differs from Brighter, which uses `ALL_CAPS`. Do not carry the
  Brighter convention across.
- Private, internal and private-protected static fields are `PascalCase` with **no** prefix — no
  leading underscore, no `s_`.

### Layout

- File-scoped namespaces: `namespace Paramore.Fences.Retry;`
- Four-space indent, LF line endings, final newline. Project, XML, JSON and YAML files use two spaces.
- `using` directives go **outside** the namespace, with `System.*` first.
- Implicit usings are enabled and declared centrally in `Directory.Build.targets`. Do not add a
  `using` for something already implicit there.
- Multiple types per file are allowed — the StyleCop rule `SA1402` is switched off. Prefer one type per
  file anyway, unless one type is clearly a detail of another.
- **There is no licence header.** Brighter puts a `#region Licence` block at the top of every file;
  Fences does not, and adding one will look like noise in review.

### Language

- Expression-bodied members are **required**, at severity `error`, for methods, constructors,
  operators, properties, indexers and accessors. This is stricter than a preference — a block body
  where an expression body would do will fail the build.
- There is no `var` preference; `IDE0007` and `IDE0008` are both switched off. Match the surrounding code.
- `<Nullable>enable</Nullable>` is set on every shipping project **except** `src/Paramore.Fences`,
  the frozen pre-v8 legacy package, which deliberately opts out. Do not "fix" that.
- Use `readonly` for fields that do not change after construction.
- `DateTime.Now`, `DateTime.Today`, `DateTimeOffset.Now`, `DateTimeOffset.Today` and the implicit
  `DateTime` to `DateTimeOffset` conversion are banned by `eng/analyzers/BannedSymbols.txt`. Take a
  `TimeProvider` instead — it is also what makes the strategies testable without real delays.
- Do not use APIs marked `[Obsolete]`.

### Design

- Every resilience strategy is either **reactive** (it responds to a failure: retry, circuit
  breaker, fallback, hedging) or **proactive** (it prevents overload: timeout, rate limiter). A new
  strategy should be clearly one or the other.
- Every strategy has a matching `*Options` class holding its configuration, and the options class
  is what the public surface exposes.
- Which outcomes a strategy handles is declared through the `PredicateBuilder` fluent API, not by
  loose predicate parameters passed around by hand.
- `ResilienceContext` is pooled. Do not hold a reference to one beyond the execution it belongs to.
- `src/Paramore.Fences` is frozen for backwards compatibility and must not gain features. New work
  goes in `src/Paramore.Fences.Core`.
- Keep methods small and focused on a single responsibility, and prefer intention-revealing names
  over comments.

### Tidy First

Follow Beck's "Tidy First" approach and separate structural changes from behavioural ones.

- **Structural**: rearranging code without changing behaviour — renaming, extracting methods, moving code.
- **Behavioural**: adding or modifying actual functionality.

Never mix the two in one commit. Make the structural change first, and prove it changed nothing by
running the tests before and after.

## Public API Changes

This is the rule contributors and coding agents get wrong most often, so it has its own section.

Every shipping project has a `.PublicAPI/` directory containing `PublicAPI.Shipped.txt` and
`PublicAPI.Unshipped.txt`. The `Microsoft.CodeAnalysis.PublicApiAnalyzers` package compares the
compiled public surface against those files and fails the build on any difference.

- **The default posture is: do not change the public API.** If the issue you are fixing does not
  ask for an API change, and you find yourself adding a public member, stop and reconsider.
- If you do add, remove or change a public member, append the corresponding entry to
  **`PublicAPI.Unshipped.txt`** for that project — not `Shipped.txt`.
- Entries are one declaration per line and the file is sorted. The analyser's code fix in your IDE
  will write the entry for you.
- Removing a public member requires a `*REMOVED*` line rather than deleting the existing one.
- `Shipped.txt` is written by the release process, not by hand.

> One deliberate exception exists in the history: the rename from Polly to Fences moved the whole
> renamed surface through `Shipped.txt`, because the fork carried the shipped surface forward
> wholesale rather than re-shipping it. That was a one-off. It is not a precedent.

## Testing

- Use TDD where practical. Write the failing test first, make it pass, then improve the design.
- Tests are xUnit. Assertions use [Shouldly](https://github.com/shouldly/shouldly), test doubles use
  [NSubstitute](https://nsubstitute.github.io/), and property-based tests use
  [FsCheck](https://fscheck.github.io/FsCheck/). Do **not** introduce FluentAssertions, Moq or
  FakeItEasy — Brighter uses FakeItEasy, Fences does not.
- Name test methods `Member_Scenario_Outcome`, matching the existing files — for example
  `Ctor_EnsureDefaults`, `Execute_GenericStrategy_NullArgument_Throws`,
  `HandleOutcomeAsync_Cancelled_Ok`. Name test classes after the type under test plus `Tests`.
- **A bug fix must include a test that fails without the fix.** If you cannot write one, say so in
  the pull request and explain why.
- Only test what a package exports. Do not reach for private members to get coverage.
- Where a test project does need internals, they are granted through `InternalsVisibleToProject` in
  the project file, which `eng/Library.targets` turns into an `InternalsVisibleTo` attribute.
- Prefer helpers we already have over new ones. `test/Paramore.Fences.TestUtils` holds the shared
  ones — `TestResilienceStrategy`, `FakeTelemetryListener`, `FakeLogger` and friends.
- Never introduce a real `Task.Delay` into a test to wait for a strategy. Timing is made
  deterministic with `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`; the
  strategies take a `TimeProvider` precisely so that tests can control the clock.
- `test/Paramore.Fences.AotTest` proves the libraries survive trimming and native AOT publish. If
  you add reflection, dynamic code generation, or an unbounded generic instantiation, expect this
  project to tell you about it.

### Mutation Testing

`src/` is covered by [Stryker](https://stryker-mutator.io/docs/stryker-net/introduction/) with a
threshold of **100%**, configured in `eng/stryker-config.json`. A test suite that passes but does
not kill the mutants is not finished.

```bash
./build.ps1 -Target MutationTestsCore
./build.ps1 -Target MutationTestsExtensions
./build.ps1 -Target MutationTestsLegacy
./build.ps1 -Target MutationTestsRateLimiting
./build.ps1 -Target MutationTestsTesting

# or all five
./build.ps1 -Target MutationTests
```

Coverage is collected by coverlet during the normal test run and uploaded to Codecov by CI.

## Documentation

Documentation lives in `docs/` and is published with [docfx](https://dotnet.github.io/docfx/) to
[the Fences documentation site](https://brightercommand.github.io/Fences/). New pages must be added
to the relevant `toc.yml`.

### XML Documentation Comments

- Add XML documentation comments to everything a package exports — public and protected members of
  public types. StyleCop enforces this through `documentExposedElements`, and does not ask for it on
  internal or private members, where `documentInternalElements` is off.
- Use `<summary>` for what the type or member is for; `<param>`, `<returns>`, `<typeparam>` and
  `<exception>` for the contract; `<value>` for properties; `<remarks>` for implementation notes
  and design decisions that a maintainer would want but a caller would not.
- Remember these show up in IntelliSense. Be useful, not exhaustive.
- Update the comments when the API changes.

### Code Samples in Documentation

**Fenced code blocks in `README.md` and under `docs/` are generated, not written.** They are
extracted from compiled sources in `src/Snippets/` and `samples/` by
[MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets) and injected between
`<!-- snippet: name -->` and `<!-- endSnippet -->` markers on every build.

Editing inside a snippet region is wasted work — the next build overwrites it. To change a sample,
change the C# in `src/Snippets/` and rebuild. Because the snippets compile, a sample that no longer
builds is caught by CI rather than by a reader.

Prose outside the snippet markers is ordinary Markdown and is yours to edit.

### Markdown Linting and Spelling

CI runs `markdownlint-cli2` against `.markdownlint.json` and a spell check against
`.github/wordlist.txt` over every `*.md` file. If you introduce a technical term the dictionary does
not know, either wrap it in backticks — inline code is skipped — or add it to the word list, which
is sorted case-insensitively.

## Dependency Management

- **Do not add or update dependencies unless the issue specifically asks for it.** Fences is a
  low-level library that ends up deep in other people's dependency graphs, and every package we
  reference is a constraint we impose on them.
- Versions are managed centrally in `Directory.Packages.props` using `PackageVersion` elements.
  Project files reference packages by name only, with no version:

  ```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
  </ItemGroup>
  ```

- Keep `Microsoft.Extensions.*` and `System.*` versions aligned with each other.
- Do not mix preview and stable versions.
- `NuGetAuditMode` is set to `direct`, so audit warnings are about packages we reference ourselves.
- Remember the multi-targeting matrix. `Paramore.Fences.Core` targets
  `net8.0;net6.0;netstandard2.0;net472;net462`, so a dependency that has no `netstandard2.0` asset
  is not a candidate, and a conditional `PackageReference` is often needed to polyfill older targets.

## Making Changes

- Make sure you have the latest version of the default branch.
- Raise an issue on the GitHub issue tracker before starting work — for features as well as defects.
  It lets us give specific advice, and check the change does not collide with work already in
  flight.
- Check first whether someone else has raised the same thing. If someone is already on it, say
  hello and collaborate rather than duplicating.
- Add a comment to an issue when you pick it up, so everyone else knows.
- For a defect, please include:
  - Steps to reproduce the issue
  - A failing test, if you can
  - The stack trace for any exception you saw

### Build and Test

The full build — clean, restore, build, AOT validation, test, package, documentation validation —
is orchestrated by [Cake](https://cakebuild.net/) through `cake.cs`, bootstrapped by `build.ps1`:

```bash
./build.ps1
```

That is what CI runs, and it is the thing to run before you open a pull request. It takes a few
minutes.

For a faster inner loop:

```bash
# Build only
dotnet build

# All tests
dotnet test

# One test project
dotnet test ./test/Paramore.Fences.Core.Tests

# One test, by filter
dotnet test ./test/Paramore.Fences.Core.Tests --filter "FullyQualifiedName~CircuitBreakerTests"

# One target framework — test projects multi-target, so this is much quicker
dotnet test ./test/Paramore.Fences.Core.Tests --framework net10.0
```

Notes:

- `build.ps1` needs PowerShell. If you do not have it, `dotnet tool restore` from `tools/` followed
  by `dotnet cake.cs -- --target=Default --configuration=Release` does the same thing.
- Ensure the build produces no warnings. CI treats them as errors when packing.
- `SKIP_FENCES_ANALYZERS=true` disables the analyser pass. It exists so the documentation build can
  skip work that is irrelevant to it. Do not use it to get a change past a rule.
- Some timing-sensitive tests can be flaky on a loaded machine. Before treating a failure as a
  regression, re-run that test on its own.

### Commit Messages

- Write a [good commit message](https://cbea.ms/git-commit/): a short imperative subject, a blank
  line, then why the change was needed.
- Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#specification).
- Reference issues or pull requests where relevant.
- Keep structural and behavioural changes in separate commits — see [Tidy First](#tidy-first).

### Repository Branching Strategy

| Branch | Description |
| --- | --- |
| `main` | The tip of active development. Anything in `main` should ship at the next release. Code here must compile, pass tests, and produce no warnings. |
| `release/*` | The code for an actively supported release. Created when `main` needs breaking changes that are not compatible with the current release. |
| Other | Any work that is not ready for `main` — experimental, or work in progress that would break CI. |

**When submitting a pull request:** target `main`, unless you are fixing a bug in a specific
release branch.

Our recommended fork-and-rebase workflow is written up in
[Git Workflow](docs/community/git-workflow.md).

### Submitting Changes

- Fork the project and clone your fork
- Branch your fork — never work on, or send a pull request from, your fork's `main`
- Work on your changes, rebasing onto `upstream/main` as it moves
- Run `./build.ps1` and confirm it is clean
- Push to your fork and open a pull request against `main`
- Sit back, and wait

If your history ends up messy we can squash it before merging, so do not let that stop you sending
the change.

### Contributor License Agreement

To safeguard the project we ask you to sign a Contributor License Agreement.

**You keep your copyright.** Section 2.1(a) of the agreement says so explicitly: you retain
ownership of the copyright in your contribution and keep the same rights to use or license it that
you had before signing. What you grant us is a perpetual, worldwide, non-exclusive, royalty-free,
irrevocable **licence** — not an assignment. The point is that the project is not left exposed to a
single past contributor withdrawing permission for code that is now load-bearing.

The agreement is [`CLA.txt`](CLA.txt) in this repository. Signing happens on your first pull
request: a bot will comment with the terms and you agree by replying to it. You only do this once.

## Support for Agentic Coding

We are evolving our support for agentic coding, focused on Claude Code, though other agents benefit
from the same material.

- `AGENTS.md` is the entry point, and points at `.agent_instructions/` for the agent-facing version
  of these guidelines.
- `CLAUDE.md` gives Claude Code the same pointer, along with the workflow and change-scope rules.
- `.github/copilot-instructions.md` mirrors both for GitHub Copilot.
- `.claude/commands/` holds commands that walk an agent through our preferred workflow.

Because the instructions live in `.agent_instructions/` rather than in a vendor-specific file, they
are straightforward for other agents to consume.

We accept code written with the help of an agent. You remain responsible for what you submit: read
it, understand it, and check it follows these guidelines. In particular, agents are unreliable about
[public API changes](#public-api-changes) and about the no-new-dependencies rule.

## Contributor Code of Conduct

This project is released with a [Contributor Code of Conduct](CODE_OF_CONDUCT.md). By participating
in this project you agree to abide by its terms.

The code of conduct is from [Contributor Covenant](https://www.contributor-covenant.org/).

Fences was forked from a .NET Foundation project, but Fences is **not** a .NET Foundation project
and contributing here does not bind you to the .NET Foundation's code of conduct or its contributor
agreement.

## Project Structure

Shipping code lives under `src/`:

- **`Paramore.Fences.Core`** — the version 8 engine and the package almost everything else builds
  on. Holds `ResiliencePipeline`, `ResiliencePipelineBuilder`, `ResilienceContext`, the
  `PredicateBuilder`, and the built-in retry, circuit breaker, fallback, hedging and timeout
  strategies. Multi-targets `net8.0;net6.0;netstandard2.0;net472;net462`.
- **`Paramore.Fences`** — the pre-v8 API, kept for backwards compatibility. Frozen: it must not
  gain features, and it is the one project that deliberately does not enable nullable reference types.
- **`Paramore.Fences.Extensions`** — `IServiceCollection` registration and telemetry, including the
  OpenTelemetry integration.
- **`Paramore.Fences.RateLimiting`** — the rate limiter strategy, wrapping `System.Threading.RateLimiting`.
  Separate so that users who do not need it do not take the dependency.
- **`Paramore.Fences.Testing`** — `ResiliencePipelineDescriptor` and helpers for asserting against a
  pipeline in your own tests. Reaches into `Paramore.Fences.Core` internals by design.
- **`LegacySupport`** — polyfills injected into `Paramore.Fences` as shared source at compile time
  rather than shipped as a package.
- **`Shared`** — source shared across projects the same way.
- **`Snippets`** — the compiled source of every code sample in the documentation. Not shipped.

Tests live under `test/` — note the singular, unlike Brighter's `tests/`:

- **`Paramore.Fences.Core.Tests`** — the version 8 engine.
- **`Paramore.Fences.Specs`** — the legacy `Paramore.Fences` package.
- **`Paramore.Fences.Extensions.Tests`**, **`Paramore.Fences.RateLimiting.Tests`**,
  **`Paramore.Fences.Testing.Tests`** — one per supporting package.
- **`Paramore.Fences.TestUtils`** — shared test helpers and fakes.
- **`Paramore.Fences.AotTest`** — publishes a trimmed, native-AOT application to prove the libraries
  survive it.
- **`Shared`** — test source shared across projects, including the strong-name tests.

Supporting directories:

- **`bench/`** — BenchmarkDotNet projects for the engine and the legacy package.
- **`samples/`** — runnable sample applications, in C#, F# and Visual Basic. Some of these reference
  Polly directly and on purpose, to demonstrate interoperation; that is not an oversight.
- **`docs/`** — the docfx site, including `docs/adr/`.
- **`eng/`** — shared MSBuild properties and targets, analyser configuration, and release scripts.
