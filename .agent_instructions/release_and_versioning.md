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
  No `Paramore.Fences.*` package has shipped, so there is nothing to validate against, and pointing
  it at a package that does not exist fails restore outright. It gets reinstated once 9.0.0 ships.

## The release process

1. `release.yml` is dispatched manually with a version. It creates a **draft** GitHub release and
   tag; publishing the tag is what MinVer picks up.
2. Publishing the release triggers `after-release.yml`, which runs, in order:
   - `eng/bump-version.ps1 <version>` — updates the `Paramore.Fences*` `PackageVersion` entries in
     `Directory.Packages.props` so the samples and tests build against the released packages.
   - `eng/update-changelog.ps1 <version> <notes> <server-url>` — prepends the release notes to
     `CHANGELOG.md`.
   - `eng/update-baselines.ps1` — moves every entry from each
     `.PublicAPI/PublicAPI.Unshipped.txt` into the matching `PublicAPI.Shipped.txt`, re-sorts both,
     and preserves the `#nullable enable` header.

   It then opens a pull request with the result.
3. `nuget-packages-published.yml` waits for the packages to appear on NuGet.

**This is why you never hand-edit `PublicAPI.Shipped.txt` or `CHANGELOG.md`.** A script owns both.
See [public_api.md](public_api.md).

## `CHANGELOG.md`

`CHANGELOG.md` is the historical record, and it still describes Polly releases up to the fork. That
is deliberate — it was excluded from the rename. Do not rewrite it, and do not "correct" the
historical entries to say Fences.

## Publishing

**Fences has not been published to NuGet.** Do not run anything that would publish it. A package ID,
once published, can be unlisted but never deleted.
