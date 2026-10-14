<div align="center">

<img src="docs/marketing/capsules/header_capsule.png" alt="The Last Cartographer" width="720">

# The Last Cartographer

**A hand-inked 2.5D metroidvania about a kingdom that forgot how to fly.**

*She fell off the edge of the map and drew her way back.*

<br>

[![CI](https://github.com/lakshgupta8/LastCartographer/actions/workflows/ci.yml/badge.svg)](https://github.com/lakshgupta8/LastCartographer/actions/workflows/ci.yml)
![Unity](https://img.shields.io/badge/Unity-6000.3_LTS-000000?logo=unity&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![Status](https://img.shields.io/badge/status-vertical_slice-C8963E)
![Genre](https://img.shields.io/badge/genre-metroidvania-5B7DB1)

[Story](#the-world) · [Play](#play-it) · [Controls](#controls) · [Docs](#documentation) · [Build](#build-and-test) · [Contribute](#reporting-a-bug)

</div>

<br>

## The pitch

You are **Wren**, a small cartographer with a needle-quill. The map you carry is forgetting itself, and every place
you leave undrawn begins to fade to blank paper. Fight through ink-and-wash birds, climb layered paper dioramas with
real depth, and decide for each place whether to **anchor** it, **hold** it, or **let it go**.

Inspired by *Hollow Knight: Silksong*. Built to be read as well as played: the code, tests and content tools are all in this repository.

<table>
  <tr>
    <td><img src="docs/marketing/screenshots/saltmarrow_lighthouse.png" alt="Saltmarrow boardwalk and lighthouse"></td>
    <td><img src="docs/marketing/screenshots/halden_observatory.png" alt="Halden Reach observatory"></td>
  </tr>
  <tr>
    <td><img src="docs/marketing/screenshots/verdance_aldermere.png" alt="The Verdance, Aldermere"></td>
    <td><img src="docs/marketing/screenshots/windreach_camp.png" alt="Windreach, the moving camp"></td>
  </tr>
</table>

## Highlights

| | |
|---|---|
| 🖋️ **Ink on paper** | Cut-out paper dioramas with real depth, Freestyle ink lines, foreground blur and paper grain, all drawn procedurally in headless Blender. |
| 🗺️ **A map that forgets** | Places fade in five stages. Survey a vantage to draw it, then anchor, hold or release the place, once and for all. |
| ⚔️ **Quill combat** | Frame-tuned strikes, Wingbeat dash, Flourishes, Charters and Instruments. Fifteen bosses, each with its own arena and reason. |
| 🐦 **A cast of birds** | Sable, Isolde, Pell, Runa and a crowd of townsfolk, with portraits, voices and a tune everyone sings. |
| 🎶 **Sound made in code** | Music, ambience and every effect are synthesised in engine from data: nothing is a recorded sample. |
| 🔀 **Four endings** | Decisions across five living regions, the Greyfold and the Blank decide which ending the map gets. |
| ♿ **Accessible** | Full remapping, hold or toggle options, hitstop and shake sliders, high-contrast ink, localisation-ready text. |

## The world

| Region | Character |
|---|---|
| **Saltmarrow** | Stilt-roosts, reeds and a coast of lighthouses. Where the climb begins. |
| **Emberdown** | A furnace highland of chimneys, baths and caves. |
| **The Verdance** | A canopy kingdom with a quiet library and a keystone check. |
| **Halden Reach** | A clockwork plateau of bridges, mills and an observatory. |
| **Windreach** | A steppe and a camp that walks on at first light. |
| **The Greyfold / The Blank** | The edge of the map, and what is left when it is erased. |

## Play it

> [!NOTE]
> The game is in **active development** (vertical slice plus a greybox of the full map). There is no public
> release yet. To try it, build from source.

**Requirements:** Unity Hub with editor **6000.3.7f1**, Windows 10/11.

```bash
git clone https://github.com/lakshgupta8/LastCartographer.git
cd LastCartographer
git lfs pull          # art and trailer assets are stored in Git LFS
```

1. Add the editor in Unity Hub, then open the `LastCartographer/` folder as a project.
2. Choose **OWSBG → Play From Start (the Edge)** (`Ctrl+Shift+P`).
3. Click inside the Game view so it takes keyboard focus, then walk off the right edge.

Other entry points: **OWSBG → Play From Saltmarrow** skips the prologue, and **OWSBG → Play the Feel Course** opens the
controller test course.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | `WASD` / arrows | Left stick / d-pad |
| Jump (hold for height) | `Space` | South |
| Quill strike (up / down in air) | `J` | West |
| Wingbeat dash | `Shift` or `K` | RB / RT |
| Bind (hold, spends 3 ink) | `E` | East |
| Flourish (neutral, forward, up) | `L` | LT / LB |
| Use Instrument | `I` | Right stick press |
| Next Instrument slot | `Tab` | Left stick press |
| Survey (hold at a vantage) | `Q` | North |
| Atlas (map, journal, travel) | `M` | Select |
| Options | `Esc` | Start |

Everything but Move can be remapped in **Options → Controls**.

## Documentation

The design, story and production papers are working documents and are kept out of this repository. What lives here is
the game, its tests, the build and content tools under [`tools/`](tools/), and the press material in
[`docs/marketing/`](docs/marketing/). To work on the game, read [CONTRIBUTING.md](CONTRIBUTING.md).

## Build and test

```bash
# Windows release build, then a smoke run of the built player
pwsh tools/build.ps1

# Play-mode tests, headless
Unity.exe -batchmode -projectPath LastCartographer -runTests -testPlatform PlayMode -testResults logs/playmode-results.xml
```

CI uses GameCI (both test suites, the Windows build, a smoke run and a CPU-side performance probe). It runs only when
started by hand or on a pull request from `dev` into `main`. See [CONTRIBUTING.md](CONTRIBUTING.md) for the branch model.

## Reporting a bug

Press **F12** in the game. It writes a `report.md`, the save, the log and a screenshot to a `BugReports` folder under
`%USERPROFILE%\AppData\LocalLow\<company>\LastCartographer\`. Attach that folder to a
[new issue](https://github.com/lakshgupta8/LastCartographer/issues/new). The bug form asks for the
severity and area.

## Built with

Unity 6 (URP Forward+) · Cinemachine 3 · Input System · Addressables · Yarn Spinner 3 · Blender 5.2 · Git LFS · GitHub Actions

## License

No license has been chosen yet, so all rights are reserved for now.
