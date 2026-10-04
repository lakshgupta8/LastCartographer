# 03 · Tooling for the AI-built version

Checked 2026-09-29 on the build machine (RTX 3050 6 GB laptop, Windows 11). This is what the
AI-built "version one" can be made with, before the team reworks dialogue, art, music,
animation and direction by hand. Status is what was verified, not what is advertised.

## 1. Verified and usable now

| Tool | What it gives the game | Status |
|---|---|---|
| Unity 6000.3.7f1, headless | Everything in-engine: scenes, prefabs, Timeline cutscenes, Cinemachine cameras, particle VFX, Shader Graph, URP post, Addressables, 2D Animation bone rigs, PSD Importer, Splines, ProBuilder greybox, Localization, Yarn Spinner | Working; all setup scripts run by `-executeMethod` |
| Unity skills (Claude) | 2d-pixel-perfect, sprite-editor, sprite atlas, tilemap palette and rule tiles, URP post-processing, Shader Graph custom nodes, audio mixer routing, audio optimisation, UI Toolkit and uGUI, localization, unity-cli | Available |
| Python 3.13 + Pillow, numpy, scipy | Procedural textures and paper kit, palette and ink-fade masks, sprite-sheet packing, procedural SFX synthesis, MIDI writing and soft-synth rendering to WAV | Working |
| ffmpeg | Audio conversion and loudness, sprite frames from video, trailer assembly | Working |
| GitHub Actions | Edit and play tests, Windows build, headless smoke and perf probe, Steam upload via the `steam` environment secrets | Green |
| Figma MCP | UI and HUD mockups, diagrams, design tokens; reading works | Connected (starter plan, View seat, writes untested) |
| Claude Docs, Desktop Commander | Team-facing docs; long-running local processes (Blender renders, a local image server) | Available |
| WebSearch and WebFetch | CC0 sources: Poly Haven, ambientCG, Kenney, OpenGameArt, Freesound (CC0 filter), Google Fonts | Available |

## 2. Installed but not reachable yet

| Tool | What it would give | What is needed |
|---|---|---|
| Blender 5.2 + MCP addon | 3D Wren and enemies rigged and rendered to side-view sprite sheets (the Dead Cells route); props and paper-kit pieces rendered to layers; Poly Haven textures, HDRIs and models; Sketchfab and Poly Pizza imports; Hyper3D, Hunyuan3D and Tripo mesh generation if the premium plan is on | Open Blender, enable the addon (`uvx mcp-for-blender install-addon`), keep it open while work runs |
| Mixamo (web, free Adobe account) | Humanoid idle, run, jump, attack, hurt and death clips retargeted onto the Blender rig | User downloads FBX clips into `Assets/_Project/Art/Mocap/` |
| Local image generation (ComfyUI or diffusers) | Concept sheets, portraits, textures, key art, unlimited and free | Torch is CPU-only today; needs the CUDA build plus a model that fits 6 GB (SD 1.5, SDXL with offload, or Flux schnell quantised); about 10 GB of disk |
| Local music generation (MusicGen small) | Region theme sketches from text prompts | Same CUDA install |

## 3. Connected but unfunded or unauthorised

| Tool | What it would give | Status |
|---|---|---|
| Higgsfield MCP | Image, video, audio and 3D generation, voice, upscale, background removal | Free plan, 0 credits |
| Canva | Capsule art, store page graphics, press kit layout | Needs authorising in claude.ai connectors |
| Linear, Notion, Slack, Asana, ClickUp | Team task tracking once the group project starts | Need authorising; not needed for version one |

## 4. Not present

gh CLI, Aseprite, Krita, GIMP, Inkscape, Audacity, LMMS. None is required by the pipelines below.

## 5. Pipelines for version one

Every pipeline leaves a source the team can rework by hand, not just a baked output.

- **Characters and animation.** Concept image → mesh (Hunyuan3D or Tripo through Blender, or a hand-blocked Blender mesh) → Rigify or Mixamo rig → Mixamo clips plus hand-keyed ability moves → Blender renders side-view frames at the game's pixel scale → Python packs sprite sheets → Unity Animator. The team later replaces frames per action without touching the rig or the Animator.
- **Environments.** Paper-kit pieces as Blender-rendered or Python-procedural layers with a separate `_Ink` mask; Poly Haven and ambientCG textures for paper, stone, wood and water; parallax set in Unity prefabs. The team repaints layers in place.
- **VFX.** Unity particle systems and Shader Graph, all authored as assets in the repo.
- **Music.** Themes written as MIDI (melody, harmony, motifs shared between regions and the Blank's reversals) and rendered with a Python soft-synth. The composer gets the MIDI and the motif sheet.
- **SFX.** Procedural synthesis in Python for strikes, pogo, dash, Bind and UI, plus Freesound CC0 for foley. Every sound is a script or a credited file.
- **Cutscenes and direction.** Timeline plus Cinemachine, driven by the same Yarn nodes. The director edits Timeline assets.
- **Key art and capsule.** Local image generation once the CUDA install is done, otherwise Higgsfield or Canva.
