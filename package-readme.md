# Fences

Fences is a .NET resilience and transient-fault-handling library that allows developers to express strategies such as Retry, Circuit Breaker, Timeout, Rate-limiting, Hedging and Fallback in a fluent and thread-safe manner.

Fences is a community fork of [Polly](https://github.com/App-vNext/Polly), maintained by [Brighter Command](https://github.com/BrighterCommand). It is not affiliated with, endorsed by, or supported by App vNext or the Polly maintainers.

[![NuGet version](https://img.shields.io/nuget/v/Paramore.Fences?logo=nuget&label=NuGet&color=blue)](https://www.nuget.org/packages/Paramore.Fences/) [![Build status](https://github.com/BrighterCommand/Fences/actions/workflows/build.yml/badge.svg?branch=main&event=push)](https://github.com/BrighterCommand/Fences/actions/workflows/build.yml?query=branch%3Amain+event%3Apush)

![Fences logo](https://raw.githubusercontent.com/BrighterCommand/Fences/main/images/fences-264.png)

## Why Fences exists

**Polly's source is BSD 3-Clause licensed, and it stays open source.** That licence lets anyone redistribute it, in source or binary form, provided the copyright notice and disclaimer are retained. Building and publishing our own binaries from that source is squarely within the grant.

The Open Source Maintainers Fee (OSMF) is App vNext's policy for the binaries **they** publish: a commercial consumer of those binaries may owe a fee above a revenue threshold. It governs their distribution, not the licence on the source, and it does not reach binaries built by anyone else.

**That freedom is the OSMF's own argument.** The case made for charging for binaries is that it stays within open source precisely because the source remains open and anyone is free to build and publish binaries of their own. It is one door, and it opens both ways: the principle that makes the fee compatible with open source is the same one that makes Fences legitimate.

**Fences is the binary somebody else builds.** We compile from the BSD 3-Clause source, publish under our own branding, and **we do not apply the OSMF. Fences binaries are free, for everyone - no revenue threshold, no fee.**

We track the Polly API today and expect to keep doing so. But Fences is under Brighter Command's stewardship now, so a future major version may diverge from Polly and stop being API-compatible. We will say so plainly when it does.

None of this is a criticism of App vNext, who have maintained Polly for years and are entitled to be paid for their work. Publishing our own binaries is exactly what the licence they chose allows.

## Migrating from Polly

Fences 9.0 is forked from Polly 8.7.0 and the API is unchanged apart from one renamed type. For most projects the move is a package reference and a namespace: `Polly.Core` becomes `Paramore.Fences.Core`, and `using Polly;` becomes `using Paramore.Fences;`.

The telemetry names changed too, and that is the one part a find-and-replace over your own source will not fix. See the [migration guide](https://github.com/BrighterCommand/Fences/blob/main/docs/migration-from-polly.md) for the full mapping.

## Release notes

- The repository's [changelog](https://github.com/BrighterCommand/Fences/blob/main/CHANGELOG.md) describes changes by release. Entries for 8.7.0 and below are Polly's history, retained as the record of the code Fences is built on.

## Documentation and samples

Documentation and samples for using Fences can be found in the repository's [README](https://github.com/BrighterCommand/Fences#readme) and [documentation](https://brightercommand.github.io/Fences/).
