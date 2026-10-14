# Contributing

## Branches

| Branch | What it is |
|---|---|
| `dev` | The working branch and the repository's default. Everything lands here. |
| `main` | A frozen checkpoint of an earlier state, kept so development can restart another way. It changes only when the owner opens a `dev` → `main` pull request. |
| `feature/<topic>`, `fix/<topic>`, `chore/<topic>` | One per piece of work, made from `dev`, merged back into `dev`, then deleted. |

Nobody commits straight to `dev` or `main`; both are protected. Open a pull request into `dev`; the owner is requested
as reviewer by CODEOWNERS; then merge. No CI runs on these pull requests or on pushes to `dev`.

### CI

CI (both test suites, the Windows build, a smoke run) runs only when the owner starts it by hand (Actions → CI → Run
workflow, choosing the jobs and the branch) or on a pull request from `dev` into `main`. `main` takes `dev` that way
only when the owner wants a new checkpoint.

### Parallel sessions (people or agents)

Two sessions must never share one working copy: they would share its index and its checked-out branch. Give each its
own worktree and branch:

```bash
git fetch origin
git worktree add ../OWSBG-wt/<topic> -b feature/<topic> origin/dev
# work, commit, then
git push -u origin feature/<topic>
gh pr create --base dev
```

Unity can open only one editor per project folder, and each worktree imports its own `Library/`, so coordinate who
has the editor, and pull `dev` into your branch (`git merge origin/dev`) before opening the PR.
Remove a worktree when its branch is merged: `git worktree remove ../OWSBG-wt/<topic>`.

## What is published

The repository holds the game (`LastCartographer/`), the tools that build its content and the game (`tools/`), the CI
and repository settings (`.github/`) and the press material (`docs/marketing/`).

Development papers are not published: the planning, story and design documents, the playtest sheets, the model
sheets and the rendered audio under `docs/` (everything but `docs/marketing/`), plus `logs/`. They stay in your working
copy, are ignored by `.gitignore`, and the tests that read them skip themselves when they are absent (see
`DevDocs` in the EditMode tests). Do not use `git add -f` on them.

## Commits

One change per commit; the subject says what the player or the developer can now do. Binary art, audio and video go
through Git LFS (`.gitattributes`). Never commit Unity licence files, `Library/`, `Temp/` or build output.

## Reporting bugs and vulnerabilities

Bugs: the **Bug report** issue form. Vulnerabilities: see [SECURITY.md](SECURITY.md).
