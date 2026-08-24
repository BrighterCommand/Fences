# Release and Versioning

You will not normally run a release. This file exists so that you recognise the machinery when you
meet it, and do not hand-edit something a script owns.

## Versioning

Versions come from [MinVer](https://github.com/adamralph/minver), which derives them from git tags.
There is no version number in any project file.

- `eng/Common.props` sets `<MinVerMinimumMajorMinor>9.0</MinVerMinimumMajorMinor>`. Fences starts at
  **9.0**, one major above Polly's 8.x, so that the version numbers cannot be confused with the
  upstream ones.
- With no reachable tag, MinVer produces a pre-release from that floor — currently something like
  `9.0.0-alpha.0.94`. That is expected on a working branch and is not a misconfiguration.
- `eng/Common.targets` overrides `FileVersion` on GitHub Actions to embed the run number, and
  stamps pull request builds with a `-pr.<number>.<run>` suffix.

### Pre-release tags

Fences ships pre-releases as **`9.0.0-alpha001`, `9.0.0-alpha002`, …** — zero-padded to three
digits, and with **no dot** before the number.

- `9.0.0-alpha.001` would be *invalid* SemVer: a dot-separated numeric identifier may not carry a
  leading zero. `alpha001` is a single alphanumeric identifier, which is legal and ASCII-sorts
  correctly all the way to `alpha999`.
- The padding is what makes `alpha010` sort *after* `alpha009` rather than before it.
- None of this is configured in MSBuild. MinVer takes the git tag verbatim, so the convention lives
  in the tag you hand to `release.yml` and nowhere else. Do not add
  `MinVerDefaultPreReleaseIdentifiers` to make untagged builds match — they are not releases and
  should not look like one.
- A commit *after* the `9.0.0-alpha001` tag builds as `9.0.0-alpha001.1`, `.2` and so on. That sorts
  above `alpha001` and below `alpha002`, which is correct, but such a build is not a release and
  must never be pushed to NuGet.

Do not add a `<Version>`, `<VersionPrefix>` or `<PackageVersion>` property to a project to work
around a version you did not expect.

## Package identity

`eng/Library.targets` holds everything shared by the five shipping packages: `Authors` and `Company`
are `Brighter Command`; `Copyright` carries App vNext's notice alongside ours, because Fences is a
derivative work and BSD-3-Clause clause 1 requires it; the licence expression is `BSD-3-Clause`; the
icon, readme and `LICENSE` are packed into every `.nupkg`.

Constraints worth knowing before you edit any of that:

- **Do not put "Polly" in `Authors`, `Company` or `PackageTags`.** Leading with the lineage in prose
  is agreed and correct; claiming it in authorship metadata is not.
- **Do not remove `Copyright (c) 2015-2025, App vNext`** from `LICENSE` or from the `Copyright`
  property. The licence requires it.
- `EnablePackageValidation` is on, but there is deliberately **no** `PackageValidationBaselineVersion`.
  It stays absent through the alpha series — a baseline pinned to a pre-release would freeze the API
  against a package that carries no compatibility promise. It gets reinstated once 9.0.0 ships.

## The release process

1. `release.yml` is dispatched manually with a version, e.g. `9.0.0-alpha001`. It creates a
   **draft** GitHub release and tag; publishing the tag is what MinVer picks up.
2. The tag push runs `build.yml`. Its `attest-packages` and `publish-nuget` jobs are gated on
   `startsWith(github.ref, 'refs/tags/')`, so they run for a tag and are skipped everywhere else.
   `publish-nuget` authenticates with NuGet.org **trusted publishing**: `NuGet/login` exchanges the
   job's OIDC token for a short-lived API key, so no long-lived NuGet key lives in the repository.
   The only secret involved is `NUGET_USER`.
3. Publishing the release triggers `after-release.yml`, which runs, in order:
   - `eng/update-changelog.ps1 <version> <notes> <server-url>` — prepends the release notes to
     `CHANGELOG.md`.
   - `eng/update-baselines.ps1` — moves every entry from each
     `.PublicAPI/PublicAPI.Unshipped.txt` into the matching `PublicAPI.Shipped.txt`, re-sorts both,
     and preserves the `#nullable enable` header.

   It then opens a pull request with the result. It runs on `GITHUB_TOKEN`, so that pull request
   **does not trigger `build.yml`** — push a commit to the branch, or close and reopen it, to get CI.

There is no `eng/bump-version.ps1`. Upstream had one, to update the `Polly*` `PackageVersion`
entries in `Directory.Packages.props` on each new release. Fences has no such entries — the samples
use `ProjectReference` — so it had nothing to update and was removed.

**This is why you never hand-edit `PublicAPI.Shipped.txt` or `CHANGELOG.md`.** A script owns both.
See [public_api.md](public_api.md).

## `CHANGELOG.md`

`CHANGELOG.md` is the historical record, and it still describes Polly releases up to the fork. That
is deliberate — it was excluded from the rename. Do not rewrite it, and do not "correct" the
historical entries to say Fences.

## Publishing

Fences publishes **pre-release packages only**, as `9.0.0-alpha001` and onwards, to secure the
package identifiers and to gather feedback. Publishing is CI's job and CI's alone:

- **Never run `dotnet nuget push` yourself.** `.claude/settings.json` denies it, and that deny beats
  any `Bash(dotnet:*)` a command file grants itself. A package ID, once published, can be unlisted
  but never deleted.
- Publishing happens only from a tag created through `release.yml`. Nothing done on a branch can
  publish.
- Fences may still be **retired**. It exists because of Polly's Open Source Maintainers Fee; if that
  is reversed, Brighter Command may go back to Polly. `README.md` and `package-readme.md` both say
  so, and that wording is deliberate — do not soften it while the question is open.
