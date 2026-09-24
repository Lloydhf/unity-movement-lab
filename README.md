# SHIFT — Mass Protocol

A two-level, side-view puzzle platformer about preparing a route with a robot's **light and heavy modes**. The robot can change mode only at service stations: light mode reaches high ledges; heavy mode breaks marked panels and locks pistons to open gates and a bridge.

Part of [Kuzey's game design portfolio](https://github.com/Lloydhf). This repository keeps its original `unity-movement-lab` name and preserves the movement exercises and earlier **Polar Relay** magnet prototype alongside SHIFT.

**Stage:** playable, AI-assisted learning prototype; player research and the final portfolio presentation are unfinished. **Snapshot:** 24 September 2026. **Editor:** Unity 6000.3.24f1, URP, 3D physics on an X/Y movement plane.

## Open and play

**[Download the Windows prototype](https://github.com/Lloydhf/unity-movement-lab/releases/tag/shift-prototype-2026-09-24)** — extract the entire ZIP and open `SHIFT.exe`. Keep its data folder and DLLs beside it. Unity Editor is not required to play the packaged build.

To inspect or edit the source:

1. Clone or download this repository and add its root folder in Unity Hub.
2. Open it with **Unity 6000.3.24f1** and allow the included package manifest to resolve.
3. Open `Assets/Scenes/05_ShiftFirstShift.unity`, enter Play mode and press the in-game Start button. Click the Game view if the keyboard has no focus.
4. Completing the first level leads to `06_ShiftPrepareTheWay`. The menus support **English and Turkish**.

| Input | Action |
| --- | --- |
| A / D or arrow keys | Move |
| Space | Jump |
| E | Change mode at a nearby station; activate a piston while standing on it in heavy mode |
| R | Reset the whole current level |
| Esc | Pause / resume |
| Enter | Start, resume or continue after completion |

Falling returns the robot to its last used station while keeping opened mechanisms. R resets the mechanisms as well. The prototype currently supports a keyboard; controller, touch and accessibility coverage are not claimed.

To produce a Windows build from the saved SHIFT scenes, use **Portfolio → SHIFT → Build Windows game**. The result is `Builds/SHIFT-0.1/SHIFT.exe`; distribute its entire folder, not the executable alone. This requires Unity's Windows build support module.

## What is in the project?

| Scenes | Purpose |
| --- | --- |
| `01_Blockout`, `02_MovementLab` | Earlier movement and geometry exercises |
| `03_FirstContact`, `04_DoubleRelay` | Polar Relay: attract a ball into a socket to open a gate |
| `05_ShiftFirstShift` | SHIFT: learn station-based mode changes, a breakable panel and a piston |
| `06_ShiftPrepareTheWay` | SHIFT: prepare a bridge and recover from taking the lower route first |

SHIFT includes a geometric robot, state feedback, camera follow and a short mechanism reveal, generated event tones, pause/restart/completion screens and level transitions. Its second level supports a bridge-first route and an early-descent recovery route.

[Design notebook](docs/DESIGN-NOTEBOOK.md) · [Editing guide](docs/DEVELOPMENT.md) · [Verification and limitations](docs/TESTING.md) · [Contribution record](docs/CREDITS.md) · [Türkçe başlangıç rehberi](SHIFT_BASLANGIC.md)

## Next design iteration

Observe first-time players: do they understand where mode changes are allowed, see what the first piston opens, and discover the return route after an early descent? Record help, hesitation and the build version. Change one layout or cue at a time and retest. The [onboarding research proposal](https://github.com/Lloydhf/game-design-research) remains a separate, unexecuted study; this game's automated tests are not participant evidence.

## Authorship and assets

Kuzey supplied project direction, movement preferences and feedback on earlier prototypes. OpenAI Codex assisted with implementation, initial level geometry, the geometric robot, generated tones, documentation and technical tests. The [contribution record](docs/CREDITS.md) separates those contributions from work still to be done. Personal reflections and player findings must come from actual experience.

Unity template assets and packages retain their respective terms. No blanket open-source license is applied to Unity or third-party content. Generated editor caches, private user settings and builds are excluded from source control.
