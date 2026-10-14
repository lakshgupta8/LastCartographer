# Security policy

The Last Cartographer is a single-player Windows game with no servers, accounts or network features. Even so, a
vulnerability in the game, the build pipeline or the tools is welcome news if it is reported privately.

## Reporting

Use GitHub's private reporting: **Security → Report a vulnerability** on this repository
(<https://github.com/lakshgupta8/LastCartographer/security/advisories/new>). Please do not open a public issue.

Include what you found, how to reproduce it, and the build or commit. You can expect an acknowledgement within a week.

## Scope

- The game and its save, settings and bug-report files.
- The scripts under `tools/` and the workflows under `.github/`, including anything that could leak the Unity or Steam
  secrets the workflows use.

Only the latest commit of `dev` is supported. `main` is a frozen checkpoint and is not patched.

## Secrets

No licence file (`*.alf`, `*.ulf`), token or Steam credential belongs in the repository. They are Actions secrets
and the Steam upload runs only in the protected `steam` environment. If you find one committed, report it as above.
