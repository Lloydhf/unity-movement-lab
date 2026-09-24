# Movement and puzzle design notebook

## 24 September 2026 — SHIFT

The project now contains two implemented SHIFT levels, the earlier two Polar Relay rooms and the original movement exercises. This is a development record, not a completed participant study or a personal reflection written on Kuzey's behalf.

The earlier magnet prototype led to feedback that it felt too easy. Development returned to a heavy/light robot direction. The current implementation offers these design choices for the next player test:

| Implemented choice | Intended effect; not a proven result | What to observe |
| --- | --- | --- |
| Change mode only at service stations | Make the order of movement and mechanism use matter | Does a new player understand where and why they can change mode? |
| Light jumps higher; heavy breaks panels and activates pistons | Give each mode a distinct environmental purpose | Can a player infer the required mode before receiving help? |
| Piston effects stay active after leaving | Let earlier actions prepare a later route | Does the bridge reveal explain the connection? |
| Early descent in level 2 has a service return route | Allow recovery while keeping mechanism progress | Does the player find the return path, or think they are stuck? |
| Falling preserves progress; full restart clears it | Separate movement mistakes from choosing a fresh attempt | Is the difference between falling and pressing R clear? |

These initial layouts and implementations were prepared with Codex assistance. Kuzey's next authored iteration should preserve a before/after scene, identify one changed decision and explain it using observations. No predicted outcome should be entered as a measured result.

## 24 September 2026 — Polar Relay retained

`03_FirstContact` and `04_DoubleRelay` preserve the previous magnet-and-ball prototype. `PlayerMovement` now includes a no-friction collider material, short jump input buffering and coyote time. These are implemented movement aids, not evidence that every reported corner snag is fixed for every player.

## 20 September 2026 — original movement exercise

The initial repository contained three scenes and one movement script. The saved `02_MovementLab` scene used speed 4, jump speed 5 and reset height 0. These remain historical exercise values, not the current SHIFT robot's tuning. Kuzey reported preferring speed 4 and jump speed 5 and described a corner snag while jumping.

An earlier experiment proposal was to keep the obstacle layout constant while comparing one movement value at a time. It has not been turned into a formal results table.

## Record the next session

| Date and build | Scene and decision | Intended change | Observed behavior | Next decision |
| --- | --- | --- | --- | --- |
| Not yet recorded | — | — | — | — |

Record help and failures as well as completion. Keep participant identities and raw feedback outside this public repository. Add a before/after image from the same camera view where it explains a real change.

Reflection prompts for Kuzey: What did you change yourself? What help did you use? Which observation supports your decision? What can you now explain, and what remains uncertain?
