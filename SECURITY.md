# Security Policy

This document defines security reporting and handling for Fences.

Fences is a community fork of [Polly](https://github.com/App-vNext/Polly), maintained by
[Brighter Command](https://github.com/BrighterCommand). It is not affiliated with, endorsed by, or
supported by App vNext or the Polly maintainers.

> [!IMPORTANT]
> **Report Fences vulnerabilities here, and Polly vulnerabilities to Polly.** We cannot fix, or
> issue advisories for, code we do not ship. If a vulnerability affects both projects because the
> code predates the fork, please tell us that in your report so that we can coordinate disclosure
> with the Polly maintainers.

## Supported Versions

Fences follows the current release line only. Security fixes are made against the default branch
and released from there; where a fix applies to a release branch that is still supported, it is
backported to that branch as well.

## Reporting a Vulnerability

To privately report a security vulnerability in Fences, please create a security advisory in this
[repository's Security tab][report-vulnerability].

1. Navigate to the [Security][security] tab of this repository.
2. Click on [Advisories][report-vulnerability] in the left sidebar.
3. Click on the green **Report a vulnerability** button and follow the prompts and instructions.
   Please provide as much detail as possible.

> [!IMPORTANT]
> Please do not open public GitHub issues, pull requests or discussions for anything you think might
> have a security implication.

Please allow **up to 7 days** for an initial response from a maintainer. If you do not receive a
response within that time, please follow up by commenting on the advisory. You can also reach any of
the maintainers directly on GitHub: [@iancooper][iancooper], [@holytshirt][holytshirt],
[@DevJonny][devjonny] or [@preardon][preardon].

A maintainer may respond in a shorter time frame than stated above, but the maintainers may not be
in your time zone, may be on holiday, or may otherwise be unavailable, so please be patient. We take
any vulnerability reports we receive seriously.

> [!TIP]
> Further details on how to privately report a vulnerability using GitHub can be found in the
> [GitHub documentation][privately-reporting].

## Incident Response Process

> [!NOTE]
> Information about how we handle security incidents can be found in our
> [Incident Response Plan][irp].

[devjonny]: https://github.com/DevJonny
[holytshirt]: https://github.com/holytshirt
[iancooper]: https://github.com/iancooper
[irp]: .github/IRP.md "Incident Response Plan"
[preardon]: https://github.com/preardon
[privately-reporting]: https://docs.github.com/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability "Privately reporting a security vulnerability"
[report-vulnerability]: https://github.com/BrighterCommand/Fences/security/advisories
[security]: https://github.com/BrighterCommand/Fences/security
