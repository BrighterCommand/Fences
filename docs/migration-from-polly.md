# Migrate from Polly to Fences

Fences is a community fork of [Polly](https://github.com/App-vNext/Polly), maintained by
[Brighter Command](https://github.com/BrighterCommand). It is not affiliated with, endorsed by,
or supported by App vNext or the Polly maintainers.

Fences 9.0.0 is forked from Polly 8.7.0. **The API is unchanged**, apart from the one type rename
described below. Moving from Polly 8.7.0 to Fences is a change of package reference and namespace,
not a rewrite: for most projects it is two lines of `.csproj` and a find-and-replace over `using`
directives.

The one thing you cannot fix with a find-and-replace is the telemetry names — see
[Telemetry](#telemetry) below.

## Package references

Replace each Polly package reference with its Fences equivalent. Versions restart at `9.0.0`; see
[Versioning](#versioning).

| Polly | Fences |
| :---- | :----- |
| `Polly.Core` | `Paramore.Fences.Core` |
| `Polly.Extensions` | `Paramore.Fences.Extensions` |
| `Polly.RateLimiting` | `Paramore.Fences.RateLimiting` |
| `Polly.Testing` | `Paramore.Fences.Testing` |
| `Polly` (the pre-v8 API) | `Paramore.Fences` |

```diff
-<PackageReference Include="Polly.Core" Version="8.7.0" />
-<PackageReference Include="Polly.Extensions" Version="8.7.0" />
+<PackageReference Include="Paramore.Fences.Core" Version="9.0.0" />
+<PackageReference Include="Paramore.Fences.Extensions" Version="9.0.0" />
```

The package identifiers carry the `Paramore` prefix because that is the identifier family used by
the rest of the Brighter Command projects. The library, the repository and the documentation site
are all called **Fences**; only the package and assembly names are prefixed.

## Namespaces

Every namespace maps one-for-one: `Polly` becomes `Paramore.Fences`, and every child namespace
follows.

| Polly | Fences |
| :---- | :----- |
| `Polly` | `Paramore.Fences` |
| `Polly.CircuitBreaker` | `Paramore.Fences.CircuitBreaker` |
| `Polly.DependencyInjection` | `Paramore.Fences.DependencyInjection` |
| `Polly.Fallback` | `Paramore.Fences.Fallback` |
| `Polly.Hedging` | `Paramore.Fences.Hedging` |
| `Polly.RateLimiting` | `Paramore.Fences.RateLimiting` |
| `Polly.Registry` | `Paramore.Fences.Registry` |
| `Polly.Retry` | `Paramore.Fences.Retry` |
| `Polly.Simmy` | `Paramore.Fences.Simmy` |
| `Polly.Telemetry` | `Paramore.Fences.Telemetry` |
| `Polly.Testing` | `Paramore.Fences.Testing` |
| `Polly.Timeout` | `Paramore.Fences.Timeout` |

A case-sensitive replacement of `Polly` with `Paramore.Fences` across your `using` directives is
enough. If you use implicit or global usings, the change is a single line:

```diff
-global using Polly;
+global using Paramore.Fences;
```

## The one renamed type

`Polly.PollyServiceCollectionExtensions` is called
`Paramore.Fences.ResilienceServiceCollectionExtensions` in Fences. It is the only public type whose
name changed, and it holds the `AddResiliencePipeline`, `AddResiliencePipelines` and
`AddResiliencePipelineRegistry` extension methods.

Because those are extension methods, callers write `services.AddResiliencePipeline(...)` and never
name the class, so in practice this affects nobody. If you did reference the type by name — to call
a method non-extensively, or in a `using static` — update it:

```diff
-PollyServiceCollectionExtensions.AddResiliencePipeline(services, "my-pipeline", builder => { });
+ResilienceServiceCollectionExtensions.AddResiliencePipeline(services, "my-pipeline", builder => { });
```

The name changed because shipping a type called `Polly*` from a package called `Paramore.Fences`
would be a use of the Polly name as branding rather than as a statement of fact. It also brings the
type into line with the rest of the surface — `ResiliencePipeline`, `ResilienceContext`,
`ResiliencePipelineBuilder` — none of which carry a brand token.

## Telemetry

**This is the one change a find-and-replace on your own source will not fix.** The meter,
`ActivitySource` and metric names are emitted by Fences, so they change in your dashboards,
alerts and collector configuration, not in your code.

| Kind | Polly | Fences |
| :--- | :---- | :----- |
| Meter and `ActivitySource` name | `Polly` | `Paramore.Fences` |
| Metric | `resilience.polly.pipeline.duration` | `resilience.fences.pipeline.duration` |
| Metric | `resilience.polly.strategy.events` | `resilience.fences.strategy.events` |
| Metric | `resilience.polly.strategy.attempt.duration` | `resilience.fences.strategy.attempt.duration` |

The instrument names, units, descriptions and tags are otherwise identical, so a dashboard query
usually only needs its metric name substituted.

Anywhere you enable the meter by name, use the new one:

```diff
 builder.Services.AddOpenTelemetry()
     .WithMetrics(metrics => metrics
-        .AddMeter("Polly"));
+        .AddMeter("Paramore.Fences"));
```

The instrumentation scope takes the full `Paramore.Fences` because the convention is that it
matches the assembly name. The metric names keep the short `fences` token because they are
lowercase dotted identifiers and should not carry an organisation prefix.

## Assembly identity and strong naming

Fences assemblies are strong-named with a key belonging to Brighter Command, not the Polly key.
The public key token is `6998a40d28482b6d`. This matters if you have `InternalsVisibleTo`
attributes naming Fences assemblies, binding redirects, or anything else that pins an assembly
identity — those need the new name and the new token.

Fences and Polly can be installed side by side. The assembly names do not collide, the namespaces
do not collide, and the meter names do not collide, so a gradual migration is possible.

## What has not changed

- Every strategy: retry, circuit breaker, timeout, rate limiter, hedging, fallback, and the Simmy
  chaos strategies.
- Every option type, builder, delegate signature and default value.
- The target frameworks: `net8.0`, `net6.0`, `netstandard2.0`, `net472` and `net462`.
- The licence: BSD 3-Clause, the same as Polly's, with App vNext's copyright notice retained.

## `Microsoft.Extensions.Http.Resilience`

Fences cannot remove Polly from an application that uses
`Microsoft.Extensions.Http.Resilience` — for example `AddStandardResilienceHandler` or
`AddResilienceHandler`. That package has a hard dependency on `Microsoft.Extensions.Resilience`,
which depends on `Polly.Extensions` and `Polly.RateLimiting`, and its `AddResilienceHandler`
callback is handed a Polly `ResiliencePipelineBuilder<T>`. Those types come from Polly, and no
change on our side alters that.

If you use that package, expect Polly to remain in your dependency graph after migrating. What
Fences removes is the resilience dependency that Brighter itself imposed on its users, which is
the part we control.

## Versioning

Fences starts at `9.0.0`. The major version is bumped past Polly's `8.x` so that a Fences version
number is never mistaken for a Polly one, and so there is no ambiguity about which project a given
`8.7.0` refers to.

`CHANGELOG.md` in the repository retains Polly's history in full. Entries for `8.7.0` and below
describe Polly releases, not Fences ones.

## Why the fork exists

See [ADR 0002](adr/0002-fork-polly-as-fences.md) for the reasoning, and
[`NOTICE.md`](https://github.com/BrighterCommand/Fences/blob/main/NOTICE.md) for provenance and
attribution.
