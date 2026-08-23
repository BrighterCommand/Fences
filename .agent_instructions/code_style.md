# Code Style

**These conventions are Fences's own. They come from Polly, and they are not Brighter's.** If you
also work on Brighter or Darker, read this before applying a habit from there — several rules are
the exact opposite.

[CONTRIBUTING.md](../CONTRIBUTING.md) is the source of truth for contributors. This file says the
same thing for agents. Where the two ever disagree, `CONTRIBUTING.md` is right.

Everything here is enforced by `.editorconfig`, `eng/analyzers/` and the analysers that run during
the build, unless it says otherwise.

## The differences from Brighter, first

| Rule | Fences | Brighter |
| --- | --- | --- |
| Constants | `PascalCase` | `ALL_CAPS` |
| Licence header | **None.** Do not add one. | `#region Licence` on every file |
| Test directory | `test/` | `tests/` |
| Test doubles | NSubstitute | FakeItEasy |
| Expression-bodied members | Required, at `error` | Preferred |
| One type per file | Not enforced — `SA1402` is off | Default |
| Public API baseline | Every change updates `.PublicAPI/` | No equivalent |

## Naming

- Follow [Microsoft's C# naming conventions for identifiers](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names).
- Constants are `PascalCase`.
- Private, internal and private-protected **static** fields are `PascalCase` with **no** prefix — no
  leading underscore, no `s_`. This is a naming rule in `.editorconfig` and it warns.
- Suffix asynchronous methods with `Async`.

## Layout

- File-scoped namespaces: `namespace Paramore.Fences.Retry;`
- Four-space indent, LF line endings, a final newline, no trailing whitespace.
- Project, XML, JSON and YAML files use two-space indent.
- `using` directives go **outside** the namespace, with `System.*` first. This is StyleCop's
  `orderingRules` in `eng/analyzers/Stylecop.json`.
- Implicit usings are on, and the implicit set is declared centrally in `Directory.Build.targets`.
  Do not add a `using` for something already there. Note that `System.Net.Http` is explicitly
  **removed** from the implicit set, and that projects setting `IncludeFencesUsings` get the eleven
  legacy `Paramore.Fences.*` namespaces implicitly.
- `SA1402` is switched off, so more than one type per file is allowed. Prefer one type per file
  anyway, unless one type is clearly a detail of another.
- **There is no licence header.** Do not add a `#region Licence` block.

## Language rules

- **Expression-bodied members are required, at severity `error`**, for methods, constructors,
  operators, properties, indexers and accessors. This is stricter than a preference: writing a block
  body where an expression body would do fails the build. `csharp_place_expr_method_on_single_line`
  is `false`, so the expression may go on its own line.
- There is **no `var` preference**. `IDE0007` and `IDE0008` are both switched off. Match the
  surrounding code rather than converting either way.
- `<Nullable>enable</Nullable>` is set on every shipping project **except `src/Paramore.Fences`**,
  the frozen pre-v8 package, which deliberately opts out. Do not "fix" that.
- `LangVersion` is `latest`.
- Use `readonly` for fields that do not change after construction.
- Do not use APIs marked `[Obsolete]`.

## Banned APIs

`eng/analyzers/BannedSymbols.txt` fails the build on:

| Banned | Use instead |
| --- | --- |
| `DateTime.Now` | `TimeProvider.GetLocalNow().DateTime` |
| `DateTime.Today` | `TimeProvider.GetLocalNow().DateTime.Date` |
| `DateTimeOffset.Now` | `TimeProvider.GetLocalNow()` |
| `DateTimeOffset.Today` | `TimeProvider.GetLocalNow().Date` |
| Implicit `DateTime` to `DateTimeOffset` | An explicit conversion |

This is not only about correctness under a changing clock. `TimeProvider` is what makes the
strategies testable without real delays — see [testing.md](testing.md).

The banned-symbols list is applied to `src/` only; test projects are exempt.

## Comments

- A comment explains the *why* behind a non-obvious decision. Do not narrate what the code does, and
  do not describe the change relative to how the code used to be.
- Do not put issue or pull request numbers in code or XML documentation comments. The traceability
  belongs in git history and in the ADR.
- An inline comment is not a note to a reviewer. Context about alternatives considered, previous
  behaviour, or the history of a change is reviewer-facing rationale and belongs in the ADR.
- Keep comments short. One tight line beats a paragraph.

## Tidy First

Follow Beck's *Tidy First* approach and keep structural changes apart from behavioural ones.

- **Structural** — rearranging code without changing behaviour: renaming, extracting a method,
  moving code.
- **Behavioural** — adding or modifying functionality.

Never mix the two in one commit. Make the structural change first, and prove it changed nothing by
running the tests before and after. The `/tidy-first` command
([docs](../.claude/commands/refactor/README.md)) enforces this.

## Scope

- **Do not change the public API unless you were asked to.** See [public_api.md](public_api.md).
- Do not add or update dependencies unless you were asked to. See
  [dependency_management.md](dependency_management.md).
- Do not change defaults, or make changes beyond what was asked for. No unrequested "improvements"
  alongside the fix.
