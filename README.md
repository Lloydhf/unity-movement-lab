# Unity Movement Lab

A small 3D physics prototype for learning movement, jumps and level blockouts. Part of [Kuzey's game design portfolio](https://github.com/Lloydhf).

**Stage:** learning prototype, not a finished game. **Editor:** Unity 6000.3.24f1. **Rendering:** URP.

## Open and explore

1. Download the repository and open its folder through Unity Hub using the recorded editor version.
2. Open `Assets/Scenes/02_MovementLab.unity` and enter Play mode.
3. A/D or arrow keys move, Space jumps, and R returns to the starting position. Click the Game view to give it keyboard focus.

The player moves on the X/Y plane using a 3D Rigidbody. Ground contact is determined by collision normals so a wall is not treated as the ground. Falling below the configured limit resets the player.

## Read the design

The learning sequence moves from [01_Blockout](Assets/Scenes/01_Blockout.unity) to [02_MovementLab](Assets/Scenes/02_MovementLab.unity). The saved movement scene currently overrides the script defaults with **move speed 4, jump speed 5 and fall limit 0**. These are the observed saved values; their design rationale has not yet been documented. Fall limit 0 should be checked against the course geometry during playtesting.

[Movement code](Assets/Scripts/PlayerMovement.cs) · [Design notebook](docs/DESIGN-NOTEBOOK.md) · [Verification](docs/TESTING.md)

## Next iteration

Build a three-obstacle route with a clear finish. Change one movement value at a time, record how the same jump feels, and compare two versions. Connect this to the [onboarding research proposal](https://github.com/Lloydhf/game-design-research).

## Authorship and assets

Kuzey's learning project; movement code and learning guidance were prepared with OpenAI Codex. The saved scenes show local blockout and tuning work. Personal reflections must be written from actual experience. Unity template assets and packages retain their respective terms. Generated Library, Logs and UserSettings folders are excluded. No blanket open-source license is added to Unity or third-party content.
