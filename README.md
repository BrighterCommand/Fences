# CLA signatures

This branch is the signature record for the Fences Contributor License Agreement. It is written to
automatically by [`contributor-assistant/github-action`][action], configured in
[`.github/workflows/cla.yml`][workflow] on `main`, which stores signatures at
`signatures/version1/cla.json`.

It is an orphan branch: it shares no history with `main` and holds no source code. Nothing here is
ever merged into `main`, and `main` is never merged into it.

**Do not add branch protection to this branch.** The action commits to it directly using
`GITHUB_TOKEN`, and any rule requiring a pull request will break CLA checks on every incoming
contribution. **Do not delete it either** — deleting it destroys the record of who has signed, and
the action fails with `Branch cla-signatures not found` until it is recreated.

[action]: https://github.com/contributor-assistant/github-action
[workflow]: https://github.com/BrighterCommand/Fences/blob/main/.github/workflows/cla.yml
