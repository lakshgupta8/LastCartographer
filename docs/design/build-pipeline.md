# Build pipeline (PRG-25, v1)

This covers how the game becomes a Windows build: on this machine, in CI on every push, and on Steam from a version
tag. Each build is smoke-run before anyone plays it.

## The build

`OWSBG.Build.GameBuild` (`Assets/Editor/Build`, its own editor assembly so the tests can reach it) is the one entry
point, used by the menu (**OWSBG → Build → Windows**), a headless editor and CI. A build goes through these steps:
1. **Names the version.** The source, in order: `-buildVersion`, the `OWSBG_VERSION` environment variable, then
   `git describe --tags --always --dirty`.
   - `BuildVersion.Clean` makes it semver-shaped: `v0.5.0` → `0.5.0`, three commits past the tag → `0.5.0+3`,
     with uncommitted changes → `0.5.0+3.dirty`. With no tag yet it's `0.0.0-dev`.
   - The commit comes from `GITHUB_SHA` in CI, else git.
2. **Stamps the build.** It writes `Settings/Build/Resources/build_info.json` and sets the player's bundle version.
   - The game reads the stamp as `BuildInfo` (Core), and the options page shows it at the bottom ("version 0.5.0 ·
     1a2b3c4"), so a bug report can quote it.
   - The checked-in file says `dev`, and the build puts it back afterwards however the build ends.
3. **Builds the Addressables content.** Every room is a bundle (PRG-07); play mode doesn't need this, a player does.
4. **Builds the player:** 64-bit Windows, Mono, from Build Settings' scenes (only Persistent; the rooms come from the
   bundles). The exe is `LastCartographer.exe`, a name with no spaces for Steam's launch options; the window title
   is still the product name.
5. **Writes what goes beside it:** a copy of `build_info.json`. When `-steamAppId` and `-steamDepotId` are given,
   it also writes Steam's depot scripts to `Builds/steam`.
6. **Exits with 1 on failure**, so scripts and CI see it.

**Measured on this machine (2026-09-28):**
- **Release build** of `v0.1.0`: 101 MB, 17 s with the Addressables content already built.
- **Development build:** 166 MB, 36 s including the content build.
- **Smoke runs:** both builds passed in under a second after boot, into `Greybox_Greyfold_Edge` and
  `Greybox_Saltmarrow_A`, both from bundles.

| Argument | Meaning |
|---|---|
| `-buildOutput <dir>` | where the player goes (default `Builds/Windows`, relative to the project) |
| `-customBuildPath <path>` | GameCI's form: the exe's full path; its folder is used |
| `-buildVersion <v>` | the version, before cleaning |
| `-development` | a development build (debugging on, the pseudo-locale on the picker) |
| `-skipContent` | reuse the last Addressables content build |
| `-steamAppId`, `-steamDepotId`, `-steamBranch` | write the depot scripts; the branch it goes live on |

On this machine, with Unity closed on the project:
```
pwsh tools/build.ps1                  # release build to LastCartographer/Builds/Windows, then a smoke run
pwsh tools/build.ps1 -Development     # a development build
pwsh tools/build.ps1 -SteamAppId <app> -SteamDepotId <depot> -SteamBranch beta
```
The build's log is `logs/build.log`, and the smoke run's is `logs/smoke.log`.

## The smoke test

