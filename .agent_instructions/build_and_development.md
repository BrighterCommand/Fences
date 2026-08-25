# Build and Development Commands

These are the commands that exist in this repository. Everything here is derived from `cake.cs`,
`build.ps1`, `.config/dotnet-tools.json` and `eng/`. If a command is not listed here, check before
inventing it.

## The full build

The whole pipeline — clean, restore, validate documentation snippets, build, validate AOT, test,
pack — is orchestrated by [Cake](https://cakebuild.net/) through `cake.cs` and bootstrapped by
`build.ps1`:

```bash
./build.ps1
```

That is exactly what CI runs, and it is the gate a change has to pass. It takes a few minutes.

If PowerShell is not available, run Cake directly. This is equivalent:

```bash
(cd tools && dotnet tool restore)
dotnet cake.cs -- --target=Default --configuration=Release --verbosity=Normal
```

`--target` and `--configuration` are the only two arguments the script takes. `configuration`
defaults to `Release`, `target` to `Default`.

## Cake targets

| Target | What it does |
| --- | --- |
| `Default` | Aliases `Build`. |
| `Build` | Clean, restore, validate documentation snippets, build every `*.slnx`, publish the AOT test app, run every test project, pack the five shipping projects. |
| `MutationTestsCore` | Stryker over `src/Paramore.Fences.Core` driven by `test/Paramore.Fences.Core.Tests`. |
| `MutationTestsExtensions` | Stryker over `src/Paramore.Fences.Extensions`. |
| `MutationTestsRateLimiting` | Stryker over `src/Paramore.Fences.RateLimiting`. |
| `MutationTestsTesting` | Stryker over `src/Paramore.Fences.Testing`. |
| `MutationTestsLegacy` | Stryker over `src/Paramore.Fences` driven by `test/Paramore.Fences.Specs`. |
| `MutationTests` | All five of the above. |

```bash
./build.ps1 -Target MutationTestsCore
```

Note that the build finds solutions with `GetFiles("./**/*.slnx")`, so it builds `samples/Samples.slnx`
as well as `Fences.slnx`. A change that breaks a sample breaks the build.

## The inner loop

```bash
# Build only
dotnet build

# Build one project
dotnet build src/Paramore.Fences.Core/Paramore.Fences.Core.csproj

# All tests
dotnet test

# One test project
dotnet test ./test/Paramore.Fences.Core.Tests

# One test, by filter
dotnet test ./test/Paramore.Fences.Core.Tests --filter "FullyQualifiedName~CircuitBreakerTests"

# One target framework. Test projects multi-target net10.0, net9.0 and net8.0, so naming one
# framework is roughly three times faster and is the right default while iterating.
dotnet test ./test/Paramore.Fences.Core.Tests --framework net10.0
```

There is **no external infrastructure**. Every test in this repository runs in process. There is no
Docker Compose file, no database, no broker — if you find yourself looking for one, you are applying
a habit from another repository.

## Documentation snippets

The build runs `dotnet mdsnippets --validate-content` before it compiles anything, and fails if a
snippet reference cannot be resolved. If you changed anything under `src/Snippets/` or `samples/`,
run `dotnet mdsnippets` to regenerate the injected blocks. See
[documentation.md](documentation.md) — the fenced code blocks in Markdown are generated, and
hand-editing them is wasted work.

## Analysers

StyleCop, SonarAnalyzer and BannedApiAnalyzers run during every build, and the Cake build passes
`TreatAllWarningsAs=Error`. A warning locally is a broken build in CI.

`SKIP_FENCES_ANALYZERS=true` switches the analyser pass off. It exists so the documentation build
can skip work irrelevant to it, and that is its only legitimate use. Do not use it to get a change
past a rule.

## Tools

Tool versions are pinned in `.config/dotnet-tools.json`; `dotnet tool restore` installs them. The
ones that matter day to day are `dotnet-cake`, `dotnet-stryker`, `mdsnippets` and `docfx`.

## Lint and CI

Everything under `.github/workflows/` runs on push and pull request. The gates a local change can
break are:

- `build.yml` — the Cake build on Linux, macOS and Windows.
- `gh-pages.yml` — `markdownlint-cli2` against `.markdownlint.json`, then a spell check against
  `.github/wordlist.txt`, then the docfx build. Both linting steps cover every `*.md` file in the
  repository except the ones explicitly excluded.
- `lint.yml` — `actionlint`, `zizmor` and PSScriptAnalyzer over the workflows and PowerShell.
- `mutation-tests.yml` — Stryker.
- `code-ql.yml`, `dependency-review.yml`, `ossf-scorecard.yml` — security analysis.

C# linting is not a separate step; it is the analysers in the build.
