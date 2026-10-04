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
3. **Builds the Addressables content.** Every room is a bundle (PRG-07), and the art the rooms share is a second group packed by label (PRG-24); play mode doesn't need this, a player does.
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
| Smoke run | Windows | downloads the build and runs `tools/smoke.ps1` against it, then the probe headless (`tools/perf.ps1 -Batch`: transitions and garbage, `docs/design/performance.md`) |
| Steam upload | Ubuntu, `v*` tags only, the `steam` environment | `game-ci/steam-deploy` uploads the build folder as depot 1 and sets it live on `STEAM_BRANCH` (default `beta`) |

- **Library cache:** the Library folder is cached per job, keyed on the package lock and the editor version.
- **Checkout:** the build job fetches full history so `git describe` can name the version.

**A manual run** (Actions → CI → Run workflow) has a checkbox per job: Tests, Windows build, Smoke run (on by
default) and Steam upload (off). A job runs only when everything before it that ran passed. The smoke run and
Steam use this run's build, so they need the build ticked. Pushes, pull requests and the Monday schedule always run
tests, build and smoke run; Steam otherwise only follows a `v*` tag, after a passing smoke run. (Before this, a
manual run that skipped the tests skipped the smoke run too, the ninth run: GitHub skips a job whose chain holds a
skipped job unless its condition says otherwise.)

**Secrets and variables to set** (repository settings; nothing is in the repository):
- `UNITY_EMAIL`, `UNITY_PASSWORD`: the Unity account GameCI activates the editor with, inside the container, on
  every run. Without them the editor exits with code 1 and prints nothing (the second run, 2026-09-29). For a
  Personal licence these two are the whole of it: Unity no longer issues offline licence files (`.ulf`) to
  Personal seats ("offline activation is available only for Enterprise and Industry seats", 2026-09-29), so
  `UNITY_LICENSE` stays unset and GameCI signs the editor in with the account instead. The account must be able
  to sign in from a command line: two-factor authentication on it will stop the container, and a fresh account
  has to have opened the Hub once to hold a Personal licence.
- `UNITY_LICENSE`: only for a licence file, which Personal seats can't get now; `UNITY_SERIAL` (with the email
  and password) for a Pro or Plus seat, which the workflow's `env` would need added.
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

The second run got the image and died silently: no licence secrets. The third, with `UNITY_EMAIL` and
`UNITY_PASSWORD` set, ran the tests. Edit mode: 184 of 185, the one failure a real bug the runner found, the
Input System naming the space key as nothing on a machine with no keyboard, so a prompt read "Hold  ";
`Controls.DisplayName` now falls back to the binding's own name. Play mode: the editor crashed with a
segmentation fault in `ScriptingCoverage::FilterRecordedMethods` during the assembly reload for play mode. GameCI
switches code coverage on by default and Unity 6 on Linux dies inside it. The action's `coverageEnabled: false`
didn't take (the fourth run): the action hands it to GameCI's CLI as `--no-coverageEnabled`, and the CLI's parser
has negation switched off, so it warned "Unknown argument" and left coverage on. The CLI also reads every option
from a `GAME_CI_`-prefixed environment variable, so the test step sets `GAME_CI_COVERAGE_ENABLED=false`.
The fifth run: edit mode 185 of 185 on Linux, the job still marked failed because posting the results check was
refused ("Resource not accessible by integration"); the workflow now asks for `checks: write` (and only
`contents: read` besides). Play mode 261 of 262: `UiScreenshotTests` wrote two of its five pictures and hit its three-minute timeout.
The runner has no GPU and draws in software (the lantern render tests, on a small scene, passed). A first guard
timed ten plain frames, which were fast enough; the sixth run wrote one picture and timed out again, so the slow
part is the capture itself (a forced render into a texture, two read-backs and a per-pixel blend), over a minute
each there. The test now times its first capture and reports itself ignored, with the seconds and the device,
past ten seconds; here a capture takes well under one and the whole test 4 s.

The eleventh run (2026-10-02, after the first pass of every plan row): play mode failed on the same test, in a
new way. It wrote the first picture inside the ten-second guard and still ran into the three-minute timeout, 292
seconds in all, so the slow part there is now past the first capture (or before it, in the scene's load: the guard
timed only the capture). The test now keeps a clock over every step (the scene, each picture) and steps aside past
sixty seconds in all, naming each step's seconds and the graphics device, so the runner's report says where the
time went rather than only that it ran out. A `Null` graphics device (`-nographics`) steps aside at once. Here the
whole test takes two seconds.