`LastCartographer.exe -batchmode -nographics -smoke -logFile smoke.log` runs `SmokeTest` (Narrative) inside the
build. It proves the build works as shipped.
- **What it checks:**
  1. The build boots to its first room (the prologue's, in a new game).
  2. It walks into the Drowned Quay through Addressables.
  3. It finds the pipeline's version stamp.
  4. It starts a conversation from the compiled Yarn project, then stops it.
- **How it fails:** any error or exception logged along the way fails it.
- **What it reports:** one line, `[OWSBG] smoke: passed (4.2 s) first room … (addressable); then Greybox_Saltmarrow_A
  (addressable); build 0.5.0 · 1a2b3c4; dialogue Camp_Ashes_Ahead`.
- **Exit codes:** 0 passed, 1 failed, 2 timed out.

`tools/smoke.ps1` runs it and passes the exit code on. `SmokeTestTests` runs the same check in the editor.

## CI (`.github/workflows/ci.yml`)

GitHub Actions with GameCI's Unity actions, on every push to `main`, every pull request, and every `v*` tag:

| Job | Runs on | What it does |
|---|---|---|
| Tests (editmode, playmode) | Ubuntu, GameCI's Unity 6000.3.7f1 image | both suites (play mode limited to `OWSBG.Tests.PlayMode`), results as a check and an artifact |
| Windows build | Ubuntu | `unity-builder` calls `GameBuild.BuildFromCommandLine`; the build is an artifact for 14 days |
| Smoke run | Windows | downloads the build and runs `tools/smoke.ps1` against it |
| Steam upload | Ubuntu, `v*` tags only, the `steam` environment | `game-ci/steam-deploy` uploads the build folder as depot 1 and sets it live on `STEAM_BRANCH` (default `beta`) |

- **Library cache:** the Library folder is cached per job, keyed on the package lock and the editor version.
- **Checkout:** the build job fetches full history so `git describe` can name the version.

**Secrets and variables to set** (repository settings; nothing is in the repository):
- `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`: a Unity licence for GameCI. Without them the editor in the
  container exits with code 1 and prints nothing (the second run, 2026-09-29). To get them, for a Personal licence:
  1. Actions → **Unity activation file** → Run workflow (`.github/workflows/activation.yml`, no secrets needed).
     Download its artifact, `Unity_v6000.3.7f1.alf`.
  2. At https://license.unity3d.com/manual, signed in with the Unity account, upload the `.alf`, choose Personal,
     and download the `Unity_v6000.x.ulf` it gives back.
  3. Settings → Secrets and variables → Actions → New repository secret: `UNITY_LICENSE` is the whole text of the
     `.ulf` file (open it in an editor, copy everything); `UNITY_EMAIL` and `UNITY_PASSWORD` are the Unity
     account's. Then re-run the failed jobs.
  A Pro or Plus licence uses `UNITY_SERIAL` with the email and password instead, and the workflow would need
  that variable added to its `env`.
- In the `steam` environment:
  - `STEAM_USERNAME`: a Steamworks build account;
  - `STEAM_CONFIG_VDF`: its cached `config.vdf` after one Steam Guard login, base64 (see steam-deploy's docs);
  - variables `STEAM_APP_ID` and optionally `STEAM_BRANCH`.

Until these are set, the workflow's jobs fail at the licence step. Nothing else in it has been run yet; see Open
below.

## Steam packaging

`SteamDepot` writes SteamPipe's two scripts:
- **The app build** (`app_build_<app>.vdf`): the app, a description ("The Last Cartographer 0.5.0 (1a2b3c4)"), the
  content root, `SetLive` for a beta branch, and one depot.
- **The depot build** (`depot_build_<depot>.vdf`): the whole build folder, recursively, except debug symbols
  (`*.pdb`) and the Burst and IL2CPP leftovers marked "DoNotShip".

The default branch is never set live by a script; Steam wants that done by hand in Steamworks.
`tools/steam-upload.ps1 -AppId <app>` runs `steamcmd +login $STEAM_USERNAME +run_app_build … +quit`. steamcmd asks
for the password and Steam Guard code itself and caches them after the first time.

## Tests

`BuildPipelineTests` (EditMode, 6):
- versions from tags and `git describe`;
- the arguments a person, `build.ps1` and GameCI pass;
- the Steam scripts' contents, escaping and forward-slash paths;
- the checked-in stamp is `dev`;
- Persistent is in Build Settings and the smoke test's second room is a bundle;
- the workflow names a build method, a test assembly, an editor version and scripts that exist.

`SmokeTestTests` (PlayMode, 1): the smoke test passes in the editor.

## The first run

The first push (2026-09-29) ran both test jobs and both died before a test ran: `docker: failed to register layer:
no space left on device`, pulling the editor image. GameCI's image is over ten gigabytes and `ubuntu-latest` has
about fourteen free with its preinstalled toolchains. Both Unity jobs now start by removing the ones the game
doesn't use (.NET, Android, Haskell, CodeQL, Boost) and pruning Docker's images, then print `df -h` so the next
failure of this kind shows its number. The "no files found at test-results" warnings were the same failure: nothing
ran, so nothing was written.

## Open

- **CI hasn't run.** The workflow was checked by those tests and a YAML parse, not on GitHub; the first push with
  the secrets set is its real test.
  - Building Windows on Ubuntu relies on GameCI's `windows-mono` image.
  - If that image falls short, the build job can move to `windows-latest`.
- **No Steamworks SDK in the game** (overlay, achievements, cloud saves) and no store assets; this row only
  packages and uploads. An app id and depot id come with the Steamworks partner account.
- **Not yet in the pipeline:** code signing, a Mac or Linux build, and IL2CPP.
- **Performance budgets** belong to PRG-24; this pipeline would be where they are measured.
