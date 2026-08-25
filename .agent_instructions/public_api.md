# Public API Changes

**Read this before adding, removing or changing anything `public` or `protected` in `src/`.** It is
the rule agents get wrong most often in this repository, and getting it wrong fails the build.

## The default posture

**Do not change the public API unless you were specifically asked to.**

Fences is a low-level library. Its public surface ends up in other people's code, and every addition
is something we then have to support. If the issue you are working on does not ask for an API
change, and you find yourself writing `public`, stop and reconsider: an `internal` type, an existing
overload, or a change inside an existing member is almost always the right answer.

## How the gate works

`Microsoft.CodeAnalysis.PublicApiAnalyzers` is referenced by every project that
`eng/Library.targets` applies to. It compares the compiled public surface against two text files and
raises an error on **any** difference — a member in the code but not in the file (`RS0016`), or a
member in the file but not in the code (`RS0017`).

The files live in a `.PublicAPI/` directory beside the project file:

```text
src/Paramore.Fences.Core/.PublicAPI/PublicAPI.Shipped.txt
src/Paramore.Fences.Core/.PublicAPI/PublicAPI.Unshipped.txt
```

All five shipping projects have exactly this — `Paramore.Fences.Core`, `Paramore.Fences`,
`Paramore.Fences.Extensions`, `Paramore.Fences.RateLimiting` and `Paramore.Fences.Testing`.

**The layout is flat.** There is one pair of files per project and nothing else. `eng/Library.targets`
*can* pick up per-framework subdirectories — `.PublicAPI/.NETStandard/`, `.PublicAPI/net8.0/` — but
none exist and you should not create one. If a member is conditional on a target framework, the
declaration in the flat file carries the condition, in the form the analyser's code fix writes.

## What to do when you do change the API

1. Append the new entry to **`PublicAPI.Unshipped.txt`** for that project. Never to `Shipped.txt`.
2. One declaration per line, in the analyser's own format — for example:

   ```text
   Paramore.Fences.Retry.RetryStrategyOptions<TResult>.MaxRetryAttempts.get -> int
   Paramore.Fences.Retry.RetryStrategyOptions<TResult>.MaxRetryAttempts.set -> void
   ```

3. Leave the `#nullable enable` header on the first line alone. Four of the five `Unshipped.txt`
   files consist of nothing else; that header is not an entry and the release script preserves it.
4. The file is sorted, case-insensitively first and then ordinally — the same comparer
   `eng/update-baselines.ps1` uses.
5. **Removing** a public member means adding a line prefixed `*REMOVED*` to `Unshipped.txt`, not
   deleting the line from `Shipped.txt`.
6. Do not hand-edit `Shipped.txt`. The release process promotes `Unshipped.txt` into it, by running
   `eng/update-baselines.ps1`. See [release_and_versioning.md](release_and_versioning.md).

The easiest way to get the text exactly right is the analyser's own code fix. In an IDE it is
offered on the `RS0016` error as *"Add to public API"* and it writes the entry for you. If you are
working without one, build the project and read the entry out of the error message — the analyser
prints the declaration in the exact form the file wants.

## The one exception, which is not a precedent

The rename from Polly to Fences carried the entire renamed surface through **`Shipped.txt`**, not
`Unshipped.txt`, because the fork brought the already-shipped surface forward wholesale rather than
re-shipping it. That is recorded in `docs/adr/0002-fork-polly-as-fences.md`.

It was a one-off. Do not cite it as a reason to put anything in `Shipped.txt` by hand.

## Related, and often confused with it

- **`InternalsVisibleTo` is fine here.** A test project that needs internals is granted them with
  `<InternalsVisibleToProject Include="Paramore.Fences.Core.Tests" />` in the *source* project's
  file; `eng/Library.targets` turns that into the attribute with the right public key. This is the
  opposite of Brighter's rule, which bans the attribute outright. Do not carry Brighter's rule here.
- Making something `internal` to dodge the public API gate is legitimate when the type really is an
  implementation detail, and dishonest when it is not. If a consumer needs it, it is public and it
  gets an entry.
- Everything a package exports needs XML documentation — StyleCop enforces it on exposed elements.
  See [documentation.md](documentation.md).
