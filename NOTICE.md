# NOTICE

## What Fences is

Fences is a .NET resilience and transient-fault-handling library: retry, circuit breaker,
timeout, rate limiter, hedging, fallback and chaos strategies, composed into resilience
pipelines.

Fences is maintained by [Brighter Command](https://github.com/BrighterCommand) and is part of
the Brighter family of projects.

## Provenance

Fences is a fork of [Polly](https://github.com/App-vNext/Polly), taken from `App-vNext/Polly`
at commit
[`47e3b412e8c3b7e6db1629acd98f3e3b6b529d6c`](https://github.com/App-vNext/Polly/commit/47e3b412e8c3b7e6db1629acd98f3e3b6b529d6c)
(Polly 8.7.0, 17 August 2026).

Everything in this repository below that commit is the work of Polly's authors and
contributors. The `CHANGELOG.md` in this repository retains Polly's history in full: entries
for 8.7.0 and below are Polly's changelog, not Fences'. See
[`ACKNOWLEDGEMENTS.md`](ACKNOWLEDGEMENTS.md) for credit, and
[`docs/adr/0002-fork-polly-as-fences.md`](docs/adr/0002-fork-polly-as-fences.md) for why the
fork was made.

## Licence

Fences is distributed under the BSD 3-Clause Licence, the same licence as Polly. The upstream
copyright notice is retained as clause 1 of that licence requires:

```text
Copyright (c) 2015-2025, App vNext
Copyright (c) 2026, Brighter Command
```

The full text is in [`LICENSE`](LICENSE).

## No affiliation or endorsement

**Fences is not affiliated with, endorsed by, sponsored by, or supported by App vNext or the
Polly maintainers.** It is an independent project under separate governance.

Do not report Fences issues to the Polly project, and do not report Polly issues here. Support
for Fences comes from Brighter Command; support for Polly comes from App vNext.

"Polly" is the name of the upstream project and is used in this repository only to describe
that lineage accurately, as clause 3 of the BSD 3-Clause Licence permits and requires — never
to endorse or promote Fences.