The twelfth run (2026-10-02): three play-mode tests that had never run on the runner, all measuring hardware it
lacks. `LookTests` measures the foreground blur against a sharp render of the Quay's reeds: here a pen edge crosses
in 0.4 px and the blur spreads it to 4; the runner's software rasteriser draws the sharp edge 2.6 px wide and the
blurred one 3.3, so the measure has nothing to stand on. The test now steps aside when the sharp render's edge is
two pixels or wider, naming both widths and the device; the paper-grain check, which does not depend on it, runs
first. Two of `MusicDriverTests` (AUD-04) measure the stems' timing on the DSP clock: with no audio device the
runner mixes to nothing and its clock ran ahead of real time (the boss test's bar-line wait ended at once, the test
took 1.4 s against 4.2 here; stems scheduled on one sample read 3072 samples apart; the resolution's seconds passed
in a frame). The fixture now measures the DSP clock against the wall clock once, half a second, and those two tests
step aside when it is off by more than a quarter, naming the rate. The other five, which check levels and buses, still run.

The thirteenth run (2026-10-02): the region handover test, which waits on the coast's bar line, failed the same
way (its scheduled start was behind a DSP clock reading 232,486 s, two and a half days, twenty minutes into the
job). It and the boss-layer test, the other two that wait on bar lines or a resolution, now step aside on the same
measure; the three that check levels and buses still run there.

The fourteenth run (2026-10-02): the ambience crossfade test (AUD-06), whose outgoing layers fade on the DSP clock,
found them gone at the halfway mark on the same runner. The clock measure is now one helper for every play
fixture that needs it (`AudioClock`: measured once per run, a test steps aside past a quarter off real time), and
that test uses it too.

The fifteenth run (2026-10-03) reached the smoke job for the first time since the probe joined it, and the probe
failed it: transitions of 97–850 ms and a collection in four quiet rooms. The first reading was that the hosted
runner is not the target machine; the same build then failed the same way here (125–462 ms, collections in three
rooms), and the cause was the art that had landed since the probe was last run: every room's bundle carried its own
copies of the sheets, materials and shaders it used, and the audio drivers, the NPC animators and the HUD allocated
on quiet frames. Both are fixed (`docs/design/performance.md`, "The art's cost"), the probe reports where a
transition's time goes and who allocates, and it gates CI as before. The same run's play mode found two things the
Brood's commit had left: its fire was a Shape in its kit while it cost a mask (now a Window, read as the 240 frames
of a step), and the arena-room tests expected every kit in a planned room where the Brood stands in a built one
(`ArenaRooms.SceneFor` now names the Iris Fields).

The seventh run: both test jobs green. The Windows build failed at once: `unity-builder@v4` demands
`UNITY_LICENSE` or `UNITY_SERIAL` before it starts. v6 is a thin wrapper round the same CLI the test runner uses,
which signs in with the account, so the build job now uses v6. It also passes `-buildOutput` to the folder the
upload step reads, so the build lands there whatever path the CLI hands Unity.

The eighth run built the game (100.7 MB, no errors, in `build/StandaloneWindows64`) and was still marked failed.
The CLI decides a build passed by reading the log: `Build succeeded!`, or a `# Build results #` block down to a
`Size:` line with `Errors: 0`, which GameCI's own build method prints and a custom one doesn't. `GameBuild` now
prints that block after every player build (`GameBuild.ResultsBlock`; a failed build always reports at least
one error), and `BuildPipelineTests` holds it to the CLI's two patterns. Both test jobs have a ninety-minute limit now, in case a play-mode hang is next.

## Open

- ~~CI hasn't run.~~ Green end to end on the tenth run (2026-09-29): both test suites, the Windows build (7 min
  with a warm cache, built on Ubuntu from GameCI's `windows-mono` image) and the smoke run of that build on
  `windows-latest` (25 s). Each run's failure and its fix are above.
- **Steam upload hasn't run.** It waits on the `steam` environment's secrets and an app id.
- **Two warnings to tidy:** a duplicate `System.Runtime.CompilerServices.Unsafe.dll` (Collections' test copy and
  Yarn Spinner's analyser copy; Unity picks the newer on both machines), and GitHub's notice that the v4 actions
  target Node 20.
- **Play mode takes about twenty-five minutes on CI**, against nine here; the UI screenshots, the blur's measure
  and the music's four timing tests step aside there (no GPU, no audio device), with their reasons in the report.
- **No Steamworks SDK in the game** (overlay, achievements, cloud saves) and no store assets; this row only
  packages and uploads. An app id and depot id come with the Steamworks partner account.
- **Not yet in the pipeline:** code signing, a Mac or Linux build, and IL2CPP.
- **Performance budgets** belong to PRG-24; this pipeline would be where they are measured.
