# Security policy

## Supported versions

Only released versions are supported. Preview versions (`x.y.z-previewNN`) are published for
evaluation only: they can change or be removed at any time, including breaking changes, and come
with no support commitment. Do not use them in production.

| Version | Supported |
|---|---|
| 9.x releases (from 9.0.0, once released) | Yes |
| 9.0.0 previews (`9.0.0-previewNN`) | No |
| 8.x (latest release 8.3.2, `release/8.3.2`) | Critical fixes only |
| Earlier versions | No |

## Report a vulnerability

Do not report security vulnerabilities in public issues, pull requests or discussions.

Report them privately with GitHub's
[private vulnerability reporting](https://github.com/Arc4u-org/Arc4u/security/advisories/new)
(**Security** tab → **Report a vulnerability**). Include:

- the affected packages and versions, and the target framework;
- a description of the vulnerability and its impact;
- the steps or a minimal reproduction to trigger it.

We aim to acknowledge a report within 5 working days and to send a first assessment within
15 working days, and we keep you informed until it is fixed. The fix is coordinated through a
GitHub security advisory, with a CVE requested where appropriate. Once the fix is published, the
advisory is made public, the vulnerability is listed in the `Security` section of
[`CHANGELOG.md`](CHANGELOG.md), and you are credited unless you ask not to be.

## Known issues

Security weaknesses that are already known are documented under "Known issues" in each
[guide](https://arc4u-org.github.io/Arc4u/) and tracked as issues. Check them before reporting.

## Disclaimer

Arc4u is provided "AS IS", without warranties or conditions of any kind, as stated in sections 7
and 8 of the [Apache License 2.0](LICENSE). You are responsible for assessing whether it is
suitable and secure for your use.
