# Acknowledgements

Fences exists because of Polly. Every line of code in this repository at the point of the fork
was written by Polly's authors and contributors, and the design — resilience pipelines,
strategy options, `ResilienceContext`, the telemetry model — is theirs.

## Polly

* **[App vNext](https://github.com/App-vNext)**, the organisation that has stewarded Polly since
  2015 and holds the upstream copyright.
* **[Michael Wolfenden](https://github.com/michael-wolfenden)**, who created Polly and released it
  under a permissive licence, without which none of this would exist.
* **[Martin Costello](https://github.com/martincostello)** and the Polly maintainer team, whose
  work on Polly v8 is the codebase Fences forked.
* The **Polly contributor community** — several hundred people whose pull requests are recorded in
  `CHANGELOG.md` and preserved in this repository's git history. `git blame` reaches them still:
  the rename commit is listed in `.git-blame-ignore-revs` precisely so that it does not stand
  between a line and its author.
* **The .NET Foundation**, which hosted Polly for much of its life.

The upstream project is at <https://github.com/App-vNext/Polly>. It is actively maintained and
remains the right choice for many users. Fences is a divergence, not a replacement, and no
criticism of Polly is implied by its existence — see
[`docs/adr/0002-fork-polly-as-fences.md`](docs/adr/0002-fork-polly-as-fences.md).

## Licence

Polly is licensed under the BSD 3-Clause Licence. Fences retains that licence and the upstream
copyright notice. See [`LICENSE`](LICENSE) and [`NOTICE.md`](NOTICE.md).

Fences is not affiliated with or endorsed by App vNext or the Polly maintainers.
