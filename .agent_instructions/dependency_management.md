# Dependency Management

## The rule

**Do not add or update a dependency unless you were specifically asked to.**

Fences is a low-level library. It ends up deep in other people's dependency graphs, and every
package we reference is a constraint we impose on every application that uses us. A dependency is a
design decision, not a convenience — if you find yourself wanting one to save a few lines, write the
few lines.

This applies to updates as well as additions. Dependabot proposes updates on a schedule
(`.github/dependabot.yml`); that is the route they take.

## Central package management

Versions live in `Directory.Packages.props` and nowhere else.
`ManagePackageVersionsCentrally` is set to `true` in `Directory.Build.props`.

Declare the version once, centrally:

```xml
<Project>
  <ItemGroup>
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
  </ItemGroup>
</Project>
```

and reference it from a project file **without a version**:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
</ItemGroup>
```

A `PackageReference` that carries its own `Version` is an error under central package management.

Several references you will not find in project files at all, because `eng/*.targets` add them by
`ProjectType`: analysers and MinVer from `eng/Analyzers.targets` and `eng/Common.props`, the whole
test stack from `eng/Test.targets`. Do not add xUnit, Shouldly, NSubstitute or coverlet to a test
project — they are already there.

## Conventions

- Keep `Microsoft.Extensions.*` and `System.*` versions aligned with one another.
- Do not mix preview and stable versions.
- `NuGetAuditMode` is `direct` (`eng/Common.props`), so audit warnings concern packages we reference
  ourselves rather than transitive ones.

## The multi-targeting constraint

`Paramore.Fences.Core`, `Paramore.Fences.Extensions` and `Paramore.Fences.RateLimiting` target
`net8.0;net6.0;netstandard2.0;net472;net462`. That is the real limit on what can be referenced:

- A package with no `netstandard2.0` asset is not a candidate at all.
- A package needed only to polyfill an older target belongs behind a conditional
  `PackageReference`, not an unconditional one.
- Before reaching for a package to fill a gap on an old target, check `src/LegacySupport` — the
  polyfills there are compiled in as shared source, which costs consumers nothing.

## The Polly dependency that cannot be removed

`Microsoft.Extensions.Http.Resilience` depends on Polly, hard, through
`Microsoft.Extensions.Resilience`. Its `AddResilienceHandler` hands the callback a **Polly**
`ResiliencePipelineBuilder<T>`.

Fences therefore cannot remove Polly from an application that calls
`AddStandardResilienceHandler`. Do not write documentation or a commit message implying a
Polly-free graph. This is recorded in `docs/adr/0002-fork-polly-as-fences.md`.

Relatedly: `src/Snippets` and `samples/Chaos` take a deliberate direct reference on `Polly.Core` and
use Polly's API on purpose, with comments saying so. **Do not "fix" those to Fences.**
