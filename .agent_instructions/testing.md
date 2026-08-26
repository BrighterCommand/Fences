# Testing

## The rule that matters most

**A bug fix must include a test that fails without the fix.** Write the test first, watch it fail
for the right reason, then fix the code. If you cannot write such a test, say so and explain why —
do not quietly skip it.

Use `/test-first <behavior>` ([docs](../.claude/commands/tdd/README.md)) for this. It has a
mandatory approval gate: you write the test, you stop, the user reviews it, and only then do you
implement. Do not write a test by hand and carry straight on into the implementation.

## The stack

| Concern | Library | Notes |
| --- | --- | --- |
| Test framework | xUnit | `Xunit` and `Shouldly` are implicit usings in every test project. |
| Assertions | [Shouldly](https://github.com/shouldly/shouldly) | `result.ShouldBe(expected)`. |
| Test doubles | [NSubstitute](https://nsubstitute.github.io/) | **Not** FakeItEasy — that is Brighter. |
| Property-based | [FsCheck.Xunit](https://fscheck.github.io/FsCheck/) | Referenced by `Paramore.Fences.Core.Tests` only. |
| Time | `FakeTimeProvider` | From `Microsoft.Extensions.TimeProvider.Testing`. |
| Coverage | coverlet | Collected on every non-.NET-Framework run, reported by ReportGenerator. |
| Mutation | [Stryker](https://stryker-mutator.io/docs/stryker-net/introduction/) | 100% threshold. See below. |

Do not introduce FluentAssertions, Moq or FakeItEasy.

The package references and the implicit usings come from `eng/Test.targets`, selected by
`<ProjectType>Test</ProjectType>` in the project file. You do not add xUnit or Shouldly to a test
project by hand.

## Naming

Test methods are named `Member_Scenario_Outcome`, matching the existing files:

```text
Ctor_EnsureDefaults
ExecuteAsync_CanceledBeforeExecution_EnsureNotExecuted
Execute_GenericStrategy_NullArgument_Throws
HandleOutcomeAsync_Cancelled_Ok
Retry_RetryCount_Respected
```

Test classes are named after the type under test plus `Tests` — `RetryResilienceStrategyTests`.

This is **not** Brighter's BDD-style `When_X_Then_Y`. Do not carry that convention here.

## Time

**Never put a real `Task.Delay` in a test to wait for a strategy.** The strategies take a
`TimeProvider` precisely so that a test can control the clock. Use `FakeTimeProvider` and advance it:

```csharp
var timeProvider = new FakeTimeProvider();
// ... build the pipeline with timeProvider ...
timeProvider.Advance(TimeSpan.FromSeconds(1));
```

This is the same reason `DateTime.Now` and friends are banned in `src/` — see
[code_style.md](code_style.md).

Some timing-sensitive tests are still flaky on a loaded machine. Before treating a failure as a
regression, re-run that test on its own with `--filter`.

## Reaching internals

- Only test what a package exports. Do not reach for private members to get coverage up.
- Where a test project genuinely needs internals, they are granted by
  `<InternalsVisibleToProject Include="Paramore.Fences.Core.Tests" />` in the **source** project's
  file. `eng/Library.targets` turns that into an `InternalsVisibleTo` attribute carrying the strong
  name public key. Add the entry there, not an attribute in a source file.
- This is the opposite of Brighter's rule, which bans `InternalsVisibleTo` outright. Here it is the
  established mechanism.

## Shared helpers

Prefer a helper we already have over a new one. `test/Paramore.Fences.TestUtils` holds them:

- `TestResilienceStrategy` and `TestResilienceStrategyOptions` — a strategy you can drive from a
  test.
- `FakeTelemetryListener`, `MeteringEvent` — assert on what was emitted.
- `FakeLogger`, `FakeLoggerFactory`, `LogRecord` — assert on what was logged.
- `TestUtilities`, `OutcomeExtensions`, `ResilienceStrategyExtensions`, `TestArguments`.

`test/Shared` holds source compiled into several test projects, including `StrongNameTests.cs`,
which asserts the assemblies are signed with the expected key and token. If you ever rotate
`Fences.snk`, that file and `eng/Common.props` both have to change.

## AOT and trimming

`test/Paramore.Fences.AotTest` publishes a trimmed, native-AOT application. The Cake build runs it as
`__ValidateAot`, before the tests. If you add reflection, dynamic code generation, or an unbounded
generic instantiation, this is what will tell you.

## Mutation testing

`src/` is covered by Stryker with **high and low thresholds both set to 100**, configured in
`eng/stryker-config.json`. A suite that passes but does not kill the mutants is not finished.

```bash
./build.ps1 -Target MutationTestsCore
./build.ps1 -Target MutationTestsExtensions
./build.ps1 -Target MutationTestsLegacy
./build.ps1 -Target MutationTestsRateLimiting
./build.ps1 -Target MutationTestsTesting

# or all five
./build.ps1 -Target MutationTests
```

Stryker runs against `net10.0` in `Debug`. `block` and `statement` mutations are ignored, as are
mutations inside `Dispose`, `ConfigureAwait`, `Debug.Assert`, the logging methods and exception
constructors.

**When you touch a project under `src/`, the mutation score for that project must not regress.**
The report lands in `artifacts/mutation-report/`.

## Running tests

See [build_and_development.md](build_and_development.md). The short version:

```bash
dotnet test ./test/Paramore.Fences.Core.Tests --framework net10.0 --filter "FullyQualifiedName~CircuitBreaker"
```

Naming one framework matters: test projects multi-target `net10.0;net9.0;net8.0`, so leaving it out
runs everything three times.
