# Project Structure

Fences is a .NET resilience library. It is a fork of [Polly](https://github.com/App-vNext/Polly),
maintained by [Brighter Command](https://github.com/BrighterCommand), and it is not affiliated with
or endorsed by App vNext.

Everything below is one solution, `Fences.slnx`. The samples are a second solution,
`samples/Samples.slnx`, and the build builds both.

## Shipping code — `src/`

| Project | Package | Purpose |
| --- | --- | --- |
| `Paramore.Fences.Core` | yes | The version 8 engine. Almost everything else builds on it. |
| `Paramore.Fences` | yes | The pre-v8 API, kept for backwards compatibility. **Frozen.** |
| `Paramore.Fences.Extensions` | yes | `IServiceCollection` registration, telemetry, OpenTelemetry. |
| `Paramore.Fences.RateLimiting` | yes | The rate limiter strategy, wrapping `System.Threading.RateLimiting`. |
| `Paramore.Fences.Testing` | yes | `ResiliencePipelineDescriptor` and helpers for asserting on a pipeline. |
| `LegacySupport` | no | Polyfills, injected as shared source at compile time. |
| `Shared` | no | Source shared across projects the same way. |
| `Snippets` | no | The compiled source of every code sample in the documentation. |

`Paramore.Fences.Core`, `Paramore.Fences.Extensions` and `Paramore.Fences.RateLimiting` multi-target
`net8.0;net6.0;netstandard2.0;net472;net462`. `Paramore.Fences.Testing` targets `net8.0;netstandard2.0`.
`Paramore.Fences` targets `net6.0;netstandard2.0;net472;net462`.

`LegacySupport` is not a project reference. `eng/Common.props` compiles `src/LegacySupport/*.cs`
directly into any project that sets `<LegacySupport>true</LegacySupport>` and is targeting something
older than `netcoreapp3.1`.

## Core abstractions — `src/Paramore.Fences.Core`

- **`ResiliencePipeline`** / **`ResiliencePipeline<T>`** — the main user-facing type. Wraps one or
  more strategies and executes them in sequence.
- **`ResiliencePipelineBuilder`** / **`ResiliencePipelineBuilder<T>`** — the fluent builder used to
  compose strategies.
- **`ResilienceStrategy`** / **`ResilienceStrategy<T>`** — the base class for built-in and custom
  strategies.
- **`ResilienceContext`** — the per-execution context that flows through the pipeline: cancellation
  token, properties, result type. It is **pooled**.
- **`PredicateBuilder`** — the fluent API through which a strategy declares which outcomes it
  handles.

### The strategy taxonomy

Every strategy is either reactive or proactive. This is the load-bearing distinction in the design;
see [design_principles.md](design_principles.md).

**Reactive** — respond to a failure that has already happened:

- `RetryResilienceStrategy` — configurable backoff, jitter, attempt limits.
- `CircuitBreakerResilienceStrategy` — a state machine, Closed to Open to HalfOpen.
- `FallbackResilienceStrategy` — returns an alternative value or action on failure.
- `HedgingResilienceStrategy` — fires parallel attempts and returns the fastest success.

**Proactive** — prevent overload before it happens:

- `TimeoutResilienceStrategy` — bounds execution duration.
- `RateLimiterResilienceStrategy` — in `Paramore.Fences.RateLimiting`.

Each has a matching `*Options` class — `RetryStrategyOptions`, `CircuitBreakerStrategyOptions` and
so on — which is what the public surface exposes.

## Tests — `test/`

Note the singular `test/`. Brighter uses `tests/`; this repository does not.

| Project | Covers |
| --- | --- |
| `Paramore.Fences.Core.Tests` | The version 8 engine. |
| `Paramore.Fences.Specs` | The legacy `Paramore.Fences` package. |
| `Paramore.Fences.Extensions.Tests` | `Paramore.Fences.Extensions`. |
| `Paramore.Fences.RateLimiting.Tests` | `Paramore.Fences.RateLimiting`. |
| `Paramore.Fences.Testing.Tests` | `Paramore.Fences.Testing`. |
| `Paramore.Fences.TestUtils` | Shared helpers and fakes. Not a test project itself. |
| `Paramore.Fences.AotTest` | Publishes a trimmed, native-AOT app to prove the libraries survive it. |
| `Shared` | Test source shared across projects, including the strong-name tests. |

Test projects target `net10.0;net9.0;net8.0`, plus `net481` on Windows. The Cake build discovers
them with the glob `./test/**/*{Tests,Specs}.csproj` — which is why the legacy suite is named
`Specs` and is still picked up.

## Supporting directories

- **`bench/`** — BenchmarkDotNet projects for the engine and the legacy package.
- **`samples/`** — runnable samples in C#, F# and Visual Basic. `samples/Chaos` and `src/Snippets`
  take a deliberate direct dependency on Polly to demonstrate interoperation. **That is not an
  oversight and must not be "fixed" to Fences.**
- **`docs/`** — the docfx site, including `docs/adr/`.
- **`eng/`** — shared MSBuild properties and targets, analyser configuration, release scripts.
- **`.agent_instructions/`** — this directory.
- **`.claude/commands/`** — the agent workflows.

## Build system

- **Cake** through `cake.cs`, bootstrapped by `build.ps1`. See
  [build_and_development.md](build_and_development.md).
- **Central package management** — every version lives in `Directory.Packages.props`.
- **`Directory.Build.props` / `Directory.Build.targets`** import `eng/Common.props` and
  `eng/Common.targets`, and then `eng/$(ProjectType).targets`. A project declares
  `<ProjectType>Library</ProjectType>`, `Test` or `Benchmark`, and that one property selects its
  package references, analyser rule set and defaults.
- **Strong naming** with `Fences.snk`; the public key is in `eng/Common.props` as
  `FencesStrongNamePublicKey`, and `test/Shared/StrongNameTests.cs` asserts on it.
- **MinVer** derives the version from git tags. See
  [release_and_versioning.md](release_and_versioning.md).
