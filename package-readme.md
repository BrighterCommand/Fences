# Fences

Fences is a .NET resilience and transient-fault-handling library that allows developers to express strategies such as Retry, Circuit Breaker, Timeout, Rate-limiting, Hedging and Fallback in a fluent and thread-safe manner.

Fences is a community fork of [Polly](https://github.com/App-vNext/Polly), maintained by [Brighter Command](https://github.com/BrighterCommand). It is not affiliated with, endorsed by, or supported by App vNext or the Polly maintainers.

[![Build status](https://github.com/BrighterCommand/Fences/actions/workflows/build.yml/badge.svg?branch=main&event=push)](https://github.com/BrighterCommand/Fences/actions/workflows/build.yml?query=branch%3Amain+event%3Apush) [![Code coverage](https://codecov.io/gh/BrighterCommand/Fences/branch/main/graph/badge.svg)](https://codecov.io/gh/BrighterCommand/Fences)

![Fences logo](https://raw.githubusercontent.com/BrighterCommand/Fences/main/images/fences-264.png)

## Migrating from Polly

Fences 9.0.0 is forked from Polly 8.7.0 and the API is unchanged apart from one renamed type. For most projects the move is a package reference and a namespace: `Polly.Core` becomes `Paramore.Fences.Core`, and `using Polly;` becomes `using Paramore.Fences;`.

The telemetry names changed too, and that is the one part a find-and-replace over your own source will not fix. See the [migration guide](https://github.com/BrighterCommand/Fences/blob/main/docs/migration-from-polly.md) for the full mapping.

## Release notes

- The repository's [changelog](https://github.com/BrighterCommand/Fences/blob/main/CHANGELOG.md) describes changes by release. Entries for 8.7.0 and below are Polly's history, retained as the record of the code Fences is built on.

## Documentation and samples

Documentation and samples for using Fences can be found in the repository's [README](https://github.com/BrighterCommand/Fences#readme) and [documentation](https://brightercommand.github.io/Fences/).
